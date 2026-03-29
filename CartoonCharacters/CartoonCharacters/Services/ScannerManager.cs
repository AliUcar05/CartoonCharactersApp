using System;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Runtime.Versioning;

namespace CartoonCharacters.Services;

public class ScannerManager
{
    private SerialPort? _mySerialPort;

    public QueueBuffer SerialBuffer { get; } = new();

    public void OpenPort()
    {
        ClosePort();

        string? portDetected = DetectPort();

        if (string.IsNullOrWhiteSpace(portDetected))
            throw new InvalidOperationException("No device found ...");

        _mySerialPort = new SerialPort
        {
            BaudRate = 9600,
            PortName = portDetected,
            Parity = Parity.None,
            DataBits = 8,
            StopBits = StopBits.One,
            ReadTimeout = 10000,
            WriteTimeout = 10000
        };

        _mySerialPort.DataReceived += DataHandler;

        try
        {
            _mySerialPort.Open();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }

    public void ClosePort()
    {
        if (_mySerialPort == null || !_mySerialPort.IsOpen)
            return;

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

    private static string? DetectPort()
    {
        if (OperatingSystem.IsWindows())
            return DetectWindowsPort();

        if (OperatingSystem.IsLinux())
            return DetectLinuxPort();

        return null;
    }

    [SupportedOSPlatform("windows")]
    private static string? DetectWindowsPort()
    {
        using var searcher =
            new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

        foreach (var baseObject in searcher.Get())
        {
            if (baseObject is not ManagementObject queryObj)
                continue;

            string id = queryObj["PNPDeviceID"]?.ToString() ?? string.Empty;
            string name = queryObj["Name"]?.ToString() ?? string.Empty;

            if (!id.Contains("PID_A4A7", StringComparison.OrdinalIgnoreCase))
                continue;

            int start = name.LastIndexOf("COM", StringComparison.Ordinal);
            int end = name.LastIndexOf(")", StringComparison.Ordinal);

            if (start >= 0 && end > start)
                return name.Substring(start, end - start);
        }

        return null;
    }

    private static string? DetectLinuxPort()
    {
        const string byId = "/dev/serial/by-id";

        if (!Directory.Exists(byId))
            return null;

        foreach (var device in Directory.GetFiles(byId))
        {
            if (device.Contains("20080411", StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(device);
        }

        return null;
    }

    private void DataHandler(object sender, SerialDataReceivedEventArgs e)
    {
        _ = e;

        if (sender is not SerialPort sp)
            return;

        SerialBuffer.Enqueue(sp.ReadExisting());
    }

    public sealed class QueueBuffer : Queue
    {
        public event EventHandler? Changed;

        public override void Enqueue(object? obj)
        {
            base.Enqueue(obj);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}