using System.Windows;
using System.Windows.Controls;
using Tool.Core.NetTools;

namespace Tool.UI.Views;

public partial class PingToolView : UserControl
{
    public PingToolView()
    {
        InitializeComponent();
    }

    private async void PingButton_Click(object sender, RoutedEventArgs e)
    {
        string address = AddressTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(address))
        {
            ResultTextBox.Text = "地址不能为空。";
            return;
        }

        PingButton.IsEnabled = false;
        ResultTextBox.Text = "正在检测，请稍候...";

        try
        {
            var pingTool = new PingTool();
            PingResult result = await pingTool.PingAsync(address);

            ResultTextBox.Text =
                $"地址         : {result.Address}{Environment.NewLine}" +
                $"是否成功     : {result.Success}{Environment.NewLine}" +
                $"状态         : {result.Status}{Environment.NewLine}" +
                $"IP 地址      : {result.IpAddress ?? "-"}{Environment.NewLine}" +
                $"往返耗时(ms) : {result.RoundtripTime}";

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                ResultTextBox.Text += $"{Environment.NewLine}错误信息     : {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"检测失败：{ex.Message}";
        }
        finally
        {
            PingButton.IsEnabled = true;
        }
    }
}
