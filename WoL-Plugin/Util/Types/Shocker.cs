using System;

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
        public string Name { get; set; }
        public string Code { get; set; }
        public ShockerType Type { get; set; }

        [NonSerialized]
        public ShockerStatus Status = ShockerStatus.Unchecked;

        public Shocker(string name, string code, ShockerType type)
        {
            Name = name;
            Code = code;
            Type = type;
        }



    }
}
