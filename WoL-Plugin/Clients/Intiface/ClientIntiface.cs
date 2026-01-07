using Buttplug.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Clients.Intiface;
using WoLightning.WoL_Plugin.Util;


namespace WoLightning.Clients.Intiface
{
    public class ClientIntiface : IDisposable
    {
        public enum ConnectionStatusIntiface
        {
            NotStarted = 0,
            Unavailable = 1,
            FatalError = 3,
            Disconnected = 4,

            Connecting = 99,
            ConnectedNoDevices = 101,
            Connected = 200,
        }

        private Plugin? Plugin;

        public ButtplugClient? Client;
        public ConnectionStatusIntiface Status { get; set; } = ConnectionStatusIntiface.NotStarted;
        public int ConnectionAttempts = 0;
        private static int ConnectionAttemptsMax = 7;

        private List<IntifaceTask> RunningTasks = new();


        public ClientIntiface(Plugin plugin)
        {
            Plugin = plugin;
        }
        public async void Setup()
        {
            await SetupAllData();
        }

        public async Task SetupAllData()
        {
            if (Client != null)
            {
                await Connect();
                return;
            }

            Client = new ButtplugClient("WoLightning");

            Client.DeviceAdded += OnDeviceAdded;
            Client.DeviceRemoved += OnDeviceRemoved;
            Client.ServerDisconnect += OnDisconnect;

            await Connect();
        }

        private async Task Connect()
        {

            if (Client == null) return;
            if (Status == ConnectionStatusIntiface.Connected || Status == ConnectionStatusIntiface.Connecting) return;

            Status = ConnectionStatusIntiface.Connecting;
            ConnectionAttempts = 0;

            while (Status != ConnectionStatusIntiface.Connected && ConnectionAttempts < ConnectionAttemptsMax)
            {
                try
                {
                    Logger.Log(4, $"{ConnectionAttempts}/{ConnectionAttemptsMax} Connecting to Intiface on {Plugin.Authentification.IntifaceURL}");
                    await Client.ConnectAsync(new ButtplugWebsocketConnector(new Uri(Plugin.Authentification.IntifaceURL)));
                    Logger.Log(4, $"[Intiface] Succesfully connected!");
                    Status = ConnectionStatusIntiface.Connected;
                }
                catch (Exception e)
                {
                    Logger.Log(4, $"[Intiface] Couldnt connect! Trying again in 5 seconds...");
                    ConnectionAttempts++;
                    await Task.Delay(5000);
                }
            }

            if (Status != ConnectionStatusIntiface.Connected)
            {
                Status = ConnectionStatusIntiface.Unavailable;
                Logger.Log(4, "[Intiface] Failed to connect after 7 attempts!");
            }
        }

        private void OnDisconnect(object? sender, EventArgs e)
        {
            Logger.Log(3, $"Intiface Server Disconnected!");
            Status = ConnectionStatusIntiface.Disconnected;
            SetupAllData();
        }

        private void OnDeviceAdded(object? sender, DeviceAddedEventArgs Arguments)
        {
            Logger.Log(4, $"[Intiface] Device {Arguments.Device.Name} Connected!");
            UpdateDeviceList();
        }

        private void OnDeviceRemoved(object? sender, DeviceRemovedEventArgs Arguments)
        {
            Logger.Log(4, $"[Intiface] Device {Arguments.Device.Name} Removed!");
            UpdateDeviceList();
        }

        public void UpdateDeviceList()
        {
            Logger.Log(4, $"[Intiface] Updating Intiface Device list from {Plugin.Authentification.DevicesIntiface.Count} to {Client.Devices.Length}");
            Plugin.Authentification.DevicesIntiface.Clear();
            if (Client == null || Client.Devices == null) return;
            foreach (var device in Client.Devices)
            {
                if(device == null) continue;//sometimes we can just get null???
                DeviceIntiface converted = new(device);
                Plugin.Authentification.DevicesIntiface.Add(converted);
            }
        }

        public async void SendRequest(ShockOptions options)
        {
            #region Validation

            if (Plugin.IsFailsafeActive)
            {
                Logger.Log(3, " -> [Intiface] Blocked request due to failsafe mode!");
                return;
            }

            if (!options.Validate())
            {
                Logger.Log(3, " -> [Intiface] Blocked due to invalid Buttplug Options!");
                return;
            }


            if (options.DevicesIntiface.Count == 0)
            {
                Logger.Log(3, " -> [Intiface] No Devices assigned, discarding!");
                return;
            }
            #endregion

            if (Client == null || Status != ConnectionStatusIntiface.Connected)
            {
                if (Status == ConnectionStatusIntiface.Connecting) return;
                Logger.Log(3, "-> [Intiface] No Connection is made! Trying to connect now...");
                await SetupAllData();
            }

            Logger.Log(4, "[Intiface] Intiface Request successful! Creating Tasks...");



            foreach (var Device in options.DevicesIntiface)
            {

                if (Client.Devices == null) return;

                ButtplugClientDevice? realDevice = Client.Devices.First(dev => dev.Index == Device.Index);
                if (realDevice == null)
                {
                    Logger.Log(4, $"[Intiface] Couldnt match Index: {Device.Index}");
                    continue;
                }

                RunningTasks.RemoveAll(tsk => tsk == null || tsk.IsCancelled || tsk.Task.Status != TaskStatus.Running);

                IntifaceTask? running = RunningTasks.Find(tsk => tsk.Device.Index == realDevice.Index);

                if (running == null)
                {
                    IntifaceTask newTask = new(realDevice, options.Intensity / 100.0, options.getDurationOpenShock());
                    RunningTasks.Add(newTask);
                    newTask.CreateAndStart();
                }
                else
                {
                    if (running.Intensity < options.Intensity / 100.0) return;
                    if (running.Duration - running.Timer.TimeLeft < options.getDurationOpenShock()) return;
                    running.IsCancelled = true;

                    IntifaceTask newTask = new(realDevice, options.Intensity / 100.0, options.getDurationOpenShock());
                    RunningTasks.Add(newTask);
                    newTask.CreateAndStart();
                }
            }
        }

        public void Dispose()
        {
            if (Client == null) return;
            Client.DeviceAdded -= OnDeviceAdded;
            Client.DeviceRemoved -= OnDeviceRemoved;
            Client.ServerDisconnect -= OnDisconnect;

            Client?.DisconnectAsync().Wait();
            Client?.Dispose();
        }
    }
}














/*  public ButtplugClient TestClient;
    public async Task WebSocketTest()
    {
            Logger.Log(4,"[BUTTPLUG] Starting Test");
            // Creating a Websocket Connector is as easy as using the right
            // options object.
            
            TestClient = new ButtplugClient("Example Client");

            TestClient.DeviceAdded += (aObj, aDeviceEventArgs) =>
                Logger.Log(4,$"Device {aDeviceEventArgs.Device.Name} Connected!");

            TestClient.DeviceRemoved += (aObj, aDeviceEventArgs) =>
                Logger.Log(4,$"Device {aDeviceEventArgs.Device.Name} Removed!");

            

            try 
            {
            
            await TestClient.ConnectAsync(new ButtplugWebsocketConnector(new Uri("ws://127.0.0.1:12345")));
            
            }
            catch (Exception e)
        {
            
            Logger.Log(4,"[BUTTPLUG] Couldnt connect!");
            Logger.Log(4,e?.InnerException?.Message);

        }
            
            Logger.Log(4,TestClient.ToString());

            Logger.Log(4,"[BUTTPLUG] Connected to Intiface");


            Logger.Log(4,"[BUTTPLUG] Starting Device Scan");

            await TestClient.StartScanningAsync();
            
            foreach (var device in TestClient.Devices)
            {
                Logger.Log(4,$"[BUTTPLUG] {device.Name} is connected");
            }
            await Task.Delay(5000);
            Logger.Log(4,"[BUTTPLUG] Waited 5 seconds, stopping scan");

            await TestClient.StopScanningAsync();
            

            foreach (var device in TestClient.Devices)
            {
                Logger.Log(4,$"[BUTTPLUG] {device.Name} supports vibration: {device.VibrateAttributes.Count > 0}");
                if (device.VibrateAttributes.Count > 0)
                {
                   Logger.Log(4,$"[BUTTPLUG]  - Number of Vibrators: {device.VibrateAttributes.Count}");
                }
            }

            Logger.Log(4,"[BUTTPLUG] ________FINISHED SETUP_________");

            Logger.Log(4,"[BUTTPLUG] Now trying to send commands");


            var testClientDevice = TestClient.Devices[0];
            await testClientDevice.VibrateAsync(0.3);

            await Task.Delay(2000);

            await testClientDevice.Stop();
            Logger.Log(4,"[BUTTPLUG] Stopped Command!");
            //test
        }
    }

}*/