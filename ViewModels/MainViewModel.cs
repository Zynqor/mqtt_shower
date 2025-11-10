using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MqttMonitor.Models;
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
    private readonly LayoutSettingsService _layoutSettingsService;
    private readonly MqttSettings _mqttSettings;
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
    public ICommand ShowUserManualCommand { get; }
    public ICommand ShowAlarmConfigCommand { get; }
    public ICommand ShowAlertSettingsCommand { get; }
    public ICommand ShowHistoryQueryCommand { get; }
    public ICommand ShowAlarmHistoryQueryCommand { get; }
    public ICommand ClearDataCommand { get; }
    public ICommand ShowChartViewCommand { get; }
    public ICommand ShowTableViewCommand { get; }
    public ICommand ShowLogViewCommand { get; }
    public ICommand ShowAlarmViewCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SubscribeTopicCommand { get; }
    public ICommand UnsubscribeTopicCommand { get; }

    public MainViewModel(MqttService mqttService, DataProcessingService dataProcessingService, LogService logService, CsvDataStorageService csvStorageService, EncryptionService encryptionService, LayoutSettingsService layoutSettingsService, MqttSettings mqttSettings)
    {
        _mqttService = mqttService;
        _dataProcessingService = dataProcessingService;
        _logService = logService;
        _csvStorageService = csvStorageService;
        _encryptionService = encryptionService;
        _layoutSettingsService = layoutSettingsService;
        _mqttSettings = mqttSettings;

        // 从 MqttSettings 获取标题
        Title = mqttSettings.Title;

        // 订阅 MQTT 服务的属性变化
        _mqttService.PropertyChanged += OnMqttServicePropertyChanged;

        // 初始化命令
        ExitCommand = new RelayCommand(OnExit);
        ShowSettingsCommand = new RelayCommand(OnShowSettings);
        ShowChartSettingsCommand = new RelayCommand(OnShowChartSettings);
        ShowContactUsCommand = new RelayCommand(OnShowContactUs);
        ShowUserManualCommand = new RelayCommand(OnShowUserManual);
        ShowAlarmConfigCommand = new RelayCommand(OnShowAlarmConfig);
        ShowAlertSettingsCommand = new RelayCommand(OnShowAlertSettings);
        ShowHistoryQueryCommand = new RelayCommand(OnShowHistoryQuery);
        ShowAlarmHistoryQueryCommand = new RelayCommand(OnShowAlarmHistoryQuery);
        ClearDataCommand = new RelayCommand(OnClearData);
        ShowChartViewCommand = new RelayCommand(() => SelectedTabIndex = 0);
        ShowTableViewCommand = new RelayCommand(() => SelectedTabIndex = 1);
        ShowLogViewCommand = new RelayCommand(() => SelectedTabIndex = 2);
        ShowAlarmViewCommand = new RelayCommand(() => SelectedTabIndex = 4); // 告警是第5个标签（索引4）
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
    /// 显示历史数据查询窗口
    /// </summary>
    private void OnShowHistoryQuery()
    {
        var historyQueryWindow = App.ServiceProvider?.GetService<HistoryQueryWindow>();
        if (historyQueryWindow != null)
        {
            historyQueryWindow.Owner = System.Windows.Application.Current.MainWindow;
            historyQueryWindow.ShowDialog();
        }
    }

    /// <summary>
    /// 显示历史告警查询窗口
    /// </summary>
    private void OnShowAlarmHistoryQuery()
    {
        var alarmHistoryQueryWindow = App.ServiceProvider?.GetService<AlarmHistoryQueryWindow>();
        if (alarmHistoryQueryWindow != null)
        {
            alarmHistoryQueryWindow.Owner = System.Windows.Application.Current.MainWindow;
            alarmHistoryQueryWindow.ShowDialog();
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
    /// 显示告警配置窗口
    /// </summary>
    private void OnShowAlarmConfig()
    {
        var alarmConfigWindow = App.ServiceProvider?.GetService<AlarmConfigWindow>();
        if (alarmConfigWindow != null)
        {
            alarmConfigWindow.Owner = System.Windows.Application.Current.MainWindow;
            alarmConfigWindow.ShowDialog();
        }
    }

    /// <summary>
    /// 显示提醒设置窗口
    /// </summary>
    private void OnShowAlertSettings()
    {
        var alertSettingsWindow = App.ServiceProvider?.GetService<AlertSettingsWindow>();
        if (alertSettingsWindow != null)
        {
            alertSettingsWindow.Owner = System.Windows.Application.Current.MainWindow;
            alertSettingsWindow.ShowDialog();
        }
    }

    /// <summary>
    /// 显示使用说明
    /// </summary>
    private void OnShowUserManual()
    {
        try
        {
            // 获取exe所在目录
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var manualPath = System.IO.Path.Combine(exeDirectory, "使用说明.txt");

            // 如果文件不存在，创建使用说明文件
            if (!System.IO.File.Exists(manualPath))
            {
                var manualContent = GenerateUserManualContent();
                System.IO.File.WriteAllText(manualPath, manualContent, System.Text.Encoding.UTF8);
                _logService.LogInfo("已创建使用说明文件");
            }

            // 使用记事本打开文件
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{manualPath}\"",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "打开使用说明失败");
            System.Windows.MessageBox.Show($"打开使用说明失败：{ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 生成使用说明内容
    /// </summary>
    private string GenerateUserManualContent()
    {
        return @"==========================================
MQTT Monitor 使用说明
==========================================

本软件是一个功能强大的 MQTT 监控和管理工具，支持数据可视化、命令发送和日志记录。

==========================================
一、主界面布局
==========================================

1. 顶部菜单栏
   - 文件菜单
     * MQTT 连接设置：配置 MQTT 服务器连接参数
     * 图表设置：配置图表显示参数
     * 退出：关闭应用程序

   - 帮助菜单
     * 使用说明：打开本说明文档
     * 联系我们：查看公司联系方式

2. 功能标签页（左侧导航）
   - 图表：数据可视化图表
   - 表格：数据表格视图
   - 日志：系统日志记录
   - 命令：MQTT 命令发送

3. 右侧主题订阅区域
   - 订阅的主题列表：显示当前已订阅的所有 MQTT 主题
   - 新主题订阅：输入框添加新的订阅主题
   - 订阅按钮：点击订阅新主题
   - 取消订阅：右键菜单取消订阅主题

4. 底部状态栏
   - 连接状态：显示当前 MQTT 连接状态
   - 连接按钮：连接/断开 MQTT 服务器
   - 清空数据：清除当前显示的所有数据

==========================================
二、MQTT 连接设置
==========================================

通过 [文件] -> [MQTT 连接设置] 打开配置窗口

1. 基础连接配置
   - 服务器地址：MQTT 服务器的 IP 地址或域名
   - 端口：MQTT 服务器端口（默认 1883，TLS 加密通常使用 8883）
   - 用户名：MQTT 服务器认证用户名（可选）
   - 密码：MQTT 服务器认证密码（可选，会自动加密保存）
   - 客户端ID：MQTT 客户端标识（留空则自动生成）
   - 基础 Topic：用于命令发送和响应的基础主题

2. TLS/SSL 加密配置
   - 启用 TLS/SSL 加密连接：勾选后使用加密连接
   - CA 证书路径：用于验证服务器身份的根证书（单向 TLS）
   - 客户端证书路径：客户端身份证书（双向认证时使用，如 AWS IoT）
   - 客户端私钥路径：客户端私钥文件（双向认证时使用）
   - 忽略证书错误：仅用于测试环境，生产环境不推荐使用

   使用场景：
   * 自托管 Mosquitto（单向 TLS）：只需填写 CA 证书
   * AWS IoT/Azure IoT（双向 TLS）：需要 CA 证书 + 客户端证书 + 私钥
   * 测试环境：可勾选""忽略证书错误""

==========================================
三、图表页面
==========================================

图表页面提供实时数据可视化功能。

主要功能：
- 实时曲线图：显示 MQTT 消息中的数值数据
- 多条曲线：支持同时显示多个数据字段
- 图例管理：点击图例可显示/隐藏对应曲线
- 自动刷新：根据设置的间隔自动更新图表

图表设置（通过 [文件] -> [图表设置]）：
- 图表最大数据点数量：控制图表显示的历史数据量（默认 1000）
- 图表更新间隔：控制图表刷新频率（单位：毫秒，默认 800）

注意事项：
- 数据点过多会影响性能，建议根据需要调整
- 更新间隔越小越流畅，但会占用更多 CPU 资源

==========================================
四、表格页面
==========================================

表格页面以表格形式显示接收到的 MQTT 数据。

主要功能：
- 数据列表：按时间倒序显示所有接收的消息
- 字段显示：自动解析 JSON 数据并显示各个字段
- 排序功能：点击列头可对数据排序
- 数据导出：所有数据自动保存为 CSV 文件

CSV 文件位置：
- 文件名格式：data_YYYYMMDD.csv（按天存储）
- 位置：程序根目录下

==========================================
五、日志页面
==========================================

日志页面记录系统运行的所有事件和错误信息。

主要功能：
- 实时日志：自动滚动显示最新日志（类似 tail -f）
- 日志级别：INFO（信息）、WARNING（警告）、ERROR（错误）
- 日志复制：选中日志文本可直接复制
- 字体缩放：Ctrl + 鼠标滚轮可调整日志字体大小
- 日志文件：自动按天保存日志到 logs 文件夹

日志文件位置：
- 文件名格式：log_YYYY-MM-DD.txt
- 位置：程序根目录下的 logs 文件夹

快捷键：
- Ctrl + 滚轮向上：放大字体
- Ctrl + 滚轮向下：缩小字体

==========================================
六、命令页面
==========================================

命令页面用于向 MQTT 设备发送预配置的命令。

主要功能：
1. 命令选择下拉框
   - 显示所有可用的预配置命令
   - 命令从 commands.json 文件加载

2. 命令参数输入
   - 根据选中的命令，显示对应的参数输入框
   - 支持多种参数类型（数值、文本等）

3. 发送命令按钮
   - 点击后将命令发送到 MQTT 服务器
   - 发送前会验证参数有效性

4. 命令历史记录
   - 显示已发送的命令历史
   - 包含发送时间、命令内容和返回结果
   - 双击历史记录可查看详细信息

命令配置文件：
- 文件名：commands.json
- 位置：程序根目录
- 格式：JSON 格式，可手动编辑添加新命令

示例命令配置：
{
  ""commandName"": ""设置温度"",
  ""description"": ""设置目标温度"",
  ""topic"": ""device/control/temperature"",
  ""parameters"": [
    {
      ""name"": ""temperature"",
      ""displayName"": ""温度值"",
      ""type"": ""number"",
      ""defaultValue"": ""25""
    }
  ]
}

==========================================
七、主题订阅管理
==========================================

主题订阅区域位于主界面右侧。

订阅新主题：
1. 在""新主题""输入框中输入 MQTT 主题
2. 点击""订阅""按钮
3. 订阅成功后主题会出现在上方列表中

取消订阅：
1. 在已订阅列表中找到要取消的主题
2. 右键点击主题
3. 选择""取消订阅""

主题格式示例：
- sensor/temperature  （单层主题）
- device/sensor/temperature  （多层主题）
- sensor/+/temperature  （单层通配符，+ 匹配一层）
- sensor/#  （多层通配符，# 匹配所有子主题）

注意事项：
- 订阅的主题会自动保存到配置文件
- 重启程序后会自动重新订阅
- 必须先连接 MQTT 服务器才能订阅主题

==========================================
八、数据流程说明
==========================================

1. 连接流程
   配置连接参数 -> 点击连接 -> 建立 MQTT 连接 -> 自动订阅保存的主题

2. 数据接收流程
   MQTT 服务器 -> 订阅的主题 -> 数据解析 -> 更新图表/表格 -> 保存到 CSV

3. 命令发送流程
   选择命令 -> 填写参数 -> 发送命令 -> MQTT 发布 -> 记录历史

==========================================
九、故障排查
==========================================

1. 无法连接 MQTT 服务器
   - 检查服务器地址和端口是否正确
   - 检查网络连接是否正常
   - 检查用户名密码是否正确
   - 查看日志页面的详细错误信息
   - 如果使用 TLS，检查证书文件路径是否正确

2. 订阅主题失败
   - 确保已连接到 MQTT 服务器
   - 检查主题格式是否正确
   - 检查是否有权限订阅该主题
   - 查看日志页面的错误信息

3. 数据不显示
   - 确认主题已成功订阅
   - 检查 MQTT 消息格式是否为 JSON
   - 查看日志确认是否收到数据
   - 检查数据字段名称是否匹配

4. 命令发送失败
   - 确保已连接到 MQTT 服务器
   - 检查命令参数是否填写完整
   - 查看日志页面的错误信息
   - 验证 commands.json 格式是否正确

==========================================
十、配置文件说明
==========================================

1. config.json
   - 位置：程序根目录
   - 内容：MQTT 连接配置、窗口状态、订阅主题等
   - 格式：JSON 格式
   - 备注：密码字段会自动加密（显示为 ENCRYPTED: 开头）

2. commands.json
   - 位置：程序根目录
   - 内容：预配置的 MQTT 命令模板
   - 格式：JSON 数组
   - 编辑：可手动编辑添加新命令

3. logs/ 文件夹
   - 位置：程序根目录
   - 内容：每日日志文件（log_YYYY-MM-DD.txt）
   - 作用：记录所有系统运行日志

4. CSV 数据文件
   - 位置：程序根目录
   - 命名：data_YYYYMMDD.csv
   - 内容：每日接收的 MQTT 数据
   - 格式：CSV 格式，可用 Excel 打开

==========================================
十一、技术支持
==========================================

如需技术支持或遇到问题，请通过以下方式联系我们：
[帮助] -> [联系我们] 查看详细联系方式

或查看日志文件获取详细错误信息，发送给技术支持人员。

==========================================
版本信息
==========================================

软件名称：MQTT Monitor
版本：1.0
框架：.NET 8.0
协议：MQTT 3.1.1 / 5.0

感谢使用本软件！
==========================================
";
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
                settings.ClientId,
                settings.UseTls,
                settings.CaCertificatePath,
                settings.ClientCertificatePath,
                settings.ClientKeyPath,
                settings.IgnoreCertificateErrors);

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
            var layoutSettings = _layoutSettingsService.LoadLayoutSettings();

            // 恢复窗口大小
            if (layoutSettings.WindowWidth > 0)
                window.Width = layoutSettings.WindowWidth;
            if (layoutSettings.WindowHeight > 0)
                window.Height = layoutSettings.WindowHeight;

            // 恢复窗口位置
            if (!double.IsNaN(layoutSettings.WindowLeft) && !double.IsNaN(layoutSettings.WindowTop))
            {
                window.Left = layoutSettings.WindowLeft;
                window.Top = layoutSettings.WindowTop;
            }

            // 恢复窗口状态
            if (Enum.TryParse<System.Windows.WindowState>(layoutSettings.WindowState, out var windowState))
            {
                window.WindowState = windowState;
            }

            _logService.LogInfo("已恢复窗口布局");
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
            var layoutSettings = _layoutSettingsService.LoadLayoutSettings();

            // 保存窗口大小和位置（仅在非最小化和非最大化时保存）
            if (window.WindowState == System.Windows.WindowState.Normal)
            {
                layoutSettings.WindowWidth = window.Width;
                layoutSettings.WindowHeight = window.Height;
                layoutSettings.WindowLeft = window.Left;
                layoutSettings.WindowTop = window.Top;
            }

            layoutSettings.WindowState = window.WindowState.ToString();

            _layoutSettingsService.SaveLayoutSettings(layoutSettings);
            _logService.LogInfo("已保存窗口布局");
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
