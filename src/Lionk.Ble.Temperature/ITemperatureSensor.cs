namespace Lionk.Ble.Temperature;

interface ITemperatureSensor
{
    /// <summary>
    /// Method to get the temperature of the sensor.
    /// </summary>
    /// <param name="nbDecimal"> Number of decimal to display</param>
    /// <returns> The temperature of the sensor</returns>
    double GetTemperature(int nbDecimal = 2);
}
