using System.Collections.Generic;

namespace WoLightning.Util.Types
{
    public class UpdateEntry
    {
        public int Version { get; set; }
        public bool breaksConfig { get; set; }
        public bool breaksAuthentification { get; set; }
        public List<string> Changes { get; set; }

        public UpdateEntry()
        {

        }

    }
}
