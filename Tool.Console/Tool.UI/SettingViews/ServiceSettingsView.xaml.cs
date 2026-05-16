using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tool.Core.SysTools;

namespace Tool.UI.SettingViews;

public partial class ServiceSettingsView : UserControl
{
    private const string ServiceName = "ProcessLimitService";

    private readonly RulesConfigService _configService = new();

    private ObservableCollection<StartItemViewModel> _rules = new();

    public ServiceSettingsView()
    {
        InitializeComponent();
        ConfigPathTextBlock.Text = $"c：{RulesConfigService.GetConfigFilePath()}";
        LoadRules();
        RefreshServiceStatus();
    }

    private void RefreshServiceStatus()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            ServiceControllerStatus status = sc.Status;

            bool running = status == ServiceControllerStatus.Running;
            bool stopped = status == ServiceControllerStatus.Stopped;

            ServiceStatusText.Text = status switch
            {
                ServiceControllerStatus.Running      => "运行中",
                ServiceControllerStatus.Stopped      => "已停止",
                ServiceControllerStatus.StartPending => "正在启动...",
                ServiceControllerStatus.StopPending  => "正在停止...",
                _                                    => status.ToString()
            };
            ServiceStatusText.Foreground = running
                ? new SolidColorBrush(Color.FromRgb(22, 163, 74))
                : new SolidColorBrush(Color.FromRgb(107, 114, 128));

            InstallServiceButton.IsEnabled   = false;
            UninstallServiceButton.IsEnabled = stopped;
            StartServiceButton.IsEnabled     = stopped;
            StopServiceButton.IsEnabled      = running;
        }
        catch (InvalidOperationException)
        {
            ServiceStatusText.Text = "未安装";
            ServiceStatusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));

            InstallServiceButton.IsEnabled   = true;
            UninstallServiceButton.IsEnabled = false;
            StartServiceButton.IsEnabled     = false;
            StopServiceButton.IsEnabled      = false;
        }
    }

    private async void InstallServiceButton_Click(object sender, RoutedEventArgs e)
    {
        // Tool.Service.exe 与 Tool.UI.exe 发布在同一目录
        string exePath = System.IO.Path.Combine(
            AppContext.BaseDirectory, "Tool.Service.exe");

        if (!System.IO.File.Exists(exePath))
        {
            MessageBox.Show(
                $"未找到服务程序：\n{exePath}\n\n请确认 Tool.Service.exe 与本程序在同一目录。",
                "安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SetServiceButtons(false);
        StatusTextBlock.Text = "正在安装服务...";

        (bool ok, string msg) = await Task.Run(() => RunSc(
            $"create {ServiceName} binPath= \"{exePath}\" start= auto DisplayName= \"进程限制服务\""));

        if (ok)
        {
            StatusTextBlock.Text = "服务安装成功。";
            MessageBox.Show("服务安装成功。", "安装成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            StatusTextBlock.Text = "安装失败。";
            MessageBox.Show($"安装失败：\n{msg}\n\n请以管理员身份运行本程序。",
                "安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        RefreshServiceStatus();
    }

    private async void UninstallServiceButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "确认卸载服务？卸载后进程限制功能将停止工作。",
            "确认卸载", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.OK) return;

        SetServiceButtons(false);
        StatusTextBlock.Text = "正在卸载服务...";

        (bool ok, string msg) = await Task.Run(() => RunSc($"delete {ServiceName}"));

        if (ok)
        {
            StatusTextBlock.Text = "服务已卸载。";
            MessageBox.Show("服务已卸载。", "卸载成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            StatusTextBlock.Text = "卸载失败。";
            MessageBox.Show($"卸载失败：\n{msg}\n\n请以管理员身份运行本程序。",
                "卸载失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        RefreshServiceStatus();
    }

    /// <summary>
    /// 执行 sc.exe 命令，返回 (成功, 输出信息)。
    /// sc.exe 成功时退出码为 0，失败时输出错误描述。
    /// </summary>
    private static (bool ok, string output) RunSc(string arguments)
    {
        using var p = new System.Diagnostics.Process();
        p.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "sc.exe",
            Arguments              = arguments,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };
        p.Start();
        string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode == 0, output.Trim());
    }

    private void SetServiceButtons(bool enabled)
    {
        InstallServiceButton.IsEnabled   = enabled;
        UninstallServiceButton.IsEnabled = enabled;
        StartServiceButton.IsEnabled     = enabled;
        StopServiceButton.IsEnabled      = enabled;
        RefreshServiceButton.IsEnabled   = enabled;
    }

    private void StartServiceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动服务失败：{ex.Message}\n\n请确认以管理员身份运行。",
                "启动失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            RefreshServiceStatus();
        }
    }

    private void StopServiceButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"停止服务失败：{ex.Message}",
                "停止失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            RefreshServiceStatus();
        }
    }

    private void RefreshServiceButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshServiceStatus();
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
