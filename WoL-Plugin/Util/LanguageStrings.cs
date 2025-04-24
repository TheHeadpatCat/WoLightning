using Dalamud.Game;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.WoL_Plugin.Util
{
    public class LanguageStrings // This could probably be handled in a static way.
    {

        Plugin Plugin { get; set; }
        public ClientLanguage Language { get; set; }

        public LanguageStrings(Plugin Plugin)
        {
            this.Plugin = Plugin;
            Language = (ClientLanguage)Plugin.GameConfig.System.GetUInt("Language");
        }

        public string FailCraftTrigger()
        {
            string output = "";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "Your synthesis fails!";
                    break;
                case ClientLanguage.French:
                    output = "Your synthesis fails!";
                    break;
                case ClientLanguage.German:
                    output = "Your synthesis fails!";
                    break;
                case ClientLanguage.Japanese:
                    output = "Your synthesis fails!";
                    break;
            }
            return output;
        }

        public string FishEscapedTrigger()
        {
            string output = "";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "The fish gets away...";
                    break;
                case ClientLanguage.French:
                    output = "The fish gets away...";
                    break;
                case ClientLanguage.German:
                    output = "The fish gets away...";
                    break;
                case ClientLanguage.Japanese:
                    output = "The fish gets away...";
                    break;
            }
            return output;
        }


    }
}
