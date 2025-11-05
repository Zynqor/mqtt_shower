using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MqttMonitor.Models;
using MqttMonitor.Services;
using Newtonsoft.Json;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 设置窗口 ViewModel
/// </summary>
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private readonly EncryptionService _encryptionService;
    private readonly MqttSettings _mqttSettings; // Inject MqttSettings
    private readonly string _configFilePath = "config.json";

    private string _server = "localhost";
    private int _port = 1883;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _clientId = string.Empty;
    private string _baseTopic = "iot/devices";
    private bool _isPasswordVisible = false;
    private int _maxChartDataPoints = 1000;
    private int _chartUpdateInterval = 800;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnSettingsSaved;
    public event Action? OnCancelled;

    /// <summary>
    /// MQTT 服务器地址
    /// </summary>
    public string Server
    {
        get => _server;
        set
        {
            if (_server != value)
            {
                _server = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// MQTT 服务器端口
    /// </summary>
    public int Port
    {
        get => _port;
        set
        {
            if (_port != value)
            {
                _port = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 密码
    /// </summary>
    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId
    {
        get => _clientId;
        set
        {
            if (_clientId != value)
            {
                _clientId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 基础 Topic
    /// </summary>
    public string BaseTopic
    {
        get => _baseTopic;
        set
        {
            if (_baseTopic != value)
            {
                _baseTopic = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 密码是否可见
    /// </summary>
    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set
        {
            if (_isPasswordVisible != value)
            {
                _isPasswordVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordVisibilityIcon));
            }
        }
    }

    /// <summary>
    /// 图表最大数据点数量
    /// </summary>
    public int MaxChartDataPoints
    {
        get => _maxChartDataPoints;
        set
        {
            if (_maxChartDataPoints != value)
            {
                _maxChartDataPoints = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 图表更新间隔（毫秒）
    /// </summary>
    public int ChartUpdateInterval
    {
        get => _chartUpdateInterval;
        set
        {
            if (_chartUpdateInterval != value)
            {
                _chartUpdateInterval = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 密码可见性图标文本
    /// </summary>
    public string PasswordVisibilityIcon => IsPasswordVisible ? "👁" : "👁‍🗨";

    // 命令
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }

    public SettingsViewModel(LogService logService, EncryptionService encryptionService, MqttSettings mqttSettings)
    {
        _logService = logService;
        _encryptionService = encryptionService;
        _mqttSettings = mqttSettings; // Assign MqttSettings

        // 初始化命令
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(OnCancel);
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);

        // 加载配置
        LoadSettings();
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    private void OnSave()
    {
        try
        {
            // Update the singleton MqttSettings instance
            _mqttSettings.Server = Server;
            _mqttSettings.Port = Port;
            _mqttSettings.Username = string.IsNullOrWhiteSpace(Username) ? null : Username;
            _mqttSettings.Password = string.IsNullOrWhiteSpace(Password) ? null : Password;
            _mqttSettings.ClientId = string.IsNullOrWhiteSpace(ClientId) ? null : ClientId;
            _mqttSettings.BaseTopic = BaseTopic;
            _mqttSettings.MaxChartDataPoints = MaxChartDataPoints;
            _mqttSettings.ChartUpdateInterval = ChartUpdateInterval;

            // Load existing settings to preserve subscribed topics and other properties not in SettingsViewModel
            var existingSettings = LoadSettingsFromFile();
            if (existingSettings != null)
            {
                _mqttSettings.SubscribedTopics = existingSettings.SubscribedTopics;
                // Copy other properties that are not directly bound in SettingsViewModel if necessary
                _mqttSettings.Title = existingSettings.Title;
                _mqttSettings.WindowWidth = existingSettings.WindowWidth;
                _mqttSettings.WindowHeight = existingSettings.WindowHeight;
                _mqttSettings.WindowLeft = existingSettings.WindowLeft;
                _mqttSettings.WindowTop = existingSettings.WindowTop;
                _mqttSettings.WindowState = existingSettings.WindowState;
            }

            // 创建临时设置对象用于保存（加密密码）
            var settingsToSave = new MqttSettings
            {
                Title = _mqttSettings.Title,
                Server = _mqttSettings.Server,
                Port = _mqttSettings.Port,
                Username = _mqttSettings.Username,
                Password = !string.IsNullOrWhiteSpace(_mqttSettings.Password)
                    ? _encryptionService.Encrypt(_mqttSettings.Password)  // 加密密码
                    : null,
                ClientId = _mqttSettings.ClientId,
                BaseTopic = _mqttSettings.BaseTopic,
                SubscribedTopics = _mqttSettings.SubscribedTopics,
                WindowWidth = _mqttSettings.WindowWidth,
                WindowHeight = _mqttSettings.WindowHeight,
                WindowLeft = _mqttSettings.WindowLeft,
                WindowTop = _mqttSettings.WindowTop,
                WindowState = _mqttSettings.WindowState,
                MaxChartDataPoints = _mqttSettings.MaxChartDataPoints,
                ChartUpdateInterval = _mqttSettings.ChartUpdateInterval
            };

            var json = JsonConvert.SerializeObject(settingsToSave, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);

            _logService.LogInfo("设置已保存（密码已加密）");
            OnSettingsSaved?.Invoke();
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存设置失败");
            System.Windows.MessageBox.Show($"保存设置失败：{ex.Message}", "错误",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 取消
    /// </summary>
    private void OnCancel()
    {
        OnCancelled?.Invoke();
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public void LoadSettings()
    {
        var settings = LoadSettingsFromFile();
        if (settings != null)
        {
            Server = settings.Server;
            Port = settings.Port;
            Username = settings.Username ?? string.Empty;
            Password = settings.Password ?? string.Empty;
            ClientId = settings.ClientId ?? string.Empty;
            BaseTopic = settings.BaseTopic;
            MaxChartDataPoints = settings.MaxChartDataPoints;
            ChartUpdateInterval = settings.ChartUpdateInterval;
        }
    }

    /// <summary>
    /// 从文件加载设置
    /// </summary>
    public MqttSettings? LoadSettingsFromFile()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var settings = JsonConvert.DeserializeObject<MqttSettings>(json);

                // 解密密码（如果已加密）
                if (settings != null && !string.IsNullOrEmpty(settings.Password))
                {
                    try
                    {
                        settings.Password = _encryptionService.Decrypt(settings.Password);
                    }
                    catch (Exception decryptEx)
                    {
                        _logService.LogException(decryptEx, "解密密码失败，可能需要重新设置密码");
                        // 解密失败时清空密码，用户需要重新输入
                        settings.Password = null;
                    }
                }

                return settings;
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载设置失败");
        }
        return null;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
