namespace Tendeos.Network
{
    public class NetworkSyncSettings
    {
        internal INetworkSync[] networkSyncs;

        public NetworkSyncSettings(params INetworkSync[] networkSyncs)
        {
            this.networkSyncs = networkSyncs;
        }
    }
}