using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.WoL_Plugin.Util
{
    public static class StringExtensions
    {

        public static int CountSpaces(this string value)
        {
            int result = 0;
            foreach (char c in value)
            {
                if (c == ' ') result++;
            }
            return result;
        }

    }
}
