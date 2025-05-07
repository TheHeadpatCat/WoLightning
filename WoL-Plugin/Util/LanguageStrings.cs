using Dalamud.Game;

namespace WoLightning.WoL_Plugin.Util
{
    public static class LanguageStrings // This could probably be handled in a static way.
    {

        public static readonly string HQSymbol = "";

        public static string FailCraftTrigger()
        {
            ClientLanguage Language = (ClientLanguage)Service.GameConfig.System.GetUInt("Language");

            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "Your synthesis fails!";
                    break;
                case ClientLanguage.French:
                    output = "La synthèse échoue...";
                    break;
                case ClientLanguage.German:
                    output = "Deine Synthese ist fehlgeschlagen!";
                    break;
                case ClientLanguage.Japanese:
                    output = "は製作に失敗した……";
                    break;
            }
            return output;
        }

        public static string FailCraftHQTrigger() // Todo: implement
        {
            ClientLanguage Language = (ClientLanguage)Service.GameConfig.System.GetUInt("Language");
           
            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "You synthesize a";
                    break;
                case ClientLanguage.French:
                    output = "Vous fabriquez un";
                    break;
                case ClientLanguage.German:
                    output = "Du hast erfolgreich ein";
                    break;
                case ClientLanguage.Japanese:
                    output = "を完成させた！";
                    break;
            }
            return output;
        }

        public static string FishEscapedTrigger()
        {
            ClientLanguage Language = (ClientLanguage)Service.GameConfig.System.GetUInt("Language");

            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "The fish gets away...";
                    break;
                case ClientLanguage.French:
                    output = "Le poisson a réussi à se défaire de l'hameçon...";
                    break;
                case ClientLanguage.German:
                    output = "Der Fisch konnte sich vom Haken reißen.";
                    break;
                case ClientLanguage.Japanese:
                    output = "釣り針にかかった魚に逃げられてしまった……";
                    break;
            }
            return output;
        }

        public static string DeathrollTrigger()
        {
            ClientLanguage Language = (ClientLanguage)Service.GameConfig.System.GetUInt("Language");

            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "Random! You roll a ";
                    break;
                case ClientLanguage.French:
                    output = "Vous jetez les dés et obtenez ";
                    break;
                case ClientLanguage.German:
                    output = "Du würfelst eine ";
                    break;
                case ClientLanguage.Japanese:
                    output = "ダイス！ ---"; // Doesnt work since it always includes the name of the player
                    break;
            }
            return output;
        }

    }
}
