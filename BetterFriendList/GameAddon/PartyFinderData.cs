using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static BetterFriendList.GameAddon.PartyFinderData;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group.SharedGroupLayoutInstance;

namespace BetterFriendList.GameAddon;

internal class PartyFinderData : IDisposable
{
    #region Singleton
    private PartyFinderData()
    {
        hookManagerPartyFinderRefresh = new HookManagerPartyFinderRefresh();
        Plugin.PartyFinderGui.ReceiveListing += OnReceivedListing;
    }

    public static void Initialize() { Instance = new PartyFinderData(); }

    public static PartyFinderData Instance { get; private set; } = null!;

    ~PartyFinderData()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Plugin.PartyFinderGui.ReceiveListing -= OnReceivedListing;
        //this.RequestPfListingsHook?.Dispose();
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        Instance = null!;
    }
    #endregion

    private HookManagerPartyFinderRefresh hookManagerPartyFinderRefresh;
    public Dictionary<ulong, IPartyFinderListing> data = new Dictionary<ulong, IPartyFinderListing>();

    public static void RefreshListing()
    {
        if (Instance == null)
        {
            return;
        }
        Instance.hookManagerPartyFinderRefresh.RefreshListings();
    }

    private void OnReceivedListing(IPartyFinderListing listing, IPartyFinderListingEventArgs args)
    {
        Plugin.Log.Debug($"{listing.Id} {listing.ContentId} {listing.Description}");
        try
        {
            if (!data.ContainsKey(listing.ContentId))
            {
                data.TryAdd(listing.ContentId, listing);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Debug("problem adding entry in data pf");
        }
    }

    public static void ResetData()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.data.Clear();
    }

    public static IPartyFinderListing? GetData(ulong id)
    {
        if (Instance == null)
        {
            return null;
        }
        try
        {
            return Instance.data[id];
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public unsafe class HookManagerPartyFinderRefresh
    {
        //https://github.com/Infiziert90/BetterPartyFinder/blob/main/BetterPartyFinder/HookManager.cs
        [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 0F 10 81")]
        private readonly delegate* unmanaged<AgentLookingForGroup*, byte, byte> RequestPartyFinderListings = null!;

        public HookManagerPartyFinderRefresh()
        {
            Plugin.GameInteropProvider.InitializeFromAttributes(this);
        }

        public void RefreshListings()
        {
            if (RequestPartyFinderListings == null)
                throw new InvalidOperationException("Could not find signature for Party Finder listings");

            var agent = AgentLookingForGroup.Instance();
            var searchAreaTab = AgentLookingForGroup.Instance()->SearchAreaTab;
            AgentLookingForGroup.Instance()->SearchAreaTab = 0;
            RequestPartyFinderListings(agent, 0);
            AgentLookingForGroup.Instance()->SearchAreaTab = 1;
            RequestPartyFinderListings(agent, 0);
            AgentLookingForGroup.Instance()->SearchAreaTab = 2;
            RequestPartyFinderListings(agent, 0);
            AgentLookingForGroup.Instance()->SearchAreaTab = searchAreaTab;
        }
    }

}
