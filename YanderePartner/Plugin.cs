using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace YanderePartner;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IDutyState DutyState { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] public static IFlyTextGui FlyTextGui { get; private set; } = null!;
    [PluginService] public static IPartyList PartyList { get; private set; } = null!;
    [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui_ { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;

    private readonly Configuration config;
    private readonly WindowSystem windowSystem = new("YanderePartner");
    private readonly ConfigWindow configWindow;
    private readonly MessageManager messageManager;
    private readonly EventWatcher eventWatcher;

    public Plugin()
    {
        config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        messageManager = new MessageManager(config);

        configWindow = new ConfigWindow(config, SaveConfig);
        windowSystem.AddWindow(configWindow);

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += () => configWindow.IsOpen = true;
        PluginInterface.UiBuilder.OpenMainUi += () => configWindow.IsOpen = true;

        CommandManager.AddHandler("/yandere", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Yandere Partner settings",
            ShowInHelp = true,
        });

        eventWatcher = new EventWatcher(config, messageManager);
    }

    private void OnCommand(string command, string args)
    {
        configWindow.IsOpen ^= true;
    }

    private void SaveConfig()
    {
        PluginInterface.SavePluginConfig(config);
    }

    public void Dispose()
    {
        eventWatcher.Dispose();
        CommandManager.RemoveHandler("/yandere");
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        windowSystem.RemoveAllWindows();
        configWindow.Dispose();
    }
}
