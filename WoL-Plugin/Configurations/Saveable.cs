using Newtonsoft.Json;
using System;
using System.IO;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;
using Logger = WoLightning.WoL_Plugin.Util.Logger;
using Version = WoLightning.WoL_Plugin.Util.Types.Version;

namespace WoLightning.WoL_Plugin.Configurations
{
    [Serializable]
    public abstract class Saveable
    {
        [JsonIgnore] abstract protected string FileName { get; }
        [JsonIgnore] virtual protected string SaveLocation { get; init; }
        [JsonIgnore] abstract protected Version CurrentVersion { get; init; }
        public Version SavedVersion { get; set; }

        public Saveable(string saveLocation, bool reset = false) 
        {
            SaveLocation = saveLocation;
        }

        abstract public void Load();
        virtual public bool Save()
        {
            SavedVersion = CurrentVersion;
            return saveFile();
        }
        virtual internal bool saveFile()
        {
            try
            {
                File.WriteAllText(SaveLocation + FileName, serialize());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to save file: " + this.GetType().ToString());
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
            return false;
        }
        virtual internal string serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        virtual internal string loadFile()
        {
            string dataString = "";
            if (File.Exists(SaveLocation + FileName)) dataString = File.ReadAllText(SaveLocation + FileName);
            return dataString;
        }
         
        abstract internal void updateFile();

        //abstract internal void injectProperties();
        //foreach (PropertyInfo property in typeof(Authentification).GetProperties().Where(p => p.CanWrite)) property.SetValue(this, property.GetValue(s, null), null);


    }
}
