using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tool.Core.SysTools;

namespace Tool.UI.Views;

public partial class ProcessViewerToolView : UserControl
{
    private readonly ProcessViewerTool _processViewerTool = new();
    private List<ProcessInfo> _cachedProcesses = new();
    private bool _isSettingPriority;

    public ProcessViewerToolView()
    {
        InitializeComponent();
        SortFieldComboBox.SelectedIndex = 0;
        SortOrderComboBox.SelectedIndex = 0;
        ProcessDataGrid.MouseDoubleClick += ProcessDataGrid_MouseDoubleClick;
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

    private async void ProcessDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_isSettingPriority)
        {
            return;
        }

        DependencyObject source = e.OriginalSource as DependencyObject;
        if (source == null)
        {
            return;
        }

        DataGridRow row = ItemsControl.ContainerFromElement(ProcessDataGrid, source) as DataGridRow;
        if (row == null)
        {
            return;
        }

        ProcessInfo selected = ProcessDataGrid.SelectedItem as ProcessInfo;
        if (selected == null)
        {
            return;
        }

        MessageBoxResult confirmResult = MessageBox.Show(
            string.Format("是否将进程“{0}”(PID: {1}) 的优先级设置为最低？", selected.ProcessName, selected.ProcessId),
            "确认设置",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (confirmResult != MessageBoxResult.OK)
        {
            return;
        }

        await SetSelectedProcessPriorityAsync(selected);
    }

    private async Task LoadCacheAsync()
    {
        RefreshButton.IsEnabled = false;
        OnlyWithWindowCheckBox.IsEnabled = false;
        SortFieldComboBox.IsEnabled = false;
        SortOrderComboBox.IsEnabled = false;
        SearchTextBox.IsEnabled = false;
        ProcessDataGrid.IsEnabled = false;

        try
        {
            // 进程数量较多时放到后台线程读取，避免界面卡顿。
            _cachedProcesses = await Task.Run(() => _processViewerTool.GetProcesses());
            ApplyView();
        }
        catch
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
            ProcessDataGrid.IsEnabled = true;
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

    private async Task SetSelectedProcessPriorityAsync(ProcessInfo selected)
    {
        _isSettingPriority = true;
        RefreshButton.IsEnabled = false;
        OnlyWithWindowCheckBox.IsEnabled = false;
        SortFieldComboBox.IsEnabled = false;
        SortOrderComboBox.IsEnabled = false;
        SearchTextBox.IsEnabled = false;
        ProcessDataGrid.IsEnabled = false;

        try
        {
            // 设置优先级放到后台执行，避免界面线程被系统调用阻塞。
            SetProcessPriorityResult result = await Task.Run(() => _processViewerTool.SetLowestPriority(selected.ProcessId));

            if (result.Success)
            {
                MessageBox.Show(
                    $"设置成功。\n进程：{result.ProcessName}\nPID：{result.ProcessId}\n原优先级：{result.OldPriority}\n当前优先级：{result.NewPriority}",
                    "设置结果",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadCacheAsync();
                return;
            }

            MessageBox.Show(
                $"设置失败：{result.ErrorMessage}",
                "设置结果",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"设置失败：{ex.Message}",
                "设置结果",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isSettingPriority = false;
            RefreshButton.IsEnabled = true;
            OnlyWithWindowCheckBox.IsEnabled = true;
            SortFieldComboBox.IsEnabled = true;
            SortOrderComboBox.IsEnabled = true;
            SearchTextBox.IsEnabled = true;
            ProcessDataGrid.IsEnabled = true;
        }
    }
}