using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Server;
using LibreHardwareMonitor.Hardware;

namespace IqraInfrastructure.Services.Server
{
    public class ServerManager
    {
        private readonly ServerHistoricalStatusRepository _serverHistoricalStatusRepository;

        private ServerStatusData _currentServerStatus;

        private CancellationTokenSource _monitorCancellation;
        private Task? _monitorTask;
        private int _monitorDelayMS = 1000;
        private int _databaseSaveDelayMinutes = 1;

        private DateTime _lastDatabaseSave;

        private static Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = false,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true
        };

        public ServerManager(string serverIdentifier, ServerHistoricalStatusRepository serverHistoricalStatusRepository)
        {
            _currentServerStatus = new ServerStatusData();

            _serverHistoricalStatusRepository = serverHistoricalStatusRepository;

            _lastDatabaseSave = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(_databaseSaveDelayMinutes * 2));
        }

        public void StartServerMonitor(CancellationTokenSource cts)
        {
            if (_monitorTask != null) return;

            computer.Open();

            _monitorCancellation = cts;
            _monitorTask = Task.Run(MonitorServerHardware, _monitorCancellation.Token);
        }

        public void StopServerMonitor()
        {
            if (_monitorTask == null) return;

            _monitorCancellation.Cancel();

            _monitorTask.Wait();
            _monitorTask = null;

            computer.Close();
        }

        public async Task MonitorServerHardware()
        {
            List<IHardware> CPUs = new List<IHardware>();
            List<IHardware> Rams = new List<IHardware>();
            List<IHardware> Disks = new List<IHardware>();
            List<IHardware> Networks = new List<IHardware>();

            foreach (IHardware hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    CPUs.Add(hardware);
                }

                if (hardware.HardwareType == HardwareType.Memory)
                {
                    Rams.Add(hardware);
                }

                if (hardware.HardwareType == HardwareType.Storage)
                {
                    Disks.Add(hardware);
                }

                if (hardware.HardwareType == HardwareType.Network)
                {
                    Networks.Add(hardware);
                }
            }

            while (!_monitorCancellation.Token.IsCancellationRequested)
            {
                // Update All Hardware
                CPUs.ForEach(x => x.Update());
                Rams.ForEach(x => x.Update());
                Disks.ForEach(x => x.Update());
                Networks.ForEach(x => x.Update());

                // Get Cpu Usage
                List<ServerHardwareStatusItemData> CPUUsage = new List<ServerHardwareStatusItemData>();
                foreach (var cpu in CPUs)
                {
                    var cpuItem = new ServerHardwareStatusItemData()
                    {
                        Identifier = cpu.Identifier.ToString(),
                        Name = cpu.Name,
                        Sensors = new List<ServerHardwareStatusItemSensorData>()
                    };

                    foreach (var cpuSensor in cpu.Sensors)
                    {
                        cpuItem.Sensors.Add(
                            new ServerHardwareStatusItemSensorData()
                            {
                                Name = cpuSensor.Name,
                                Value = cpuSensor.Value,
                                Min = cpuSensor.Min,
                                Max = cpuSensor.Max
                            }
                        );
                    }

                    CPUUsage.Add(cpuItem);
                }

                // Get Ram Usage
                List<ServerHardwareStatusItemData> RAMUsage = new List<ServerHardwareStatusItemData>();
                foreach (var ram in Rams)
                {
                    var ramItem = new ServerHardwareStatusItemData()
                    {
                        Identifier = ram.Identifier.ToString(),
                        Name = ram.Name,
                        Sensors = new List<ServerHardwareStatusItemSensorData>()
                    };

                    foreach (var ramSensor in ram.Sensors)
                    {
                        ramItem.Sensors.Add(
                            new ServerHardwareStatusItemSensorData()
                            {
                                Name = ramSensor.Name,
                                Value = ramSensor.Value,
                                Min = ramSensor.Min,
                                Max = ramSensor.Max
                            }
                        );
                    }

                    RAMUsage.Add(ramItem);
                }

                // Get Disk Usage
                List<ServerHardwareStatusItemData> DiskUsage = new List<ServerHardwareStatusItemData>();
                foreach (var disk in Disks)
                {
                    var diskItem = new ServerHardwareStatusItemData()
                    {
                        Identifier = disk.Identifier.ToString(),
                        Name = disk.Name,
                        Sensors = new List<ServerHardwareStatusItemSensorData>()
                    };

                    foreach (var diskSensor in disk.Sensors)
                    {
                        diskItem.Sensors.Add(
                            new ServerHardwareStatusItemSensorData()
                            {
                                Name = diskSensor.Name,
                                Value = diskSensor.Value,
                                Min = diskSensor.Min,
                                Max = diskSensor.Max
                            }
                        );
                    }

                    DiskUsage.Add(diskItem);
                }

                // Get Network Usage
                List<ServerHardwareStatusItemData> NetworkUsage = new List<ServerHardwareStatusItemData>();
                foreach (var network in Networks)
                {
                    var networkItem = new ServerHardwareStatusItemData()
                    {
                        Identifier = network.Identifier.ToString(),
                        Name = network.Name,
                        Sensors = new List<ServerHardwareStatusItemSensorData>()
                    };

                    foreach (var networkSensor in network.Sensors)
                    {
                        networkItem.Sensors.Add(
                            new ServerHardwareStatusItemSensorData()
                            {
                                Name = networkSensor.Name,
                                Value = networkSensor.Value,
                                Min = networkSensor.Min,
                                Max = networkSensor.Max
                            }
                        );
                    }

                    NetworkUsage.Add(networkItem);
                }

                _currentServerStatus.HardwareStatus = new ServerHardwareStatusData()
                {
                    CPUUsage = CPUUsage,
                    DiskUsage = DiskUsage,
                    NetworkUsage = NetworkUsage,
                    RamUsage = RAMUsage
                };

                if (DateTime.UtcNow.Subtract(_lastDatabaseSave).Minutes > _databaseSaveDelayMinutes)
                {
                    await _serverHistoricalStatusRepository.InsertAsync(new ServerHistoricalStatusData()
                    {
                        Id = Guid.NewGuid().ToString(),
                        DateTime = DateTime.UtcNow,
                        HardwareStatus = _currentServerStatus.HardwareStatus,
                        OnGoingCalls = _currentServerStatus.OnGoingCalls,
                        QueuedCalls = _currentServerStatus.QueuedCalls
                    });

                    _lastDatabaseSave = DateTime.UtcNow;
                }

                await Task.Delay(_monitorDelayMS);
            }
        }
    }
}
