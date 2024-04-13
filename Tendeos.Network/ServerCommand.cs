using Lidgren.Network;

public class ServerCommand
{
    public bool Global { get; set; } = false;
    public Func<NetConnection, byte[], byte[]> Action { get; set; } = (e, b) => Array.Empty<byte>();
}