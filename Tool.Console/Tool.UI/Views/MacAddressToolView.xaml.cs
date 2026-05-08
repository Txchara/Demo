using System.Windows;
using System.Windows.Controls;
using Tool.Core.SysTools;

namespace Tool.UI.Views;

public partial class MacAddressToolView : UserControl
{
    public MacAddressToolView()
    {
        InitializeComponent();
        LoadMacAddressInfo();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadMacAddressInfo();
    }

    private void LoadMacAddressInfo()
    {
        RefreshButton.IsEnabled = false;

        try
        {
            var macAddressTool = new MacAddressTool();
            List<MacAddressInfo> result = macAddressTool.GetMacAddresses();

            if (result.Count == 0)
            {
                ResultTextBox.Text = "未获取到可用 MAC 地址";
                return;
            }

            var lines = new List<string>();

            foreach (MacAddressInfo item in result)
            {
                lines.Add($"名称         : {item.Name}");
                lines.Add($"描述         : {item.Description}");
                lines.Add($"类型         : {item.InterfaceType}");
                lines.Add($"MAC 地址     : {item.MacAddress}");
                lines.Add(string.Empty);
            }

            ResultTextBox.Text = string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"加载 MAC 地址失败：{ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }
}
