using System;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;

namespace WoLightning.Clients.Webserver
{
    internal class Packet
    {
        public string Type { get; set; } = string.Empty;
        public int ExecutionCode = -1;

        public string Hash { get; set; } = "";
        public string? DevKey { get; set; } = null;

        public Player? Sender { get; set; } = null;
        public Player? Target { get; set; } = null;

        public string[]? OperationData { get; set; } = null;

        [NonSerialized] Plugin Plugin;
        public Packet(Plugin Plugin, string Type) { Initialize(Plugin, Type, null, null); }
        public Packet(Plugin Plugin, string Type, string[] OperationData) { Initialize(Plugin, Type, null, OperationData); }
        public Packet(Plugin Plugin, string Type, Player Target) { Initialize(Plugin, Type, Target, null); }
        [JsonConstructor]
        public Packet(string Type, Player Target, string[] OperationData) { Initialize(null, Type, Target, OperationData); }


        private void Initialize(Plugin? Plugin, string Type, Player? Target, string[]? OperationData)
        {
            this.Plugin = Plugin;
            this.Type = Type;
            if (Plugin != null) Sender = Plugin.LocalPlayer;
            this.Target = Target;
            this.OperationData = OperationData;
        }

        public override string ToString()
        {
            return "[Packet] Type: " + Type + " Hash: " + Hash + " Sender: " + Sender + " Target: " + Target;
        }
    }
}
