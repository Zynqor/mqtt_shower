using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using MqttMonitor.Models;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 表格视图 ViewModel
/// </summary>
public class TableViewModel : INotifyPropertyChanged
{
    private readonly DataProcessingService _dataProcessingService;
    private readonly LogService _logService;
    private readonly ChartConfig _chartConfig;
    private readonly Dictionary<string, DeviceDataRow> _deviceRowMap = new(); // 快速查找设备行
    private readonly HashSet<string> _existingColumns = new() { "设备ID" }; // 已存在的列名
    private readonly DispatcherTimer _cleanupTimer; // 定时清理超时设备
    private readonly DispatcherTimer _updateTimer; // 定时更新显示项
    private bool _needsUpdate = false; // 是否需要更新显示项

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 表格数据行集合
    /// </summary>
    public ObservableCollection<DeviceDataRow> DataRows { get; } = new ObservableCollection<DeviceDataRow>();

    /// <summary>
    /// 列名集合（用于动态生成列）
    /// </summary>
    public ObservableCollection<string> ColumnNames { get; } = new ObservableCollection<string> { "设备ID" };

    /// <summary>
    /// 每行最大列数
    /// </summary>
    public int MaxColumnsPerRow => _chartConfig.MaxColumnsPerRow;

    /// <summary>
    /// 设备显示项集合（用于多行布局）
    /// </summary>
    public ObservableCollection<DeviceDisplayItem> DeviceDisplayItems { get; } = new ObservableCollection<DeviceDisplayItem>();

    public TableViewModel(DataProcessingService dataProcessingService, LogService logService, ChartConfig chartConfig)
    {
        _dataProcessingService = dataProcessingService;
        _logService = logService;
        _chartConfig = chartConfig;

        // 订阅事件
        _dataProcessingService.OnUpstreamDataParsed += OnUpstreamDataParsed;
        _dataProcessingService.OnDataCleared += OnDataCleared;

        // 初始化定时清理器，每30秒检查一次超时设备
        _cleanupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _cleanupTimer.Tick += CleanupTimeoutDevices;
        _cleanupTimer.Start();

        // 初始化定时更新器，每500毫秒更新一次显示项（如果有变化）
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _updateTimer.Tick += UpdateTimerTick;
        _updateTimer.Start();
    }

    /// <summary>
    /// 当收到上行数据时
    /// </summary>
    private void OnUpstreamDataParsed(UpstreamDataPacket dataPacket)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // 使用Dictionary快速查找设备行
                if (!_deviceRowMap.TryGetValue(dataPacket.DeviceId, out var deviceRow))
                {
                    deviceRow = new DeviceDataRow { DeviceId = dataPacket.DeviceId };
                    _deviceRowMap[dataPacket.DeviceId] = deviceRow;
                    DataRows.Clear(); // Clear and re-add to trigger UI update and sorting
                    foreach (var row in _deviceRowMap.Values.OrderBy(r => r.DeviceId))
                    {
                        DataRows.Add(row);
                    }
                }

                // 更新测点数据
                if (dataPacket.Payload != null)
                {
                    foreach (var metric in dataPacket.Payload)
                    {
                        // 只在列真正不存在时才添加（使用HashSet快速检查）
                        if (!_existingColumns.Contains(metric.Name))
                        {
                            _existingColumns.Add(metric.Name);
                            ColumnNames.Add(metric.Name);
                        }

                        // 更新数据
                        var displayValue = metric.Unit != null
                            ? $"{metric.Value} {metric.Unit}"
                            : metric.Value.ToString();

                        deviceRow.SetMetricValue(metric.Name, displayValue);
                    }

                    // 更新最后更新时间
                    deviceRow.LastUpdateTime = DateTime.Now;

                    // 标记需要更新显示项
                    _needsUpdate = true;
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, "更新表格数据失败");
            }
        });
    }

    /// <summary>
    /// 清空数据
    /// </summary>
    private void OnDataCleared()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            DataRows.Clear();
            _deviceRowMap.Clear();
            DeviceDisplayItems.Clear();
            ColumnNames.Clear();
            _existingColumns.Clear();
            ColumnNames.Add("设备ID");
            _existingColumns.Add("设备ID");
        });
    }

    /// <summary>
    /// 定时更新显示项
    /// </summary>
    private void UpdateTimerTick(object? sender, EventArgs e)
    {
        if (_needsUpdate)
        {
            _needsUpdate = false;
            UpdateDeviceDisplayItems();
        }
    }

    /// <summary>
    /// 定时清理超时设备
    /// </summary>
    private void CleanupTimeoutDevices(object? sender, EventArgs e)
    {
        try
        {
            var timeoutSeconds = _chartConfig.DeviceTimeoutSeconds;
            var now = DateTime.Now;
            var timeoutDevices = _deviceRowMap
                .Where(kvp => (now - kvp.Value.LastUpdateTime).TotalSeconds > timeoutSeconds)
                .Select(kvp => kvp.Key)
                .ToList();

            if (timeoutDevices.Any())
            {
                foreach (var deviceId in timeoutDevices)
                {
                    _deviceRowMap.Remove(deviceId);
                }

                // 重新构建 DataRows
                DataRows.Clear();
                foreach (var row in _deviceRowMap.Values.OrderBy(r => r.DeviceId))
                {
                    DataRows.Add(row);
                }

                _logService.LogInfo($"自动清理了 {timeoutDevices.Count} 个超时设备");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "清理超时设备失败");
        }
    }

    /// <summary>
    /// 更新设备显示项（用于多行布局）
    /// </summary>
    public void UpdateDeviceDisplayItems()
    {
        DeviceDisplayItems.Clear();

        var maxCols = MaxColumnsPerRow;

        foreach (var deviceRow in DataRows.OrderBy(d => d.DeviceId))
        {
            var displayItem = new DeviceDisplayItem
            {
                DeviceId = deviceRow.DeviceId,
                LastUpdateTime = deviceRow.LastUpdateTime
            };

            // 获取该设备实际拥有的测点名称（而不是全局的列名）
            var deviceMetricNames = deviceRow.GetMetricNames();

            // 将该设备的参数按 maxCols 分组
            int groupIndex = 0;
            for (int i = 0; i < deviceMetricNames.Count; i += maxCols)
            {
                var rowGroup = new DataRowGroup { GroupIndex = groupIndex };
                var metricsInGroup = deviceMetricNames.Skip(i).Take(maxCols);

                foreach (var metricName in metricsInGroup)
                {
                    rowGroup.Metrics.Add(new MetricItem
                    {
                        Name = metricName,
                        Value = deviceRow.GetMetricValue(metricName)
                    });
                }

                displayItem.DataRowGroups.Add(rowGroup);
                groupIndex++;
            }

            DeviceDisplayItems.Add(displayItem);
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 设备数据行（支持动态属性）
/// </summary>
public class DeviceDataRow : INotifyPropertyChanged
{
    private readonly Dictionary<string, string> _metricValues = new Dictionary<string, string>();
    private DateTime _lastUpdateTime = DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set
        {
            if (_lastUpdateTime != value)
            {
                _lastUpdateTime = value;
                OnPropertyChanged(nameof(LastUpdateTime));
            }
        }
    }

    /// <summary>
    /// 设置测点值
    /// </summary>
    public void SetMetricValue(string metricName, string value)
    {
        _metricValues[metricName] = value;
        // WPF DataGrid indexer binding requires "Item[]" property change notification
        OnPropertyChanged("Item[]");
    }

    /// <summary>
    /// 获取测点值
    /// </summary>
    public string GetMetricValue(string metricName)
    {
        return _metricValues.TryGetValue(metricName, out var value) ? value : "-";
    }

    /// <summary>
    /// 获取该设备实际拥有的测点名称列表
    /// </summary>
    public List<string> GetMetricNames()
    {
        return _metricValues.Keys.ToList();
    }

    /// <summary>
    /// 索引器，用于数据绑定
    /// </summary>
    public string this[string metricName]
    {
        get => GetMetricValue(metricName);
        set => SetMetricValue(metricName, value);
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 设备显示项（用于多行布局）
/// </summary>
public class DeviceDisplayItem : INotifyPropertyChanged
{
    private DateTime _lastUpdateTime = DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set
        {
            if (_lastUpdateTime != value)
            {
                _lastUpdateTime = value;
                OnPropertyChanged(nameof(LastUpdateTime));
            }
        }
    }

    /// <summary>
    /// 数据行集合（每行包含多个参数）
    /// </summary>
    public ObservableCollection<DataRowGroup> DataRowGroups { get; } = new ObservableCollection<DataRowGroup>();

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 数据行组（包含表头和数据）
/// </summary>
public class DataRowGroup : INotifyPropertyChanged
{
    private int _groupIndex;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 参数列表
    /// </summary>
    public ObservableCollection<MetricItem> Metrics { get; } = new ObservableCollection<MetricItem>();

    /// <summary>
    /// 组索引（用于交替背景色）
    /// </summary>
    public int GroupIndex
    {
        get => _groupIndex;
        set
        {
            if (_groupIndex != value)
            {
                _groupIndex = value;
                OnPropertyChanged(nameof(GroupIndex));
            }
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 测点数据项
/// </summary>
public class MetricItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _value = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    /// <summary>
    /// 参数值
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
