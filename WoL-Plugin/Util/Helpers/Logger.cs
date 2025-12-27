using System;
using System.IO;
using WoLightning.Configurations;

namespace WoLightning.WoL_Plugin.Util
{
    public static class Logger
    {

        public static Configuration? CONFIGURATION_REF;

        public static string FilePath()
        {
            try
            {
                return Service.PluginInterface.GetPluginConfigDirectory() + "\\" + "Log.txt";
            }
            catch { }
            return "NO FILE";
        }

        public static void SetupFile()
        {
            //if (!ValidateFile()) return;

            if (!File.Exists(FilePath()))
            {
                File.Create(FilePath()).Close();
                if (!File.Exists(FilePath())) return;
            }
            else
            {
                try
                {
                    long length = new FileInfo(FilePath()).Length;

                    Service.PluginLog.Verbose("Log Size: " + length);
                    if (length > 20480)
                    {
                        File.Delete(FilePath());
                        File.Create(FilePath()).Close();
                    }
                }
                catch { }
            }
            try
            {
                File.AppendAllText(FilePath(), $"\n\n======================\nNew Session Started\nVersion {Plugin.currentVersion}");
                File.AppendAllText(FilePath(), "\n" + DateTime.Now.ToShortDateString() + "  " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second);
            }
            catch { }
        }


        private static bool ValidateFile()
        {
            if (FilePath().Equals("NO FILE")) return false;
            return File.Exists(FilePath());
        }



        private static async void WriteLog(string message)
        {
            if (!ValidateFile()) return;
            DateTime now = DateTime.Now;
            try
            {
                await File.AppendAllTextAsync(FilePath(), "\n" + $"[{now.Hour}:{now.Minute}:{now.Second}] " + message);
            }
            catch { }
        }

        private static async void WriteLog(object obj)
        {
            if (!ValidateFile()) return;

            DateTime now = DateTime.Now;
            try
            {
                await File.AppendAllTextAsync(FilePath(), "\n" + $"[{now.Hour}:{now.Minute}:{now.Second}] " + obj.GetType().Name);
                foreach (var prop in obj.GetType().GetProperties())
                {
                    await File.AppendAllTextAsync(FilePath(), "\n - " + prop.Name + ": " + prop.GetValue(obj, null));
                }
            }
            catch { }
        }

        public static void Log(DebugLevel level, string message)
        {
            if (CONFIGURATION_REF != null && CONFIGURATION_REF.DebugLevel < level) return;

            switch (level)
            {
                case DebugLevel.Dev:
                    Service.PluginLog.Verbose(message);
                    WriteLog("[Dev] " + message);
                    break;

                case DebugLevel.Verbose:
                    Service.PluginLog.Verbose(message);
                    WriteLog("[Verbose] " + message);
                    break;

                case DebugLevel.Debug:
                    Service.PluginLog.Debug(message);
                    WriteLog("[Debug] " + message);
                    break;

                case DebugLevel.Info:
                    Service.PluginLog.Info(message);
                    WriteLog("[Info] " + message);
                    break;

                case DebugLevel.None: default: break;
            }
        }

        public static void LogObject(DebugLevel level, Object obj)
        {
            switch (level)
            {
                case DebugLevel.Dev:
                    if (obj.ToString() != null) Service.PluginLog.Verbose(obj.ToString()!);
                    WriteLog("[Dev - Object]");
                    WriteLog(obj);
                    break;

                case DebugLevel.Verbose:
                    if (obj.ToString() != null) Service.PluginLog.Verbose(obj.ToString()!);
                    WriteLog("[Verbose - Object]");
                    WriteLog(obj);
                    break;

                case DebugLevel.Debug:
                    if (obj.ToString() != null) Service.PluginLog.Debug(obj.ToString()!);
                    WriteLog("[Debug - Object]");
                    WriteLog(obj);
                    break;

                case DebugLevel.Info:
                    if (obj.ToString() != null) Service.PluginLog.Info(obj.ToString()!);
                    WriteLog("[Info - Object]");
                    WriteLog(obj);
                    break;

                case DebugLevel.None: default: break;
            }
        }

        public static void Log(DebugLevel level, Object obj)
        {
            string? message = obj.ToString();
            if (message != null) Logger.Log(level, message);
        }

        public static void Log(int level, string message)
        {
            Logger.Log((DebugLevel)level, message);
        }

        public static void Log(int level, Object obj)
        {
            string? message = obj.ToString();
            if (message != null) Logger.Log(level, message);
        }

        public static void LogObject(int level, Object obj)
        {
            LogObject((DebugLevel)level, obj);
        }

        public static void Error(string message)
        {
            Service.PluginLog.Error(message);
            WriteLog("====================" +
                     "\n[ERROR] " + message +
                     "\n====================");
        }

        public static void Error(Object obj)
        {
            string? message = obj.ToString();
            if (message != null) Error(message);
        }
    }

}

