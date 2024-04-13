using System.Diagnostics;

namespace Tendeos.Network
{
    public class SyncTimer : INetworkSyncObject
    {
        private Stopwatch stopwatch;
        private float time;
        public float Time
        {
            get => stopwatch?.ElapsedMilliseconds / 1000f ?? time;
            set => time = value;
        }

        public SyncTimer(bool server)
        {
            if (server)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }
        }

        public byte[] Send()
        {
            Log.Don(Time);
            return BitConverter.GetBytes(Time);
        }

        public void Accept(byte[] data)
        {
            Time = BitConverter.ToSingle(data);
            Log.Don(Time);
        }
    }
}
