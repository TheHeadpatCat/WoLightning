using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Clients.Pishock
{
    internal static class CommandPublish
    {

        public static string Generate(ShockOptions Options, Plugin Plugin, string UserId)
        {
            List<Command> commands = new();
            List<string> targets = new();

            int x = 0;
            foreach (var shocker in Options.ShockersPishock)
            {
                Plugin.Log(shocker);
                var cmd =  new Command(shocker, Options, UserId);
                string target;
                if (shocker.isPersonal) target = "c" + shocker.clientId + "-ops";
                else target = "c" + shocker.clientId + "-sops-" + shocker.shareCode;
                Plugin.Log(cmd);
                commands.Add(cmd);
                targets.Add(target);
            }

            Plugin.Log("Commands:");
            foreach (var cmd in commands)
            {
                Plugin.Log(cmd);
            }

            return JsonSerializer.Serialize(new { Operation = "PUBLISH", PublishCommands = commands.ToArray() });
        }
    }


    [Serializable]
    internal class Command
    {
        // Using actual var names from Pishock API
        public string Target { get; set; } //Client ID
        public object Body { get; set; } // All targeted Shockers
        public Command(ShockerPishock Shocker, ShockOptions Options, string UserId)
        {
            string type;
            if (Shocker.isPersonal)
            {
                this.Target = "c" + Shocker.clientId + "-ops";
            }
            else
            {
                this.Target = "c" + Shocker.clientId + "-sops-" + Shocker.shareCode;
            }

            this.Body = new
            {
                id = Shocker.shockerId,
                m = Options.getOpModePishock(),
                i = Options.Intensity,
                d = Options.Duration,
                r = false,
                l = new
                {
                        u = int.Parse(UserId),
                        ty = "api",
                        o = "WoLightning"
                    }
                };
        }

        public override string ToString()
        {
            return Target + " " + Body.ToString();
        }
    }

}
