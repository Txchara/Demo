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
