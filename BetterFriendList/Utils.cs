using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using NetStone;
using NetStone.Search.Character;
using Dalamud.Utility;

namespace BetterFriendList;

public class LodeStoneService : IDisposable
{
    LodestoneClient? client;

    public LodeStoneService()
    {
        SetupClient();
    }

    public void Dispose()
    {
        client?.Dispose();
    }

    public async void SetupClient()
    {
        try{
	        client = await LodestoneClient.GetClientAsync();
        } catch(HttpRequestException ex){
            client = null;
        }
        return;
    }

    public void OpenLodestoneProfile(string name, string server)
    {   
        if (client == null)
        {
            Plugin.Log.Debug($"client not setup");
            return;
        }

        try
        {
            Task.Run(async () =>
            {
                try
                {
                    var searchResponse = await client.SearchCharacter(new CharacterSearchQuery()
                    {
                        CharacterName = name,
                        World = server
                    });
                    var lodestoneCharacter = 
                        searchResponse?.Results
                        .FirstOrDefault(entry => entry.Name == name);
                    
                    if (lodestoneCharacter == null)
                    {
                        Plugin.Log.Debug("profile not found");
                        return;
                    }
                    string lodestoneId = lodestoneCharacter.Id;
                    //Plugin.Log.Debug($"lodestone id : {lodestoneId}");
                    string url = $"https://na.finalfantasyxiv.com/lodestone/character/{lodestoneId}/";
                    Dalamud.Utility.Util.OpenLink(url);
                }
                catch (HttpRequestException e)
                {
                    Plugin.Log.Debug("lodestone request diodnt work");
                }
            });
        }
        catch
        {
            Plugin.Log.Debug("lodestone request diodnt work");
        }
    }

}

public class ForcedColour
{
    public ushort ColourKey { get; set; }
    public Dictionary<string, double> Color { get; set; }
    public Vector3? Glow { get; set; }
    public string PlayerName { get; set; }
    public string WorldName { get; set; }
}

public class ChannelConfig
{
    public bool Sender { get; set; }
    public bool Message { get; set; }
}

public class SimpleTweaksChatNameColor
{
    public List<ForcedColour> ForcedColours { get; set; }
    public bool RandomColours { get; set; }
    public bool LegacyColours { get; set; }
    public bool ApplyDefaultColour { get; set; }
    public ushort DefaultColourKey { get; set; }
    public Vector3 DefaultColour { get; set; }

    public ChannelConfig DefaultChannelConfig { get; set; }
    public Dictionary<XivChatType, ChannelConfig> ChannelConfigs { get; set; }

    public ushort Version { get; set; }
}

public static class Extensions
{
    public static string toBaliseString(this Icons icon)
    {
        if (icon == Icons.None)
            return "";
        return $"<icon({(int)icon})>";
    }
}

[Flags]
public enum Grp
{
    Nothing = 0x0,
    Star = 0x1,
    Circle = 0x2,
    Triangle = 0x4,
    Diamond = 0x8,
    Heart = 0x10,
    Spade = 0x20,
    Club = 0x40,
    None = 0x80,
    All = Star | Circle | Triangle | Diamond | Heart | Spade | Club | None
}

public struct SortingKeys
{
    public uint index;
}

public enum Sorting
{
    Oldest = 0,
    Newest = 1,
    Alphabetical = 2,
    HomeWorld = 3,
    Color = 4,
    Group = 5
}

public enum Icons
{
    None = 0,
    Limsa = 51,
    Gridania = 52,
    Uldah = 53,
    GLA = 128,
    PGL = 129,
    MRD = 130,
    LNC = 131,
    ARC = 132,
    CNJ = 133,
    THM = 134,
    CRP = 135,
    BSM = 136,
    ARM = 137,
    GSM = 138,
    LTW = 139,
    WVR = 140,
    ALC = 141,
    CUL = 142,
    MIN = 143,
    BTN = 144,
    FSH = 145,
    PLD = 146,
    MNK = 147,
    WAR = 148,
    DRG = 149,
    BRD = 150,
    WHM = 151,
    BLM = 152,
    ACN = 153,
    SMN = 154,
    SCH = 155,
    ROG = 156,
    NIN = 157,
    MCH = 158,
    DRK = 159,
    AST = 160,
    SAM = 161,
    RDM = 162,
    BLU = 163,
    GNB = 164,
    DNC = 165,
    RPR = 166,
    SGE = 167,
    VRP = 170,
    PCT = 171
}
