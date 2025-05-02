using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Clients.Pishock;

namespace WoLightning.WoL_Plugin.Clients.OpenShock
{
    internal static class CommandPublish
    {
        public static string Generate(List<ShockerOpenShock> shockers, ShockOptions Options)
        {

            List<Command> shocks = new();

            foreach (ShockerOpenShock shocker in shockers)
            {
                shocks.Add(new Command(shocker.id, Options.getOpModeOpenShock(), Options.Intensity, Options.getDurationOpenShock(), true));
            }

            return JsonSerializer.Serialize(new { shocks = shocks.ToArray() });
        }


    }

    [Serializable]
    internal class Command
    {
        public string id {  get; set; }
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
