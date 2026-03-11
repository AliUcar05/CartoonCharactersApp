using System;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace CartoonCharacters.Services;

public partial class ScannerManager
{
    private SerialPort? _mySerialPort;
    private string? _portDetected = null;
    public QueueBuffer SerialBuffer = new();
    
    public void OpenPort()
    {
        if (_mySerialPort != null)
        {
            _mySerialPort.Dispose();
            _mySerialPort = null;
        }
        
        if (OperatingSystem.IsWindows())
        {
            var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

            foreach (System.Management.ManagementObject queryObj in searcher.Get())
            {
                string id = queryObj["PNPDeviceID"]?.ToString() ?? "";
                string nom = queryObj["Name"]?.ToString() ?? "";

                if (id.Contains("PID_A4A7"))
                {
                    int debut = nom.LastIndexOf("COM");
                    int fin = nom.LastIndexOf(")");

                    if (debut != -1 && fin != -1)
                    {
                        _portDetected = nom.Substring(debut, fin - debut);
                        break;
                    }
                }
            }
        }else if (OperatingSystem.IsLinux())
        {
            string byId = "/dev/serial/by-id";

            if (Directory.Exists(byId))
            {
                foreach (var device in Directory.GetFiles(byId))
                {
                    if (device.Contains("20080411", StringComparison.OrdinalIgnoreCase))
                    {
                        _portDetected = Path.GetFullPath(device);
                        break;
                    }
                }
            }
        }

        if (_portDetected == null) throw new InvalidOperationException("No device found ...");
        
        if (_portDetected != null)
        {
            _mySerialPort = new SerialPort
            {
                BaudRate = 9600,
                PortName = _portDetected,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                ReadTimeout = 10000,
                WriteTimeout = 10000
            };

            _mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataHandler);

            try
            {
                _mySerialPort.Open();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
    }
    public void ClosePort()
    {
        if (_mySerialPort != null && _mySerialPort.IsOpen)
        {
            try
            {
                _mySerialPort.Close();
                _mySerialPort.Dispose();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
            finally
            {
                _mySerialPort = null;
            }
        }
    }
    private void DataHandler(object sender, EventArgs arg)
    {
        SerialPort sp = (SerialPort)sender;

        SerialBuffer.Enqueue(sp.ReadExisting());
    }
    
    public sealed partial class QueueBuffer : Queue
    {
        public event EventHandler? Changed;
        public override void Enqueue(object? obj)
        { 
            base.Enqueue(obj);
            Changed?.Invoke(this,EventArgs.Empty);
        }    
    }
}