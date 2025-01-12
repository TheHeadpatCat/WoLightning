using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules
{

    public enum RuleCategory
    {
        Unknown = 0,
        General = 1,
        Social = 2,
        PVE = 3,
        PVP = 4,
        misc = 5,
        master = 6,
    }

    [Serializable]
    public class BaseRule : IDisposable
    {
        [NonSerialized] protected Plugin Plugin;
        public ShockOptions ShockOptions { get; init; }

        public string Name { get; } = "BaseRule";
        public string Description { get; } = "Sample Description";
        public RuleCategory Category { get; } = RuleCategory.Unknown;

        public bool IsEnabled { get; set; } = false;
        public bool IsLocked { get; set; } = false;

        public bool isRunning { get; } = false;

        [NonSerialized] public Action<BaseRule> Triggered;
        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Trigger(string Text)
        {
            Plugin.sendNotif(Text);
        }

        public void Dispose()
        {
            Stop();
        }

    }
}
