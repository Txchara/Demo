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

    /// <summary>
    /// 单击行时同步更新"限制选中进程"按钮的可用状态。
    /// </summary>
    private void ProcessDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyLimitsButton.IsEnabled = ProcessDataGrid.SelectedItem is ProcessInfo && !_isSettingPriority;
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
                $"是否将进程 \"{selected.ProcessName}\" (PID: {selected.ProcessId}) 的优先级设置为最低？",
                "确认设置",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

        if (confirmResult != MessageBoxResult.OK)
        {
            return;
        }

        await SetSelectedProcessPriorityAsync(selected);
    }

    /// <summary>
    /// 点击"限制选中进程"按钮：弹确认框后异步执行优先级 + CPU 亲和性限制，完成后弹结果提示。
    /// </summary>
    private async void ApplyLimitsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSettingPriority)
        {
            return;
        }

        ProcessInfo selected = ProcessDataGrid.SelectedItem as ProcessInfo;
        if (selected == null)
        {
            return;
        }

        MessageBoxResult confirmResult = MessageBox.Show(
                $"即将对进程 \"{selected.ProcessName}\" (PID: {selected.ProcessId}) 执行以下操作：\n\n" +
                "  · 优先级 → 空闲（最低）\n" +
                "  · CPU 相关性 → 仅最后一个核心\n\n" +
                "确认继续？",
                "确认限制",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

        if (confirmResult != MessageBoxResult.OK)
        {
            return;
        }

        await ApplyLimitsAsync(selected);
    }

    private async Task LoadCacheAsync()
    {
        RefreshButton.IsEnabled = false;
        ApplyLimitsButton.IsEnabled = false;
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
            SortFieldComboBox.IsEnabled = true;
            SortOrderComboBox.IsEnabled = true;
            SearchTextBox.IsEnabled = true;
            ProcessDataGrid.IsEnabled = true;
            // 刷新后重新判断是否有选中行
            ApplyLimitsButton.IsEnabled = ProcessDataGrid.SelectedItem is ProcessInfo && !_isSettingPriority;
        }
    }

    private void ApplyView()
    {
        IEnumerable<ProcessInfo> query = _cachedProcesses;

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
        ApplyLimitsButton.IsEnabled = false;
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
            SortFieldComboBox.IsEnabled = true;
            SortOrderComboBox.IsEnabled = true;
            SearchTextBox.IsEnabled = true;
            ProcessDataGrid.IsEnabled = true;
            ApplyLimitsButton.IsEnabled = ProcessDataGrid.SelectedItem is ProcessInfo;
        }
    }

    /// <summary>
    /// 异步执行优先级 + CPU 亲和性组合限制，完成后弹出详细结果。
    /// 后台线程执行系统调用，不阻塞 UI 线程。
    /// </summary>
    private async Task ApplyLimitsAsync(ProcessInfo selected)
    {
        _isSettingPriority = true;
        RefreshButton.IsEnabled = false;
        ApplyLimitsButton.IsEnabled = false;
        SortFieldComboBox.IsEnabled = false;
        SortOrderComboBox.IsEnabled = false;
        SearchTextBox.IsEnabled = false;
        ProcessDataGrid.IsEnabled = false;

        try
        {
            // 两项系统调用均在后台线程执行，避免阻塞 UI。
            ApplyLimitsResult result = await Task.Run(() => _processViewerTool.ApplyLimits(selected.ProcessId));

            // 拼接结果文本，两项操作各自独立展示，方便定位哪一步失败。
            string priorityLine = result.PriorityResult.Success
                ? $"优先级：{result.PriorityResult.OldPriority} → {result.PriorityResult.NewPriority}  ✔"
                : $"优先级：设置失败 — {result.PriorityResult.ErrorMessage}  ✘";

            string affinityLine = result.AffinityResult.Success
                ? $"CPU 相关性：{result.AffinityResult.OldAffinityText} → {result.AffinityResult.NewAffinityText}（共 {result.AffinityResult.CoreCount} 核）  ✔"
                : $"CPU 相关性：设置失败 — {result.AffinityResult.ErrorMessage}  ✘";

            MessageBoxImage icon = result.FullSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning;
            string title = result.FullSuccess ? "限制成功" : "部分操作失败";

            MessageBox.Show(
                $"进程：{result.ProcessName}  PID：{result.ProcessId}\n\n{priorityLine}\n{affinityLine}",
                title,
                MessageBoxButton.OK,
                icon);

            // 操作完成后刷新列表，使优先级列显示最新值。
            await LoadCacheAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"操作异常：{ex.Message}",
                "限制失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isSettingPriority = false;
            RefreshButton.IsEnabled = true;
            SortFieldComboBox.IsEnabled = true;
            SortOrderComboBox.IsEnabled = true;
            SearchTextBox.IsEnabled = true;
            ProcessDataGrid.IsEnabled = true;
            ApplyLimitsButton.IsEnabled = ProcessDataGrid.SelectedItem is ProcessInfo;
        }
    }
}
