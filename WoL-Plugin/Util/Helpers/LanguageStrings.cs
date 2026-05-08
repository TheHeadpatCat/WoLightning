using Dalamud.Game;

namespace WoLightning.WoL_Plugin.Util
{
    public static class LanguageStrings // This could probably be handled in a static way.
    {

        public static readonly string HQSymbol = "";
        public static readonly ClientLanguage Language = (ClientLanguage)Service.GameConfig.System.GetUInt("Language");

        public static string FailCraftTrigger()
        {

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

        public static string MateriaMeldFailedTrigger()
        {

            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "You are unable to attach a ";
                    break;
                case ClientLanguage.French:
                    output = "Le sertissage du ";
                    break;
                case ClientLanguage.German:
                    output = "Einsetzen von Materia in den ";
                    break;
                case ClientLanguage.Japanese:
                    output = "へのマテリア装着に失敗した ---"; // Doesnt work since it always includes the name of the player
                    break;
            }
            return output;
        }

        public static string FateLevelNotSynchedTrigger()
        {

            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "Unable to attack FATE Target. Your level is too high.";
                    break;
                case ClientLanguage.French:
                    output = "Impossible d'attaquer. Votre niveau est trop élevé pour cet ALÉA.";
                    break;
                case ClientLanguage.German:
                    output = "Du kannst nicht angreifen, weil deine Stufe zu hoch ist.";
                    break;
                case ClientLanguage.Japanese:
                    output = "uh, no";
                    break;
            }
            return output;
        }

        public static string SellMarketItemTrigger()
        {
            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "you put up for sale";
                    break;
                case ClientLanguage.French:
                    output = "Un servant a vendu une";
                    break;
                case ClientLanguage.German:
                    output = "verkauft und"; // kinda scuffed, but german has a stupid sentence
                    break;
                case ClientLanguage.Japanese:
                    output = "uh, no";
                    break;
            }
            return output;
        }

        public static string TreasureExpelledTrap()
        {
            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "A trap is triggered! You are expelled from the area!";
                    break;
                case ClientLanguage.French:
                    output = "Un piège se déclenche, vous expulsant de l'endroit !";
                    break;
                case ClientLanguage.German:
                    output = "Eine Falle! Du wirst aus der Schatzkammer teleportiert ...";
                    break;
                case ClientLanguage.Japanese:
                    output = "罠が作動し、宝物庫から強制転送された……";
                    break;
            }
            return output;
        }

        public static string TreasureExpelledDevour()
        {
            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "and wake far from the sanctum.";
                    break;
                case ClientLanguage.French:
                    output = "vous aspire et vous expulse de l'endroit...";
                    break;
                case ClientLanguage.German:
                    output = "saugt dich ein und spuckt dich aus der Altarhalle ...";
                    break;
                case ClientLanguage.Japanese:
                    output = "に吸い込まれ、宝物庫から強制転送された……";
                    break;
            }
            return output;
        }

        public static string TreasureExpelledWake()
        {
            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "Your party wakes far from";
                    break;
                case ClientLanguage.French:
                    output = "Votre équipe a été terrassée. Vous êtes expulsé";
                    break;
                case ClientLanguage.German:
                    output = "Du bist der Glücksaltäre nicht länger würdig ...";
                    break;
                case ClientLanguage.Japanese:
                    output = "全滅したことで、宝物庫から強制転送された……";
                    break;
            }
            return output;
        }

        public static string TreasureExpelledWakeExtra()
        {
            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "---------------";
                    break;
                case ClientLanguage.French:
                    output = "---------------";
                    break;
                case ClientLanguage.German:
                    output = "Ihr erwacht weit, weit weg vom Tresor Oneiron...";
                    break;
                case ClientLanguage.Japanese:
                    output = "全滅したことで、ボルト・オネイロンから強制転送された……";
                    break;
            }
            return output;
        }
        

        #region Duty Types

        public static string DutyTypeBallad()
        {
            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "the Minstrel's Ballad: ";
                    break;
                case ClientLanguage.French:
                    output = "(extrême)";
                    break;
                case ClientLanguage.German:
                    output = "un, no";
                    break;
                case ClientLanguage.Japanese:
                    output = "uh, no";
                    break;
            }
            return output;
        }
        public static string DutyTypeExtreme()
        {
            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "(Extreme)";
                    break;
                case ClientLanguage.French:
                    output = "(extrême)";
                    break;
                case ClientLanguage.German:
                    output = "uh, no";
                    break;
                case ClientLanguage.Japanese:
                    output = "uh, no";
                    break;
            }
            return output;
        }

        public static string DutyTypeSavage()
        {

            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "(Savage)";
                    break;
                case ClientLanguage.French:
                    output = "(sadique)";
                    break;
                case ClientLanguage.German:
                    output = "(episch)";
                    break;
                case ClientLanguage.Japanese:
                    output = "uh, no";
                    break;
            }
            return output;
        }

        public static string DutyTypeUltimate()
        {

            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "(Ultimate)";
                    break;
                case ClientLanguage.French:
                    output = "(fatal)";
                    break;
                case ClientLanguage.German:
                    output = "(fatal)";
                    break;
                case ClientLanguage.Japanese:
                    output = "uh, no";
                    break;
            }
            return output;
        }

        public static string DutyTypeUnreal()
        {

            string output = "Unknown";
            switch (Language)
            {
                case ClientLanguage.English:
                    output = "(Unreal)";
                    break;
                case ClientLanguage.French:
                    output = "(irréel)";
                    break;
                case ClientLanguage.German:
                    output = "Traumprüfung";
                    break;
                case ClientLanguage.Japanese:
                    output = "uh, no";
                    break;
            }
            return output;
        }



        #endregion

    }
}