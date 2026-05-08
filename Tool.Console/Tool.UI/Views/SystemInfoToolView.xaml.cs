using System.Windows;
using System.Windows.Controls;
using Tool.Core.SysTools;

namespace Tool.UI.Views;

public partial class SystemInfoToolView : UserControl
{
    public SystemInfoToolView()
    {
        InitializeComponent();
        LoadSystemInfo();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadSystemInfo();
    }

    private void LoadSystemInfo()
    {
        RefreshButton.IsEnabled = false;

        try
        {
            var systemInfoTool = new SystemInfoTool();
            SystemInfoResult result = systemInfoTool.GetSystemInfo();

            var lines = new List<string>
            {
                $"CPU           : {result.CpuName}",
                $"内存(GB)      : {result.TotalMemoryGb:F2}",
                $"系统名称      : {result.OsName}",
                $"系统版本      : {result.OsVersion}",
                string.Empty,
                "显卡列表："
            };

            if (result.GpuNames.Count == 0)
            {
                lines.Add("- 未获取到显卡信息");
            }
            else
            {
                foreach (string gpu in result.GpuNames)
                {
                    lines.Add($"- {gpu}");
                }
            }

            lines.Add(string.Empty);
            lines.Add("磁盘列表：");

            if (result.Disks.Count == 0)
            {
                lines.Add("- 未获取到磁盘信息");
            }
            else
            {
                foreach (DiskInfo disk in result.Disks)
                {
                    lines.Add($"- {disk.Name} [{disk.Format}] 总容量: {disk.TotalSizeGb:F2} GB, 可用: {disk.FreeSizeGb:F2} GB");
                }
            }

            ResultTextBox.Text = string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"加载系统信息失败：{ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }
}
