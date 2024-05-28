using Lidgren.Network;
using Socketize;

namespace Tendeos.Network
{
    public class Client : Socketize.Client
    {
        private NetConnection connection;

        public class Process : IProcessingService
        {
            internal Client client;
            private readonly Action onConnected;
            private readonly Action onDisconnected;
            private readonly ClientCommand[] commands;
            private readonly NetworkSyncSettings networkSyncSettings;

            public Process(Action onConnected, Action onDisconnected,  ClientCommand[] commands, NetworkSyncSettings networkSyncSettings)
            {
                this.onConnected = onConnected;
                this.onDisconnected = onDisconnected;
                this.commands = commands;
                this.networkSyncSettings = networkSyncSettings;
            }

            public void ProcessConnected(NetConnection connection)
            {
                client.connection = connection;
                onConnected();
            }

            public void ProcessDisconnected(NetConnection connection)
            {
                onDisconnected();
                client.connection = null;
                client.Stop();
            }

            public void ProcessMessage(string route, NetIncomingMessage message, bool failWhenNoHandlers = true)
            {
                if (message.LengthBytes == 1 && message.Data[0] == 0)
                {
                    GotPulse(message.SenderConnection);
                    return;
                }
                ushort type = BitConverter.ToUInt16(message.Data);
                if (type >= commands.Length) networkSyncSettings.networkSyncs[ushort.MaxValue - type].Accept(message.Data[2..]);
                else commands[type].Action(message.Data[2..]);
            }

            public void GotPulse(NetConnection connection)
            {
                var outMessage = client.NetPeer.CreateMessage();
                outMessage.Data = new byte[] { 0 };
                outMessage.LengthBytes = 1;
                connection.SendMessage(outMessage, NetDeliveryMethod.ReliableUnordered, 1);
            }
        }

        private void Send(byte[] data, NetConnection to)
        {
            var outMessage = NetPeer.CreateMessage();
            outMessage.Data = data;
            outMessage.LengthBytes = data.Length;
            switch (to.SendMessage(outMessage, NetDeliveryMethod.ReliableUnordered, 1))
            {
                case NetSendResult.FailedNotConnected:
                    connection = null;
                    Stop();
                    return;
                case NetSendResult.Sent: return;
                case NetSendResult.Queued: return;
            }
        }

        public void Send(ushort command, byte[] data)
        {
            byte[] message = new byte[data.Length + 2];
            Array.Copy(data, 0, message, 2, data.Length);
            Array.Copy(BitConverter.GetBytes(command), 0, message, 0, 2);
            Send(message, connection);
        }

        public Client(string address, int port, Process process, string name, bool consoleLog = true) : base(process, consoleLog ? new ConsoleLogger<Client>() : new IgnoreLogger<Client>(), new ClientOptions(address, port, name))
        {
            process.client = this;
        }
        
        public override void Start()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            base.Start();
        }
    }
}
