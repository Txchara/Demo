using System.Windows;
using System.Windows.Controls;
using Tool.Core.SysTools;

namespace Tool.UI.Views;

public partial class LocalIpToolView : UserControl
{
    public LocalIpToolView()
    {
        InitializeComponent();
        LoadLocalIpInfo();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadLocalIpInfo();
    }

    private void LoadLocalIpInfo()
    {
        RefreshButton.IsEnabled = false;

        try
        {
            var localIpTool = new LocalIpTool();
            List<LocalIpInfo> result = localIpTool.GetLocalIps();

            if (result.Count == 0)
            {
                ResultTextBox.Text = "未获取到可用本机 IP";
                return;
            }

            var lines = new List<string>();

            foreach (LocalIpInfo item in result)
            {
                lines.Add($"名称         : {item.Name}");
                lines.Add($"描述         : {item.Description}");
                lines.Add($"类型         : {item.InterfaceType}");
                lines.Add($"本机 IP      : {item.IpAddress}");
                lines.Add(string.Empty);
            }

            ResultTextBox.Text = string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"加载本机 IP 失败：{ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }
}
