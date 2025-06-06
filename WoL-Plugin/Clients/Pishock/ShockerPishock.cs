﻿namespace WoLightning.WoL_Plugin.Clients.Pishock
{
    public class ShockerPishock : ShockerBase
    {
        public int clientId { get; set; }
        public int shockerId { get; set; }
        public bool isPaused { get; set; } = false;
        public bool isPersonal { get; set; } = true;
        public int shareId { get; set; }
        public string shareCode { get; set; }
        public string username { get; set; } = "";
        public ShockerPishock(string name, int clientId, int shockerId) : base(ShockerType.Pishock, name)
        {
            this.clientId = clientId;
            this.shockerId = shockerId;
        }

        override public string getInternalId()
        {
            return Type + "#" + name + "#" + shockerId;
        }

        public override string ToString()
        {
            return "[ShockerPishock] Name: " + name + " ShockerId: " + shockerId + " ClientId: " + clientId;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is ShockerPishock)
            {
                ShockerPishock other = (ShockerPishock)obj;
                return clientId == other.clientId && shockerId == other.shockerId && shareId == other.shareId && shareCode == other.shareCode;
            }
            return false;
        }

    }
}
