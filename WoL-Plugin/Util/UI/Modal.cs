using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;

namespace WoLightning.WoL_Plugin.Util.UI
{
    public class Modal // todo: reconsider if i actually need this
    {
        public bool isOpen { get; set; } = false;
        public Modal() { }
    }
}
