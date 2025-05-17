using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;
using BetterFriendList.Windows;
using Dalamud.Storage.Assets;
using BetterFriendList.GameAddon;
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using Lumina.Extensions;

namespace BetterFriendList;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IDalamudAssetManager DalamudAssetManager { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IPartyFinderGui PartyFinderGui { get; private set; } = null!;

    private const string CommandName = "/betterfriendlist";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("BetterFriendList");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the friend list"
        });

        ChatHelper.Initialize();
        PartyFinderData.Initialize();

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        ChatHelper.Instance?.Dispose();
        PartyFinderData.Instance?.Dispose();

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public unsafe void ToggleMainUI()
    {
        this.MainWindow.Toggle();

        if (MainWindow.IsOpen && Configuration.RefreshFriendOnOpen)
        {
            var agent = AgentFriendlist.Instance();
            if (agent == null) return;

            if (agent->InfoProxy == null) return;

            Plugin.Log.Debug("update request?");

            if (IsRequestDataAllowed())
                agent->InfoProxy->RequestData();
        }
    }

    public static bool IsRequestDataAllowed()
    {
        // https://github.com/nebel/xivPartyIcons/blob/main/PartyIcons/Runtime/ViewModeSetter.cs line 75
        if (!ClientState.IsLoggedIn)
            return false;
        ExcelSheet<ContentFinderCondition> _contentFinderConditionsSheet = Plugin.DataManager.GameData.GetExcelSheet<ContentFinderCondition>() ?? throw new InvalidOperationException();

        var maybeContent = _contentFinderConditionsSheet.FirstOrNull(t => t.TerritoryType.RowId == Plugin.ClientState.TerritoryType);

        unsafe
        {
            var gameMain = GameMain.Instance();
            if (gameMain != null)
            {
                if (GameMain.Instance()->CurrentContentFinderConditionId is var conditionId and not 0)
                {
                    if (_contentFinderConditionsSheet.GetRowOrDefault(conditionId) is { } conditionContent)
                    {
                        maybeContent = conditionContent;
                    }
                }
            }
        }
        if (maybeContent is not { } content || content.RowId is 0)
        {
            Log.Debug($"Refresh allowed -- Content null {Plugin.ClientState.TerritoryType}");
            //logged in + in overworld
            return true;
        }
        else
        {
            Log.Debug($"Refresh NOT allowed -- {Plugin.ClientState.TerritoryType} {content.Name}");
            return false;
        }
    }
}
