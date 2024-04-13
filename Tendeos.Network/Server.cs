using Lidgren.Network;
using Socketize;

namespace Tendeos.Network
{
    public class Server : Socketize.Server
    {
        public class Process : IProcessingService
        {
            internal Server server;
            private readonly ServerCommand[] commands;
            private readonly NetworkSyncSettings networkSyncSettings;
            internal List<NetConnection> connections;
            internal List<int> pulseMessages;

            public Process(ServerCommand[] commands, NetworkSyncSettings networkSyncSettings)
            {
                this.commands = commands;
                this.networkSyncSettings = networkSyncSettings;
                pulseMessages = new List<int>();
                connections = new List<NetConnection>();
            }

            public void ProcessConnected(NetConnection connection)
            {
                pulseMessages.Add(0);
                connections.Add(connection);
                Pulse(connection, pulseMessages.Count - 1);
                Sync(connection);
            }

            public void ProcessDisconnected(NetConnection connection)
            {
                pulseMessages.RemoveAt(connections.IndexOf(connection));
                connections.Remove(connection);
            }

            public async void Sync(NetConnection connection)
            {
                ushort type;
                bool hasSelf;
                int j, l;
                NetConnection other;
                byte[][] messages;
                while (true)
                {
                    hasSelf = false;
                    l = server.NetPeer.Connections.Count;
                    for (j = 0; j < l; j++)
                    {
                        if (j >= server.NetPeer.Connections.Count) break;
                        other = server.NetPeer.Connections[j];
                        if (other.RemoteEndPoint.Address.Equals(connection.RemoteEndPoint.Address) &&
                            other.RemoteEndPoint.Port == connection.RemoteEndPoint.Port)
                            hasSelf = true;
                    }
                    if (!hasSelf) return;

                    for (j = 0; j < networkSyncSettings.networkSyncs.Length; j++)
                    {
                        type = (ushort)(ushort.MaxValue - j);
                        messages = networkSyncSettings.networkSyncs[j].Send();
                        foreach (byte[] message in messages)
                            server.Send(type, message, connection, SendType.Inaccurate);
                    }
                    await Task.Delay(100);
                }
            }

            public async void Pulse(NetConnection connection, int i)
            {
                bool hasSelf;
                int j, l;
                NetConnection other;
                while (true)
                {
                    hasSelf = false;
                    l = server.NetPeer.Connections.Count;
                    for (j = 0; j < l; j++)
                    {
                        if (j >= server.NetPeer.Connections.Count) break;
                        other = server.NetPeer.Connections[j];
                        if (other.RemoteEndPoint.Address.Equals(connection.RemoteEndPoint.Address) &&
                            other.RemoteEndPoint.Port == connection.RemoteEndPoint.Port)
                        {
                            hasSelf = true;
                            i = j;
                        }
                    }
                    if (!hasSelf) return;

                    if (pulseMessages[i] > 0)
                        connection.Disconnect("Connection timeout expired.");

                    var outMessage = server.NetPeer.CreateMessage();
                    outMessage.Data = new byte[] { 0 };
                    outMessage.LengthBytes = 1;

                    if (connection.SendMessage(outMessage, NetDeliveryMethod.ReliableUnordered, 1) == NetSendResult.FailedNotConnected)
                        connection.Disconnect("Not valid connection.");

                    pulseMessages[i]++;
                    await Task.Delay(1000);
                }
            }

            public void GotPulse(NetConnection connection, int i)
            {
                pulseMessages[i]--;
            }

            public void ProcessMessage(string route, NetIncomingMessage message, bool failWhenNoHandlers = true)
            {
                if (message.LengthBytes == 1 && message.Data[0] == 0)
                {
                    GotPulse(message.SenderConnection, server.NetPeer.Connections.IndexOf(message.SenderConnection));
                    return;
                }
                ushort type = BitConverter.ToUInt16(message.Data);
                byte[] back = commands[type].Action(message.SenderConnection, message.Data[2..message.LengthBytes]);
                byte[] data = new byte[back.Length + 2];
                Array.Copy(back, 0, data, 2, back.Length);
                Array.Copy(BitConverter.GetBytes(type), 0, data, 0, 2);

                if (commands[type].Global) server.Send(data, SendType.Accurate);
                else server.Send(data, message.SenderConnection, SendType.Accurate);
            }
        }

        private void Send(byte[] data, NetConnection to, SendType type)
        {
            var outMessage = NetPeer.CreateMessage();
            outMessage.Data = data;
            outMessage.LengthBytes = data.Length;
            switch (to.SendMessage(outMessage, type == SendType.Accurate ? NetDeliveryMethod.ReliableUnordered : NetDeliveryMethod.Unreliable, 1))
            {
                case NetSendResult.FailedNotConnected:
                    if (NetPeer.Connections.Contains(to))
                    {
                        to.Disconnect("Not valid connection.");
                    }
                    break;
            }
        }

        private void Send(byte[] data, SendType type)
        {
            foreach (NetConnection connection in NetPeer.Connections)
                Send(data, connection, type);
        }

        public void Send(ushort command, byte[] data, NetConnection to, SendType type)
        {
            byte[] message = new byte[data.Length + 2];
            Array.Copy(data, 0, message, 2, data.Length);
            Array.Copy(BitConverter.GetBytes(command), 0, message, 0, 2);
            Send(message, to, type);
        }

        public void Send(ushort command, byte[] data, SendType type)
        {
            byte[] message = new byte[data.Length + 2];
            Array.Copy(data, 0, message, 2, data.Length);
            Array.Copy(BitConverter.GetBytes(command), 0, message, 0, 2);
            Send(message, type);
        }

        public Server(int port, Process process, bool consoleLog = true) : base(process, consoleLog ? new ConsoleLogger<Server>() : new IgnoreLogger<Server>(), new ServerOptions(port, "Tendeos"))
        {
            process.server = this;
        }
        public override void Start()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            base.Start();
        }
    }

    public enum SendType { Accurate, Inaccurate }
}
