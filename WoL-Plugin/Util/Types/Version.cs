using System;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Util.Types
{
    [Serializable]
    public readonly struct Version
    {

        public short Manifest { get; } = 0;
        public short Major { get; init; }
        public short Minor { get; init; }
        public short Bugfix { get; init; }
        public char Suffix { get; init; }


        [JsonConstructor]
        public Version(short major, short minor, short bugfix, char suffix = ' ')
        {
            Major = major;
            Minor = minor;
            Bugfix = bugfix;
            Suffix = suffix;
        }

        public Version(string versionString)
        {
            string[] array = versionString.Split('.');
            if (array.Length != 4)
                throw new FormatException("Versioning String is not in a correct format.");

            short major = short.Parse(array[1]);
            short minor = short.Parse(array[2]);
            short bugfix = short.Parse(array[3][0] + "");
            char suffix = array[3][1];

            Major = major;
            Minor = minor;
            Bugfix = bugfix;
            Suffix = suffix;
        }

        public override string ToString()
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

        public override bool Equals(object? other)
        {
            if(other == null) return false;
            if(other.GetType() != this.GetType()) return false;
            return ((Version)other) == this;
        }

        public static bool operator >(Version a, Version b)
        {
            if (a.Manifest > b.Manifest
                || a.Major > b.Major
                || a.Minor > b.Minor)
                return true;
            return false;
        }

        public static bool operator <(Version a, Version b)
        {
            if (a.Manifest < b.Manifest
                || a.Major < b.Major
                || a.Minor < b.Minor)
                return true;
            return false;
        }

        public static bool operator ==(Version a, Version b)
        {
            if (a.Manifest == b.Manifest
                && a.Major == b.Major
                && a.Minor == b.Minor)
                return true;
            return false;
        }

        public static bool operator !=(Version a, Version b)
        {
            if (a.Manifest != b.Manifest
                && a.Major != b.Major
                && a.Minor != b.Minor)
                return true;
            return false;
        }

        public static bool operator >=(Version a, Version b)
        {
            return a > b || a == b;
        }

        public static bool operator <=(Version a, Version b)
        {
            return a < b || a == b;
        }

        
    }

}
