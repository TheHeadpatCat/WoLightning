using System;
using System.Collections.Generic;
using System.Linq;
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
            Command[] commands = new Command[Options.ShockersPishock.Count];

            int x = 0;
            foreach (var shocker in Options.ShockersPishock)
            {
                var cmd =  new Command(shocker, Options, UserId);
                Plugin.Log(cmd);
                commands[x] = cmd;
            }
            
            return JsonSerializer.Serialize(new { Operation = "PUBLISH", PublishCommands = commands });
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
                type = "api";
            }
            else
            {
                this.Target = "c" + Shocker.clientId + "-sops-" + Shocker.shareCode;
                type = "sc";
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
