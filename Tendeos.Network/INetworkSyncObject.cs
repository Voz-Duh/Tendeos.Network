
namespace Tendeos.Network
{
    public interface INetworkSyncObject : INetworkSync
    {
        byte[][] INetworkSync.Send() => new byte[][] { Send() };

        new byte[] Send();
    }
}
