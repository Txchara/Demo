using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Tool.Core.SysTools;

namespace Tool.UI.Views;

public partial class StartupItemToolView : UserControl
{
    private readonly StartupItemTool _tool = new();
    private List<StartupItemViewModel> _cachedItems = new();

    // true = 当前展示已启用，false = 已禁用
    private bool _showEnabled = true;

    public StartupItemToolView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadCacheAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadCacheAsync();
    }

    private void ShowEnabledButton_Click(object sender, RoutedEventArgs e)
    {
        _showEnabled = true;
        ApplyView();
    }

    private void ShowDisabledButton_Click(object sender, RoutedEventArgs e)
    {
        _showEnabled = false;
        ApplyView();
    }

    private async Task LoadCacheAsync()
    {
        RefreshButton.IsEnabled = false;
        ShowEnabledButton.IsEnabled = false;
        ShowDisabledButton.IsEnabled = false;
        StartupDataGrid.IsEnabled = false;
        SummaryTextBlock.Text = "加载中...";

        try
        {
            List<StartupItemTool.StartUpItemInfo> items =
                await Task.Run(() => _tool.LoadAll());

            _cachedItems = items
                .Select(x => new StartupItemViewModel(x))
                .ToList();

            ApplyView();
        }
        catch
        {
            _cachedItems = new List<StartupItemViewModel>();
            StartupDataGrid.ItemsSource = null;
            SummaryTextBlock.Text = "加载失败";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            ShowEnabledButton.IsEnabled = true;
            ShowDisabledButton.IsEnabled = true;
            StartupDataGrid.IsEnabled = true;
        }
    }

    private void ApplyView()
    {
        List<StartupItemViewModel> view = _cachedItems
            .Where(x => x.Enabled == _showEnabled)
            .ToList();

        StartupDataGrid.ItemsSource = null;
        StartupDataGrid.ItemsSource = view;

        string label = _showEnabled ? "已启用" : "已禁用";
        SummaryTextBlock.Text = $"{label} {view.Count} 条";
    }

    private async void StartupDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (StartupDataGrid.SelectedItem is not StartupItemViewModel vm) return;

        if (vm.ReadOnly)
        {
            MessageBox.Show("该启动项需要管理员权限，无法操作。", "权限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (vm.Source is "用户文件夹" or "公共文件夹")
        {
            MessageBox.Show("文件夹来源的启动项暂不支持启用/禁用操作。", "不支持", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool targetState = !vm.Enabled;
        string action = targetState ? "启用" : "禁用";

        var confirm = MessageBox.Show(
                $"确认要{action}启动项\"{vm.Name}\"吗？",
                $"确认{action}",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

        if (confirm != MessageBoxResult.OK) return;

        RefreshButton.IsEnabled = false;
        ShowEnabledButton.IsEnabled = false;
        ShowDisabledButton.IsEnabled = false;
        StartupDataGrid.IsEnabled = false;
        SummaryTextBlock.Text = $"正在{action}...";

        Exception? error = null;
        try
        {
            await Task.Run(() => _tool.SetEnabled(new StartupItemTool.StartUpItemInfo
            {
                Name     = vm.Name,
                Path     = vm.Path,
                Source   = vm.Source,
                Enabled  = vm.Enabled,
                ReadOnly = vm.ReadOnly
            }, targetState));
        }
        catch (Exception ex)
        {
            error = ex;
        }

        await LoadCacheAsync();

        if(error is null)
            MessageBox.Show($"“{vm.Name}”已{action}。",action + "成功",MessageBoxButton.OK,MessageBoxImage.Information);
        else
            MessageBox.Show($"{action}失败：{error.Message}","错误",MessageBoxButton.OK,MessageBoxImage.Error);
    }
}

public sealed class StartupItemViewModel
{
    public string Name       { get; }
    public string Path       { get; }
    public string Source     { get; }
    public bool   Enabled    { get; }
    public bool   ReadOnly   { get; }
    public string StatusText => Enabled ? "已启用" : "已禁用";

    public StartupItemViewModel(StartupItemTool.StartUpItemInfo item)
    {
        Name     = item.Name;
        Path     = item.Path;
        Source   = item.Source;
        Enabled  = item.Enabled;
        ReadOnly = item.ReadOnly;
    }
}
