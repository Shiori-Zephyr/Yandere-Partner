using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace YanderePartner;

public unsafe class EventWatcher : IDisposable
{
    static readonly uint[] HealerJobs = [6, 24, 28, 33, 40, 42];
    static readonly uint[] TankJobs = [1, 3, 19, 21, 32, 37];

    private readonly Configuration config;
    private readonly MessageManager msg;

    private bool wasGPosing;
    private bool wasMounted;
    private bool wasCrafting;
    private bool wasInDeepDungeon;
    private bool wasChocoboRacing;
    private bool wasPerforming;
    private bool durabilityWarned;
    private bool spiritbondWarned;
    private int lastPartyCount;
    private ushort lastTerritory;
    private byte lastClassJobId;
    private long glamourTimestamp;
    private bool wasInFate;
    private bool wasInOceanFishing;
    private byte lastOceanZone;

    private delegate void UpdateGearsetDelegate(nint raptureGearsetModule, int gearsetId);
    private Hook<UpdateGearsetDelegate>? updateGearsetHook;

    public EventWatcher(Configuration config, MessageManager msg)
    {
        this.config = config;
        this.msg = msg;

        lastPartyCount = Plugin.PartyList.Length;
        lastTerritory = Plugin.ClientState.TerritoryType;

        Plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
        Plugin.ClientState.Logout += OnLogout;
        Plugin.ClientState.CfPop += OnCfPop;

        Plugin.DutyState.DutyStarted += OnDutyStarted;
        Plugin.DutyState.DutyCompleted += OnDutyCompleted;
        Plugin.DutyState.DutyWiped += OnDutyWiped;
        Plugin.DutyState.DutyRecommenced += OnDutyRecommenced;

        Plugin.Condition.ConditionChange += OnConditionChange;
        Plugin.ChatGui.ChatMessage += OnChatMessage;
        Plugin.Framework.Update += OnFrameworkUpdate;
        Plugin.FlyTextGui.FlyTextCreated += OnFlyText;

        RegisterAddonListeners();
        SetupHooks();
    }

    public void Dispose()
    {
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        Plugin.ClientState.Logout -= OnLogout;
        Plugin.ClientState.CfPop -= OnCfPop;

        Plugin.DutyState.DutyStarted -= OnDutyStarted;
        Plugin.DutyState.DutyCompleted -= OnDutyCompleted;
        Plugin.DutyState.DutyWiped -= OnDutyWiped;
        Plugin.DutyState.DutyRecommenced -= OnDutyRecommenced;

        Plugin.Condition.ConditionChange -= OnConditionChange;
        Plugin.ChatGui.ChatMessage -= OnChatMessage;
        Plugin.Framework.Update -= OnFrameworkUpdate;
        Plugin.FlyTextGui.FlyTextCreated -= OnFlyText;

        UnregisterAddonListeners();

        updateGearsetHook?.Disable();
        updateGearsetHook?.Dispose();
    }

    private void SetupHooks()
    {
        try
        {
            var gearsetAddr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 89 9B ?? ?? ?? ?? 48 8B CB 48 8B 17");
            updateGearsetHook = Plugin.GameInterop.HookFromAddress<UpdateGearsetDelegate>(
                gearsetAddr, UpdateGearsetDetour);
            updateGearsetHook.Enable();
        }
        catch
        {
            try
            {
                var addr = (nint)FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.MemberFunctionPointers.UpdateGearset;
                if (addr != nint.Zero)
                {
                    updateGearsetHook = Plugin.GameInterop.HookFromAddress<UpdateGearsetDelegate>(
                        addr, UpdateGearsetDetour);
                    updateGearsetHook.Enable();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "Failed to hook UpdateGearset via both sig scan and MemberFunctionPointers");
            }
        }
    }

    private void RegisterAddonListeners()
    {
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RepairRequest", OnAddonRepairRequest);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GrandCompanySupplyReward", OnAddonGcReward);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalResult", OnAddonJournalResult);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MiragePrismMiragePlate", OnAddonGlamour);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "RaceChocoboResult", OnAddonRaceResult);
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Repair", OnAddonRepairClose);
    }

    private void UnregisterAddonListeners()
    {
        Plugin.AddonLifecycle.UnregisterListener(OnAddonRepairRequest);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonGcReward);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonJournalResult);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonGlamour);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonRaceResult);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonRepairClose);
    }

    private void PollJobChange()
    {
        if (!config.Surveillance || !config.SurGearsetChange) return;

        var classJobId = PlayerState.Instance()->CurrentClassJobId;
        if (classJobId == lastClassJobId || lastClassJobId == 0)
        {
            lastClassJobId = classJobId;
            return;
        }

        lastClassJobId = classJobId;

        if (HealerJobs.Contains(classJobId))
            msg.Send(MessageCategory.Surveillance, DialoguePool.SurGearsetChangeHealer);
        else if (TankJobs.Contains(classJobId))
            msg.Send(MessageCategory.Surveillance, DialoguePool.SurGearsetChangeTank);
        else
            msg.Send(MessageCategory.Surveillance, DialoguePool.SurGearsetChange);
    }

    private void UpdateGearsetDetour(nint raptureGearsetModule, int gearsetId)
    {
        updateGearsetHook!.Original(raptureGearsetModule, gearsetId);

        if (!config.Enabled || !config.Surveillance || !config.SurGearsetUpdate) return;

        try
        {
            msg.Send(MessageCategory.Surveillance, DialoguePool.SurGearsetUpdate);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error in UpdateGearset detour");
        }
    }

    private void OnTerritoryChanged(ushort territory)
    {
        if (config.SeparationAnxiety && config.SepTerritoryChanged && territory != lastTerritory)
            msg.Send(MessageCategory.SeparationAnxiety, DialoguePool.SepTerritoryChanged);

        lastTerritory = territory;
        durabilityWarned = false;
        spiritbondWarned = false;
    }

    private void OnLogout(int type, int code)
    {
        if (config.SeparationAnxiety && config.SepLogout)
            msg.Send(MessageCategory.SeparationAnxiety, DialoguePool.SepLogout);
    }

    private void OnCfPop(ContentFinderCondition cfc)
    {
        if (config.Possessiveness && config.PosCfPop)
            msg.Send(MessageCategory.Possessiveness, DialoguePool.PosCfPop);
    }

    private void OnDutyStarted(object? sender, ushort territory)
    {
        if (config.Possessiveness && config.PosDutyStarted)
            msg.Send(MessageCategory.Possessiveness, DialoguePool.PosDutyStarted);
    }

    private void OnDutyCompleted(object? sender, ushort territory)
    {
        if (config.Evaluation && config.EvaDutyCompleted)
            msg.Send(MessageCategory.Evaluation, DialoguePool.EvaDutyCompleted);
    }

    private void OnDutyWiped(object? sender, ushort territory)
    {
        if (config.Evaluation && config.EvaDutyWiped)
            msg.Send(MessageCategory.Evaluation, DialoguePool.EvaDutyWiped);
    }

    private void OnDutyRecommenced(object? sender, ushort territory)
    {
        if (config.Evaluation && config.EvaDutyRecommenced)
            msg.Send(MessageCategory.Evaluation, DialoguePool.EvaDutyRecommenced);
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (!config.Enabled) return;

        switch (flag)
        {
            case ConditionFlag.BetweenAreas when value && config.SeparationAnxiety && config.SepBetweenAreas
                                                       && !Plugin.Condition[ConditionFlag.InDeepDungeon]:
                msg.Send(MessageCategory.SeparationAnxiety, DialoguePool.SepBetweenAreas);
                break;
            case ConditionFlag.BetweenAreas when !value && Plugin.Condition[ConditionFlag.InDeepDungeon]
                                                        && config.SpecialContent && config.SpcDeepDungeon:
                msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcDeepDungeonFloor);
                break;
            case ConditionFlag.Unconscious when value && config.Evaluation && config.EvaDeath:
                msg.Send(MessageCategory.Evaluation, DialoguePool.EvaDeath);
                break;
            case ConditionFlag.OccupiedSummoningBell when value && config.Surveillance && config.SurSummoningBell:
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurSummoningBell);
                break;
            case ConditionFlag.Fishing when value && config.Surveillance && config.SurFishing:
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurFishing);
                break;
            case ConditionFlag.Crafting when value && config.Surveillance && config.SurCrafting:
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurCrafting);
                break;
            case ConditionFlag.Crafting when !value && wasCrafting && config.Surveillance && config.SurCraftFinished:
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurCraftFinishedSuccess);
                break;
            case ConditionFlag.Gathering when value && config.Surveillance && config.SurGathering:
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurGathering);
                break;
            case ConditionFlag.Mounted when value && config.SeparationAnxiety && config.SepMounted:
                msg.Send(MessageCategory.SeparationAnxiety, DialoguePool.SepMounted);
                break;
            case ConditionFlag.Mounted when !value && wasMounted && config.SeparationAnxiety && config.SepMountedDismount:
                msg.Send(MessageCategory.SeparationAnxiety, DialoguePool.SepMountedDismount);
                break;
            case ConditionFlag.InFlight when value && config.SeparationAnxiety && config.SepInFlight:
                msg.Send(MessageCategory.SeparationAnxiety, DialoguePool.SepInFlight);
                break;
            case ConditionFlag.InDeepDungeon when value && !wasInDeepDungeon && config.SpecialContent && config.SpcDeepDungeon:
                msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcDeepDungeonEnter);
                break;
            case ConditionFlag.ChocoboRacing when value && !wasChocoboRacing && config.SpecialContent && config.SpcChocoboRacing:
                msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcChocoboRacing);
                break;
        }

        switch (flag)
        {
            case ConditionFlag.Mounted: wasMounted = value; break;
            case ConditionFlag.Crafting: wasCrafting = value; break;
            case ConditionFlag.InDeepDungeon: wasInDeepDungeon = value; break;
            case ConditionFlag.ChocoboRacing: wasChocoboRacing = value; break;
        }
    }

    private void OnChatMessage(XivChatType type, int timestamp,
        ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (!config.Enabled) return;

        switch (type)
        {
            case XivChatType.TellIncoming when config.Possessiveness && config.PosTellReceived:
                msg.Send(MessageCategory.Possessiveness, DialoguePool.PosTellReceived);
                break;
            case XivChatType.RetainerSale when config.Surveillance && config.SurRetainerSale:
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurRetainerSale);
                break;
        }

        if (config.Evaluation && config.EvaLootObtained
            && Plugin.Condition[ConditionFlag.BoundByDuty]
            && !Plugin.Condition[ConditionFlag.InDeepDungeon]
            && !Plugin.Condition[ConditionFlag.TradeOpen]
            && !Plugin.Condition[ConditionFlag.OccupiedInQuestEvent]
            && !Plugin.Condition[ConditionFlag.OccupiedInEvent]
            && !Plugin.Condition[ConditionFlag.Crafting])
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now - glamourTimestamp < 3000) return;

            var text = message.TextValue;
            var hasItem = message.Payloads.Any(p => p is Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload);
            if (hasItem && (text.Contains("obtain") || text.Contains("added to the loot list") ||
                            text.Contains("を手に入れた") || text.Contains("に入りました")))
            {
                msg.Send(MessageCategory.Evaluation, DialoguePool.EvaLootObtained);
            }
        }
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework fw)
    {
        if (!config.Enabled || !Plugin.ClientState.IsLoggedIn) return;

        PollPartyChange();
        PollJobChange();
        PollGPose();
        PollPerformance();
        PollFate();
        PollEquipmentDurability();
        PollOceanFishing();
    }

    private void PollPartyChange()
    {
        var count = Plugin.PartyList.Length;
        if (count != lastPartyCount && lastPartyCount > 0 && count > 0)
        {
            if (config.Possessiveness && config.PosPartyChanged)
                msg.Send(MessageCategory.Possessiveness, DialoguePool.PosPartyChanged);
        }
        lastPartyCount = count;
    }

    private void PollGPose()
    {
        var gposing = Plugin.ClientState.IsGPosing;
        if (gposing && !wasGPosing && config.Surveillance && config.SurGPose)
            msg.Send(MessageCategory.Surveillance, DialoguePool.SurGPose);
        wasGPosing = gposing;
    }

    private void PollPerformance()
    {
        var ptr = Plugin.GameGui_.GetAddonByName("PerformanceMode");
        var performing = ptr.Address != nint.Zero;
        if (!performing)
        {
            ptr = Plugin.GameGui_.GetAddonByName("PerformanceModeWide");
            performing = ptr.Address != nint.Zero;
        }
        if (performing && !wasPerforming && config.Surveillance && config.SurPerformance)
            msg.Send(MessageCategory.Surveillance, DialoguePool.SurPerformance);
        wasPerforming = performing;
    }

    private void PollFate()
    {
        if (!config.Outburst) return;

        var inFate = FateManager.Instance()->CurrentFate != null;
        if (inFate && !wasInFate && config.OutFateEnter)
            msg.Send(MessageCategory.Outburst, DialoguePool.OutFateEnter);
        if (!inFate && wasInFate && config.OutFateLeave)
            msg.Send(MessageCategory.Outburst, DialoguePool.OutFateLeave);
        wasInFate = inFate;
    }

    private void PollEquipmentDurability()
    {
        if (!config.Equipment) return;
        if (msg.IsOnCooldown(MessageCategory.Equipment)) return;

        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null) return;
        var container = inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);
        if (container == null) return;

        var checkDurability = config.EqpLowDurability && !durabilityWarned;
        var checkSpiritbond = config.EqpSpiritbondFull;
        if (!checkDurability && !checkSpiritbond) return;

        var anyBondFull = false;
        var slot = container->GetInventorySlot(0);
        for (var i = 0; i < 13; i++, slot++)
        {
            if (slot->ItemId == 0) continue;

            if (checkDurability && !durabilityWarned && slot->Condition < 3000)
            {
                durabilityWarned = true;
                msg.Send(MessageCategory.Equipment, DialoguePool.EqpLowDurability);
                checkDurability = false;
            }

            if (slot->SpiritbondOrCollectability >= 10000)
            {
                anyBondFull = true;
                if (checkSpiritbond && !spiritbondWarned)
                {
                    spiritbondWarned = true;
                    msg.Send(MessageCategory.Equipment, DialoguePool.EqpSpiritbondFull);
                }
            }
        }

        if (spiritbondWarned && !anyBondFull)
            spiritbondWarned = false;
    }

    private void PollOceanFishing()
    {
        if (!config.SpecialContent || !config.SpcOceanFishing) return;

        var oceanFishing = EventFramework.Instance()->GetInstanceContentOceanFishing();
        if (oceanFishing != null)
        {
            if (!wasInOceanFishing)
            {
                wasInOceanFishing = true;
                msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcOceanFishingEnter);
                lastOceanZone = (byte)oceanFishing->CurrentZone;
            }
            else
            {
                var currentZone = (byte)oceanFishing->CurrentZone;
                if (currentZone != lastOceanZone)
                {
                    lastOceanZone = currentZone;
                    msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcOceanFishingZone);
                }
            }
        }
        else
        {
            wasInOceanFishing = false;
        }
    }

    private void OnFlyText(
        ref FlyTextKind kind,
        ref int val1,
        ref int val2,
        ref SeString text1,
        ref SeString text2,
        ref uint color,
        ref uint icon,
        ref uint damageTypeIcon,
        ref float yOffset,
        ref bool handled)
    {
        if (!config.Enabled || !config.Outburst) return;

        if (config.OutCritDh &&
            (kind == FlyTextKind.DamageCritDh ||
             kind == FlyTextKind.DamageCrit ||
             kind == FlyTextKind.AutoAttackOrDotCritDh ||
             kind == FlyTextKind.AutoAttackOrDotCrit))
        {
            msg.Send(MessageCategory.Outburst, DialoguePool.OutCritDh);
        }

        if ((config.OutHealCrit && kind == FlyTextKind.HealingCrit) ||
            (config.OutHealOther && (kind == FlyTextKind.Healing || kind == FlyTextKind.HealingCrit)))
        {
            var localPlayer = Plugin.ObjectTable.LocalPlayer;
            if (localPlayer != null && HealerJobs.Contains(localPlayer.ClassJob.RowId))
            {
                if (config.OutHealCrit && kind == FlyTextKind.HealingCrit)
                    msg.Send(MessageCategory.Outburst, DialoguePool.OutHealCrit);
                if (config.OutHealOther)
                    msg.Send(MessageCategory.Outburst, DialoguePool.OutHealOther);
            }
        }
    }

    private void OnAddonRepairRequest(AddonEvent type, AddonArgs args)
    {
        if (config.Possessiveness && config.PosRepairRequest)
            msg.Send(MessageCategory.Possessiveness, DialoguePool.PosRepairRequest);
    }

    private void OnAddonGcReward(AddonEvent type, AddonArgs args)
    {
        if (config.SpecialContent && config.SpcGcTurnin)
            msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcGcTurnin);
    }

    private void OnAddonJournalResult(AddonEvent type, AddonArgs args)
    {
        if (config.SpecialContent && config.SpcLeve)
            msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcLeve);
    }

    private void OnAddonGlamour(AddonEvent type, AddonArgs args)
    {
        glamourTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (config.Surveillance && config.SurGlamour)
            msg.Send(MessageCategory.Surveillance, DialoguePool.SurGlamour);
    }

    private void OnAddonRaceResult(AddonEvent type, AddonArgs args)
    {
        if (config.SpecialContent && config.SpcChocoboRacing)
            msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcChocoboRacingResult);
    }

    private void OnAddonRepairClose(AddonEvent type, AddonArgs args)
    {
        if (config.Equipment && config.EqpRepair)
            msg.Send(MessageCategory.Equipment, DialoguePool.EqpRepair);
    }
}
