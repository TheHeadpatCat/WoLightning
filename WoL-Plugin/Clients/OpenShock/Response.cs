using System;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Clients.OpenShock
{
    [Serializable]
    internal class ResponseAccount
    {
        public string message { get; set; }
        public ResponseAccountData data { get; set; }

        [JsonConstructor]
        public ResponseAccount(string message, ResponseAccountData data)
        {
            this.message = message;
            this.data = data;
        }
        public override string ToString()
        {
            return "Message: " + message + " Data: " + data;
        }
    }

    [Serializable]
    internal class ResponseAccountData
    {

        public string id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string image { get; set; }
        public string[] roles { get; set; }
        public string rank { get; set; }

        [JsonConstructor]
        public ResponseAccountData(string id, string name, string email, string image, string[] roles, string rank)
        {
            this.id = id;
            this.name = name;
            this.email = email;
            this.image = image;
            this.roles = roles;
            this.rank = rank;
        }
        public override string ToString()
        {
            return "ID: " + id + " Name: " + name + " Rank: " + rank;
        }
    }

    [Serializable]
    internal class ResponseDevices
    {
        public string message { get; set; }
        public ResponseDeviceData[] data { get; set; }

        [JsonConstructor]
        public ResponseDevices(string message, ResponseDeviceData[] data)
        {
            this.message = message;
            this.data = data;
        }

        public override string ToString()
        {
            string output = "Message: " + message + " Devices:\n";
            foreach (var device in data)
            {
                output += " -> " + device + "\n";
            }
            return output;
        }
    }

    [Serializable]
    internal class ResponseDeviceData
    {
        public string id { get; set; }
        public string name { get; set; }
        public string createdOn { get; set; }

        [JsonConstructor]
        public ResponseDeviceData(string id, string name, string createdOn)
        {
            this.id = id;
            this.name = name;
            this.createdOn = createdOn;
        }
        public override string ToString()
        {
            return "ID: " + id + " Name: " + name;
        }
    }

    [Serializable]
    internal class ResponseDeviceLCG
    {


        public string message { get; set; }
        public ResponseDeviceLCGData data { get; set; }

        [JsonConstructor]
        public ResponseDeviceLCG(string message, ResponseDeviceLCGData data)
        {
            this.message = message;
            this.data = data;
        }
        public override string ToString()
        {
            return "Message: " + message + " Data: " + data;
        }

    }

    [Serializable]
    internal class ResponseDeviceLCGData
    {
        public string gateway { get; set; }
        public string country { get; set; }

        [JsonConstructor]
        public ResponseDeviceLCGData(string gateway, string country)
        {
            this.gateway = gateway;
            this.country = country;
        }
        public override string ToString()
        {
            return " Gateway: " + gateway + " Country: " + country;
        }
    }


    [Serializable]
    internal class ResponseDeviceShockers
    {


        public string message { get; set; }
        public ResponseDeviceShockersData[] data { get; set; }

        [JsonConstructor]
        public ResponseDeviceShockers(string message, ResponseDeviceShockersData[] data)
        {
            this.message = message;
            this.data = data;
        }
        public override string ToString()
        {
            string output = "Message: " + message + " Shockers:\n";
            foreach (var device in data)
            {
                output += " -> " + device + "\n";
            }
            return output;
        }

    }
    internal class ResponseDeviceShockersData
    {
        public string id { get; set; }
        public int rfId { get; set; }
        public string model { get; set; }
        public string name { get; set; }
        public bool isPaused { get; set; }
        public string createdOn { get; set; }

        [JsonConstructor]
        public ResponseDeviceShockersData(string id, int rfId, string model, string name, bool isPaused, string createdOn)
        {
            this.id = id;
            this.rfId = rfId;
            this.model = model;
            this.name = name;
            this.isPaused = isPaused;
            this.createdOn = createdOn;
        }

        public override string? ToString()
        {
            return "ID: " + id + " rfId: " + rfId + " Name: " + name + " isPaused: " + isPaused;
        }
    }

}
