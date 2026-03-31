using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace YanderePartner;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;
    private readonly Action save;
    private bool themePushed;

    static readonly (ImGuiCol, Vector4)[] ThemeColors =
    [
        (ImGuiCol.Text, new(0.13f, 0.13f, 0.13f, 1f)),
        (ImGuiCol.TextDisabled, new(0.45f, 0.30f, 0.35f, 1f)),
        (ImGuiCol.WindowBg, new(0.96f, 0.66f, 0.72f, 0.94f)),
        (ImGuiCol.ChildBg, new(0f, 0f, 0f, 0f)),
        (ImGuiCol.PopupBg, new(0.98f, 0.82f, 0.86f, 0.94f)),
        (ImGuiCol.Border, new(0.85f, 0.45f, 0.55f, 0.5f)),
        (ImGuiCol.BorderShadow, new(0.75f, 0.35f, 0.45f, 0.15f)),
        (ImGuiCol.FrameBg, new(0.88f, 0.52f, 0.60f, 0.54f)),
        (ImGuiCol.FrameBgHovered, new(0.93f, 0.58f, 0.67f, 0.65f)),
        (ImGuiCol.FrameBgActive, new(0.90f, 0.45f, 0.55f, 0.67f)),
        (ImGuiCol.TitleBg, new(0.78f, 0.38f, 0.48f, 1f)),
        (ImGuiCol.TitleBgActive, new(0.88f, 0.42f, 0.55f, 1f)),
        (ImGuiCol.TitleBgCollapsed, new(0.82f, 0.40f, 0.50f, 0.75f)),
        (ImGuiCol.ScrollbarBg, new(0.85f, 0.50f, 0.58f, 0.30f)),
        (ImGuiCol.ScrollbarGrab, new(0.80f, 0.40f, 0.50f, 0.80f)),
        (ImGuiCol.ScrollbarGrabHovered, new(0.88f, 0.48f, 0.58f, 1f)),
        (ImGuiCol.ScrollbarGrabActive, new(0.92f, 0.55f, 0.65f, 1f)),
        (ImGuiCol.CheckMark, new(0.85f, 0.25f, 0.40f, 1f)),
        (ImGuiCol.SliderGrab, new(0.80f, 0.35f, 0.48f, 1f)),
        (ImGuiCol.SliderGrabActive, new(0.90f, 0.40f, 0.55f, 1f)),
        (ImGuiCol.Button, new(0.85f, 0.45f, 0.55f, 0.50f)),
        (ImGuiCol.ButtonHovered, new(0.90f, 0.52f, 0.62f, 0.75f)),
        (ImGuiCol.ButtonActive, new(0.82f, 0.38f, 0.50f, 1f)),
        (ImGuiCol.Header, new(0.88f, 0.50f, 0.60f, 0.45f)),
        (ImGuiCol.HeaderHovered, new(0.92f, 0.55f, 0.65f, 0.70f)),
        (ImGuiCol.HeaderActive, new(0.90f, 0.48f, 0.58f, 1f)),
        (ImGuiCol.Separator, new(0.80f, 0.40f, 0.50f, 0.50f)),
        (ImGuiCol.SeparatorHovered, new(0.88f, 0.50f, 0.60f, 0.78f)),
        (ImGuiCol.SeparatorActive, new(0.90f, 0.45f, 0.55f, 1f)),
        (ImGuiCol.ResizeGrip, new(0.88f, 0.50f, 0.60f, 0.20f)),
        (ImGuiCol.ResizeGripHovered, new(0.92f, 0.55f, 0.65f, 0.67f)),
        (ImGuiCol.ResizeGripActive, new(0.85f, 0.40f, 0.52f, 0.95f)),
        (ImGuiCol.Tab, new(0.82f, 0.42f, 0.52f, 0.86f)),
        (ImGuiCol.TabHovered, new(0.92f, 0.55f, 0.65f, 0.80f)),
        (ImGuiCol.TabActive, new(0.88f, 0.48f, 0.58f, 1f)),
    ];

    public ConfigWindow(Configuration config, Action save)
        : base("Yandere Partner Settings###YanderePartnerConfig", ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.config = config;
        this.save = save;
        Size = new Vector2(440, 650);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void PreDraw()
    {
        if (!themePushed)
        {
            foreach (var (col, val) in ThemeColors)
                ImGui.PushStyleColor(col, val);
            themePushed = true;
        }
        base.PreDraw();
    }

    public override void PostDraw()
    {
        if (themePushed)
        {
            ImGui.PopStyleColor(ThemeColors.Length);
            themePushed = false;
        }
        base.PostDraw();
    }

    public override void Draw()
    {
        var newEnabled = DrawToggle("Enable Yandere Partner", config.Enabled);
        if (newEnabled != config.Enabled) { config.Enabled = newEnabled; save(); }

        var newPopup = DrawToggle("Popup Window", config.PopupEnabled);
        if (newPopup != config.PopupEnabled) { config.PopupEnabled = newPopup; save(); }

        var newDissoc = DrawToggle("Dissociation", config.Dissociation);
        if (newDissoc != config.Dissociation) { config.Dissociation = newDissoc; save(); }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("She starts to lose coherence when you\ndon't give her anything to react to.");

        ImGui.Separator();

        ImGui.SetNextItemWidth(250);
        if (ImGui.InputText("Partner Name", ref config.PartnerName, 64))
            save();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("The name shown before each message.\nUse English or Japanese characters.");

        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderFloat("Cooldown (sec)", ref config.MessageCooldown, 5f, 120f, "%.0f"))
            save();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Minimum seconds between messages per category.");

        ImGui.Separator();

        DrawCategory("Separation Anxiety", ref config.SeparationAnxiety, () =>
        {
            DrawCheck("Territory changed (teleport/zone)", ref config.SepTerritoryChanged);
            DrawCheck("Logout", ref config.SepLogout);
            DrawCheck("Loading screen", ref config.SepBetweenAreas);
            DrawCheck("Mount up", ref config.SepMounted);
            DrawCheck("Dismount", ref config.SepMountedDismount);
            DrawCheck("Take flight", ref config.SepInFlight);
        });

        DrawCategory("Possessiveness", ref config.Possessiveness, () =>
        {
            DrawCheck("Receive /tell", ref config.PosTellReceived);
            DrawCheck("Party member change", ref config.PosPartyChanged);
            DrawCheck("Duty Finder pop", ref config.PosCfPop);
            DrawCheck("Duty started", ref config.PosDutyStarted);
            DrawCheck("Repair request (player)", ref config.PosRepairRequest);
            DrawCheck("Emote received (from others)", ref config.PosEmoteReceived);
        });

        DrawCategory("Evaluation (Idealize / Devalue)", ref config.Evaluation, () =>
        {
            DrawCheck("Duty completed", ref config.EvaDutyCompleted);
            DrawCheck("Death", ref config.EvaDeath);
            DrawCheck("Party wipe", ref config.EvaDutyWiped);
            DrawCheck("Duty recommenced", ref config.EvaDutyRecommenced);
            DrawCheck("Loot obtained", ref config.EvaLootObtained);
        });

        DrawCategory("Surveillance", ref config.Surveillance, () =>
        {
            DrawCheck("Fishing", ref config.SurFishing);
            DrawCheck("Crafting (start)", ref config.SurCrafting);
            DrawCheck("Crafting (finish/fail)", ref config.SurCraftFinished);
            DrawCheck("Gathering", ref config.SurGathering);
            DrawCheck("GPose", ref config.SurGPose);
            DrawCheck("Performance (bard)", ref config.SurPerformance);
            DrawCheck("Job change", ref config.SurGearsetChange);
            DrawCheck("Gearset update (save)", ref config.SurGearsetUpdate);
            DrawCheck("Glamour plate", ref config.SurGlamour);
            DrawCheck("Summoning bell", ref config.SurSummoningBell);
            DrawCheck("Retainer sale", ref config.SurRetainerSale);
            DrawCheck("Cutscene", ref config.SurCutscene);
            DrawCheck("Triple Triad result", ref config.SurTripleTriad);
            DrawCheck("Weather change", ref config.SurWeatherChange);
        });

        DrawCategory("Emotional Outburst", ref config.Outburst, () =>
        {
            DrawCheck("Critical / Direct Hit", ref config.OutCritDh);
            DrawCheck("Heal another player", ref config.OutHealOther);
            DrawCheck("Critical heal on others", ref config.OutHealCrit);
            DrawCheck("Enter FATE area", ref config.OutFateEnter);
            DrawCheck("Leave FATE area", ref config.OutFateLeave);
        });

        DrawCategory("Special Content", ref config.SpecialContent, () =>
        {
            DrawCheck("Deep Dungeon", ref config.SpcDeepDungeon);
            DrawCheck("Ocean Fishing", ref config.SpcOceanFishing);
            DrawCheck("Chocobo Racing", ref config.SpcChocoboRacing);
            DrawCheck("Grand Company turn-in", ref config.SpcGcTurnin);
            DrawCheck("Levequest", ref config.SpcLeve);
            DrawCheck("Island Sanctuary", ref config.SpcIslandSanctuary);
            DrawCheck("Cosmic Exploration", ref config.SpcCosmicExploration);
            DrawCheck("FC Workshop", ref config.SpcFCWorkshop);
            DrawCheck("Spectral current (ocean fishing)", ref config.SpcSpectralCurrent);
        });

        DrawCategory("Equipment", ref config.Equipment, () =>
        {
            DrawCheck("Low durability warning", ref config.EqpLowDurability);
            DrawCheck("Gear repaired", ref config.EqpRepair);
            DrawCheck("Spiritbond full", ref config.EqpSpiritbondFull);
        });
    }

    private bool DrawToggle(string label, bool value)
    {
        ImGui.PushID(label);

        float toggleWidth = 50f;
        float totalWidth = ImGui.GetContentRegionAvail().X;
        float labelWidth = totalWidth - toggleWidth - 8f;

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 12f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12f, 6f));

        ImGui.PushStyleColor(ImGuiCol.Button, 0x40000000u);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x40000000u);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x40000000u);
        ImGui.Button(label, new Vector2(labelWidth, 0));
        ImGui.PopStyleColor(3);

        ImGui.SameLine(0, 4f);

        var onCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.55f, 0.75f, 1f, 1f));
        var onHover = ImGui.ColorConvertFloat4ToU32(new Vector4(0.65f, 0.82f, 1f, 1f));
        var offCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.05f, 0.05f, 0.05f, 1f));
        var offHover = ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.15f, 1f));

        ImGui.PushStyleColor(ImGuiCol.Button, value ? onCol : offCol);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, value ? onHover : offHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, value ? onHover : offHover);
        ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFFFFFu);
        bool clicked = ImGui.Button(value ? "ON" : "OFF", new Vector2(toggleWidth, 0));
        ImGui.PopStyleColor(4);

        ImGui.PopStyleVar(2);
        ImGui.PopID();

        return clicked ? !value : value;
    }

    private void DrawCheck(string label, ref bool value)
    {
        ImGui.Indent(16);
        if (ImGui.Checkbox(label, ref value))
            save();
        ImGui.Unindent(16);
    }

    private void DrawCategory(string label, ref bool master, Action drawChildren)
    {
        var newMaster = DrawToggle(label, master);
        if (newMaster != master) { master = newMaster; save(); }

        if (master && ImGui.CollapsingHeader($"##{label}_children"))
        {
            ImGui.Indent(10);
            drawChildren();
            ImGui.Unindent(10);
        }

        ImGui.Separator();
    }

    public void Dispose() { }
}
