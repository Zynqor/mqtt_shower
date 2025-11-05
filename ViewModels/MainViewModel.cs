using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MqttMonitor.Services;
using MqttMonitor.Views;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly MqttService _mqttService;
    private readonly DataProcessingService _dataProcessingService;
    private readonly LogService _logService;
    private readonly CsvDataStorageService _csvStorageService;
    private readonly EncryptionService _encryptionService;
    private string _connectionStatusText = "未连接";
    private bool _isConnecting = false;
    private int _selectedTabIndex = 0;
    private string _newTopic = string.Empty;
    private string _title = "Mqtt Monitor";

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 已订阅的 Topics
    /// </summary>
    public ObservableCollection<string> SubscribedTopics => _mqttService.SortedActiveSubscriptions;

    /// <summary>
    /// 新 Topic 输入
    /// </summary>
    public string NewTopic
    {
        get => _newTopic;
        set
        {
            if (_newTopic != value)
            {
                _newTopic = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 连接状态文本
    /// </summary>
    public string ConnectionStatusText
    {
        get => _connectionStatusText;
        private set
        {
            if (_connectionStatusText != value)
            {
                _connectionStatusText = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否正在连接中
    /// </summary>
    public bool IsConnecting
    {
        get => _isConnecting;
        private set
        {
            if (_isConnecting != value)
            {
                _isConnecting = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 选中的 Tab 索引
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_selectedTabIndex != value)
            {
                _selectedTabIndex = value;
                OnPropertyChanged();
            }
        }
    }

    // 命令
    public ICommand ExitCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ShowChartSettingsCommand { get; }
    public ICommand ShowContactUsCommand { get; }
    public ICommand ClearDataCommand { get; }
    public ICommand ShowChartViewCommand { get; }
    public ICommand ShowTableViewCommand { get; }
    public ICommand ShowLogViewCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SubscribeTopicCommand { get; }
    public ICommand UnsubscribeTopicCommand { get; }

    public MainViewModel(MqttService mqttService, DataProcessingService dataProcessingService, LogService logService, CsvDataStorageService csvStorageService, EncryptionService encryptionService)
    {
        _mqttService = mqttService;
        _dataProcessingService = dataProcessingService;
        _logService = logService;
        _csvStorageService = csvStorageService;
        _encryptionService = encryptionService;

        // 订阅 MQTT 服务的属性变化
        _mqttService.PropertyChanged += OnMqttServicePropertyChanged;

        // 初始化命令
        ExitCommand = new RelayCommand(OnExit);
        ShowSettingsCommand = new RelayCommand(OnShowSettings);
        ShowChartSettingsCommand = new RelayCommand(OnShowChartSettings);
        ShowContactUsCommand = new RelayCommand(OnShowContactUs);
        ClearDataCommand = new RelayCommand(OnClearData);
        ShowChartViewCommand = new RelayCommand(() => SelectedTabIndex = 0);
        ShowTableViewCommand = new RelayCommand(() => SelectedTabIndex = 1);
        ShowLogViewCommand = new RelayCommand(() => SelectedTabIndex = 2);
        ConnectCommand = new RelayCommand(OnConnect, CanConnect);
        DisconnectCommand = new RelayCommand(OnDisconnect, CanDisconnect);
        SubscribeTopicCommand = new RelayCommand(OnSubscribeTopic);
        UnsubscribeTopicCommand = new RelayCommand<string>(OnUnsubscribeTopic);

        // 初始化连接状态
        UpdateConnectionStatus();

        // 从配置文件加载已订阅的 Topics
        LoadSubscribedTopicsFromConfig();
    }

    /// <summary>
    /// 当 MQTT 服务属性变化时
    /// </summary>
    private void OnMqttServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MqttService.CurrentState))
        {
            UpdateConnectionStatus();

            // 更新命令的 CanExecute 状态
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                (ConnectCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (DisconnectCommand as RelayCommand)?.NotifyCanExecuteChanged();
            });
        }
        else if (e.PropertyName == nameof(MqttService.SortedActiveSubscriptions))
        {
            OnPropertyChanged(nameof(SubscribedTopics));
        }
    }

    /// <summary>
    /// 更新连接状态文本和连接中标志
    /// </summary>
    private void UpdateConnectionStatus()
    {
        var state = _mqttService.CurrentState;
        ConnectionStatusText = state switch
        {
            ConnectionState.Disconnected => "未连接",
            ConnectionState.Connecting => "连接中...",
            ConnectionState.Connected => "已连接",
            ConnectionState.Reconnecting => "重新连接中...",
            ConnectionState.Error => "连接错误",
            _ => "未知状态"
        };

        IsConnecting = state == ConnectionState.Connecting || state == ConnectionState.Reconnecting;
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    private void OnExit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// 显示设置窗口
    /// </summary>
    private void OnShowSettings()
    {
        var settingsWindow = App.ServiceProvider?.GetService<SettingsWindow>();
        if (settingsWindow != null)
        {
            settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
            settingsWindow.ShowDialog();
        }
    }

    /// <summary>
    /// 显示图表设置窗口
    /// </summary>
    private void OnShowChartSettings()
    {
        var chartSettingsWindow = App.ServiceProvider?.GetService<ChartSettingsWindow>();
        if (chartSettingsWindow != null)
        {
            chartSettingsWindow.Owner = System.Windows.Application.Current.MainWindow;
            chartSettingsWindow.ShowDialog();
        }
    }

    /// <summary>
    /// 显示联系我们窗口
    /// </summary>
    private void OnShowContactUs()
    {
        var contactUsWindow = App.ServiceProvider?.GetService<ContactUsWindow>();
        if (contactUsWindow != null)
        {
            contactUsWindow.Owner = System.Windows.Application.Current.MainWindow;
            contactUsWindow.ShowDialog();
        }
    }

    /// <summary>
    /// 连接到 MQTT 服务器
    /// </summary>
    private async void OnConnect()
    {
        try
        {
            var settingsViewModel = App.ServiceProvider?.GetService<SettingsViewModel>();
            var settings = settingsViewModel?.LoadSettingsFromFile();

            if (settings == null)
            {
                System.Windows.MessageBox.Show("请先配置 MQTT 连接设置", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                OnShowSettings();
                return;
            }

            // 启用CSV数据存储
            var csvEnabled = await _csvStorageService.EnableAsync();
            if (!csvEnabled)
            {
                var result = System.Windows.MessageBox.Show(
                    "无法启用CSV数据存储，可能是数据目录被占用或无权限访问。\n\n是否继续连接？",
                    "警告",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }
            }

            await _mqttService.ConnectAsync(
                settings.Server,
                settings.Port,
                settings.Username,
                settings.Password,
                settings.ClientId);

            // 重新加载并订阅配置文件中的所有 Topics
            // 这样确保每次连接都使用最新的配置
            if (settings.SubscribedTopics != null && settings.SubscribedTopics.Count > 0)
            {
                _logService.LogInfo($"准备订阅 {settings.SubscribedTopics.Count} 个主题");
                foreach (var topic in settings.SubscribedTopics.OrderBy(t => t))
                {
                    await _mqttService.SubscribeAsync(topic);
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "连接 MQTT 服务器失败");
            System.Windows.MessageBox.Show($"连接失败：{ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 断开 MQTT 连接
    /// </summary>
    private async void OnDisconnect()
    {
        try
        {
            await _mqttService.DisconnectAsync();

            // 禁用CSV数据存储，释放文件权限
            await _csvStorageService.DisableAsync();

            _logService.LogInfo("已断开MQTT连接，CSV文件已关闭");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "断开连接失败");
        }
    }

    /// <summary>
    /// 订阅 Topic
    /// </summary>
    private async void OnSubscribeTopic()
    {
        try
        {
            // 检查是否输入了Topic
            if (string.IsNullOrWhiteSpace(NewTopic))
            {
                var message = "请输入要订阅的 Topic";
                _logService.LogWarning(message);
                System.Windows.MessageBox.Show(message, "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 检查MQTT连接状态
            if (_mqttService.CurrentState != ConnectionState.Connected)
            {
                var message = $"MQTT未连接，当前状态：{ConnectionStatusText}。请先连接MQTT服务器。";
                _logService.LogWarning(message);
                System.Windows.MessageBox.Show(message, "无法订阅",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 检查是否已订阅
            if (SubscribedTopics.Contains(NewTopic))
            {
                var message = $"已订阅 Topic: {NewTopic}";
                _logService.LogInfo(message);
                System.Windows.MessageBox.Show(message, "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            // 订阅Topic
            await _mqttService.SubscribeAsync(NewTopic);

            // 保存到配置
            SaveSubscribedTopics();

            _logService.LogInfo($"成功订阅 Topic: {NewTopic}");
            NewTopic = string.Empty;
        }
        catch (Exception ex)
        {
            var message = $"订阅 Topic 失败：{ex.Message}";
            _logService.LogException(ex, "订阅 Topic 失败");
            System.Windows.MessageBox.Show(message, "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 取消订阅 Topic
    /// </summary>
    private async void OnUnsubscribeTopic(string? topic)
    {
        try
        {
            if (string.IsNullOrEmpty(topic))
            {
                return;
            }

            // 检查MQTT连接状态
            if (_mqttService.CurrentState != ConnectionState.Connected)
            {
                var message = $"MQTT未连接，当前状态：{ConnectionStatusText}。请先连接MQTT服务器。";
                _logService.LogWarning(message);
                System.Windows.MessageBox.Show(message, "无法取消订阅",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            await _mqttService.UnsubscribeAsync(topic);

            // 保存到配置
            SaveSubscribedTopics();

            _logService.LogInfo($"已取消订阅 Topic: {topic}");
        }
        catch (Exception ex)
        {
            var message = $"取消订阅失败：{ex.Message}";
            _logService.LogException(ex, "取消订阅 Topic 失败");
            System.Windows.MessageBox.Show(message, "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 保存已订阅的 Topics 到配置文件
    /// </summary>
    private void SaveSubscribedTopics()
    {
        try
        {
            var settingsViewModel = App.ServiceProvider?.GetService<SettingsViewModel>();
            var settings = settingsViewModel?.LoadSettingsFromFile();
            if (settings != null)
            {
                settings.SubscribedTopics = new ObservableCollection<string>(_mqttService.SortedActiveSubscriptions);

                // 加密密码（如果有密码且未加密）
                if (!string.IsNullOrEmpty(settings.Password) &&
                    !_encryptionService.IsEncrypted(settings.Password))
                {
                    settings.Password = _encryptionService.Encrypt(settings.Password);
                }

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText("config.json", json);
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存订阅列表失败");
        }
    }

    // CanExecute 方法
    private bool CanConnect() => _mqttService.CurrentState == ConnectionState.Disconnected;
    private bool CanDisconnect() => _mqttService.CurrentState == ConnectionState.Connected;

    /// <summary>
    /// 清空所有数据
    /// </summary>
    private void OnClearData()
    {
        var result = System.Windows.MessageBox.Show(
            "确定要清空所有数据吗？",
            "确认",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _dataProcessingService.ClearAllData();
        }
    }

    /// <summary>
    /// 恢复窗口状态
    /// </summary>
    public void RestoreWindowState(System.Windows.Window window)
    {
        try
        {
            var settingsViewModel = App.ServiceProvider?.GetService<SettingsViewModel>();
            var settings = settingsViewModel?.LoadSettingsFromFile();
            if (settings != null)
            {
                Title = settings.Title;
                // 恢复窗口大小
                if (settings.WindowWidth > 0)
                    window.Width = settings.WindowWidth;
                if (settings.WindowHeight > 0)
                    window.Height = settings.WindowHeight;

                // 恢复窗口位置
                if (!double.IsNaN(settings.WindowLeft) && !double.IsNaN(settings.WindowTop))
                {
                    window.Left = settings.WindowLeft;
                    window.Top = settings.WindowTop;
                }

                // 恢复窗口状态
                if (Enum.TryParse<System.Windows.WindowState>(settings.WindowState, out var windowState))
                {
                    window.WindowState = windowState;
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "恢复窗口状态失败");
        }
    }

    /// <summary>
    /// 保存窗口状态
    /// </summary>
    public void SaveWindowState(System.Windows.Window window)
    {
        try
        {
            var settingsViewModel = App.ServiceProvider?.GetService<SettingsViewModel>();
            var settings = settingsViewModel?.LoadSettingsFromFile();
            if (settings != null)
            {
                // 保存窗口大小和位置（仅在非最小化和非最大化时保存）
                if (window.WindowState == System.Windows.WindowState.Normal)
                {
                    settings.WindowWidth = window.Width;
                    settings.WindowHeight = window.Height;
                    settings.WindowLeft = window.Left;
                    settings.WindowTop = window.Top;
                }

                settings.WindowState = window.WindowState.ToString();
                // 不修改订阅列表，保留原有配置
                // settings.SubscribedTopics 保持不变

                // 加密密码（如果有密码且未加密）
                if (!string.IsNullOrEmpty(settings.Password) &&
                    !_encryptionService.IsEncrypted(settings.Password))
                {
                    settings.Password = _encryptionService.Encrypt(settings.Password);
                }

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText("config.json", json);
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存窗口状态失败");
        }
    }

    /// <summary>
    /// 从配置文件加载已订阅的 Topics
    /// </summary>
    private void LoadSubscribedTopicsFromConfig()
    {
        try
        {
            var settingsViewModel = App.ServiceProvider?.GetService<SettingsViewModel>();
            var settings = settingsViewModel?.LoadSettingsFromFile();
            if (settings?.SubscribedTopics != null && settings.SubscribedTopics.Count > 0)
            {
                // Clear existing subscriptions in MqttService and re-subscribe
                // This ensures MqttService's internal state is consistent with loaded config
                foreach (var topic in _mqttService.SortedActiveSubscriptions.ToList())
                {
                    _mqttService.UnsubscribeAsync(topic).Wait(); // Use .Wait() for synchronous call in this context
                }

                foreach (var topic in settings.SubscribedTopics.OrderBy(t => t))
                {
                    _mqttService.SubscribeAsync(topic).Wait(); // Use .Wait() for synchronous call in this context
                }
                _logService.LogInfo($"从配置文件加载了 {_mqttService.SortedActiveSubscriptions.Count} 个订阅主题");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "从配置文件加载订阅主题失败");
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
