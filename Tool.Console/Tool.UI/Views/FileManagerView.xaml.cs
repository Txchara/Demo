using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tool.Core.FileTools;

namespace Tool.UI.Views;

public partial class FileManagerView : UserControl
{
    private readonly FileInfoTool _fileInfoTool = new();
    private readonly FileHashTool _hashTool = new();
    private CancellationTokenSource? _hashCts;
    private CancellationTokenSource? _detailCts;
    private CancellationTokenSource? _loadCts;

    private string? _selectedFilePath;

    public FileManagerView()
    {
        InitializeComponent();
        LoadDrives();
    }

    // ── 驱动器初始化 ──────────────────────────────────────────────

    private void LoadDrives()
    {
        DirectoryTreeView.Items.Clear();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            var node = CreateNode(drive.RootDirectory);
            DirectoryTreeView.Items.Add(node);
        }
    }

    // ── 目录节点模型 ──────────────────────────────────────────────

    private static DirectoryNode CreateNode(DirectoryInfo dir)
    {
        var node = new DirectoryNode(dir);
        // 占位子节点：展开时才真正加载
        try
        {
            if (dir.EnumerateDirectories().Any())
                node.Children.Add(DirectoryNode.Placeholder);
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        return node;
    }

    // ── TreeView 展开懒加载 ───────────────────────────────────────

    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is not TreeViewItem tvi) return;
        if (tvi.DataContext is not DirectoryNode node) return;
        if (node.Children.Count == 1 && node.Children[0] == DirectoryNode.Placeholder)
        {
            node.Children.Clear();
            try
            {
                foreach (var sub in node.Info.EnumerateDirectories().OrderBy(d => d.Name))
                    node.Children.Add(CreateNode(sub));
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
        e.Handled = true;
    }

    // ── TreeView 选中目录 → 加载文件列表 ─────────────────────────

    private async void DirectoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not DirectoryNode node) return;
        PathTextBox.Text = node.Info.FullName;
        await LoadFileListAsync(node.Info.FullName);
    }

    // ── 路径栏回车 ────────────────────────────────────────────────

    private async void PathTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await NavigateToAsync(PathTextBox.Text.Trim());
    }

    // ── 选择文件夹按钮 ────────────────────────────────────────────

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        // WPF .NET 6 无内置文件夹选择器，借用 OpenFileDialog 导航到目标目录后取其父路径
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择文件夹",
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "选择此文件夹",
        };
        if (dialog.ShowDialog() != true) return;
        string folder = System.IO.Path.GetDirectoryName(dialog.FileName)!;
        await NavigateToAsync(folder);
    }

    // ── 上一级按钮 ────────────────────────────────────────────────

    private async void GoUpButton_Click(object sender, RoutedEventArgs e)
    {
        string current = PathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(current)) return;
        string? parent = Directory.GetParent(current)?.FullName;
        if (parent != null)
            await NavigateToAsync(parent);
    }

    // ── 导航到指定路径 ────────────────────────────────────────────

    private async Task NavigateToAsync(string path)
    {
        if (!Directory.Exists(path)) return;
        PathTextBox.Text = path;
        await LoadFileListAsync(path);
    }

    // ── 加载文件列表 ──────────────────────────────────────────────

    private async Task LoadFileListAsync(string dirPath)
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        FileListDataGrid.ItemsSource = null;
        ClearDetail();

        List<FileListItem> items;
        try
        {
            items = await Task.Run(() =>
            {
                var result = new List<FileListItem>();
                var dir = new DirectoryInfo(dirPath);
                foreach (var sub in dir.EnumerateDirectories().OrderBy(d => d.Name))
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new FileListItem
                    {
                        Name = sub.Name, TypeLabel = "文件夹", SizeText = "",
                        ModifiedAt = sub.LastWriteTime, FullPath = sub.FullName, IsDirectory = true,
                    });
                }
                foreach (var file in dir.EnumerateFiles().OrderBy(f => f.Name))
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new FileListItem
                    {
                        Name = file.Name,
                        TypeLabel = string.IsNullOrEmpty(file.Extension) ? "文件" : file.Extension.TrimStart('.').ToUpper(),
                        SizeText = FormatSize(file.Length),
                        ModifiedAt = file.LastWriteTime, FullPath = file.FullName, IsDirectory = false,
                    });
                }
                return result;
            }, token);
        }
        catch (OperationCanceledException) { return; }
        catch (UnauthorizedAccessException) { DetailTextBox.Text = "无权限访问此目录。"; return; }
        catch (IOException ex) { DetailTextBox.Text = $"读取目录失败：{ex.Message}"; return; }

        if (token.IsCancellationRequested) return;
        FileListDataGrid.ItemsSource = items;
    }

    // ── DataGrid 选中文件 → 显示详情 ─────────────────────────────

    private async void FileListDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileListDataGrid.SelectedItem is not FileListItem item)
        {
            ClearDetail();
            return;
        }

        // 取消上一次未完成的详情加载
        _detailCts?.Cancel();
        _detailCts = new CancellationTokenSource();
        var token = _detailCts.Token;

        _selectedFilePath = item.IsDirectory ? null : item.FullPath;
        ComputeHashButton.IsEnabled = false;
        HashResultTextBox.Text = string.Empty;
        HashProgressBar.Value = 0;
        DetailTextBox.Text = "计算中...";

        FileInfoTool.FileInfoResult result;
        try
        {
            result = await Task.Run(() => _fileInfoTool.GetFileInfo(item.FullPath), token);
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                DetailTextBox.Text = $"获取信息失败：{ex.Message}";
            return;
        }

        if (token.IsCancellationRequested) return;

        DetailTextBox.Text = BuildDetailText(result);
        ComputeHashButton.IsEnabled = !item.IsDirectory;
    }

    // ── 计算哈希 ──────────────────────────────────────────────────

    private async void ComputeHashButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        _hashCts = new CancellationTokenSource();
        SetHashState(computing: true);
        HashResultTextBox.Text = "计算中...";
        HashProgressBar.Value = 0;

        var progress = new Progress<double>(v => HashProgressBar.Value = v * 100);
        var algo = Md5Radio.IsChecked == true
            ? FileHashTool.HashAlgorithmType.MD5
            : FileHashTool.HashAlgorithmType.SHA256;

        try
        {
            var result = await _hashTool.ComputeAsync(_selectedFilePath, algo, progress, _hashCts.Token);
            HashResultTextBox.Text = $"{result.Algorithm}:\n{result.HashHex}\n\n耗时：{result.Elapsed.TotalSeconds:F2}s";
            HashProgressBar.Value = 100;
        }
        catch (OperationCanceledException)
        {
            HashResultTextBox.Text = "已取消。";
            HashProgressBar.Value = 0;
        }
        catch (Exception ex)
        {
            HashResultTextBox.Text = $"计算失败：{ex.Message}";
        }
        finally
        {
            SetHashState(computing: false);
            _hashCts.Dispose();
            _hashCts = null;
        }
    }

    private void CancelHashButton_Click(object sender, RoutedEventArgs e)
    {
        _hashCts?.Cancel();
    }

    // ── 辅助方法 ──────────────────────────────────────────────────

    private void SetHashState(bool computing)
    {
        ComputeHashButton.IsEnabled = !computing && _selectedFilePath != null;
        CancelHashButton.IsEnabled = computing;
        Md5Radio.IsEnabled = !computing;
        Sha256Radio.IsEnabled = !computing;
    }

    private void ClearDetail()
    {
        DetailTextBox.Text = string.Empty;
        _selectedFilePath = null;
        ComputeHashButton.IsEnabled = false;
        HashResultTextBox.Text = string.Empty;
        HashProgressBar.Value = 0;
    }

    private static string BuildDetailText(FileInfoTool.FileInfoResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"名称      : {r.Name}");
        sb.AppendLine($"路径      : {r.FullPath}");
        sb.AppendLine($"类型      : {(r.IsDirectory ? "文件夹" : $"文件 ({r.Extension})")}");
        sb.AppendLine($"大小      : {r.FormattedSize}");
        sb.AppendLine($"修改时间  : {r.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"创建时间  : {r.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"只读      : {(r.IsReadOnly ? "是" : "否")}");
        sb.AppendLine($"隐藏      : {(r.IsHidden ? "是" : "否")}");
        return sb.ToString();
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024:F1} MB",
        _ => $"{bytes / 1024.0 / 1024 / 1024:F2} GB",
    };
}

// ── 目录树节点 ─────────────────────────────────────────────────────

public class DirectoryNode
{
    public static readonly DirectoryNode Placeholder = new(new DirectoryInfo("."));

    public DirectoryInfo Info { get; }
    public string Name => Info.Name.Length > 0 ? Info.Name : Info.FullName; // 驱动器根目录名为空
    public ObservableCollection<DirectoryNode> Children { get; } = new();

    public DirectoryNode(DirectoryInfo info) => Info = info;
}

// ── 文件列表行 ─────────────────────────────────────────────────────

public class FileListItem
{
    public string Name { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string SizeText { get; init; } = string.Empty;
    public DateTime ModifiedAt { get; init; }
    public string FullPath { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
}
