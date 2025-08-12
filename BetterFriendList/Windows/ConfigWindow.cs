using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace BetterFriendList.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("Better Friend List Settings###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(390, 200);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var refreshFriendOnOpen = Configuration.RefreshFriendOnOpen;
        if (ImGui.Checkbox("Refresh friend list on opening", ref refreshFriendOnOpen))
        {
            Configuration.RefreshFriendOnOpen = refreshFriendOnOpen;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            Configuration.Save();
        }

        var sortOnDifferentTab = Configuration.SortOnDifferentTab;
        if (ImGui.Checkbox("Open filter options with right click instead of showing above.", ref sortOnDifferentTab))
        {
            Configuration.SortOnDifferentTab = sortOnDifferentTab;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            Configuration.Save();
        }
        ImGui.NewLine();
        DrawExplication();
    }

    public void DrawExplication()
    {
        string txt = "Click on friends name to change their color.\n" +
            "Right Click wherever to show the filter options.\n" +
            "Refresh buttons are to pull info from the server.\n" +
            "More sorting things will be added later.\n" +
            "You can give idea on discord :)";
        ImGui.Text(txt);
    }
}
