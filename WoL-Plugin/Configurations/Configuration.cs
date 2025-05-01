using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using WoLightning.Util.Types;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;


namespace WoLightning.Configurations
{

    public enum DebugLevel
    {
        None = 0,
        Basic = 1,
        Verbose = 2,
        All = 3,
    }

    [Serializable]
    public class Configuration : IPluginConfiguration, IDisposable
    {
        public DebugLevel DebugLevel { get; set; } = DebugLevel.None;
        public int Version { get; set; } = 500;
        public string LastPresetName { get; set; } = "Default";
        public bool ActivateOnStart { get; set; } = false;

        // Preset Settings
        [NonSerialized] public Preset ActivePreset;
        [NonSerialized] public int ActivePresetIndex = 0;
        [NonSerialized] public List<Preset> Presets = new();
        [NonSerialized] public List<string> PresetNames = new(); // used for comboBoxes
        [NonSerialized] public Action<Preset, int> PresetChanged;
        [NonSerialized] private Plugin plugin;
        [NonSerialized] public string ConfigurationDirectoryPath;

        public void Initialize(Plugin plugin, string ConfigurationDirectoryPath)
        {
            this.plugin = plugin;
            this.ConfigurationDirectoryPath = ConfigurationDirectoryPath;

            string f = "";
            if (File.Exists(ConfigurationDirectoryPath + "Config.json")) f = File.ReadAllText(ConfigurationDirectoryPath + "Config.json");

            plugin.Log("Initializing Config...");

            Configuration s = DeserializeConfig(f);
            foreach (PropertyInfo property in typeof(Configuration).GetProperties().Where(p => p.CanWrite)) property.SetValue(this, property.GetValue(s, null), null);


            if (Directory.Exists(ConfigurationDirectoryPath + "\\Presets")) // Todo only open presets when we swap them
            {
                foreach (var file in Directory.EnumerateFiles(ConfigurationDirectoryPath + "\\Presets"))
                {
                    string p = File.ReadAllText(file);
                    Preset tPreset;
                    //plugin.Log("Deserialzing " + file);
                    try
                    {
                        tPreset = DeserializePreset(p);
                    }
                    catch (Exception e)
                    {
                        plugin.Log("Failed to deserialize Preset: " + file);
                        plugin.Log(e);
                        continue;
                    }
                    Presets.Add(tPreset);
                }
            }
            if (Presets.Count == 0 || !loadPreset(LastPresetName))
            {
                plugin.Log("No Presets found, or cannot load last preset - Creating Default.");
                ActivePreset = new Preset("Default", plugin.LocalPlayer.getFullName());
                Presets.Add(ActivePreset);
                loadPreset("Default");
                return;
            }
        }

        public void Initialize(Plugin plugin, string ConfigurationDirectoryPath, bool createNew)
        {
            //this.isAlternative = isAlternative;
            this.ConfigurationDirectoryPath = ConfigurationDirectoryPath;

            Save();
        }


        #region Save and Loading
        public void Save()
        {
            plugin.Log("Configuration.Save() called");
            try
            {
                LastPresetName = ActivePreset.Name;
                PresetNames = new();

                foreach (var preset in Presets)
                {
                    PresetNames.Add(preset.Name);
                    savePreset(preset);
                }
            }
            catch (Exception e)
            {
                plugin.NotificationHandler.send("Failed to save Presets!");
                plugin.Error("Failed to save Presets!", e);
                plugin.Log(e.ToString());
            }


            try
            {
                File.WriteAllText(ConfigurationDirectoryPath + "Config.json", SerializeConfig(this));
            }
            catch (Exception e) // scuffed crash protection - if this happens we got a serious issue.
            {
                plugin.NotificationHandler.send("Failed to save Configuration!");
                plugin.Error("Failed to save Configuration!", e);
                plugin.Log(e.ToString());
            }
        }

        public void Save(object? sender, ElapsedEventArgs? e)
        {
            Save();
        }

        public bool loadPreset(string Name)
        {

            if (!Presets.Exists(preset => preset.Name == Name)) return false;
            ActivePreset = Presets.Find(preset => preset.Name == Name);
            if (ActivePreset == null) throw new Exception("Preset not Found");
            ActivePreset.Dispose();
            ActivePresetIndex = Presets.IndexOf(ActivePreset);
            LastPresetName = ActivePreset.Name;
            ActivePreset.Initialize(plugin);
            ActivePreset.resetInvalidTriggers();
            ActivePreset.ValidateShockers();

            PresetChanged?.Invoke(ActivePreset, ActivePresetIndex);
            plugin.Log(" -> Done.");
            return true;
        }


        public void saveCurrentPreset()
        {
            plugin.Log("Saving preset: " + ActivePreset.Name);
            File.WriteAllText($"{ConfigurationDirectoryPath}\\Presets\\{ActivePreset.Name}.json", SerializePreset(ActivePreset));
        }
        public void savePreset(Preset target)
        {
            plugin.Log("Saving preset: " + target.Name);
            File.WriteAllText($"{ConfigurationDirectoryPath}\\Presets\\{target.Name}.json", SerializePreset(target));
        }
        public void savePreset(Preset target, bool isAlternative)
        {
            File.WriteAllText($"{ConfigurationDirectoryPath}\\MasterPresets\\{target.Name}.json", SerializePreset(target));
        }

        public void deletePreset(Preset target)
        {
            if (!Presets.Exists(preset => preset.Name == target.Name)) return;
            if (!File.Exists(ConfigurationDirectoryPath + "\\Presets\\" + target.Name + ".json")) return;

            File.Delete(ConfigurationDirectoryPath + "\\Presets\\" + target.Name + ".json");
            Presets.Remove(target);
            if (!Presets.Exists(preset => preset.Name == "Default")) Presets.Add(new Preset("Default", plugin.LocalPlayer.getFullName()));
            loadPreset("Default");
            Save();
        }
        #endregion

        #region Serialization
        internal static string SerializeConfig(object? config)
        {
            return JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            });
        }

        internal static Configuration DeserializeConfig(string input)
        {
            if (input == "") return new Configuration();
            return JsonConvert.DeserializeObject<Configuration>(input)!;
        }

        internal static string SerializePreset(object? preset)
        {
            return JsonConvert.SerializeObject(preset, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        internal static Preset DeserializePreset(string input)
        {
            if (input == "") return null!; // cry
            return JsonConvert.DeserializeObject<Preset>(input)!;
        }
        #endregion

        public void Dispose()
        {
            Save();
            ActivePreset.Dispose();
        }
    }
}