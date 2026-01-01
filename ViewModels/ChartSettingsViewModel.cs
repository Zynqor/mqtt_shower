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
    private readonly ChartConfig _chartConfig;
    private readonly ChartConfigService _chartConfigService;

    private int _maxChartDataPoints = 1000;
    private int _chartUpdateInterval = 800;
    private int _maxColumnsPerRow = 6;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnSettingsSaved;
    public event Action? OnCancelled;
    public event Action? OnTableSettingsChanged;

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
    /// 表格视图每行最大列数
    /// </summary>
    public int MaxColumnsPerRow
    {
        get => _maxColumnsPerRow;
        set
        {
            if (_maxColumnsPerRow != value)
            {
                _maxColumnsPerRow = value;
                OnPropertyChanged();
            }
        }
    }

    // 命令
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ChartSettingsViewModel(LogService logService, ChartConfig chartConfig, ChartConfigService chartConfigService)
    {
        _logService = logService;
        _chartConfig = chartConfig;
        _chartConfigService = chartConfigService;

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
            // 检查列数是否变化
            bool tableSettingsChanged = _chartConfig.MaxColumnsPerRow != MaxColumnsPerRow;

            // 更新单例实例
            _chartConfig.MaxChartDataPoints = MaxChartDataPoints;
            _chartConfig.ChartUpdateInterval = ChartUpdateInterval;
            _chartConfig.MaxColumnsPerRow = MaxColumnsPerRow;

            // 保存到文件
            _chartConfigService.SaveChartConfig(_chartConfig);

            _logService.LogInfo("图表设置已保存");

            // 如果表格设置变化，触发刷新事件
            if (tableSettingsChanged)
            {
                OnTableSettingsChanged?.Invoke();
            }

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
        MaxChartDataPoints = _chartConfig.MaxChartDataPoints;
        ChartUpdateInterval = _chartConfig.ChartUpdateInterval;
        MaxColumnsPerRow = _chartConfig.MaxColumnsPerRow;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
