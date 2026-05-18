using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tool.Core.FileTools;

namespace Tool.UI.Views;

public partial class FileInfoToolView : UserControl
{
    private readonly FileInfoTool _fileInfoTool = new();

    public FileInfoToolView()
    {
        InitializeComponent();
    }

    private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        PathTextBox.Text = dialog.FileName;
        StatusTextBlock.Text = "已选择文件，点击查询。";
    }

    private async void QueryButton_Click(object sender, RoutedEventArgs e)
    {
        await QueryAsync();
    }

    private async Task QueryAsync()
    {
        string path = PathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusTextBlock.Text = "请先输入或选择路径。";
            return;
        }

        SetQueryState(false);
        StatusTextBlock.Text = "正在查询...";
        ResultTextBox.Text = string.Empty;

        try
        {
            FileInfoTool.FileInfoResult result = await Task.Run(() => _fileInfoTool.GetFileInfo(path));
            ResultTextBox.Text = BuildResultText(result);
            StatusTextBlock.Text = "查询完成。";
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"查询失败：{ex.Message}";
            StatusTextBlock.Text = "查询失败。";
        }
        finally
        {
            SetQueryState(true);
        }
    }

    private static string BuildResultText(FileInfoTool.FileInfoResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"名称          : {result.Name}");
        sb.AppendLine($"完整路径      : {result.FullPath}");
        sb.AppendLine($"类型          : {(result.IsDirectory ? "文件夹" : $"文件 ({result.Extension})")}");
        sb.AppendLine($"大小          : {result.FormattedSize}");
        sb.AppendLine();
        sb.AppendLine($"创建时间      : {result.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"修改时间      : {result.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"访问时间      : {result.AccessedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine($"只读          : {(result.IsReadOnly ? "是" : "否")}");
        sb.AppendLine($"隐藏          : {(result.IsHidden ? "是" : "否")}");
        sb.AppendLine($"系统          : {(result.IsSystem ? "是" : "否")}");

        return sb.ToString();
    }

    private void SetQueryState(bool enabled)
    {
        QueryButton.IsEnabled = enabled;
        BrowseFileButton.IsEnabled = enabled;
    }
}
