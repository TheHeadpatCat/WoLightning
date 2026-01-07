using Buttplug.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Clients.Intiface
{
    public class IntifaceTask : IDisposable
    {

        public ButtplugClientDevice Device { get; set; }
        public Task Task { get; set; }
        public TimerPlus Timer { get; set; } = new();

        public double Intensity { get; set; } = 0.01;
        public int Duration { get; set; } = 1;

        public bool IsCancelled { get; set; } = false;

        public IntifaceTask(ButtplugClientDevice device, double intensity, int duration)
        {
            Device = device;
            Intensity = intensity;
            Duration = duration;
            Timer.AutoReset = false;
            Timer.Interval = duration + 200;
            Timer.Elapsed += OnElapsed;
        }

        private void OnElapsed(object? sender, ElapsedEventArgs e)
        {
            Dispose();
        }

        public void CreateAndStart()
        {
            Create();
            Start();
        }

        public void Create()
        {
            Task = new Task(async () =>
            {
                Logger.Log(4, $"[Intiface] Task for {Device.Name}: Intensity: {Intensity} Duration: {Duration}ms");
                if (!IsCancelled) await Device.VibrateAsync(Intensity);
                if (!IsCancelled) await Task.Delay(Duration, new System.Threading.CancellationToken(IsCancelled));
                if (!IsCancelled) await Device.Stop();
            });
        }

        public void Start()
        {
            Logger.Log(4, $"[Intiface] Sending Task to {Device.Name} at Index: {Device.Index}");
            Task.Start();
            Timer.Start();
        }

        public void Dispose()
        {
            if (!IsCancelled) Device?.Stop();
            IsCancelled = true;
            Task.Dispose();
        }
    }
}
