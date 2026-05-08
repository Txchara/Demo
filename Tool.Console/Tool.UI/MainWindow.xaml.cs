using System.Windows;
using System.Windows.Controls;
using Tool.UI.Views;

namespace Tool.UI;

public partial class MainWindow : Window
{
    private readonly PingToolView _pingToolView = new();
    private readonly SystemInfoToolView _systemInfoToolView = new();
    private readonly TemperatureToolView _temperatureToolView = new();

    public MainWindow()
    {
        InitializeComponent();
        ToolListBox.SelectedIndex = 0;
    }

    private void ToolListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ToolListBox.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        string? toolKey = item.Tag?.ToString();

        switch (toolKey)
        {
            case "Ping":
                ToolTitleTextBlock.Text = "Ping 工具";
                ToolDescriptionTextBlock.Text = "输入地址并执行网络连通性检测。";
                ToolContentHost.Content = _pingToolView;
                break;

            case "SystemInfo":
                ToolTitleTextBlock.Text = "系统信息";
                ToolDescriptionTextBlock.Text = "查看当前计算机的 CPU、内存、系统、显卡和磁盘信息。";
                ToolContentHost.Content = _systemInfoToolView;
                break;

            case "Temperature":
                ToolTitleTextBlock.Text = "温度监控";
                ToolDescriptionTextBlock.Text = "查看硬件温度、风扇、电压、频率和负载等传感器数据。";
                ToolContentHost.Content = _temperatureToolView;
                break;
        }
    }
}
