using Buttplug.Client;
using System;
using System.Linq;
using System.Threading.Tasks;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;


namespace WoLightning.Clients.Buttplugio
{
    public class ClientIntiface : IDisposable
    {
        public enum ConnectionStatusIntiface
        {
            NotStarted = 0,
            Unavailable = 1,
            FatalError = 3,


            Connecting = 99,
            ConnectedNoDevices = 101,
            Connected = 200,
        }

        private Plugin? Plugin;

        public ButtplugClient Client;
        public ConnectionStatusIntiface Status { get; set; } = ConnectionStatusIntiface.NotStarted;


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

            if (Client != null) return;

            Client = new ButtplugClient("WoLightning");
            Status = ConnectionStatusIntiface.Connecting;

            Client.DeviceAdded += OnDeviceAdded;
            Client.DeviceRemoved += OnDeviceRemoved;

            try
            {
                Logger.Log(4, $"Connecting to Intiface on {Plugin.Authentification.ButtplugURL}");
                await Client.ConnectAsync(new ButtplugWebsocketConnector(new Uri(Plugin.Authentification.ButtplugURL)));
                Logger.Log(4, $"Succesfully connected!" +
                    $"\nName: {Client.Name}");
                Status = ConnectionStatusIntiface.Connected;
            }
            catch (Exception e)
            {
                Logger.Log(4, "[BUTTPLUG] Couldnt connect!");
                Logger.Log(4, e?.InnerException?.Message);
                Status = ConnectionStatusIntiface.FatalError;
            }
        }



        private void OnDeviceAdded(object? sender, DeviceAddedEventArgs Arguments)
        {
            Logger.Log(4, $"Device {Arguments.Device.Name} Connected!");
            Plugin.Authentification.DevicesIntiface = Client.Devices.ToList();
        }

        private void OnDeviceRemoved(object? sender, DeviceRemovedEventArgs Arguments)
        {
            Logger.Log(4, $"Device {Arguments.Device.Name} Removed!");
            Plugin.Authentification.DevicesIntiface = Client.Devices.ToList();
        }

        public async void SendRequest(ShockOptions options)
        {
            #region Validation

            if (Plugin.IsFailsafeActive)
            {
                Logger.Log(3, " -> Blocked request due to failsafe mode!");
                return;
            }

            if (!options.Validate())
            {
                Logger.Log(3, " -> Blocked due to invalid Buttplug Options!");
                return;
            }


            if (options.DevicesIntiface.Count == 0)
            {
                Logger.Log(3, " -> No Buttplug Devices assigned, discarding!");
                return;
            }
            #endregion

            if (Client == null || Status != ConnectionStatusIntiface.Connected)
            {
                if (Status == ConnectionStatusIntiface.Connecting) return;
                Client?.Dispose();
                Client = null;
                await SetupAllData();
                return;
            }

            Logger.Log(4, "Intiface Request successful! Creating Tasks...");

            foreach (var Device in options.DevicesIntiface)
            {

                if (Client.Devices == null) return;

                ButtplugClientDevice? realDevice = Client.Devices.First(dev => dev.Index == Device.Index);
                if (realDevice == null)
                {
                    Logger.Log(4, $"Couldnt match Index: {Device.Index}");
                    continue;
                }

                Task schedule = new Task(async () =>
                {
                    Logger.Log(4, $"Task for {Device.Name}: Intensity: {options.Intensity / 100.0} Duration: {options.getDurationOpenShock()}ms");
                    await realDevice.VibrateAsync(options.Intensity / 100.0);
                    await Task.Delay(options.getDurationOpenShock());
                    await realDevice.Stop();
                });

                Logger.Log(4, $"Sending Task to {Device.Name} at Index: {Device.Index}");
                schedule.Start();
            }
        }

        public void Dispose()
        {
            if (Client == null) return;
            Client.DeviceAdded -= OnDeviceAdded;
            Client.DeviceRemoved -= OnDeviceRemoved;


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