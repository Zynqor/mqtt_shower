using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
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
    private readonly Dictionary<string, DeviceDataRow> _deviceRowMap = new(); // 快速查找设备行
    private readonly HashSet<string> _existingColumns = new() { "设备ID" }; // 已存在的列名

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 表格数据行集合
    /// </summary>
    public ObservableCollection<DeviceDataRow> DataRows { get; } = new ObservableCollection<DeviceDataRow>();

    /// <summary>
    /// 列名集合（用于动态生成列）
    /// </summary>
    public ObservableCollection<string> ColumnNames { get; } = new ObservableCollection<string> { "设备ID" };

    public TableViewModel(DataProcessingService dataProcessingService, LogService logService)
    {
        _dataProcessingService = dataProcessingService;
        _logService = logService;

        // 订阅事件
        _dataProcessingService.OnUpstreamDataParsed += OnUpstreamDataParsed;
        _dataProcessingService.OnDataCleared += OnDataCleared;
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
            ColumnNames.Clear();
            _existingColumns.Clear();
            ColumnNames.Add("设备ID");
            _existingColumns.Add("设备ID");
        });
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

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 设备ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

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
