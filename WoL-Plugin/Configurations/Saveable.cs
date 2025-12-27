using System;
using System.Collections.Generic;
using System.Text;
using WoLightning.WoL_Plugin.Util.Types;
using Version = WoLightning.WoL_Plugin.Util.Types.Version;

namespace WoLightning.WoL_Plugin.Configurations
{
    [Serializable]
    public abstract class Saveable
    {
        abstract protected Version currentVersion { get; init; }
        public Version savedVersion { get; set; }


    }
}
