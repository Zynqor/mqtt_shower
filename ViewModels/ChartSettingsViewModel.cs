using System;
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
/// 图表设置窗口 ViewModel
/// </summary>
public class ChartSettingsViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private readonly MqttSettings _mqttSettings;
    private readonly EncryptionService _encryptionService;
    private readonly string _configFilePath = "config.json";

    private int _maxChartDataPoints = 1000;
    private int _chartUpdateInterval = 800;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnSettingsSaved;
    public event Action? OnCancelled;

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

    // 命令
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ChartSettingsViewModel(LogService logService, MqttSettings mqttSettings, EncryptionService encryptionService)
    {
        _logService = logService;
        _mqttSettings = mqttSettings;
        _encryptionService = encryptionService;

        // 初始化命令
        SaveCommand = new RelayCommand(OnSave);
        CancelCommand = new RelayCommand(OnCancel);

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
            // 更新单例实例
            _mqttSettings.MaxChartDataPoints = MaxChartDataPoints;
            _mqttSettings.ChartUpdateInterval = ChartUpdateInterval;

            // 加载现有设置以保留其他属性
            var existingSettings = LoadSettingsFromFile();
            if (existingSettings != null)
            {
                _mqttSettings.Server = existingSettings.Server;
                _mqttSettings.Port = existingSettings.Port;
                _mqttSettings.Username = existingSettings.Username;
                _mqttSettings.Password = existingSettings.Password;
                _mqttSettings.ClientId = existingSettings.ClientId;
                _mqttSettings.BaseTopic = existingSettings.BaseTopic;
                _mqttSettings.SubscribedTopics = existingSettings.SubscribedTopics;
                _mqttSettings.Title = existingSettings.Title;
                _mqttSettings.WindowWidth = existingSettings.WindowWidth;
                _mqttSettings.WindowHeight = existingSettings.WindowHeight;
                _mqttSettings.WindowLeft = existingSettings.WindowLeft;
                _mqttSettings.WindowTop = existingSettings.WindowTop;
                _mqttSettings.WindowState = existingSettings.WindowState;
            }

            // 创建副本用于保存
            var settingsToSave = new MqttSettings
            {
                Server = _mqttSettings.Server,
                Port = _mqttSettings.Port,
                Username = _mqttSettings.Username,
                Password = _mqttSettings.Password,
                ClientId = _mqttSettings.ClientId,
                BaseTopic = _mqttSettings.BaseTopic,
                MaxChartDataPoints = _mqttSettings.MaxChartDataPoints,
                ChartUpdateInterval = _mqttSettings.ChartUpdateInterval,
                SubscribedTopics = _mqttSettings.SubscribedTopics,
                Title = _mqttSettings.Title,
                WindowWidth = _mqttSettings.WindowWidth,
                WindowHeight = _mqttSettings.WindowHeight,
                WindowLeft = _mqttSettings.WindowLeft,
                WindowTop = _mqttSettings.WindowTop,
                WindowState = _mqttSettings.WindowState
            };

            // 加密密码（如果有密码且未加密）
            if (!string.IsNullOrEmpty(settingsToSave.Password) &&
                !_encryptionService.IsEncrypted(settingsToSave.Password))
            {
                settingsToSave.Password = _encryptionService.Encrypt(settingsToSave.Password);
            }

            var json = JsonConvert.SerializeObject(settingsToSave, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);

            _logService.LogInfo("图表设置已保存");
            OnSettingsSaved?.Invoke();
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存图表设置失败");
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
