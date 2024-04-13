public class ClientCommand
{
    public Action<byte[]> Action { get; set; } = b => { };
}