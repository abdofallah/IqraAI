using IqraCore.Entities.App.Agent;
using ProjectIqraBackend.App.Entities;
using RJCP.IO.DeviceMgr;
using SimcomModuleManager;

namespace IqraInfrastructure.Services.App
{
    public class ModemsManager
    {
        private List<ModemInstance> _devices;
        public ModemsManager()
        {
            _devices = new List<ModemInstance>();
        }

        public async Task LoadDevices()
        {
            IList<DeviceInstance> deviceInstances = DeviceInstance.GetList();
            foreach (DeviceInstance deviceInstance in deviceInstances)
            {
                if (deviceInstance.DeviceDescription == "Simcom USB Composite Device 9001")
                {
                    if (_devices.FindIndex(d => d.CompositeInstance.BaseContainerId == deviceInstance.BaseContainerId) == -1)
                    {
                        DeviceInstance? ATDeviceInstance = deviceInstance.Children.FirstOrDefault(d => d.DeviceDescription.StartsWith("Simcom HS-USB AT PORT 9001"));
                        if (ATDeviceInstance == null)
                        {
                            Console.WriteLine($"AT Device not found for {deviceInstance.DeviceDescription} | {deviceInstance.BaseContainerId}");
                            continue;
                        }

                        DeviceInstance? AudioDeviceInstance = deviceInstance.Children.FirstOrDefault(d => d.DeviceDescription.StartsWith("Simcom HS-USB Audio 9001"));
                        if (AudioDeviceInstance == null)
                        {
                            Console.WriteLine($"Audio Device not found for {deviceInstance.DeviceDescription} | {deviceInstance.BaseContainerId}");
                            continue;
                        }

                        SimcomModemManager simcomModemManager = new SimcomModemManager(deviceInstance, ATDeviceInstance, AudioDeviceInstance);
                        if (!await simcomModemManager.Initialize())
                        {
                            Console.WriteLine($"Error initializing modem for {deviceInstance.DeviceDescription} | {deviceInstance.BaseContainerId}");
                            continue;
                        }

                        string? modemPhoneNumber = await simcomModemManager.GetModulePhoneNumber();
                        if (modemPhoneNumber == null)
                        {
                            Console.WriteLine($"Phone number not found for {deviceInstance.DeviceDescription} | {deviceInstance.BaseContainerId}");
                        }

                        ModemInstance currentModemInstance = new ModemInstance()
                        {
                            CompositeInstance = deviceInstance,
                            ATInstance = ATDeviceInstance,
                            AudioInstance = AudioDeviceInstance,
                            SimcomModemManager = simcomModemManager,
                            PhoneNumber = modemPhoneNumber == null ? "NOT_FOUND" : modemPhoneNumber
                        };

                        _devices.Add(currentModemInstance);
                    }
                }
            }
        }


        public List<ModemInstance> GetModemInstances()
        {
            return _devices;
        }

        public ModemInstance? GetModemInstanceByPhoneNumber(string phoneNumber)
        {
            ModemInstance? modemInstance = _devices.Find(d => d.PhoneNumber == phoneNumber);
            if (modemInstance == null)
            {
                Console.WriteLine("Instance not found for the given phone number");
                return null;
            }

            return modemInstance;
        }

        public async Task<bool> SetPhoneNumber(string modemCompositeBaseContainerId, string phoneNumber)
        {
            ModemInstance? modemInstance = _devices.Find(d => d.CompositeInstance.BaseContainerId == modemCompositeBaseContainerId);
            if (modemInstance == null)
            {
                Console.WriteLine("Instance not found for setting phone number");
                return false;
            }

            return await SetPhoneNumber(modemInstance, phoneNumber);
        }

        public async Task<bool> SetPhoneNumber(ModemInstance modemInstance, string phoneNumber)
        {
            modemInstance.PhoneNumber = phoneNumber;

            if (!(await modemInstance.SimcomModemManager.SetModulePhoneNumber(phoneNumber)))
            {
                Console.WriteLine("Failed to set phone number within the module");
                return false;
            }

            string? modemPhoneNumber = await modemInstance.SimcomModemManager.GetModulePhoneNumber();
            if (modemPhoneNumber != phoneNumber)
            {
                Console.WriteLine("Failed to verify the phone number within the module that was set");
                return false;
            }

            return true;
        }
    }
}
