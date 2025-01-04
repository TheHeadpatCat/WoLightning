using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WoLightning.Types;

namespace WoLightning.Webserver
{
    internal class Packet
    {
        public String Type { get; set; } = string.Empty;
        public int ExecutionCode = -1;

        public String Hash { get; set; } = "";
        public String? DevKey { get; set; } = null;

        public Player? Sender { get; set; } = null;
        public Player? Target { get; set; } = null;

        public String[]? OperationData { get; set; } = null;

        [NonSerialized] Plugin Plugin;
        public Packet(Plugin Plugin, String Type) { Initialize(Plugin, Type, null, null); }
        public Packet(Plugin Plugin, String Type, String[] OperationData) { Initialize(Plugin, Type, null, OperationData); }
        public Packet(Plugin Plugin, String Type, Player Target) { Initialize(Plugin, Type, Target, null); }
        [JsonConstructorAttribute]
        public Packet(String Type, Player Target, String[] OperationData) { Initialize(null, Type, Target, OperationData); }


        private void Initialize(Plugin? Plugin, String Type, Player? Target, String[]? OperationData)
        {
            this.Plugin = Plugin;
            this.Type = Type;
            if(Plugin != null) this.Sender = Plugin.LocalPlayer;
            this.Target = Target;
            this.OperationData = OperationData;
        }

        public override string ToString()
        {
            return "[Packet] Type: " + Type + " Hash: " + Hash + " Sender: " + Sender + " Target: " + Target;
        }
    }
}
