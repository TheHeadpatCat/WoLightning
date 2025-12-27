using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WoLightning.Configurations;

namespace WoLightning.WoL_Plugin.Configurations
{
    [Serializable]
    public class ControlSettings : IDisposable
    {
        public int Version { get; set; } = 100;
        private string? ConfigurationDirectoryPath { get; init; }


        public void Save()
        {
            File.WriteAllText(ConfigurationDirectoryPath + "ControlSettings.json", SerializeControlSettings(this));
        }
        public void Dispose()
        {
            Save();
        }

        internal static string SerializeControlSettings(object? config)
        {
            return JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }
        private ControlSettings DeserializeAuthentification(string input)
        {
            if (input == "") return new ControlSettings();
            return JsonConvert.DeserializeObject<ControlSettings>(input)!;
        }

    }
}
