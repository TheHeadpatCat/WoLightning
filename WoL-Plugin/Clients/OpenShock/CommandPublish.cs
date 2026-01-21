
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util.Types.Devices.OpenShock;

namespace WoLightning.WoL_Plugin.Clients.OpenShock
{
    internal static class CommandPublish
    {
        public static string Generate(ShockerOpenShock Device, OptionsOpenShock Options)
        {

            List<Command> shocks = new();

            shocks.Add(new Command(Device.ShockerId, Options.OperationString(), Options.Intensity, Options.Duration, true));

            return JsonSerializer.Serialize(new { shocks = shocks.ToArray() });
        }


    }

    [Serializable]
    internal class Command
    {
        public string id { get; set; }
        public string type { get; set; }
        public int intensity { get; set; }
        public int duration { get; set; }
        public bool exclusive { get; set; }

        [JsonConstructor]
        public Command(string id, string type, int intensity, int duration, bool exclusive)
        {
            this.id = id;
            this.type = type;
            this.intensity = intensity;
            this.duration = duration;
            this.exclusive = exclusive;
        }
    }
}
