using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Common.Math;
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
    private bool wasInCutscene;
    private bool wasSpectralActive;
    private bool wasInFCWorkshop;
    private byte lastWeatherId;
    private long weatherCooldownTimestamp;

    private delegate void UpdateGearsetDelegate(nint raptureGearsetModule, int gearsetId);
    private Hook<UpdateGearsetDelegate>? updateGearsetHook;

    private unsafe delegate void ReceiveActionEffectDelegate(
        uint casterEntityId, Character* casterPtr, Vector3* targetPos,
        ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects,
        GameObjectId* targetEntityIds);
    private Hook<ReceiveActionEffectDelegate>? actionEffectHook;

    private delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);
    private Hook<OnEmoteFuncDelegate>? emoteHook;

    static readonly HashSet<ushort> AffectionateEmotes = [105, 68, 42, 64, 49];
    // hug=105, pet=68, embrace=42, blowkiss=64, dote=49

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

        UnregisterAddonListeners();

        updateGearsetHook?.Disable();
        updateGearsetHook?.Dispose();

        actionEffectHook?.Disable();
        actionEffectHook?.Dispose();

        emoteHook?.Disable();
        emoteHook?.Dispose();
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

        try
        {
            actionEffectHook = Plugin.GameInterop.HookFromSignature<ReceiveActionEffectDelegate>(
                ActionEffectHandler.Addresses.Receive.String, ReceiveActionEffectDetour);
            actionEffectHook.Enable();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to hook ReceiveActionEffect");
        }

        try
        {
            emoteHook = Plugin.GameInterop.HookFromSignature<OnEmoteFuncDelegate>(
                "E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24", OnEmoteDetour);
            emoteHook.Enable();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to hook OnEmote");
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
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "TripleTriadResult", OnAddonTripleTriadResult);
    }

    private void UnregisterAddonListeners()
    {
        Plugin.AddonLifecycle.UnregisterListener(OnAddonRepairRequest);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonGcReward);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonJournalResult);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonGlamour);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonRaceResult);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonRepairClose);
        Plugin.AddonLifecycle.UnregisterListener(OnAddonTripleTriadResult);
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

    static readonly HashSet<ushort> CosmicTerritories = [1237, 1291, 1310];
    static readonly HashSet<ushort> IslandSanctuaryTerritories = [1055];

    private void OnTerritoryChanged(ushort territory)
    {
        if (config.SeparationAnxiety && config.SepTerritoryChanged && territory != lastTerritory)
            msg.Send(MessageCategory.SeparationAnxiety, DialoguePool.SepTerritoryChanged);

        if (config.SpecialContent && territory != lastTerritory)
        {
            if (config.SpcIslandSanctuary && IslandSanctuaryTerritories.Contains(territory)
                && !IslandSanctuaryTerritories.Contains(lastTerritory))
                msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcIslandSanctuary);

            if (config.SpcCosmicExploration && CosmicTerritories.Contains(territory)
                && !CosmicTerritories.Contains(lastTerritory))
                msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcCosmicExploration);
        }

        lastTerritory = territory;
        durabilityWarned = false;
        spiritbondWarned = false;
        lastWeatherId = 0;
        weatherCooldownTimestamp = 0;
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
            case ConditionFlag.WatchingCutscene when value && !wasInCutscene && config.Surveillance && config.SurCutscene:
            case ConditionFlag.WatchingCutscene78 when value && !wasInCutscene && config.Surveillance && config.SurCutscene:
            case ConditionFlag.OccupiedInCutSceneEvent when value && !wasInCutscene && config.Surveillance && config.SurCutscene:
                wasInCutscene = true;
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurCutscene);
                break;
        }

        switch (flag)
        {
            case ConditionFlag.Mounted: wasMounted = value; break;
            case ConditionFlag.Crafting: wasCrafting = value; break;
            case ConditionFlag.InDeepDungeon: wasInDeepDungeon = value; break;
            case ConditionFlag.ChocoboRacing: wasChocoboRacing = value; break;
            case ConditionFlag.WatchingCutscene:
            case ConditionFlag.WatchingCutscene78:
            case ConditionFlag.OccupiedInCutSceneEvent:
                if (!value
                    && !Plugin.Condition[ConditionFlag.WatchingCutscene]
                    && !Plugin.Condition[ConditionFlag.WatchingCutscene78]
                    && !Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent])
                    wasInCutscene = false;
                break;
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
        PollFCWorkshop();
        PollWeather();
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
        if (!config.SpecialContent) return;

        var oceanFishing = EventFramework.Instance()->GetInstanceContentOceanFishing();
        if (oceanFishing != null)
        {
            if (config.SpcOceanFishing)
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
                wasInOceanFishing = true;
            }

            var spectral = oceanFishing->SpectralCurrentActive;
            if (spectral && !wasSpectralActive && config.SpcSpectralCurrent)
                msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcSpectralCurrent);
            wasSpectralActive = spectral;
        }
        else
        {
            wasInOceanFishing = false;
            wasSpectralActive = false;
        }
    }

    private unsafe void ReceiveActionEffectDetour(
        uint casterEntityId, Character* casterPtr, Vector3* targetPos,
        ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects,
        GameObjectId* targetEntityIds)
    {
        actionEffectHook!.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);

        try
        {
            if (!config.Enabled || !config.Outburst || header->NumTargets == 0)
                return;

            var localPlayer = Plugin.ClientState.LocalPlayer;
            if (localPlayer == null) return;
            var myId = localPlayer.GameObjectId;
            var isHealer = HealerJobs.Contains(localPlayer.ClassJob.RowId);

            for (var i = 0; i < header->NumTargets; i++)
            {
                var targetId = (uint)(targetEntityIds[i] & uint.MaxValue);

                for (var j = 0; j < 8; j++)
                {
                    ref var eff = ref effects[i].Effects[j];
                    if (eff.Type == 0) continue;

                    switch (eff.Type)
                    {
                        case 3:
                        case 5:
                        case 6:
                            if (casterEntityId == myId && config.OutCritDh &&
                                ((eff.Param0 & 0x20) == 0x20 || (eff.Param0 & 0x40) == 0x40))
                                msg.Send(MessageCategory.Outburst, DialoguePool.OutCritDh);
                            break;

                        case 4:
                            if (casterEntityId != myId || !isHealer || targetId == myId)
                                break;
                            if (config.OutHealCrit && (eff.Param1 & 0x20) == 0x20)
                                msg.Send(MessageCategory.Outburst, DialoguePool.OutHealCrit);
                            if (config.OutHealOther)
                                msg.Send(MessageCategory.Outburst, DialoguePool.OutHealOther);
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error in ReceiveActionEffect detour");
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

    private unsafe void OnAddonTripleTriadResult(AddonEvent type, AddonArgs args)
    {
        if (!config.Surveillance || !config.SurTripleTriad) return;

        try
        {
            var addonPtr = Plugin.GameGui_.GetAddonByName("TripleTriadResult");
            if (addonPtr.Address == nint.Zero) return;
            var baseNode = (FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase*)addonPtr.Address;

            var rootNode = baseNode->RootNode;
            if (rootNode == null) return;

            var child = rootNode->ChildNode;
            var idx = 0;
            FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode* resultNode = null;
            while (child != null)
            {
                if (idx == 9) { resultNode = child; break; }
                child = child->PrevSiblingNode;
                idx++;
            }
            if (resultNode == null) return;

            var resultChild = resultNode->ChildNode;
            FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode* nodeDraw = null;
            FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode* nodeLose = null;
            FFXIVClientStructs.FFXIV.Component.GUI.AtkResNode* nodeWin = null;
            var ri = 0;
            while (resultChild != null)
            {
                switch (ri)
                {
                    case 0: nodeDraw = resultChild; break;
                    case 1: nodeLose = resultChild; break;
                    case 2: nodeWin = resultChild; break;
                }
                resultChild = resultChild->PrevSiblingNode;
                ri++;
            }

            if (nodeWin != null && nodeWin->IsVisible())
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurTripleTriadWin);
            else if (nodeLose != null && nodeLose->IsVisible())
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurTripleTriadLose);
            else if (nodeDraw != null && nodeDraw->IsVisible())
                msg.Send(MessageCategory.Surveillance, DialoguePool.SurTripleTriadDraw);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error reading TripleTriadResult addon");
        }
    }

    private void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
    {
        emoteHook!.Original(unk, instigatorAddr, emoteId, targetId, unk2);

        try
        {
            if (!config.Enabled || !config.Possessiveness || !config.PosEmoteReceived) return;

            var localPlayer = Plugin.ClientState.LocalPlayer;
            if (localPlayer == null || targetId != localPlayer.GameObjectId) return;

            var instigator = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr);
            if (instigator == null || instigator is not Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter)
                return;
            if (instigator.GameObjectId == localPlayer.GameObjectId) return;

            if (AffectionateEmotes.Contains(emoteId))
                msg.Send(MessageCategory.Possessiveness, DialoguePool.PosEmoteReceivedAffectionate);
            else
                msg.Send(MessageCategory.Possessiveness, DialoguePool.PosEmoteReceivedHostile);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error in OnEmote detour");
        }
    }

    private unsafe void PollFCWorkshop()
    {
        if (!config.SpecialContent || !config.SpcFCWorkshop) return;

        var housing = HousingManager.Instance();
        if (housing == null) return;

        var inWorkshop = housing->WorkshopTerritory != null
            && !IslandSanctuaryTerritories.Contains(Plugin.ClientState.TerritoryType);

        if (inWorkshop && !wasInFCWorkshop)
            msg.Send(MessageCategory.SpecialContent, DialoguePool.SpcFCWorkshop);
        wasInFCWorkshop = inWorkshop;
    }

    private unsafe void PollWeather()
    {
        if (!config.Surveillance || !config.SurWeatherChange) return;

        var env = EnvManager.Instance();
        if (env == null) return;

        var weatherId = env->ActiveWeather;
        if (weatherId == lastWeatherId)
            return;

        var wasUninitialized = lastWeatherId == 0;
        lastWeatherId = weatherId;

        if (weatherId <= 3) return;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (!wasUninitialized && now - weatherCooldownTimestamp < 20 * 60 * 1000) return;
        weatherCooldownTimestamp = now;

        string[] pool = weatherId switch
        {
            7 or 8 => DialoguePool.SurWeatherRain,
            9 or 10 or 11 or 12 => DialoguePool.SurWeatherStorm,
            4 or 5 or 6 => DialoguePool.SurWeatherFog,
            15 or 16 => DialoguePool.SurWeatherSnow,
            _ => DialoguePool.SurWeatherOther,
        };

        msg.Send(MessageCategory.Surveillance, pool);
    }
}
