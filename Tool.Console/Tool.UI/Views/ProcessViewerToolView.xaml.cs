using System.Windows;
using System.Windows.Controls;
using Tool.Core.SysTools;

namespace Tool.UI.Views;

public partial class ProcessViewerToolView : UserControl
{
    private readonly ProcessViewerTool _processViewerTool = new();
    private List<ProcessInfo> _cachedProcesses = new();

    public ProcessViewerToolView()
    {
        InitializeComponent();
        SortFieldComboBox.SelectedIndex = 0;
        SortOrderComboBox.SelectedIndex = 0;
        Loaded += ProcessViewerToolView_Loaded;
    }

    private async void ProcessViewerToolView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCacheAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadCacheAsync();
    }

    private void OnlyWithWindowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyView();
    }

    private void SortOption_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyView();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyView();
    }

    private async Task LoadCacheAsync()
    {
        RefreshButton.IsEnabled = false;
        OnlyWithWindowCheckBox.IsEnabled = false;
        SortFieldComboBox.IsEnabled = false;
        SortOrderComboBox.IsEnabled = false;
        SearchTextBox.IsEnabled = false;

        try
        {
            // 进程数量较多时放到后台线程读取，避免界面卡顿。
            _cachedProcesses = await Task.Run(() => _processViewerTool.GetProcesses());
            ApplyView();
        }
        catch (Exception ex)
        {
            _cachedProcesses = new List<ProcessInfo>();
            ProcessDataGrid.ItemsSource = null;
            SummaryTextBlock.Text = "当前共 0 个进程";
            SearchTextBox.Text = string.Empty;
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            OnlyWithWindowCheckBox.IsEnabled = true;
            SortFieldComboBox.IsEnabled = true;
            SortOrderComboBox.IsEnabled = true;
            SearchTextBox.IsEnabled = true;
        }
    }

    private void ApplyView()
    {
        IEnumerable<ProcessInfo> query = _cachedProcesses;

        if (OnlyWithWindowCheckBox.IsChecked == true)
        {
            query = query.Where(x => !string.IsNullOrWhiteSpace(x.MainWindowTitle));
        }

        string keyword = SearchTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.ProcessId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.ProcessName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        bool descending = SortOrderComboBox.SelectedIndex == 1;

        if (SortFieldComboBox.SelectedIndex == 1)
        {
            query = descending
                ? query.OrderByDescending(x => x.WorkingSetMb).ThenBy(x => x.ProcessId)
                : query.OrderBy(x => x.WorkingSetMb).ThenBy(x => x.ProcessId);
        }
        else
        {
            query = descending
                ? query.OrderByDescending(x => x.ProcessId)
                : query.OrderBy(x => x.ProcessId);
        }

        List<ProcessInfo> viewList = query.ToList();

        ProcessDataGrid.ItemsSource = null;
        ProcessDataGrid.ItemsSource = viewList;
        SummaryTextBlock.Text = $"当前共 {viewList.Count} 个进程";
    }
}
