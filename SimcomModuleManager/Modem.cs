using RJCP.IO.DeviceMgr;
using RJCP.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace SimcomModuleManager
{
    public class Modem
    {
        private readonly DeviceInstance _instance;

        private readonly SerialPortStream _serialPort;

        public Modem(DeviceInstance portInstance)
        {
            _instance = portInstance;

            _serialPort = new SerialPortStream();

            Match match = Regex.Match(_instance.FriendlyName, @"\((.*?)\)");
            _serialPort.PortName = match.Groups[1].Value;

            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;

            _serialPort.ReadBufferSize = 4096;
            _serialPort.WriteBufferSize = 4096;

            _serialPort.ReadTimeout = 3000;
            _serialPort.WriteTimeout = 3000;
        }

        public async Task WriteCommand(string Command)
        {
            Byte[] commandBuffer = Encoding.UTF8.GetBytes(Command + Environment.NewLine);

            await _serialPort.WriteAsync(commandBuffer, 0, commandBuffer.Length, CancellationToken.None);
            await Task.Delay(100);
        }

        public async Task<string> ReadBuffer()
        {
            int buffSize = _serialPort.ReadBufferSize;
            Byte[] buffer = new Byte[buffSize];
            int commandResultBytes = await _serialPort.ReadAsync(buffer, CancellationToken.None);

            if (commandResultBytes > 0)
            {
                return Encoding.Default.GetString(buffer, 0, commandResultBytes);
            }
            else
            {
                return "Buffer_Empty";
            }
        }

        public async Task<string> WriteCommandGetResult(string Command)
        {
            await WriteCommand(Command);

            return await ReadBuffer();
        }

        public bool OpenModemSerialConnection()
        {
            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public bool CloseModemSerialConnection()
        {
            try
            {
                _serialPort.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}
