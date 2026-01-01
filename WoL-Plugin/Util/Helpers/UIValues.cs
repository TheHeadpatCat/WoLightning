using System.Collections.Generic;
using System.Numerics;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Util.Helpers
{
    public static class UIValues
    {
        public static Vector4 ColorNameEnabled = new Vector4(0.5f, 1, 0.3f, 0.9f);
        public static Vector4 ColorNameBlocked = new Vector4(1.0f, 0f, 0f, 0.9f);
        public static Vector4 ColorNameDisabled = new Vector4(1, 1, 1, 0.9f);
        public static Vector4 ColorDescription = new Vector4(0.7f, 0.7f, 0.7f, 0.8f);

        public static List<int> DurationArray = [100, 300, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        public static List<CooldownModifier> ModifierArray = [CooldownModifier.Miliseconds, CooldownModifier.Seconds, CooldownModifier.Minutes, CooldownModifier.Hours];
    }
}
