using CSS.Helpers.Binary;

namespace CSS.Packets.Messages.Client
{
    // Packet 10160 - Telemetry / Performance metrics sent by Android client every PERFORMANCE_EVENTS_INTERVAL_SECONDS_ANDROID (60s)
    internal class PerformanceMetricMessage : Message
    {
        public PerformanceMetricMessage(Device device, Reader reader) : base(device, reader)
        {
        }

        internal override void Decode()
        {
            // Payload is decrypted by Message.Decrypt(); no extra decode required.
        }

        internal override void Process()
        {
            // Telemetry discarded cleanly without desynchronizing cryptography nonces.
        }
    }
}
