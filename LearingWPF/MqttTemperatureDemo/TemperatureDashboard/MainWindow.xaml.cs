using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace TemperatureDashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _mqttOptions;

    private const string TemperatureTopic = "factory/line1/machine1/temperature";
    private const string StatusTopic = "factory/line1/machine1/status";

    public MainWindow()
    {
        InitializeComponent();

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithClientId("wpf-temperature-dashboard")
            .WithCleanSession(false)
            .Build();

        ConfigureMqttEvents();
    }

    private void ConfigureMqttEvents()
    {
        _mqttClient.ConnectedAsync += async e =>
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ConnectionStatusText.Text = "Broker Status: Connected";
                AddLog("Connected to MQTT broker.");
            });

            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(TemperatureTopic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());

            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(StatusTopic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build());

            await Dispatcher.InvokeAsync(() =>
            {
                AddLog($"Subscribed to: {TemperatureTopic}");
                AddLog($"Subscribed to: {StatusTopic}");
            });
        };

        _mqttClient.DisconnectedAsync += async e =>
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ConnectionStatusText.Text = "Broker Status: Disconnected";
                AddLog("Disconnected from MQTT broker.");
            });

            await Task.Delay(TimeSpan.FromSeconds(5));

            try
            {
                await Dispatcher.InvokeAsync(() => AddLog("Trying to reconnect dashboard..."));
                await _mqttClient.ConnectAsync(_mqttOptions);
            }
            catch
            {
                await Dispatcher.InvokeAsync(() => AddLog("Reconnect failed."));
            }
        };

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            await Dispatcher.InvokeAsync(() => HandleMessage(topic, payload));
        };
    }

    private void HandleMessage(string topic, string payload)
    {
        if (topic == StatusTopic)
        {
            MachineStatusText.Text = $"Machine Status: {payload}";
            MachineStatusText.Foreground = payload == "ONLINE"
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;

            AddLog($"Status received: {payload}");
            return;
        }

        if (topic == TemperatureTopic)
        {
            try
            {
                var reading = JsonSerializer.Deserialize<TemperatureReading>(payload);
                if (reading == null) return;

                TemperatureText.Text = $"Temperature: {reading.Temperature} °C";

                if (reading.Temperature >= 85)
                {
                    AlertText.Text = "Alert: High temperature detected!";
                    AlertText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (reading.Temperature >= 75)
                {
                    AlertText.Text = "Alert: Warning temperature level.";
                    AlertText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    AlertText.Text = "Alert: Normal";
                    AlertText.Foreground = System.Windows.Media.Brushes.Green;
                }

                AddLog($"Temperature received: {reading.Temperature} °C at {reading.TimestampUtc:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                AddLog($"Invalid message received: {ex.Message}");
            }
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_mqttClient.IsConnected)
                await _mqttClient.ConnectAsync(_mqttOptions);
        }
        catch (Exception ex)
        {
            AddLog($"Connection failed: {ex.Message}");
        }
    }

    private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_mqttClient.IsConnected)
                await _mqttClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Disconnect failed: {ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        LogListBox.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} - {message}");
    }

    protected override async void OnClosed(EventArgs e)
    {
        if (_mqttClient.IsConnected)
            await _mqttClient.DisconnectAsync();

        base.OnClosed(e);
    }
}

public class TemperatureReading
{
    public string MachineId { get; set; } = "";
    public string LineId { get; set; } = "";
    public double Temperature { get; set; }
    public DateTime TimestampUtc { get; set; }
}