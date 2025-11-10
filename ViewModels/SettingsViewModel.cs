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

    private string _server = "localhost";
    private int _port = 1883;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _clientId = string.Empty;
    private string _baseTopic = "iot/devices";
    private bool _isPasswordVisible = false;
    private bool _useTls = false;
    private string _caCertificatePath = string.Empty;
    private string _clientCertificatePath = string.Empty;
    private string _clientKeyPath = string.Empty;
    private bool _ignoreCertificateErrors = false;

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
    /// 密码可见性图标文本
    /// </summary>
    public string PasswordVisibilityIcon => IsPasswordVisible ? "👁" : "👁‍🗨";

    /// <summary>
    /// 启用 TLS/SSL
    /// </summary>
    public bool UseTls
    {
        get => _useTls;
        set
        {
            if (_useTls != value)
            {
                _useTls = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// CA 证书路径
    /// </summary>
    public string CaCertificatePath
    {
        get => _caCertificatePath;
        set
        {
            if (_caCertificatePath != value)
            {
                _caCertificatePath = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 客户端证书路径
    /// </summary>
    public string ClientCertificatePath
    {
        get => _clientCertificatePath;
        set
        {
            if (_clientCertificatePath != value)
            {
                _clientCertificatePath = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 客户端私钥路径
    /// </summary>
    public string ClientKeyPath
    {
        get => _clientKeyPath;
        set
        {
            if (_clientKeyPath != value)
            {
                _clientKeyPath = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 忽略证书错误
    /// </summary>
    public bool IgnoreCertificateErrors
    {
        get => _ignoreCertificateErrors;
        set
        {
            if (_ignoreCertificateErrors != value)
            {
                _ignoreCertificateErrors = value;
                OnPropertyChanged();
            }
        }
    }

    // 命令
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand BrowseCaCertCommand { get; }
    public ICommand BrowseClientCertCommand { get; }
    public ICommand BrowseClientKeyCommand { get; }

    public SettingsViewModel(LogService logService, EncryptionService encryptionService, MqttSettings mqttSettings)
    {
        _logService = logService;
        _encryptionService = encryptionService;
        _mqttSettings = mqttSettings; // Assign MqttSettings

        // 初始化命令
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(OnCancel);
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
        BrowseCaCertCommand = new RelayCommand(BrowseCaCertificate);
        BrowseClientCertCommand = new RelayCommand(BrowseClientCertificate);
        BrowseClientKeyCommand = new RelayCommand(BrowseClientKey);

        // 加载配置
        LoadSettings();
    }

    /// <summary>
    /// 浏览 CA 证书文件
    /// </summary>
    private void BrowseCaCertificate()
    {
        var filePath = BrowseForFile("选择 CA 证书文件", "证书文件 (*.crt;*.pem;*.cer)|*.crt;*.pem;*.cer|所有文件 (*.*)|*.*");
        if (!string.IsNullOrEmpty(filePath))
        {
            CaCertificatePath = filePath;
        }
    }

    /// <summary>
    /// 浏览客户端证书文件
    /// </summary>
    private void BrowseClientCertificate()
    {
        var filePath = BrowseForFile("选择客户端证书文件", "证书文件 (*.crt;*.pem;*.cer)|*.crt;*.pem;*.cer|所有文件 (*.*)|*.*");
        if (!string.IsNullOrEmpty(filePath))
        {
            ClientCertificatePath = filePath;
        }
    }

    /// <summary>
    /// 浏览客户端私钥文件
    /// </summary>
    private void BrowseClientKey()
    {
        var filePath = BrowseForFile("选择客户端私钥文件", "私钥文件 (*.key;*.pem)|*.key;*.pem|所有文件 (*.*)|*.*");
        if (!string.IsNullOrEmpty(filePath))
        {
            ClientKeyPath = filePath;
        }
    }

    /// <summary>
    /// 打开文件浏览对话框
    /// </summary>
    private string? BrowseForFile(string title, string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = filter
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FileName;
        }

        return null;
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
            _mqttSettings.UseTls = UseTls;
            _mqttSettings.CaCertificatePath = string.IsNullOrWhiteSpace(CaCertificatePath) ? null : CaCertificatePath;
            _mqttSettings.ClientCertificatePath = string.IsNullOrWhiteSpace(ClientCertificatePath) ? null : ClientCertificatePath;
            _mqttSettings.ClientKeyPath = string.IsNullOrWhiteSpace(ClientKeyPath) ? null : ClientKeyPath;
            _mqttSettings.IgnoreCertificateErrors = IgnoreCertificateErrors;

            // Load existing settings to preserve subscribed topics and title
            var existingSettings = LoadSettingsFromFile();
            if (existingSettings != null)
            {
                _mqttSettings.SubscribedTopics = existingSettings.SubscribedTopics;
                _mqttSettings.Title = existingSettings.Title;
            }

            // 创建一个副本用于保存（避免修改单例实例）
            var settingsToSave = new MqttSettings
            {
                Server = _mqttSettings.Server,
                Port = _mqttSettings.Port,
                Username = _mqttSettings.Username,
                Password = _mqttSettings.Password,
                ClientId = _mqttSettings.ClientId,
                BaseTopic = _mqttSettings.BaseTopic,
                UseTls = _mqttSettings.UseTls,
                CaCertificatePath = _mqttSettings.CaCertificatePath,
                ClientCertificatePath = _mqttSettings.ClientCertificatePath,
                ClientKeyPath = _mqttSettings.ClientKeyPath,
                IgnoreCertificateErrors = _mqttSettings.IgnoreCertificateErrors,
                SubscribedTopics = _mqttSettings.SubscribedTopics,
                Title = _mqttSettings.Title
            };

            // 加密密码（如果有密码且未加密）
            if (!string.IsNullOrEmpty(settingsToSave.Password) &&
                !_encryptionService.IsEncrypted(settingsToSave.Password))
            {
                settingsToSave.Password = _encryptionService.Encrypt(settingsToSave.Password);
            }

            var configFilePath = PathManager.MqttConfigFile;
            var json = JsonConvert.SerializeObject(settingsToSave, Formatting.Indented);
            File.WriteAllText(configFilePath, json);

            _logService.LogInfo("设置已保存");
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
            UseTls = settings.UseTls;
            CaCertificatePath = settings.CaCertificatePath ?? string.Empty;
            ClientCertificatePath = settings.ClientCertificatePath ?? string.Empty;
            ClientKeyPath = settings.ClientKeyPath ?? string.Empty;
            IgnoreCertificateErrors = settings.IgnoreCertificateErrors;
        }
    }

    /// <summary>
    /// 从文件加载设置
    /// </summary>
    public MqttSettings? LoadSettingsFromFile()
    {
        try
        {
            var configFilePath = Path.Combine("configs", "mqtt_config.json");
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                var settings = JsonConvert.DeserializeObject<MqttSettings>(json);

                // 解密密码（如果已加密）
                if (settings != null && !string.IsNullOrEmpty(settings.Password))
                {
                    if (_encryptionService.IsEncrypted(settings.Password))
                    {
                        try
                        {
                            settings.Password = _encryptionService.Decrypt(settings.Password);
                        }
                        catch (Exception decryptEx)
                        {
                            _logService.LogException(decryptEx, "解密密码失败，密码将被清空");
                            settings.Password = null;
                        }
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
