using Buttplug.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WoLightning.WoL_Plugin.Clients;
using WoLightning.WoL_Plugin.Clients.OpenShock;
using WoLightning.WoL_Plugin.Clients.Pishock;

namespace WoLightning.Configurations
{

    [Serializable]
    public class Authentification : IDisposable // This class is here to make sure the data that gets received from the server, is actually from this plugin (well not entirely, but it helps)
    {
        public int Version { get; set; } = 401;


        private string? ConfigurationDirectoryPath { get; init; }

        // Webserver things
        public string WebserverURL { get; } = "https://theheadpatcat.ddns.net/post/WoLightning";
        public bool acceptedEula { get; set; } = false;
        public string ServerKey { get; set; } = string.Empty; // TODO: Move to own .json file to lower chance of loss
        private string Hash { get; set; }
        public string DevKey { get; set; } = string.Empty;


        // Pishock things
        public string PishockName { get; set; } = string.Empty;
        public string PishockApiKey { get; set; } = string.Empty;
        [JsonIgnore] public List<ShockerPishock> PishockShockers { get; set; } = new();

        // OpenShock things
        public string OpenShockURL { get; set; } = "https://api.openshock.app";
        public string OpenShockApiKey { get; set; } = string.Empty;
        [JsonIgnore] public List<ShockerOpenShock> OpenShockShockers { get; set; } = new();

        // Buttplug things
        public string ButtplugURL { get; set; } = "ws://127.0.0.1:12345";
        [JsonIgnore] public List<ButtplugClientDevice> ButtplugDevices { get; set; } = new();


        public Authentification() { }
        public Authentification(string ConfigDirectoryPath)
        {
            ConfigurationDirectoryPath = ConfigDirectoryPath;

            string f = "";
            if (File.Exists(ConfigurationDirectoryPath + "authentification.json")) f = File.ReadAllText(ConfigurationDirectoryPath + "authentification.json");
            Authentification s = DeserializeAuthentification(f);
            foreach (PropertyInfo property in typeof(Authentification).GetProperties().Where(p => p.CanWrite)) property.SetValue(this, property.GetValue(s, null), null);
            Save();
        }
        public Authentification(string ConfigDirectoryPath, bool delete)
        {
            ConfigurationDirectoryPath = ConfigDirectoryPath;
            Save();
        }

        #region Hashing
        public string getHash()
        {
            if (Hash != null && Hash.Length > 0) return Hash;

            byte[] arr = Encoding.ASCII.GetBytes(Plugin.CurrentVersion.ToString() + Plugin.randomKey);
            byte[] hashed = SHA256.Create().ComputeHash(arr);


            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashed.Length; i++)
            {
                builder.Append(hashed[i].ToString("x2"));
            }

            Hash = builder.ToString();

            return Hash;
        }
        public X509Certificate2 getCertificate()
        {
            X509Certificate2 certificate = new();
            if (!File.Exists(ConfigurationDirectoryPath + "/cert.pem"))
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://theheadpatcat.ddns.net");

                    request.ServerCertificateValidationCallback = (sender, cert, chain, error) =>
                    {
                        File.WriteAllBytes(ConfigurationDirectoryPath + "/cert.pem", cert.GetRawCertData());
                        certificate = new X509Certificate2(cert.GetRawCertData());
                        return true;
                    };
                    WebResponse response = request.GetResponse(); //will be 404
                }
                catch
                {
                }
            }
            else
            {
                certificate = new X509Certificate2(File.ReadAllBytes(ConfigurationDirectoryPath + "/cert.pem"));
            }
            return certificate;
        }
        #endregion

        public void Save()
        {
            if (OpenShockURL.Length < 3) OpenShockURL = "https://api.openshock.app";
            File.WriteAllText(ConfigurationDirectoryPath + "Authentification.json", SerializeAuthentification(this));
        }
        public void Dispose()
        {
            Save();
        }

        internal static string SerializeAuthentification(object? config)
        {
            return JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }
        private Authentification DeserializeAuthentification(string input)
        {
            if (input == "") return new Authentification();
            return JsonConvert.DeserializeObject<Authentification>(input)!;
        }

        public int GetDevicesCount()
        {
            return PishockShockers.Count + OpenShockShockers.Count + ButtplugDevices.Count;
        }

        public List<ShockerBase> GetShockers()
        {
            List<ShockerBase> shockers = new();
            shockers.AddRange(PishockShockers);
            shockers.AddRange(OpenShockShockers);
            return shockers;
        }

    }
}
