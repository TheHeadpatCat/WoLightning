namespace WoLightning.WoL_Plugin.Clients.OpenShock
{
    public class ShockerOpenShock : ShockerBase
    {
        public string id;
        public bool isPaused;
        internal HubOpenShock ParentHub;
        public ShockerOpenShock(string name, string id, bool isPaused) : base(ShockerType.OpenShock, name)
        {
            this.id = id;
            this.isPaused = isPaused;
        }
        internal ShockerOpenShock(HubOpenShock Parent, string name, string id, bool isPaused) : base(ShockerType.OpenShock, name)
        {
            this.ParentHub = Parent;
            this.id = id;
            this.isPaused = isPaused;
        }
        override public string getInternalId()
        {
            return Type + "#" + name + "#" + id;
        }

        public override string ToString()
        {
            return "[ShockerOpenShock] Name: " + name + " Id: " + id;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is ShockerOpenShock)
            {
                ShockerOpenShock other = (ShockerOpenShock)obj;
                return id == other.id && name == other.name;
            }
            return false;
        }
    }
}
