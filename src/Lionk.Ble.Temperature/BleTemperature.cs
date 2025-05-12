using Lionk.Ble.Models;
using Lionk.Components.Temperature;
using Lionk.Core;
using Lionk.Core.Component;
using Newtonsoft.Json;

namespace Lionk.Ble.Temperature;

[NamedElement("BleTemperatureSensor", "A Ble temperature sensor")]
public class BleTemperature : BaseTemperatureSensor, ITemperatureSensor, IBatterySensor, IOnCharacteristicData
{
    private readonly Queue<double> _lastTemperatures = new();
    private readonly Queue<double> _lastVoltages = new();
    private const int MaxValues = 5;
    private BleService? _bleService;
    private Guid _bleServiceId;
    private string? _deviceAddress;
    private DateTime _lastTimestamp;
    private double _temperature;
    private double _voltage;
    private bool _alreadyRegistreRequested = false;

    public const string CommonName = "Lionk-Temp";
    private const string VersionServiceId = "00000005-7669-6163-616d-2d63616c6563";
    private const string VersionCharacteristicId = "00000006-7669-6163-616d-2d63616c6563";
    private const string DataServiceId = "00000007-7669-6163-616d-2d63616c6563";
    private const string DataCharacteristicId = "00000008-7669-6163-616d-2d63616c6563";

    public Guid BleServiceId
    {
        get => _bleServiceId;
        set => SetField(ref _bleServiceId, value);
    }

    [JsonIgnore]
    public BleService? BleService
    {
        get => _bleService;
        set
        {
            _bleService = value;
            if (_bleService is not null)
            {
                BleServiceId = _bleService.Id;
                Register();
            }
        }
    }

    public string DeviceAddress
    {
        get => _deviceAddress ?? string.Empty;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _deviceAddress = value;
                Register();
                SetField(ref _deviceAddress, value);
            }
        }
    }

    /// <inheritdoc/>
    public override void Measure()
    {
        if (_lastTemperatures.Count > 0)
        {
            var average = _lastTemperatures.Average();
            SetTemperature(average);
            Console.WriteLine($"Mesure moyenne : {average}°C");
        }
        base.Measure();
    }

    public double GetPercentage()
    {
        return _voltage - 3.5 / (4.2 - 3.5) * 100;
    }

    public double GetTemperature()
    {
        return _temperature;
    }

    public double GetVoltage()
    {
        return _voltage;
    }

    public bool IsLowVoltage()
    {
        return _voltage <= 3.6;
    }

    public void OnNewData(string uuid, byte[] data)
    {
        // byte payloadVersion = data[0];
        int rawTemperature = (data[1] << 8) | data[2];
        int rawVoltage = (data[3] << 8) | data[4];
        _temperature = rawTemperature / 10.0;
        _voltage = rawVoltage / 1000.0;

        if (_lastTemperatures.Count >= MaxValues)
            _lastTemperatures.Dequeue();
        _lastTemperatures.Enqueue(_temperature);

        if (_lastVoltages.Count >= MaxValues)
            _lastVoltages.Dequeue();
        _lastVoltages.Enqueue(_voltage);
    }

    public void OnRegistered()
    {
        if (_deviceAddress is not null)
        {
            _bleService?.Subscribe(_deviceAddress, DataServiceId, DataCharacteristicId, this);
        }
    }

    public void OnDisconnected()
    {
        Console.WriteLine("Disonnected");
    }

    public override bool CanExecute => _bleService is not null;

    public override void InitializeSubComponents(IComponentService? componentService = null)
    {
        if (componentService is not null && !_alreadyRegistreRequested)
        {
            BleService = (BleService?)componentService.GetInstanceById(_bleServiceId);
            Register();
        }
    }

    private void Register()
    {
        if (_bleService is null || string.IsNullOrEmpty(_deviceAddress))
        {
            return;
        }
        _bleService.RegisterDevice(_deviceAddress, this);
        _alreadyRegistreRequested = true;
    }
}