using System.Windows;
using System.Windows.Controls;
using Tool.Core.SysTools;

namespace Tool.UI.Views;

public partial class TemperatureToolView : UserControl
{
    public TemperatureToolView()
    {
        InitializeComponent();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadTemperatureInfoAsync();
    }

    private async Task LoadTemperatureInfoAsync()
    {
        RefreshButton.IsEnabled = false;
        ResultTextBox.Text = "正在读取温度数据...";

        try
        {
            var temperatureTool = new TemperatureTool();
            List<SensorReading> readings = await Task.Run(() => temperatureTool.GetSensorReadings());

            if (readings.Count == 0)
            {
                ResultTextBox.Text = "未获取到温度数据。";
                return;
            }

            var lines = readings
                .OrderBy(x => x.HardwareType)
                .ThenBy(x => x.SensorName)
                .Select(x => $"[{x.HardwareType}] {x.HardwareName} | {x.SensorName} = {x.Value:F2} {x.Unit}")
                .ToList();

            ResultTextBox.Text = string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"加载温度数据失败：{ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }
}
