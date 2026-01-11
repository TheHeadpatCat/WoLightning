using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Clients
{
    public abstract class BaseClient : IDisposable
    {
        public enum ClientConnectionStatus
        {
            NotStarted = 0,
            Disabled = 1,
            MissingData = 2,
            Unknown = 3,

            Connecting = 99,
            ConnectedNoInfo = 100,
            ConnectedNoDevices = 101,
            Ready = 200,

            Disconnected = 399,
            Unavailable = 400,
            InvalidData = 401,
            ExceededAttempts = 402,
            FatalError = 403,
        }

        readonly Plugin? Plugin;
        public ClientConnectionStatus Status { get; set; } = ClientConnectionStatus.NotStarted;

        
        readonly TimerPlus ConnectionAttemptTimer = new();
        int ConnectionAttempts = 0;
        readonly int ConnectionAttemptsMax = 5;

        public BaseClient(Plugin plugin)
        {
            Plugin = plugin;
            ConnectionAttemptTimer.Interval = 10000;
            ConnectionAttemptTimer.Elapsed += OnResetAttempts;
            ConnectionAttemptTimer.AutoReset = false;

        }
        public abstract void Setup();
        public abstract bool ValidateRequest(DeviceOptions options);
        public abstract void SendRequest(DeviceOptions options);
        public virtual void Dispose()
        {
            ConnectionAttemptTimer.Elapsed -= OnResetAttempts;
            ConnectionAttemptTimer.Stop();
            ConnectionAttemptTimer.Dispose();
        }

        private void OnResetAttempts(object? _o, ElapsedEventArgs _e)
        {
            ConnectionAttempts = 0;
        }

    }
}
