using System;
using System.Collections.Generic;
using System.Text;

namespace WoLightning.WoL_Plugin.Util.Types
{
    [Serializable]
    public readonly struct Version
    {

        public short Manifest { get; } = 0;
        public short Major { get; init; }
        public short Minor { get; init; }
        public short Bugfix { get; init; }
        public char Suffix { get; init; } = ' ';


        public Version(short major, short minor, short bugfix)
        {
            Major = major;
            Minor = minor;
            Bugfix = bugfix;
        }

        public string GetVersionString()
        {
            return $"{Manifest}.{Major}.{Minor}.{Bugfix}{Suffix}";
        }

        public NeedUpdateState NeedsUpdate(Version against)
        {
            if (Major > against.Major) return NeedUpdateState.Remake;
            if (Minor > against.Minor) return NeedUpdateState.Inject;
            if (Bugfix > against.Bugfix) return NeedUpdateState.Inject;

            if (Major <= against.Major
                && Minor <= against.Minor
                && Bugfix <= against.Bugfix) 
                return NeedUpdateState.Downgrade;

            return NeedUpdateState.Keep;
        }

        public enum NeedUpdateState
        {
            Remake = 2,
            Inject = 1,
            Keep = 0,
            Downgrade = -1
        }

    }

}
