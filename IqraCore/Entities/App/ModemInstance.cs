using RJCP.IO.DeviceMgr;
using SimcomModuleManager;

namespace ProjectIqraBackend.App.Entities
{
    public class ModemInstance
    {
        public DeviceInstance CompositeInstance { get; set; }
        public DeviceInstance AudioInstance { get; set; }
        public DeviceInstance ATInstance { get; set; }

        public string PhoneNumber { get; set; }
    
        public SimcomModemManager SimcomModemManager { get; set; }
    }
}
