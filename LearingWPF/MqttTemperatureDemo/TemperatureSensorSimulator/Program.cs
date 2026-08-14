using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

Console.WriteLine("Starting Temperature Sensor Simulator...");

var factory = new MqttFactory();
var mqttClient = factory.CreateMqttClient();

var machineId = "machine1";
var lineId = "line1";

var temperatureTopic = $"factory/{lineId}/{machineId}/temperature";
var statusTopic = $"factory/{lineId}/{machineId}/status";

var options = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .WithClientId($"sensor-{machineId}")
    .WithCleanSession(false)
    .WithWillTopic(statusTopic)
    .WithWillPayload("OFFLINE")
    .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
    .WithWillRetain(true)
    .Build();

mqttClient.ConnectedAsync += async e =>
{
    Console.WriteLine("Sensor connected to MQTT broker.");

    var onlineMessage = new MqttApplicationMessageBuilder()
        .WithTopic(statusTopic)
        .WithPayload("ONLINE")
        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
        .WithRetainFlag(true)
        .Build();

    await mqttClient.PublishAsync(onlineMessage);
    Console.WriteLine($"Published ONLINE status to topic: {statusTopic}");
};

mqttClient.DisconnectedAsync += async e =>
{
    Console.WriteLine("Sensor disconnected from MQTT broker.");
    await Task.Delay(TimeSpan.FromSeconds(5));

    try
    {
        Console.WriteLine("Trying to reconnect sensor...");
        await mqttClient.ConnectAsync(options);
    }
    catch
    {
        Console.WriteLine("Reconnect failed.");
    }
};

await mqttClient.ConnectAsync(options);

var random = new Random();

while (true)
{
    var temperature = Math.Round(65 + random.NextDouble() * 30, 2);

    var payload = new TemperatureReading
    {
        MachineId = machineId,
        LineId = lineId,
        Temperature = temperature,
        TimestampUtc = DateTime.UtcNow
    };

    var json = JsonSerializer.Serialize(payload);

    var message = new MqttApplicationMessageBuilder()
        .WithTopic(temperatureTopic)
        .WithPayload(Encoding.UTF8.GetBytes(json))
        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
        .WithRetainFlag(false)
        .Build();

    await mqttClient.PublishAsync(message);
    Console.WriteLine($"Published: {json}");

    await Task.Delay(TimeSpan.FromSeconds(2));
}

public class TemperatureReading
{
    public string MachineId { get; set; } = "";
    public string LineId { get; set; } = "";
    public double Temperature { get; set; }
    public DateTime TimestampUtc { get; set; }
}