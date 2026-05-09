using System.Windows;
using System.Windows.Controls;
using Tool.UI.Views;

namespace Tool.UI;

public partial class MainWindow : Window
{
    private readonly PingToolView _pingToolView = new();
    private readonly SystemInfoToolView _systemInfoToolView = new();
    private readonly TemperatureToolView _temperatureToolView = new();
    private readonly MacAddressToolView _macAddressToolView = new();
    private readonly LocalIpToolView _localIpToolView = new();
    private readonly BatchFileCreateToolView _batchFileCreateToolView = new();

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
                ToolDescriptionTextBlock.Text = "查看当前计算机可读取到的硬件温度信息。";
                ToolContentHost.Content = _temperatureToolView;
                break;

            case "MacAddress":
                ToolTitleTextBlock.Text = "MAC 地址";
                ToolDescriptionTextBlock.Text = "查看当前计算机已启用网卡的 MAC 地址信息。";
                ToolContentHost.Content = _macAddressToolView;
                break;

            case "LocalIp":
                ToolTitleTextBlock.Text = "本机 IP";
                ToolDescriptionTextBlock.Text = "查看当前计算机已启用网卡的本机 IPv4 地址信息。";
                ToolContentHost.Content = _localIpToolView;
                break;

            case "BatchFileCreate":
                ToolTitleTextBlock.Text = "批量创建文件";
                ToolDescriptionTextBlock.Text = "导入 Excel 文件名称列表，按顺序批量创建空文件。";
                ToolContentHost.Content = _batchFileCreateToolView;
                break;
        }
    }
}
