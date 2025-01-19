using System;
using WoLightning.WoL_Plugin.Game.Rules;

namespace WoLightning.Util.Types
{
    public enum ShockerStatus
    {
        Unchecked = 0,

        Online = 1,
        Paused = 2,
        Offline = 3,

        NotAuthorized = 100,
        DoesntExist = 101,
        AlreadyUsed = 102,

        InvalidUser = 103,
    }

    public enum ShockerType
    {
        Unknown = 0,
        Pishock = 1,
        OpenShock = 2,
    }

    [Serializable]
    public class Shocker
    {
        public ShockerType Type { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        [NonSerialized]
        public ShockerStatus Status = ShockerStatus.Unchecked;

        [NonSerialized]
        public Action<BaseRule, Shocker> Triggered;
        [NonSerialized]
        public Action<int, int, Shocker> TriggeredManually;

        public Shocker(ShockerType type, string name, string code)
        {
            Type = type;
            Name = name;
            Code = code;
        }

    }
}
