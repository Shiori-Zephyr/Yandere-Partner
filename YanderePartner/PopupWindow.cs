using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace YanderePartner;

public class PopupWindow : Window, IDisposable
{
    private readonly Configuration config;
    private readonly TypingEngine engine = new();

    private string pendingText = "";
    private MessageCategory pendingCategory;
    private long queuedAtMs;
    private bool hasPending;
    private bool showCloseButton;
    private double openedAt;
    private double finishedAt;
    private bool themePushed;

    private const long PopupDelayMs = 500;
    private const double AutoDismissAfterFinish = 6.0;

    static readonly (ImGuiCol, Vector4)[] ThemeColors =
    [
        (ImGuiCol.Text, new(0.13f, 0.13f, 0.13f, 1f)),
        (ImGuiCol.TextDisabled, new(0.45f, 0.30f, 0.35f, 1f)),
        (ImGuiCol.WindowBg, new(0.96f, 0.66f, 0.72f, 0.94f)),
        (ImGuiCol.PopupBg, new(0.98f, 0.82f, 0.86f, 0.94f)),
        (ImGuiCol.Border, new(0.85f, 0.45f, 0.55f, 0.5f)),
        (ImGuiCol.BorderShadow, new(0.75f, 0.35f, 0.45f, 0.15f)),
        (ImGuiCol.FrameBg, new(0.88f, 0.52f, 0.60f, 0.54f)),
        (ImGuiCol.FrameBgHovered, new(0.93f, 0.58f, 0.67f, 0.65f)),
        (ImGuiCol.FrameBgActive, new(0.90f, 0.45f, 0.55f, 0.67f)),
        (ImGuiCol.TitleBg, new(0.78f, 0.38f, 0.48f, 1f)),
        (ImGuiCol.TitleBgActive, new(0.88f, 0.42f, 0.55f, 1f)),
        (ImGuiCol.TitleBgCollapsed, new(0.82f, 0.40f, 0.50f, 0.75f)),
        (ImGuiCol.Button, new(0.85f, 0.45f, 0.55f, 0.50f)),
        (ImGuiCol.ButtonHovered, new(0.90f, 0.52f, 0.62f, 0.75f)),
        (ImGuiCol.ButtonActive, new(0.82f, 0.38f, 0.50f, 1f)),
    ];

    public PopupWindow(Configuration config)
        : base("###YanderePartnerPopup",
            ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings)
    {
        this.config = config;
        RespectCloseHotkey = false;
    }

    public void QueueMessage(string text, MessageCategory category)
    {
        pendingText = text;
        pendingCategory = category;
        queuedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        hasPending = true;
    }

    public new void Update()
    {
        if (!config.PopupEnabled || !hasPending)
            return;

        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - queuedAtMs;
        if (elapsed < PopupDelayMs)
            return;

        hasPending = false;
        startRequested = true;
        IsOpen = true;
    }

    private bool startRequested;

    public override void PreDraw()
    {
        if (startRequested)
        {
            startRequested = false;
            openedAt = ImGui.GetTime();
            finishedAt = 0;
            showCloseButton = false;
            engine.Start(pendingText, pendingCategory, openedAt);
        }

        if (!themePushed)
        {
            foreach (var (col, val) in ThemeColors)
                ImGui.PushStyleColor(col, val);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16f, 12f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f);
            themePushed = true;
        }

        base.PreDraw();
    }

    public override void PostDraw()
    {
        if (themePushed)
        {
            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(ThemeColors.Length);
            themePushed = false;
        }
        base.PostDraw();
    }

    public override void Draw()
    {
        if (!config.PopupEnabled)
        {
            IsOpen = false;
            return;
        }

        var now = ImGui.GetTime();
        engine.Tick(now);

        var name = config.PartnerName;
        var separator = name.Length > 0 && name[0] > 0x3000 ? "\uff1a" : ": ";
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 22f);
        ImGui.Text($"{name}{separator}{engine.CurrentText}");
        ImGui.PopTextWrapPos();

        if (!engine.Finished)
        {
            ImGui.SameLine();
            var blink = ((int)(now * 2.5)) % 2 == 0;
            if (blink)
                ImGui.Text("|");
        }

        if (engine.Finished && !showCloseButton)
        {
            showCloseButton = true;
            finishedAt = now;
        }

        if (showCloseButton)
        {
            if (now - finishedAt > AutoDismissAfterFinish)
            {
                IsOpen = false;
                return;
            }

            ImGui.Spacing();

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 10f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(14f, 4f));
            var width = ImGui.GetContentRegionAvail().X;
            if (ImGui.Button("...", new Vector2(width, 0)))
                IsOpen = false;
            ImGui.PopStyleVar(2);
        }
    }

    public override void OnClose()
    {
        engine.Reset();
        hasPending = false;
    }

    public void Dispose() { }
}
