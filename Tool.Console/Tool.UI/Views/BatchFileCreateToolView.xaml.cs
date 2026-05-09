using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tool.Core.FileTools;

namespace Tool.UI.Views;

public partial class BatchFileCreateToolView : UserControl
{
    private readonly CreateFileTool _createFileTool = new();
    private List<string> _importedFileNames = new();

    public BatchFileCreateToolView()
    {
        InitializeComponent();
        ExtensionTextBox.Text = ".txt";
        CountTextBox.Text = "0";
        UpdateImportedNames(Array.Empty<string>());
        UpdateCreateSummary(null);
    }

    private void GenerateTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel 表格 (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = "文件名称模板.xlsx",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _createFileTool.CreateTemplate(dialog.FileName);
            ExcelPathTextBox.Text = dialog.FileName;
            ImportStatusTextBlock.Text = "示例表格已生成，填好后再读取就可以。";
        }
        catch (Exception ex)
        {
            ImportStatusTextBlock.Text = $"生成示例表格失败：{ex.Message}";
        }
    }

    private void BrowseExcelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel 表格 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ExcelPathTextBox.Text = dialog.FileName;
        ImportStatusTextBlock.Text = "已选好 Excel 文件。";
    }

    private void UseExcelDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        string excelPath = ExcelPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(excelPath))
        {
            ImportStatusTextBlock.Text = "请先选择一个 Excel 文件。";
            return;
        }

        string? directory = Path.GetDirectoryName(excelPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            ImportStatusTextBlock.Text = "没找到这个 Excel 所在的文件夹。";
            return;
        }

        TargetDirectoryTextBox.Text = directory;
        ImportStatusTextBlock.Text = "已把保存位置设为 Excel 所在文件夹。";
    }

    private async void ImportNamesButton_Click(object sender, RoutedEventArgs e)
    {
        await ImportNamesAsync();
    }

    private async Task ImportNamesAsync()
    {
        string filePath = ExcelPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            ImportStatusTextBlock.Text = "请先选择 Excel 文件，或把路径填进去。";
            return;
        }

        SetImportState(false);
        ImportStatusTextBlock.Text = "正在读取 Excel，请稍等...";

        try
        {
            List<string> names = await Task.Run(() => _createFileTool.ReadFileNamesFromTemplate(filePath));
            _importedFileNames = names;
            UpdateImportedNames(names);

            if (names.Count > 0 && (!int.TryParse(CountTextBox.Text.Trim(), out int count) || count <= 0))
            {
                CountTextBox.Text = names.Count.ToString();
            }

            ImportStatusTextBlock.Text = $"已读到 {names.Count} 个可用的文件名。";
        }
        catch (Exception ex)
        {
            _importedFileNames = new List<string>();
            UpdateImportedNames(_importedFileNames);
            ImportStatusTextBlock.Text = $"读取失败：{ex.Message}";
        }
        finally
        {
            SetImportState(true);
        }
    }

    private async void CreateFilesButton_Click(object sender, RoutedEventArgs e)
    {
        await CreateFilesAsync();
    }

    private async Task CreateFilesAsync()
    {
        if (_importedFileNames.Count == 0)
        {
            CreateSummaryTextBlock.Text = "请先从 Excel 里读入文件名。";
            ResultTextBox.Text = "还没有可用的文件名。";
            return;
        }

        string targetDirectory = TargetDirectoryTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            CreateSummaryTextBlock.Text = "请先选择文件要保存到哪里。";
            return;
        }

        string extension = ExtensionTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(extension))
        {
            CreateSummaryTextBlock.Text = "请先填写文件类型，例如 .txt。";
            return;
        }

        if (!int.TryParse(CountTextBox.Text.Trim(), out int count) || count <= 0)
        {
            CreateSummaryTextBlock.Text = "“要创建几个”里请填大于 0 的数字。";
            return;
        }

        CreateFilesButton.IsEnabled = false;
        CreateSummaryTextBlock.Text = "正在生成文件，请稍等...";
        ResultTextBox.Text = "正在生成文件，请稍等...";

        try
        {
            var options = new CreateFileTool.BatchCreateFilesOptions
            {
                TargetDirectory = targetDirectory,
                Extension = extension,
                Count = count,
                FileNames = new List<string>(_importedFileNames)
            };

            CreateFileTool.BatchCreateFilesResult result =
                await Task.Run(() => _createFileTool.CreateFiles(options));

            UpdateCreateSummary(result);
            ResultTextBox.Text = BuildResultText(result);
        }
        catch (Exception ex)
        {
            CreateSummaryTextBlock.Text = "生成失败。";
            ResultTextBox.Text = $"生成失败：{ex.Message}";
        }
        finally
        {
            CreateFilesButton.IsEnabled = true;
        }
    }

    private void UpdateImportedNames(IReadOnlyList<string> names)
    {
        ImportedCountTextBlock.Text = names.Count == 0
            ? "还没有读到文件名"
            : $"已读到 {names.Count} 个文件名";
        PreviewListBox.ItemsSource = null;
        PreviewListBox.ItemsSource = names;
    }

    private void UpdateCreateSummary(CreateFileTool.BatchCreateFilesResult? result)
    {
        if (result is null)
        {
            CreateSummaryTextBlock.Text = "还没有开始生成。";
            return;
        }

        CreateSummaryTextBlock.Text =
            $"从 Excel 读到 {result.ImportedCount} 个，" +
            $"计划生成 {result.Requestedcount} 个，" +
            $"实际处理 {result.ActualProcessedCount} 个。";
    }

    private static string BuildResultText(CreateFileTool.BatchCreateFilesResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"从 Excel 读到 : {result.ImportedCount}");
        builder.AppendLine($"计划生成     : {result.Requestedcount}");
        builder.AppendLine($"实际处理     : {result.ActualProcessedCount}");
        builder.AppendLine();
        builder.AppendLine("已成功生成：");

        if (result.CreatedFilePaths.Count == 0)
        {
            builder.AppendLine("- 没有");
        }
        else
        {
            foreach (string path in result.CreatedFilePaths)
            {
                builder.AppendLine($"- {path}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("已跳过（文件已存在）：");

        if (result.SkippedExistingFilePaths.Count == 0)
        {
            builder.AppendLine("- 没有");
        }
        else
        {
            foreach (string path in result.SkippedExistingFilePaths)
            {
                builder.AppendLine($"- {path}");
            }
        }

        return builder.ToString();
    }

    private void SetImportState(bool enabled)
    {
        GenerateTemplateButton.IsEnabled = enabled;
        BrowseExcelButton.IsEnabled = enabled;
        ImportNamesButton.IsEnabled = enabled;
        UseExcelDirectoryButton.IsEnabled = enabled;
    }
}
