using System;

namespace WoLightning.Clients.Webserver.Operations
{
    public class BaseOperation
    {
        public Plugin Plugin { get; init; }
        public static string Type = "BaseOperation";
        public int ExecutionCode = -1;
        public string[]? OperationData { get; init; }

        public bool isValid()
        {
            return true;
        }
        public bool Execute()
        {
            if (!isValid()) return false;
            throw new NotImplementedException();
        }

    }
}
