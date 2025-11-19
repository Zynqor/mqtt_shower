using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 历史数据查询窗口 ViewModel
/// </summary>
public class HistoryQueryViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly LogService _logService;
    private readonly HistoryDataStorageService _historyDataStorageService;
    private readonly string _dataDirectory; // 保留用于CSV导出兼容
    private DateTime _startDate = DateTime.Today;
    private DateTime _endDate = DateTime.Today;
    private string _selectedDevice = string.Empty;
    private ObservableCollection<string> _availableDevices = new();
    private ObservableCollection<HistoryDataRow> _historyData = new();
    private bool _isLoading = false;
    private string _statusMessage = "就绪";
    private CancellationTokenSource? _queryCts;

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string GetResourceString(string key, string fallback = "")
    {
        return System.Windows.Application.Current?.TryFindResource(key) as string ?? fallback;
    }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (_startDate != value)
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (_endDate != value)
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 选中的设备
    /// </summary>
    public string SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (_selectedDevice != value)
            {
                _selectedDevice = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 可用的设备列表
    /// </summary>
    public ObservableCollection<string> AvailableDevices
    {
        get => _availableDevices;
        set
        {
            if (_availableDevices != value)
            {
                _availableDevices = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 历史数据
    /// </summary>
    public ObservableCollection<HistoryDataRow> HistoryData
    {
        get => _historyData;
        set
        {
            if (_historyData != value)
            {
                _historyData = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 状态消息
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    // 命令
    public ICommand QueryCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand RefreshDevicesCommand { get; }

    public HistoryQueryViewModel(LogService logService, HistoryDataStorageService historyDataStorageService)
    {
        _logService = logService;
        _historyDataStorageService = historyDataStorageService;

        // 获取数据目录（保留用于CSV导出兼容）
        var exePath = AppDomain.CurrentDomain.BaseDirectory;
        _dataDirectory = Path.Combine(exePath, "data");

        // 初始化命令
        QueryCommand = new RelayCommand(OnQuery);
        ExportCommand = new RelayCommand(OnExport, CanExport);
        RefreshDevicesCommand = new RelayCommand(OnRefreshDevices);

        // 加载设备列表
        LoadAvailableDevices();
    }

    /// <summary>
    /// 加载可用的设备列表（从SQLite）
    /// </summary>
    private async void LoadAvailableDevices()
    {
        try
        {
            var devices = await _historyDataStorageService.GetAvailableDevicesAsync();

            AvailableDevices = new ObservableCollection<string>(devices);

            if (AvailableDevices.Count > 0 && string.IsNullOrEmpty(SelectedDevice))
            {
                SelectedDevice = AvailableDevices[0];
            }

            _logService.LogInfo($"从SQLite加载了 {AvailableDevices.Count} 个设备");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "加载设备列表失败");
        }
    }

    /// <summary>
    /// 刷新设备列表
    /// </summary>
    private void OnRefreshDevices()
    {
        LoadAvailableDevices();
        StatusMessage = GetResourceString("Status.DeviceListRefreshed", "设备列表已刷新");
    }

    /// <summary>
    /// 查询历史数据
    /// </summary>
    private async void OnQuery()
    {
        if (string.IsNullOrEmpty(SelectedDevice))
        {
            StatusMessage = GetResourceString("Error.PleaseSelectDevice", "请选择设备");
            return;
        }

        if (StartDate > EndDate)
        {
            StatusMessage = GetResourceString("Error.InvalidDateRange", "开始日期不能晚于结束日期");
            return;
        }

        // 取消之前的查询
        _queryCts?.Cancel();
        _queryCts?.Dispose();
        _queryCts = new CancellationTokenSource();
        var token = _queryCts.Token;

        try
        {
            IsLoading = true;
            StatusMessage = GetResourceString("Status.Querying", "正在查询...");
            HistoryData.Clear();

            // 设置时间范围：开始日期 00:00:00，结束日期 23:59:59
            var startTimestamp = new DateTimeOffset(StartDate.Date).ToUnixTimeMilliseconds();
            var endTimestamp = new DateTimeOffset(EndDate.Date.AddDays(1).AddSeconds(-1)).ToUnixTimeMilliseconds();

            // 从SQLite查询数据
            var dataPoints = await System.Threading.Tasks.Task.Run(async () =>
            {
                token.ThrowIfCancellationRequested();
                return await _historyDataStorageService.QueryDataAsync(
                    SelectedDevice,
                    null, // 查询所有测点
                    startTimestamp,
                    endTimestamp);
            }, token);

            token.ThrowIfCancellationRequested();

            // 将数据点转换为显示格式（按时间戳分组）
            var groupedData = dataPoints
                .GroupBy(p => p.Timestamp)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var timestampStr = DateTimeOffset.FromUnixTimeMilliseconds(g.Key)
                        .ToLocalTime()
                        .ToString("yyyy-MM-dd HH:mm:ss.fff");

                    var dataDict = g.ToDictionary(
                        p => p.MetricName,
                        p => p.Value.ToString("F2"));

                    return new HistoryDataRow
                    {
                        Timestamp = timestampStr,
                        Data = dataDict
                    };
                })
                .ToList();

            HistoryData = new ObservableCollection<HistoryDataRow>(groupedData);

            if (HistoryData.Count > 0)
            {
                var template = GetResourceString("Query.LoadedRecords", "已加载 {0} 条记录");
                StatusMessage = string.Format(template, HistoryData.Count);
            }
            else
            {
                StatusMessage = GetResourceString("Query.NoRecords", "未找到符合条件的记录");
            }

            (ExportCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = GetResourceString("Status.QueryCancelled", "查询已取消");
            _logService.LogInfo("历史数据查询被取消");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "查询历史数据失败");
            StatusMessage = $"查询失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 读取CSV文件
    /// </summary>
    private List<HistoryDataRow> ReadCsvFile(string filePath)
    {
        var result = new List<HistoryDataRow>();

        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
                return result;

            var headers = lines[0].Split(',').Skip(1).ToArray(); // 跳过"时间"列

            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                if (values.Length < 2)
                    continue;

                var timestamp = values[0];
                var dataDict = new Dictionary<string, string>();

                for (int j = 1; j < values.Length && j - 1 < headers.Length; j++)
                {
                    var header = headers[j - 1];
                    var value = values[j];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        dataDict[header] = value;
                    }
                }

                result.Add(new HistoryDataRow
                {
                    Timestamp = timestamp,
                    Data = dataDict
                });
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, $"读取CSV文件失败: {filePath}");
        }

        return result;
    }

    /// <summary>
    /// 导出数据
    /// </summary>
    private void OnExport()
    {
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV文件|*.csv",
                FileName = $"{SelectedDevice}_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ExportToCsv(saveDialog.FileName);
                StatusMessage = $"已导出到: {saveDialog.FileName}";
                _logService.LogInfo($"导出数据到: {saveDialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "导出数据失败");
            StatusMessage = $"导出失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 导出到CSV
    /// </summary>
    private void ExportToCsv(string filePath)
    {
        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);

        if (HistoryData.Count == 0)
            return;

        // 获取所有列名
        var allColumns = new HashSet<string>();
        foreach (var row in HistoryData)
        {
            foreach (var key in row.Data.Keys)
            {
                allColumns.Add(key);
            }
        }

        var orderedColumns = allColumns.OrderBy(c => c).ToList();

        // 写入表头
        writer.WriteLine("时间," + string.Join(",", orderedColumns));

        // 写入数据
        foreach (var row in HistoryData)
        {
            var values = new List<string> { row.Timestamp };
            foreach (var column in orderedColumns)
            {
                values.Add(row.Data.TryGetValue(column, out var value) ? value : "");
            }
            writer.WriteLine(string.Join(",", values));
        }
    }

    private bool CanExport() => HistoryData.Count > 0;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // IDisposable实现
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            // 取消正在进行的查询
            _queryCts?.Cancel();
            _queryCts?.Dispose();
            _queryCts = null;

            _logService.LogInfo("HistoryQueryViewModel 已释放资源");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "HistoryQueryViewModel 释放资源时出错");
        }
    }
}

/// <summary>
/// 历史数据行
/// </summary>
public class HistoryDataRow
{
    public string Timestamp { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = new();
}
