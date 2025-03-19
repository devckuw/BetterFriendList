using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterFriendList.GameFuntions;

public static unsafe class GameFuntions
{
    internal static byte[] ToTerminatedBytes(this string s)
    {
        var utf8 = Encoding.UTF8;
        var bytes = new byte[utf8.GetByteCount(s) + 1];
        utf8.GetBytes(s, 0, s.Length, bytes, 0);
        bytes[^1] = 0;
        return bytes;
    }

    // Taken from https://stackoverflow.com/a/4975942
    internal static string BytesToString(long byteCount)
    {
        string[] suf = ["B", "KB", "MB", "GB", "TB", "PB", "EB"]; // Longs run out around EB
        if (byteCount == 0)
            return "0" + suf[0];

        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        var num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString("N0") + suf[place];
    }

    internal static void InviteSameWorld(string name, ushort world, ulong contentId)
    {
        // this only works if target is on the same world
        fixed (byte* namePtr = name.ToTerminatedBytes())
        {
            InfoProxyPartyInvite.Instance()->InviteToParty(contentId, namePtr, world);
        }
    }

    internal static void InviteOtherWorld(ulong contentId, ushort worldId = 0)
    {
        // third param is world, but it requires a specific world
        // if they're not on that world, it will fail
        // pass 0 and it will work on any world EXCEPT for the world the
        // current player is on
        if (contentId == 0)
        {
            //WrapperUtil.AddNotification(Language.PartyInvite_NoId, NotificationType.Warning);
            return;
        }

        InfoProxyPartyInvite.Instance()->InviteToPartyContentId(contentId, worldId);
    }

    internal static void OpenPartyFinder(uint id)
    {
        AgentLookingForGroup.Instance()->OpenListing(id);
    }

    internal static void OpenPartyFinder()
    {
        // this whole method: 6.05: 84433A (FF 97 ?? ?? ?? ?? 41 B4 01)
        var lfg = AgentLookingForGroup.Instance();
        if (lfg->IsAgentActive())
        {
            var addonId = lfg->GetAddonId();
            var atkModule = RaptureAtkModule.Instance();
            var atkModuleVtbl = (void**)atkModule->AtkModule.VirtualTable;
            var vf27 = (delegate* unmanaged<RaptureAtkModule*, ulong, ulong, byte>)atkModuleVtbl[27];
            vf27(atkModule, addonId, 1);
        }
        else
        {
            // 6.05: 8443DD
            if (*(uint*)((nint)lfg + 0x2C20) > 0)
                lfg->Hide();
            else
                lfg->Show();
        }
    }

    internal static bool TryOpenAdventurerPlate(ulong contentId)
    {
        try
        {
            AgentCharaCard.Instance()->OpenCharaCard(contentId);
            return true;
        }
        catch (Exception e)
        {
            Plugin.Log.Warning(e, "Unable to open adventurer plate");
            return false;
        }
    }
}
