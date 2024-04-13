
namespace Tendeos.Network
{
    public interface INetworkSync
    {
        byte[][] Send();
        void Accept(byte[] data);
    }
}
