using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Windowing.Window;
using ImGuiNET;
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

        public unsafe static List<TitleBarButton> CreateTitleBarButtons()
        {
            List<TitleBarButton> titleBarButtons = new()
            {
                /*new TitleBarButton()
                {
                    Icon = Dalamud.Interface.FontAwesomeIcon.Cog,
                    Click = (msg) =>
                    {
                        InfoManager.plugin.ToggleConfigUI();
                    },
                    IconOffset = new(2,1),
                    ShowTooltip = () =>
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Open Settings");
                        ImGui.EndTooltip();
                    }
                },*/
                new TitleBarButton()
                {
                    Icon = Dalamud.Interface.FontAwesomeIcon.Recycle,
                    Click = (msg) =>
                    {
                        Plugin.Log.Debug("Refresh Friend List Start");
                        var agent = AgentFriendlist.Instance();
                        if (agent == null) return;

                        if (agent->InfoProxy == null) return;

                        Plugin.Log.Debug("update request?");
                        agent->InfoProxy->RequestData();
                        Plugin.Log.Debug("Refresh Friend List End");
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
                        Plugin.Log.Debug("Refresh Party Finder Start");
                        Plugin.Log.Debug("reset data request?");
                        PartyFinderData.ResetData();
                        Plugin.Log.Debug("refresh pf request?");
                        PartyFinderData.RefreshListing();
                        Plugin.Log.Debug("Refresh Party Finder End");
                    },
                    IconOffset = new(2,1),
                    ShowTooltip = () =>
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Refresh Party Finder");
                        ImGui.EndTooltip();
                    }
                }
            };

            return titleBarButtons;
        }

    }
}
