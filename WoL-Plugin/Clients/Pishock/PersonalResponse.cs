using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WoLightning.WoL_Plugin.Clients.Pishock
{
    [Serializable]
    internal class PersonalResponse
    {
        public int clientId {  get; set; }
        public string name { get; set; }
        public int userId { get; set; }
        public string username { get; set; }
        public ResponseShocker[] shockers { get; set; }

        [JsonConstructor]
        public PersonalResponse(int clientId, string name, int userId, string username, ResponseShocker[] shockers)
        {
            this.clientId = clientId;
            this.name = name;
            this.userId = userId;
            this.username = username;
            this.shockers = shockers;
        }

        public override string ToString()
        {
            return "[PersonalResponse] ClientID: " + clientId + " Name: " + name + " Shockers: " + shockers.Count();
        }

    }

    [Serializable]
    internal class SharedResponse
    {
        public int shareId { get; set; }
        public int clientId { get; set; }
        public int shockerId { get; set; }
        public string shockerName { get; set; }
        public bool isPaused { get; set; }
        public int maxIntensity { get; set; }
        public bool canContinuous { get; set; }
        public bool canShock {  get; set; }
        public bool canVibrate { get; set; }
        public bool canBeep { get; set; }
        public bool canLog { get; set; }
        public string shareCode { get; set; }

        [JsonConstructor]
        public SharedResponse(int shareId, int clientId, int shockerId, string shockerName, bool isPaused, int maxIntensity, bool canContinuous, bool canShock, bool canVibrate, bool canBeep, bool canLog, string shareCode)
        {
            this.shareId = shareId;
            this.clientId = clientId;
            this.shockerId = shockerId;
            this.shockerName = shockerName;
            this.isPaused = isPaused;
            this.maxIntensity = maxIntensity;
            this.canContinuous = canContinuous;
            this.canShock = canShock;
            this.canVibrate = canVibrate;
            this.canBeep = canBeep;
            this.canLog = canLog;
            this.shareCode = shareCode;
        }

        public override string ToString() { return "[SharedResponse] ShockerName: " + shockerName + " shockerId: " + shockerId; }
    }

    [Serializable]
    internal class ResponseShocker
    {
        public string name { get; set; }
        public int shockerId { get; set; }
        public bool isPaused { get; set; }

        [JsonConstructor]
        public ResponseShocker(string name, int shockerId, bool isPaused)
        {
            this.name = name;
            this.shockerId = shockerId;
            this.isPaused = isPaused;
        } 
        public override string ToString() { return "[ResponseShocker] Name: " + name + " shockerId: " + shockerId; }
    }


}
