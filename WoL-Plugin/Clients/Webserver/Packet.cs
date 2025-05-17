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

        public Packet(string Type) { Initialize(Type, null, null); }
        public Packet(string Type, string[] OperationData) { Initialize(Type, null, OperationData); }
        public Packet(string Type, Player Target) { Initialize(Type, Target, null); }
        [JsonConstructor]
        public Packet(string Type, Player Target, string[] OperationData) { Initialize(Type, Target, OperationData); }


        private void Initialize(string Type, Player? Target, string[]? OperationData)
        {
            this.Type = Type;
            //Sender = Plugin.LocalPlayer;
            this.Target = Target;
            this.OperationData = OperationData;
        }

        public override string ToString()
        {
            return "[Packet] Type: " + Type + " Hash: " + Hash + " Sender: " + Sender + " Target: " + Target;
        }
    }
}
