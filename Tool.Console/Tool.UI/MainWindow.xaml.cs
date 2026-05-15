using System.Windows;
using System.Windows.Controls;
using Tool.UI.SettingViews;
using Tool.UI.Views;

namespace Tool.UI;

public partial class MainWindow : Window
{
    private readonly PingToolView _pingToolView = new();
    private readonly SystemInfoToolView _systemInfoToolView = new();
    private readonly TemperatureToolView _temperatureToolView = new();
    private readonly MacAddressToolView _macAddressToolView = new();
    private readonly LocalIpToolView _localIpToolView = new();
    private readonly ProcessViewerToolView _processViewerToolView = new();
    private readonly BatchFileCreateToolView _batchFileCreateToolView = new();
    private readonly StartupItemToolView _startupItemToolView = new();
    private readonly ServiceSettingsView _serviceSettingsView = new();

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
                ToolContentHost.Content = _pingToolView;
                break;

            case "SystemInfo":
                ToolContentHost.Content = _systemInfoToolView;
                break;

            case "Temperature":
                ToolContentHost.Content = _temperatureToolView;
                break;

            case "MacAddress":
                ToolContentHost.Content = _macAddressToolView;
                break;

            case "LocalIp":
                ToolContentHost.Content = _localIpToolView;
                break;

            case "ProcessViewer":
                ToolContentHost.Content = _processViewerToolView;
                break;

            case "BatchFileCreate":
                ToolContentHost.Content = _batchFileCreateToolView;
                break;

            case "StartupItem":
                ToolContentHost.Content = _startupItemToolView;
                break;
        }
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // 取消导航栏选中状态，避免和设置页产生视觉冲突。
        ToolListBox.SelectedItem = null;
        ToolContentHost.Content = _serviceSettingsView;
    }
}
