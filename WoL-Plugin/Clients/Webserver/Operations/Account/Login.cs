namespace WoLightning.Clients.Webserver.Operations.Account
{

    public class Login : BaseOperation
    {
        new public static string Type = "Login";
        public Login(Plugin plugin, int ExecutionCode)
        {
            Plugin = plugin;
            this.ExecutionCode = ExecutionCode;
        }

        new public bool Execute()
        {
            if (!isValid()) return false;

            if (ExecutionCode != 0)
            {
                Plugin.ClientWebserver.Status = ConnectionStatusWebserver.InvalidKey;
                return false;
            }

            Plugin.ClientWebserver.Status = ConnectionStatusWebserver.Connected;
            return true;
        }
    }
}
