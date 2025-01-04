using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.Webserver.Operations
{
    public class BaseOperation
    {
        public Plugin Plugin { get; init; }
        public static String Type = "BaseOperation";
        public int ExecutionCode = -1;
        public String[]? OperationData { get; init; }

        public bool isValid() {
            return true;
        }
        public bool Execute() {
            if (!isValid()) return false;
            throw new NotImplementedException();
        }

    }
}
