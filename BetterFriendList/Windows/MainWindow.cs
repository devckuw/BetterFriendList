using System;
using System.Numerics;
using Dalamud.Game.Network.Structures.InfoProxy;
using Dalamud.Interface.Colors;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.Sheets;
using static FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCommonList.CharacterData;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using static FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCommonList.DisplayGroup;
using Dalamud.Interface.Components;
using System.Text;
using BetterFriendList;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Storage.Assets;
using Dalamud;

using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using BetterFriendList.GameAddon;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Plugin.Services;
using static System.Net.Mime.MediaTypeNames;
using Dalamud.Interface.Utility.Table;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Interop;
using System.Data.Common;
using Lumina.Excel;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Extensions;

namespace BetterFriendList.Windows;

public unsafe class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private SeStringDrawParams style;
    private bool useEntity;

    private string dmTargetName = string.Empty;
    private string dmTargetWorld = string.Empty;

    private int grpDisplay = (int)Grp.All;
    private string nameRegex = string.Empty;
    
    private Vector3 color = new Vector3();

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("Friend List##With a hidden ID")
    {
        TitleBarButtons = DrawCommon.CreateTitleBarButtons();

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 330),
            MaximumSize = new Vector2(760, float.MaxValue)
        };

        Plugin = plugin;
        useEntity = true;
        this.style = new() { GetEntity = this.GetEntity };
        Plugin.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        if (dmTargetName != string.Empty && dmTargetWorld != string.Empty)
        {
            ChatHelper.SetChatDM(dmTargetName, dmTargetWorld);
            dmTargetName = string.Empty;
            dmTargetWorld = string.Empty;
        }
    }

    private unsafe ulong GetContentId(IPlayerCharacter player)
    {
        var chara = (Character*)player.Address;
        return chara == null ? 0 : chara->ContentId;
    }

    private unsafe void RefreshSolo(int number, byte* name)
    {
        // never called atm friend list has to be open and maybe not safe
        AddonFriendList* addon = (AddonFriendList*)Plugin.GameGui.GetAddonByName("FriendList");
        
        AtkValue* callbackArgs = stackalloc AtkValue[3];
        callbackArgs[0] = new AtkValue { Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int, Int = 107 };
        callbackArgs[1] = new AtkValue { Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int, Int = number };
        callbackArgs[2] = new AtkValue { Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String, String = name};
        Plugin.Log.Debug($"{callbackArgs[0].Type} {callbackArgs[0].Int}");
        Plugin.Log.Debug($"{callbackArgs[1].Type} {callbackArgs[1].Int}");
        Plugin.Log.Debug($"{callbackArgs[2].Type} {callbackArgs[2].String}");

        if (addon != null )
        {
            Plugin.Log.Debug($"addon here");
            addon->FireCallback(3, callbackArgs, true);
        }
        else
        {
            Plugin.Log.Debug($"addon not here");
        }

    }

    public override void Draw()
    {
        var agent = AgentFriendlist.Instance();
        if (agent == null) return;

        if (agent->InfoProxy == null)
        {
            ImGui.Separator();
            ImGui.TextDisabled("Friend list is not loaded.");
            return;
        }
#if DEBUG
        if (ImGui.Button("update friends"))
        {
            Plugin.Log.Debug("update request?");
            agent->InfoProxy->RequestData();
            
        }
        ImGui.SameLine();
        if (ImGui.Button("update pf"))
        {
            Plugin.Log.Debug("reset data request?");
            PartyFinderData.ResetData();
            Plugin.Log.Debug("refresh pf request?");
            PartyFinderData.RefreshListing();
        }
        ImGui.SameLine();
        
        ImGui.Text($"pf loaded: {PartyFinderData.Instance.data.Count}, flag grp: {grpDisplay}, nb friend: {agent->InfoProxy->EntryCount}");

        ImGui.SameLine();
        if (ImGui.Button("test type"))
        {
            Plugin.IsRequestDataAllowed();
        }
#endif

        //ImGuiHelpers.CompileSeStringWrapped($"<icon(56)>");
        //ImGuiHelpers.CompileSeStringWrapped($"<icon(56)> <Gui(12)/> Lorem ipsum dolor <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet,<italic(0)> <colortype(500)><edgecolortype(501)>conse<->ctetur<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)><italic(1)>adipi<-><colortype(504)><edgecolortype(505)>scing<colortype(0)><edgecolortype(0)><italic(0)><colortype(0)><edgecolortype(0)> elit. <colortype(502)><edgecolortype(503)>Maece<->nas<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>digni<-><colortype(504)><edgecolortype(505)>ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>sem<colortype(0)><edgecolortype(0)> <italic(1)>at<italic(0)> inter<->dum <colortype(500)><edgecolortype(501)>ferme<->ntum.<colortype(0)><edgecolortype(0)> Praes<->ent <colortype(500)><edgecolortype(501)>ferme<->ntum<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>conva<->llis<colortype(0)><edgecolortype(0)> velit <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet<italic(0)> <colortype(500)><edgecolortype(501)>hendr<->erit.<colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>Sed<colortype(0)><edgecolortype(0)> eu nibh <colortype(502)><edgecolortype(503)>magna.<colortype(0)><edgecolortype(0)> Integ<->er nec lacus in velit porta euism<->od <colortype(504)><edgecolortype(505)>sed<colortype(0)><edgecolortype(0)> et lacus. <colortype(504)><edgecolortype(505)>Sed<colortype(0)><edgecolortype(0)> non <colortype(502)><edgecolortype(503)>mauri<->s<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>venen<-><italic(1)>atis,<colortype(0)><edgecolortype(0)><italic(0)> <colortype(502)><edgecolortype(503)>matti<->s<colortype(0)><edgecolortype(0)> <colortype(502)><edgecolortype(503)>metus<colortype(0)><edgecolortype(0)> in, <italic(1)>aliqu<->et<italic(0)> dolor. <italic(1)>Aliqu<->am<italic(0)> erat <colortype(500)><edgecolortype(501)>volut<->pat.<colortype(0)><edgecolortype(0)> Nulla <colortype(500)><edgecolortype(501)>venen<-><italic(1)>atis<colortype(0)><edgecolortype(0)><italic(0)> velit <italic(1)>ac<italic(0)> <colortype(504)><edgecolortype(505)><colortype(516)><edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>ci<->pit<colortype(0)><edgecolortype(0)> euism<->od. <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)><colortype(516)><edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>pe<->ndisse<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(502)><edgecolortype(503)>maxim<->us<colortype(0)><edgecolortype(0)> viver<->ra dui id dapib<->us. Nam torto<->r dolor, <colortype(500)><edgecolortype(501)>eleme<->ntum<colortype(0)><edgecolortype(0)> quis orci id, pulvi<->nar <colortype(500)><edgecolortype(501)>fring<->illa<colortype(0)><edgecolortype(0)> quam. <colortype(500)><edgecolortype(501)>Pelle<->ntesque<colortype(0)><edgecolortype(0)> laore<->et viver<->ra torto<->r eget <colortype(502)><edgecolortype(503)>matti<-><colortype(504)><edgecolortype(505)>s.<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>Vesti<-><bold(1)>bulum<colortype(0)><edgecolortype(0)><bold(0)> eget porta <italic(1)>ante,<italic(0)> a <colortype(502)><edgecolortype(503)>molli<->s<colortype(0)><edgecolortype(0)> nulla. <colortype(500)><edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> a ligul<->a leo. <italic(1)>Aliqu<->am<italic(0)> volut<->pat <colortype(504)><edgecolortype(505)>sagit<->tis<colortype(0)><edgecolortype(0)> dapib<->us.");
        /*for (int i = 1; i < 173; i++)
        {
            ImGui.Text($"<icon({i})> => ");
            ImGui.SameLine();
            ImGuiHelpers.CompileSeStringWrapped($"<icon({i})>");
        }
        //ImGuiHelpers.CompileSeStringWrapped("icon(61502)", this.style);
        for (int i = 61501; i < 61549; i++)
        {
            ImGui.Text($"icon({i}) => ");
            ImGui.SameLine();
            ImGuiHelpers.CompileSeStringWrapped($"icon({i})", this.style);
        }*/

        if (ImGui.BeginTable("friends", 7, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Grp", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 150);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 167);
            ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 210);
            ImGui.TableSetupColumn("Company", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 70);
            ImGui.TableSetupColumn("Lang", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 30);

            ImGui.TableHeadersRow();

            ContextMenus();

            //Plugin.Log.Debug($"{agent->InfoProxy->EntryCount}");
            string playerDataCenter = "";
            ushort playerWorld = 0;
            bool isLeader = true;
            bool isMemberCross = false;
            if (Plugin.ClientState.LocalPlayer != null)
            {
                playerWorld = (ushort)Plugin.ClientState.LocalPlayer.CurrentWorld.RowId;
                playerDataCenter = Plugin.ClientState.LocalPlayer.CurrentWorld.Value.DataCenter.Value.Name.ExtractText();
            }
            if (Plugin.PartyList != null && Plugin.ClientState.LocalPlayer != null)
            {
                if (Plugin.PartyList.Count > 1)
                    isLeader = Plugin.PartyList[(int)Plugin.PartyList.PartyLeaderIndex].ContentId == (long)GetContentId(Plugin.ClientState.LocalPlayer);

                if (InfoProxyCrossRealm.IsCrossRealmParty())
                {
                    //Plugin.Log.Debug("is crossparty");
                    if (InfoProxyCrossRealm.GetMemberByContentId((ulong)GetContentId(Plugin.ClientState.LocalPlayer))->IsPartyLeader != 0)
                    {
                        //Plugin.Log.Debug("is leader");
                    }
                    else
                    {
                        //Plugin.Log.Debug("is not leader"); 
                        isMemberCross = true;
                    }
                }
                /*else
                {
                    Plugin.Log.Debug("is not crossparty");
                }*/
            }


            for (var i = 0U; i < agent->InfoProxy->EntryCount; i++)
            {
                var friend = agent->InfoProxy->GetEntry(i);
                if (friend == null) continue;

                var name = friend->NameString;
                if (!name.ToLower().Contains(nameRegex.ToLower())) continue;

                if (!Plugin.Configuration.FriendsColors.ContainsKey(friend->ContentId))
                {
                    Plugin.Configuration.FriendsColors.Add(friend->ContentId, new Vector4(255, 255, 255, 1));
                }

                var aname = friend->Group;
                switch (friend->Group)
                {
                    case None:
                        if (!((Grp)grpDisplay).HasFlag(Grp.None)) continue;
                        break;
                    case Star:
                        if (!((Grp)grpDisplay).HasFlag(Grp.Star)) continue;
                        break;
                    case Circle:
                        if (!((Grp)grpDisplay).HasFlag(Grp.Circle)) continue;
                        break;
                    case Triangle:
                        if (!((Grp)grpDisplay).HasFlag(Grp.Triangle)) continue;
                        break;
                    case Diamond:
                        if (!((Grp)grpDisplay).HasFlag(Grp.Diamond)) continue;
                        break;
                    case Heart:
                        if (!((Grp)grpDisplay).HasFlag(Grp.Heart)) continue;
                        break;
                    case Spade:
                        if (!((Grp)grpDisplay).HasFlag(Grp.Spade)) continue;
                        break;
                    case Club:
                        if (!((Grp)grpDisplay).HasFlag(Grp.Club)) continue;
                        break;
                    default:
                        break;
                }


                ImGui.TableNextRow();


                Plugin.DataManager.GetExcelSheet<World>().TryGetRow(friend->CurrentWorld, out var friendCurrentWorld);
                Plugin.DataManager.GetExcelSheet<World>().TryGetRow(friend->HomeWorld, out var friendHomeWorld);

                ImGui.TableNextColumn();
                switch (friend->Group)
                {
                    case None:
                        break;
                    case Star:
                        ImGui.Text($"  ★");
                        break;
                    case Circle:
                        ImGui.Text($"  ●");
                        break;
                    case Triangle:
                        ImGui.Text($"  ▲");
                        break;
                    case Diamond:
                        ImGui.Text($"  ♦");
                        break;
                    case Heart:
                        ImGui.Text($"  ♥");
                        break;
                    case Spade:
                        ImGui.Text($"  ♠");
                        break;
                    case Club:
                        ImGui.Text($"  ♣");
                        break;
                    default:
                        break;
                }
                ImGui.TableNextColumn();
                int status = 0;
                switch (friend->State)
                {
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AnotherWorld):
                        status = 35;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.InDuty):
                        status = 10;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.WaitingForDutyFinder):
                        status = 17;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.ViewingCutscene):
                        status = 8;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.CameraMode):
                        status = 46;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AwayFromKeyboard):
                        status = 11;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Busy):
                        status = 9;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.RecruitingPartyMembers):
                        status = 36;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PlayingTripleTriad):
                        status = 39;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AllianceLeader):
                        status = 18;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AlliancePartyLeader):
                        status = 19;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AlliancePartyMember):
                        status = 20;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PartyLeaderCrossWorld)://?
                        status = 21;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PartyLeader)://21
                        status = 21;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PartyMemberCrossWorld)://?
                        status = 22;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PartyMember)://22
                        status = 22;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.RolePlaying):
                        status = 45;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.LookingForParty):
                        status = 15;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.LookingToMeldMateria):
                        status = 14;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.LookingForRepairs):
                        status = 12;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.LookingToRepair):
                        status = 13;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Mentor):
                        status = 40;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.BattleMentor):
                        status = 42;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.TradeMentor):
                        status = 43;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.NewAdventurer):
                        status = 23;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Returner):
                        status = 47;
                        break;
                    case var s when s.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Online):
                        status = 5;
                        break;
                    default: // Offline 4
                        status = 4;
                        break;
                }
                ImGuiHelpers.CompileSeStringWrapped($"icon({status + 61500})", this.style);
                ImGui.SameLine();

                ImGui.TextColored(Plugin.Configuration.FriendsColors[friend->ContentId], name);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                ImGui.PushID($"{498 + i}");
                if (ImGui.IsItemClicked())
                {
                    Plugin.Log.Debug($"clicked {name}");
                    color = Plugin.Configuration.FriendsColors[friend->ContentId].AsVector3();
                    ImGui.OpenPopup($"FriendContextMenu##{i}");
                }
                if (ImGui.BeginPopup($"FriendContextMenu##{i}"))
                {
                    // to be added later maybe
                    /*if (ImGui.Button("ls")) { }
                    ImGui.SameLine();
                    if (ImGui.Button("cross ls")) { }
                    ImGui.SameLine();
                    if (ImGui.Button("asign grp")) { }
                    ImGui.SameLine();
                    if (ImGui.Button("del friend")) { }*/
                    
                    if (ImGui.ColorPicker3("", ref color, ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.DisplayHex))
                    {
                        Plugin.Configuration.FriendsColors[friend->ContentId] = new Vector4(color, 1);
                        Plugin.Configuration.Save();
                    }

                    ImGui.EndPopup();
                }
                ImGui.PopID();

                ImGui.TableNextColumn();
                bool houseFlag = true;
                if (Plugin.ClientState.LocalPlayer != null)
                {
                    if (Plugin.ClientState.LocalPlayer.CurrentWorld.RowId == friend->HomeWorld)
                    {
                        houseFlag = false;
                        if (ImGuiComponents.IconButton($"buttontphouse{i}", FontAwesomeIcon.HouseChimney, new Vector2(27, 20)))
                        {
                            agent->OpenFriendEstateTeleportation(friend->ContentId);
                        }
                        DrawCommon.IsHovered("Estate Teleportation");
                    }
                }
                if (houseFlag)
                {
                    ImGui.Dummy(new Vector2(27, 20));
                }
                ImGui.SameLine();
                if (friend->State == 0 || !friendCurrentWorld.DataCenter.Value.Name.ExtractText().Contains(playerDataCenter) ||
                    friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PartyLeader) || friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PartyMember) ||
                    (friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PartyLeaderCrossWorld) && !friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.RecruitingPartyMembers)) ||
                    friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.PartyMemberCrossWorld) ||
                    friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AlliancePartyLeader) || friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AlliancePartyMember) ||
                    friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AlliancePartyMember) || friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AnotherWorld) ||
                    friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Busy) || !isLeader || isMemberCross)
                {
                    ImGui.Dummy(new Vector2(27, 20));
                }
                else
                {
                    if (friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.RecruitingPartyMembers))
                    {
                        if (ImGuiComponents.IconButton($"buttoninvite{i}", FontAwesomeIcon.UsersViewfinder, new Vector2(27, 20)))
                        {
                            var id = PartyFinderData.GetData(friend->ContentId);
                            if (id != null)
                                GameFunctions.OpenPartyFinder(id.Id);
                        }
                        DrawCommon.IsHovered("Open Party Finder");
                    }
                    else
                    {
                        if (ImGuiComponents.IconButton($"buttoninvite{i}", FontAwesomeIcon.PeopleGroup, new Vector2(27, 20)))
                        {
                            //invite;
                            if (friend->CurrentWorld == playerWorld)
                            {
                                GameFunctions.InviteSameWorld(friend->NameString, friend->CurrentWorld, friend->ContentId);
                            }
                            else
                            {
                                GameFunctions.InviteOtherWorld(friend->ContentId, friend->CurrentWorld);
                            }
                        }
                        DrawCommon.IsHovered("Invite to Party");
                    }
                }
                ImGui.SameLine();
                if (friend->State == 0 || !friendCurrentWorld.DataCenter.Value.Name.ExtractText().Contains(playerDataCenter) ||
                    friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.AnotherWorld) || friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Busy))
                {
                    ImGui.Dummy(new Vector2(27, 20));
                }
                else
                {
                    if (ImGuiComponents.IconButton($"buttondm{i}", FontAwesomeIcon.Comment, new Vector2(27, 20)))
                    {
                        dmTargetName = friend->NameString;
                        dmTargetWorld = friendHomeWorld.Name.ExtractText();
                    }
                    DrawCommon.IsHovered("Send Tell");
                }
                ImGui.SameLine();
                if (!friendCurrentWorld.DataCenter.Value.Name.ExtractText().Contains(playerDataCenter))
                {
                    ImGui.Dummy(new Vector2(27, 20));
                }
                else
                {
                    if (ImGuiComponents.IconButton($"buttonadventurer{i}", FontAwesomeIcon.ListAlt, new Vector2(27, 20)))
                    {
                        GameFunctions.TryOpenAdventurerPlate(friend->ContentId);
                    }
                    DrawCommon.IsHovered("View Adventurer Plate");
                }
                ImGui.SameLine();
                if (friend->CurrentWorld != playerWorld)
                {
                    ImGui.Dummy(new Vector2(27, 20));
                }
                else
                {
                    if (ImGuiComponents.IconButton($"buttoninfo{i}", FontAwesomeIcon.Info, new Vector2(27, 20)))
                    {
                        GameFunctions.OpenSearchInfo(friend);
                    }
                    DrawCommon.IsHovered("View Search Info");
                }

                ImGui.TableNextColumn();
                if (!Plugin.DataManager.GetExcelSheet<ClassJob>().TryGetRow(friend->Job, out var job))
                {
                    ImGui.TextDisabled("Unknown");
                }
                else
                {
                    switch (job.RowId)
                    {
                        case 0:
                            break;
                        case < 41:
                            ImGuiHelpers.CompileSeStringWrapped(((Icons)job.RowId + 127).toBaliseString());
                            break;
                        default:
                            ImGuiHelpers.CompileSeStringWrapped(((Icons)job.RowId + 129).toBaliseString());
                            break;
                    }

                }

                ImGui.TableNextColumn();
                if (!Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(friend->Location, out var location))
                {
                    if (friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Online))
                    {
                        ImGui.Text($"{friendCurrentWorld.Name}");
                    }
                    else
                    {
                        ImGui.TextDisabled($"{friendCurrentWorld.Name}");
                    }
                    bool canRefreshSolo = false;
                    if (friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Online) && friendCurrentWorld.RowId == playerWorld)
                        canRefreshSolo = true;
                    if (friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Online) && friendCurrentWorld.RowId != playerWorld)
                        canRefreshSolo = true;
                    if (friend->State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Offline) && friendCurrentWorld.RowId != playerWorld)
                        canRefreshSolo = true;
                    if (canRefreshSolo) {
                        ImGui.SameLine();
                        if (ImGuiComponents.IconButton($"try refresh solo##{i}", FontAwesomeIcon.Recycle))
                        {
                            // has to have friend list open so idk? and maybe not safe
                            /*string grpSign = "";
                            switch (friend->Group)
                            {
                                case None:
                                    break;
                                case Star:
                                    grpSign = "★";
                                    break;
                                case Circle:
                                    grpSign = "●";
                                    break;
                                case Triangle:
                                    grpSign = "▲";
                                    break;
                                case Diamond:
                                    grpSign = "♦";
                                    break;
                                case Heart:
                                    grpSign = "♥";
                                    break;
                                case Spade:
                                    grpSign = "♠";
                                    break;
                                case Club:
                                    grpSign = "♣";
                                    break;
                                default:
                                    break;
                            }
                            Span<byte> bytes = Encoding.UTF8.GetBytes($"{grpSign}{friend->NameString}");
                            //RefreshSolo((int)i, friend->Name.GetPointer<byte>(0));
                            RefreshSolo((int)i, bytes.GetPointer<byte>(0));*/
                        }
                    }
                }
                else
                {
                    ImGui.Text($"{location.PlaceName.Value.Name.ExtractText()}");
                }

                ImGui.TableNextColumn();
                switch (friend->GrandCompany)
                {
                    case FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany.TwinAdder:
                        ImGuiHelpers.CompileSeStringWrapped($"{Icons.Gridania.toBaliseString()} {friend->FCTagString}");
                        break;
                    case FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany.Maelstrom:
                        ImGuiHelpers.CompileSeStringWrapped($"{Icons.Limsa.toBaliseString()} {friend->FCTagString}");
                        break;
                    case FFXIVClientStructs.FFXIV.Client.UI.Agent.GrandCompany.ImmortalFlames:
                        ImGuiHelpers.CompileSeStringWrapped($"{Icons.Uldah.toBaliseString()} {friend->FCTagString}");
                        break;
                    default:
                        ImGuiHelpers.CompileSeStringWrapped($"{friend->FCTagString}");
                        break;
                }

                ImGui.TableNextColumn();

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(1));
                var dl = ImGui.GetWindowDrawList();
                ImGui.TextColored(friend->Languages.HasFlag(LanguageMask.Jp) ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey3, "J");
                if (friend->ClientLanguage == Language.Jp) dl.AddLine(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y + 1), ImGui.GetItemRectMax() + new Vector2(0, 1), 0xFFFFFFFF, 2);

                ImGui.SameLine();
                ImGui.TextColored(friend->Languages.HasFlag(LanguageMask.En) ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey3, "E");
                if (friend->ClientLanguage == Language.En) dl.AddLine(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y + 1), ImGui.GetItemRectMax() + new Vector2(0, 1), 0xFFFFFFFF, 2);

                ImGui.SameLine();
                ImGui.TextColored(friend->Languages.HasFlag(LanguageMask.De) ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey3, "D");
                if (friend->ClientLanguage == Language.De) dl.AddLine(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y + 1), ImGui.GetItemRectMax() + new Vector2(0, 1), 0xFFFFFFFF, 2);

                ImGui.SameLine();
                ImGui.TextColored(friend->Languages.HasFlag(LanguageMask.Fr) ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey3, "F");
                if (friend->ClientLanguage == Language.Fr) dl.AddLine(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y + 1), ImGui.GetItemRectMax() + new Vector2(0, 1), 0xFFFFFFFF, 2);
                ImGui.PopStyleVar();

                
            }

            ImGui.EndTable();
        }
        ContextMenuGlobal();
    }

    private void ContextMenuGlobal()
    {
        ImGui.PushID(11945);
        if (ImGui.IsAnyItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            ImGui.OpenPopup("ContextMenuGlobal");

        if (ImGui.BeginPopup("ContextMenuGlobal"))
        {
            ContextMenuSettings();
            ImGui.EndPopup();
        }
        ImGui.PopID();
    }

    private void ContextMenuSettings()
    {
        ImGui.SetNextItemWidth(210);
        ImGui.InputTextWithHint("", "Name..", ref nameRegex, 32);
        ImGui.SameLine();
        if (ImGui.Button("Reset")) { nameRegex = string.Empty; grpDisplay = (int)Grp.All; }

        if (ImGui.BeginTable("friends", 9))
        {
            ImGui.TableSetupColumn("  All", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("  ★", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("  ●", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("  ▲", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("  ♦", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("  ♥", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("  ♠", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("  ♣", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 20);
            ImGui.TableSetupColumn("None", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 35);

            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##All", ref grpDisplay, (int)Grp.All);
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##★", ref grpDisplay, (int)Grp.Star);
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##●", ref grpDisplay, (int)Grp.Circle);
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##▲", ref grpDisplay, (int)Grp.Triangle);
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##♦", ref grpDisplay, (int)Grp.Diamond);
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##♥", ref grpDisplay, (int)Grp.Heart);
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##♠", ref grpDisplay, (int)Grp.Spade);
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##♣", ref grpDisplay, (int)Grp.Club);
            ImGui.TableNextColumn();
            ImGui.CheckboxFlags("##None", ref grpDisplay, (int)Grp.None);

            ImGui.EndTable();
        }
    }

    private void ContextMenus()
    {
        int hovered_column = -1;
        for (int column = 0; column < 7; column++)
        {
            ImGui.PushID(column);
            if (ImGui.TableGetColumnFlags(column).HasFlag(ImGuiTableColumnFlags.IsHovered))
                hovered_column = column;
            //if (hovered_column == column && !ImGui.IsAnyItemHovered() && ImGui.IsMouseReleased(1))
            if (hovered_column == column && !ImGui.IsAnyItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            //if (hovered_column == column && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
                ImGui.OpenPopup("ContextMenu");

            if (ImGui.BeginPopup("ContextMenu"))
            {
                ContextMenuSettings();
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
    }

    private SeStringReplacementEntity GetEntity(scoped in SeStringDrawState state, int byteOffset)
    {
        if (!this.useEntity)
            return default;
        if (state.Span[byteOffset..].StartsWith("Dalamud"u8))
            return new(7, new(state.FontSize, state.FontSize), DrawDalamud);
        if (state.Span[byteOffset..].StartsWith("White"u8))
            return new(5, new(state.FontSize, state.FontSize), DrawWhite);
        if (state.Span[byteOffset..].StartsWith("DefaultIcon"u8))
            return new(11, new(state.FontSize, state.FontSize), DrawDefaultIcon);
        if (state.Span[byteOffset..].StartsWith("DisabledIcon"u8))
            return new(12, new(state.FontSize, state.FontSize), DrawDisabledIcon);
        if (state.Span[byteOffset..].StartsWith("OutdatedInstallableIcon"u8))
            return new(23, new(state.FontSize, state.FontSize), DrawOutdatedInstallableIcon);
        if (state.Span[byteOffset..].StartsWith("TroubleIcon"u8))
            return new(11, new(state.FontSize, state.FontSize), DrawTroubleIcon);
        if (state.Span[byteOffset..].StartsWith("DevPluginIcon"u8))
            return new(13, new(state.FontSize, state.FontSize), DrawDevPluginIcon);
        if (state.Span[byteOffset..].StartsWith("UpdateIcon"u8))
            return new(10, new(state.FontSize, state.FontSize), DrawUpdateIcon);
        if (state.Span[byteOffset..].StartsWith("ThirdIcon"u8))
            return new(9, new(state.FontSize, state.FontSize), DrawThirdIcon);
        if (state.Span[byteOffset..].StartsWith("ThirdInstalledIcon"u8))
            return new(18, new(state.FontSize, state.FontSize), DrawThirdInstalledIcon);
        if (state.Span[byteOffset..].StartsWith("ChangelogApiBumpIcon"u8))
            return new(20, new(state.FontSize, state.FontSize), DrawChangelogApiBumpIcon);
        if (state.Span[byteOffset..].StartsWith("InstalledIcon"u8))
            return new(13, new(state.FontSize, state.FontSize), DrawInstalledIcon);
        if (state.Span[byteOffset..].StartsWith("tex("u8))
        {
            var off = state.Span[byteOffset..].IndexOf((byte)')');
            var tex = Plugin.TextureProvider//Service<TextureManager>
                      //.Get()
                      //.Shared
                      .GetFromGame(Encoding.UTF8.GetString(state.Span[(byteOffset + 4)..(byteOffset + off)]))
                      .GetWrapOrEmpty();
            return new(off + 1, tex.Size * (state.FontSize / tex.Size.Y), DrawTexture);
        }

        if (state.Span[byteOffset..].StartsWith("icon("u8))
        {
            var off = state.Span[byteOffset..].IndexOf((byte)')');
            if (int.TryParse(state.Span[(byteOffset + 5)..(byteOffset + off)], out var parsed))
            {
                var tex = Plugin.TextureProvider//Service<TextureManager>
                          //.Get()
                          //.Shared
                          .GetFromGameIcon(parsed)
                          .GetWrapOrEmpty();
                return new(off + 1, tex.Size * (state.FontSize / tex.Size.Y), DrawIcon);
            }
        }

        return default;

        static void DrawTexture(scoped in SeStringDrawState state, int byteOffset, Vector2 offset)
        {
            var off = state.Span[byteOffset..].IndexOf((byte)')');
            var tex = Plugin.TextureProvider//Service<TextureManager>
                      //.Get()
                      //.Shared
                      .GetFromGame(Encoding.UTF8.GetString(state.Span[(byteOffset + 4)..(byteOffset + off)]))
                      .GetWrapOrEmpty();
            state.Draw(
                tex.ImGuiHandle,
                offset + new Vector2(0, (state.LineHeight - state.FontSize) / 2),
                tex.Size * (state.FontSize / tex.Size.Y),
                Vector2.Zero,
                Vector2.One);
        }

        static void DrawIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset)
        {
            var off = state.Span[byteOffset..].IndexOf((byte)')');
            if (!int.TryParse(state.Span[(byteOffset + 5)..(byteOffset + off)], out var parsed))
                return;
            var tex = Plugin.TextureProvider//Service<TextureManager>
                      //.Get()
                      //.Shared
                      .GetFromGameIcon(parsed)
                      .GetWrapOrEmpty();
            state.Draw(
                tex.ImGuiHandle,
                offset + new Vector2(0, (state.LineHeight - state.FontSize) / 2),
                tex.Size * (state.FontSize / tex.Size.Y),
                Vector2.Zero,
                Vector2.One);
        }

        static void DrawAsset(scoped in SeStringDrawState state, Vector2 offset, DalamudAsset asset) =>
        state.Draw(
                //Service<DalamudAssetManager>.Get().GetDalamudTextureWrap(asset).ImGuiHandle,
                Plugin.DalamudAssetManager.GetDalamudTextureWrap(asset).ImGuiHandle,
                offset + new Vector2(0, (state.LineHeight - state.FontSize) / 2),
                new(state.FontSize, state.FontSize),
                Vector2.Zero,
                Vector2.One);

        static void DrawDalamud(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.LogoSmall);

        static void DrawWhite(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.White4X4);

        static void DrawDefaultIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.DefaultIcon);

        static void DrawDisabledIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.DisabledIcon);

        static void DrawOutdatedInstallableIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.OutdatedInstallableIcon);

        static void DrawTroubleIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.TroubleIcon);

        static void DrawDevPluginIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.DevPluginIcon);

        static void DrawUpdateIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.UpdateIcon);

        static void DrawInstalledIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.InstalledIcon);

        static void DrawThirdIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.ThirdIcon);

        static void DrawThirdInstalledIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.ThirdInstalledIcon);

        static void DrawChangelogApiBumpIcon(scoped in SeStringDrawState state, int byteOffset, Vector2 offset) =>
            DrawAsset(state, offset, DalamudAsset.ChangelogApiBumpIcon);

    }
}
