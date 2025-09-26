using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Windowing.Window;
using Dalamud.Bindings.ImGui;
using BetterFriendList.GameAddon;
using BetterFriendList;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace BetterFriendList.Windows
{
    internal class DrawCommon
    {

        public static void IsHovered(string info)
        {
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip(info); }
        }

        public static void ClickToCopyText(string text, string textCopy = null)
        {
            textCopy ??= text;
            ImGui.Text($"{text}");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (textCopy != text) ImGui.SetTooltip(textCopy);
            }

            if (ImGui.IsItemClicked()) ImGui.SetClipboardText($"{textCopy}");
        }

        public unsafe static List<TitleBarButton> CreateTitleBarButtons(Plugin plugin)
        {
            List<TitleBarButton> titleBarButtons = new()
            {
                new TitleBarButton()
                {
                    Icon = Dalamud.Interface.FontAwesomeIcon.Recycle,
                    Click = (msg) =>
                    {
                        var agent = AgentFriendlist.Instance();
                        if (agent == null) return;

                        if (agent->InfoProxy == null) return;

                        Plugin.Log.Debug("update request?");
                        if (Plugin.IsRequestDataAllowed())
                            Plugin.Log.Debug(agent->InfoProxy->RequestData().ToString());
                    },
                    IconOffset = new(2,1),
                    ShowTooltip = () =>
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Refresh Friend List");
                        ImGui.EndTooltip();
                    }
                },

                new TitleBarButton()
                {
                    Icon = Dalamud.Interface.FontAwesomeIcon.UsersViewfinder,
                    Click = (msg) =>
                    {
                        Plugin.Log.Debug("reset data => request pf data?");
                        if (Plugin.IsRequestDataAllowed())
                        {
                            PartyFinderData.ResetData();
                            PartyFinderData.RefreshListing();
                        }
                    },
                    IconOffset = new(2,1),
                    ShowTooltip = () =>
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Refresh Party Finder");
                        ImGui.EndTooltip();
                    }
                },

                new TitleBarButton()
                {
                    Icon = Dalamud.Interface.FontAwesomeIcon.Cog,
                    Click = (msg) =>
                    {
                        plugin.ToggleConfigUI();
                    },
                    IconOffset = new(2,1),
                    ShowTooltip = () =>
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Settings");
                        ImGui.EndTooltip();
                    }
                }
            };

            return titleBarButtons;
        }

    }
}
