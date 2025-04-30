using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WoLightning.WoL_Plugin.Game.Rules;

namespace WoLightning.WoL_Plugin.Clients
{

    public enum ShockerStatus
    {
        Unchecked = 0,

        Online = 1,
        Paused = 2,
        Offline = 3,

        NotAuthorized = 100,
        DoesntExist = 101,
    }

    public enum ShockerType
    {
        Unknown = 0,
        Pishock = 1,
        OpenShock = 2,
    }

    public abstract class ShockerBase
    {

        public ShockerType Type { get; set; }
        public string name { get; set; }

        public ShockerStatus Status = ShockerStatus.Unchecked;

        public Action<RuleBase, ShockerBase> Triggered;
        public Action<int[],    ShockerBase> TriggeredManually;

        public ShockerBase(ShockerType type, string name)
        {
            Type = type;
            this.name = name;
        }

        virtual public string getInternalId()
        {
            return Type + "#" + name;
        }


    }
}
