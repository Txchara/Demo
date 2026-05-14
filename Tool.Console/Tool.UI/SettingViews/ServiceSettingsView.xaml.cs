using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Tool.Core.SysTools;

namespace Tool.UI.SettingViews;

public partial class ServiceSettingsView : UserControl
{
    private readonly RulesConfigService _configService = new();

    /// <summary>
    /// DataGrid 绑定的可观察集合，每行对应一条规则。
    /// </summary>
    private ObservableCollection<StartItemViewModel> _rules = new();

    public ServiceSettingsView()
    {
        InitializeComponent();
        ConfigPathTextBlock.Text = $"配置文件：{RulesConfigService.GetConfigFilePath()}";
        LoadRules();
    }

    private void LoadRules()
    {
        List<StartItem> items = _configService.Load();
        _rules = new ObservableCollection<StartItemViewModel>(
            items.Select(x => new StartItemViewModel(x)));
        RulesDataGrid.ItemsSource = _rules;
        StatusTextBlock.Text = $"已加载 {_rules.Count} 条规则";
    }

    private void RulesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DeleteButton.IsEnabled = RulesDataGrid.SelectedItem is StartItemViewModel;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        // 新增一条默认启用、两项限制均开启的空规则，用户在表格内直接编辑关键字。
        var newItem = new StartItemViewModel(new StartItem
        {
            Keyword = "新关键字",
            LimitPriority = true,
            LimitAffinity = true,
            Enabled = true
        });
        _rules.Add(newItem);
        RulesDataGrid.SelectedItem = newItem;
        RulesDataGrid.ScrollIntoView(newItem);
        StatusTextBlock.Text = "已添加新规则，请修改关键字后保存。";
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesDataGrid.SelectedItem is not StartItemViewModel selected)
        {
            return;
        }

        MessageBoxResult confirm = MessageBox.Show(
                $"确认删除关键字为 \"{selected.Keyword}\" 的规则？",
                "确认删除",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

        if (confirm != MessageBoxResult.OK)
        {
            return;
        }

        _rules.Remove(selected);
        StatusTextBlock.Text = "规则已删除，请保存以生效。";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            List<StartItem> items = _rules.Select(x => x.ToStartItem()).ToList();
            _configService.Save(items);
            StatusTextBlock.Text = $"已保存 {items.Count} 条规则。服务下一轮扫描时自动生效。";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"保存失败：{ex.Message}",
                "保存失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

/// <summary>
/// StartItem 的 UI 包装，实现 INotifyPropertyChanged 以支持 DataGrid 双向绑定。
/// 额外增加 Remark 字段供用户备注，不影响服务逻辑。
/// </summary>
public sealed class StartItemViewModel : INotifyPropertyChanged
{
    private string _keyword;
    private bool _limitPriority;
    private bool _limitAffinity;
    private bool _enabled;
    private string _remark;

    public StartItemViewModel(StartItem item)
    {
        _keyword = item.Keyword;
        _limitPriority = item.LimitPriority;
        _limitAffinity = item.LimitAffinity;
        _enabled = item.Enabled;
        _remark = string.Empty;
    }

    public string Keyword
    {
        get => _keyword;
        set { _keyword = value; OnPropertyChanged(); }
    }

    public bool LimitPriority
    {
        get => _limitPriority;
        set { _limitPriority = value; OnPropertyChanged(); }
    }

    public bool LimitAffinity
    {
        get => _limitAffinity;
        set { _limitAffinity = value; OnPropertyChanged(); }
    }

    public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; OnPropertyChanged(); }
    }

    public string Remark
    {
        get => _remark;
        set { _remark = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 将 ViewModel 转回 StartItem 用于持久化，Remark 不写入配置文件。
    /// </summary>
    public StartItem ToStartItem() => new StartItem
    {
        Keyword = Keyword.Trim(),
        LimitPriority = LimitPriority,
        LimitAffinity = LimitAffinity,
        Enabled = Enabled
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
