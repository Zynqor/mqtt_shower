using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using MqttMonitor.Models;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 图表视图 ViewModel - ScottPlot 版本
/// </summary>
public class ChartViewModel : INotifyPropertyChanged
{
    private readonly DataProcessingService _dataProcessingService;
    private readonly LogService _logService;
    private readonly ChartLegendConfigService _configService;
    private readonly MqttSettings _mqttSettings;
    private readonly Dictionary<string, Dictionary<string, ScatterPlotData>> _seriesMap = new();
    private readonly Dictionary<string, Dictionary<string, ChartLegendItem>> _legendItemsMap = new();
    private readonly Dictionary<string, Dictionary<string, ChartLegendConfig>> _loadedConfig = new();
    private readonly Dictionary<string, ScottPlot.Color> _deviceBaseColors = new();
    private readonly List<double> _baseHues = new() { 0, 30, 60, 120, 180, 210, 240, 270, 300, 330 };
    private readonly DispatcherTimer _updateTimer;
    private int _nextDeviceColorIndex = 0;
    private int _maxChartDataPoints;
    private bool _hasNewData = false;
    private WpfPlot? _chart;
    private ObservableCollection<ChartLegendItem> _legendItems = new();
    private ObservableCollection<ChartLegendGroupViewModel> _legendGroups = new();
    private Crosshair? _crosshair;
    private Text? _hoverLabel;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 存储每条曲线的绘图对象和数据
    /// </summary>
    private class ScatterPlotData
    {
        public ScottPlot.Plottables.Scatter Plot { get; set; } = null!;
        public List<double> XData { get; set; } = new();
        public List<double> YData { get; set; } = new();
        public List<double> OriginalYData { get; set; } = new();
    }

    /// <summary>
    /// 图例项集合
    /// </summary>
    public ObservableCollection<ChartLegendItem> LegendItems
    {
        get => _legendItems;
        set
        {
            _legendItems = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 图例分组集合
    /// </summary>
    public ObservableCollection<ChartLegendGroupViewModel> LegendGroups
    {
        get => _legendGroups;
        set
        {
            _legendGroups = value;
            OnPropertyChanged();
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

    public ChartViewModel(DataProcessingService dataProcessingService, LogService logService,
        ChartLegendConfigService configService, MqttSettings mqttSettings)
    {
        _dataProcessingService = dataProcessingService;
        _logService = logService;
        _configService = configService;
        _mqttSettings = mqttSettings;
        _maxChartDataPoints = _mqttSettings.MaxChartDataPoints;

        // Subscribe to MqttSettings changes
        _mqttSettings.PropertyChanged += OnMqttSettingsPropertyChanged;

        // 加载配置
        _loadedConfig = _configService.LoadConfig();

        // 初始化批量更新定时器
        _updateTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(_mqttSettings.ChartUpdateInterval)
        };
        _updateTimer.Tick += OnUpdateTimerTick;
        _updateTimer.Start();

        // 订阅事件
        _dataProcessingService.OnUpstreamDataParsed += OnUpstreamDataParsed;
        _dataProcessingService.OnDataCleared += OnDataCleared;
    }

    /// <summary>
    /// 初始化图表控件
    /// </summary>
    public void InitializeChart(WpfPlot chart)
    {
        _chart = chart;

        // 配置图表
        _chart.Plot.Title("");
        _chart.Plot.Axes.Bottom.Label.Text = "";
        _chart.Plot.Axes.Left.Label.Text = "";

        // 设置样式
        _chart.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E6E6E6");
        _chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
        _chart.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");

        // 初始化Crosshair和悬浮标签
        _crosshair = _chart.Plot.Add.Crosshair(0, 0);
        _crosshair.IsVisible = false;
        _crosshair.LineColor = ScottPlot.Color.FromHex("#666666");
        _crosshair.LineWidth = 1;

        _hoverLabel = _chart.Plot.Add.Text("", 0, 0);
        _hoverLabel.LabelStyle.FontSize = 11;
        _hoverLabel.LabelStyle.FontName = "Microsoft YaHei UI";
        _hoverLabel.LabelStyle.Bold = true;
        _hoverLabel.LabelStyle.ForeColor = ScottPlot.Color.FromHex("#333333");
        _hoverLabel.LabelStyle.BackgroundColor = ScottPlot.Color.FromHex("#FFFFFF").WithAlpha(0.95);
        _hoverLabel.LabelStyle.BorderColor = ScottPlot.Color.FromHex("#666666");
        _hoverLabel.LabelStyle.BorderWidth = 1;
        _hoverLabel.LabelStyle.Padding = 8;
        _hoverLabel.IsVisible = false;

        // 设置鼠标事件
        SetupMouseTracking();

        _logService.LogInfo("ScottPlot 图表已初始化（高性能模式 + 鼠标悬浮显示）");
    }

    /// <summary>
    /// 定时器回调 - 批量更新图表
    /// </summary>
    private void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        if (_hasNewData && _chart != null)
        {
            _hasNewData = false;

            // 刷新图表 - ScottPlot 自动优化渲染
            Application.Current.Dispatcher.Invoke(() =>
            {
                _chart.Plot.Axes.AutoScale();
                _chart.Refresh();
            }, DispatcherPriority.Background);
        }
    }

    private void OnMqttSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MqttSettings.MaxChartDataPoints))
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MaxChartDataPoints = _mqttSettings.MaxChartDataPoints;
                RefreshChartDataPointsLimit();
            }), DispatcherPriority.Normal);
        }
        else if (e.PropertyName == nameof(MqttSettings.ChartUpdateInterval))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _updateTimer.Interval = TimeSpan.FromMilliseconds(_mqttSettings.ChartUpdateInterval);
                _logService.LogInfo($"图表更新间隔已更新为: {_mqttSettings.ChartUpdateInterval}ms");
            });
        }
    }

    /// <summary>
    /// 刷新图表数据点限制
    /// </summary>
    private void RefreshChartDataPointsLimit()
    {
        foreach (var deviceSeriesMap in _seriesMap.Values)
        {
            foreach (var plotData in deviceSeriesMap.Values)
            {
                while (plotData.XData.Count > MaxChartDataPoints)
                {
                    plotData.XData.RemoveAt(0);
                    plotData.YData.RemoveAt(0);
                    plotData.OriginalYData.RemoveAt(0);
                }
            }
        }
        _logService.LogInfo($"图表数据点限制已更新为: {MaxChartDataPoints}");
    }

    /// <summary>
    /// 当收到上行数据时 - 只添加数据，不立即刷新UI
    /// </summary>
    private void OnUpstreamDataParsed(UpstreamDataPacket dataPacket)
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_chart == null) return;

                var deviceId = dataPacket.DeviceId;

                // 确保设备存在于映射中
                if (!_seriesMap.ContainsKey(deviceId))
                {
                    _seriesMap[deviceId] = new Dictionary<string, ScatterPlotData>();
                }

                var deviceSeries = _seriesMap[deviceId];

                // 处理每个测点
                if (dataPacket.Payload != null)
                {
                    foreach (var metric in dataPacket.Payload)
                    {
                        // 获取或创建系列
                        ScatterPlotData plotData;
                        if (!deviceSeries.ContainsKey(metric.Name))
                        {
                            var metricIndex = deviceSeries.Count;
                            plotData = CreateSeries(deviceId, metric.Name, metricIndex);
                            deviceSeries[metric.Name] = plotData;
                        }
                        else
                        {
                            plotData = deviceSeries[metric.Name];
                        }

                        // 添加时间戳（转换为 OA Date）
                        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(dataPacket.Timestamp).ToLocalTime();
                        plotData.XData.Add(timestamp.DateTime.ToOADate());

                        // 保存原始值
                        plotData.OriginalYData.Add(metric.Value);

                        // 获取偏移量
                        var offset = 0.0;
                        if (_legendItemsMap.TryGetValue(deviceId, out var legendItems) &&
                            legendItems.TryGetValue(metric.Name, out var legendItem))
                        {
                            offset = legendItem.Offset;
                        }

                        // 添加数据点（应用偏移量）
                        plotData.YData.Add(metric.Value + offset);

                        // 限制数据点数量
                        if (plotData.XData.Count > _mqttSettings.MaxChartDataPoints)
                        {
                            plotData.XData.RemoveAt(0);
                            plotData.YData.RemoveAt(0);
                            plotData.OriginalYData.RemoveAt(0);
                        }


                    }
                }

                // 标记有新数据
                _hasNewData = true;
            }, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "更新图表数据失败");
        }
    }

    /// <summary>
    /// 创建新的系列
    /// </summary>
    private ScatterPlotData CreateSeries(string deviceId, string metricName, int metricIndex)
    {
        if (_chart == null)
            throw new InvalidOperationException("Chart not initialized");

        // 检查是否有保存的配置
        var savedConfig = _configService.GetConfig(deviceId, metricName, _loadedConfig);

        ScottPlot.Color color;
        double offset = 0;

        if (savedConfig != null)
        {
            offset = savedConfig.Offset;

            // 解析保存的颜色
            if (savedConfig.ColorHex.StartsWith("#") && savedConfig.ColorHex.Length == 7)
            {
                try
                {
                    color = ScottPlot.Color.FromHex(savedConfig.ColorHex);
                    _logService.LogInfo($"使用保存的配置: [{deviceId} - {metricName}] 颜色={savedConfig.ColorHex}, 偏移={offset}");
                }
                catch
                {
                    color = GenerateDefaultColor(deviceId, metricIndex);
                }
            }
            else
            {
                color = GenerateDefaultColor(deviceId, metricIndex);
            }
        }
        else
        {
            color = GenerateDefaultColor(deviceId, metricIndex);
        }

        var plotData = new ScatterPlotData
        {
            XData = new List<double>(),
            YData = new List<double>(),
            OriginalYData = new List<double>()
        };

        // 创建 ScottPlot 散点图
        var scatter = _chart.Plot.Add.Scatter(plotData.XData, plotData.YData);
        plotData.Plot = scatter;
        scatter.Color = color;
        scatter.LineWidth = 1.5f;
        scatter.MarkerSize = 0; // 不显示标记点以提升性能
        scatter.LegendText = $"{deviceId} - {metricName}";

        // 启用平滑（可选）
        scatter.Smooth = true;

        // 创建图例项
        var legendItem = new ChartLegendItem
        {
            DeviceId = deviceId,
            ParameterName = metricName,
            ColorHex = color.ToHex(),
            Offset = offset
        };

        // 监听属性变化
        legendItem.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ChartLegendItem.Offset))
            {
                UpdateSeriesOffset(deviceId, metricName);
                SaveConfig();
            }
            else if (e.PropertyName == nameof(ChartLegendItem.ColorHex))
            {
                UpdateSeriesColor(deviceId, metricName);
                SaveConfig();
            }
        };

        // 保存图例项
        if (!_legendItemsMap.ContainsKey(deviceId))
        {
            _legendItemsMap[deviceId] = new Dictionary<string, ChartLegendItem>();
        }
        _legendItemsMap[deviceId][metricName] = legendItem;

        // 添加到图例列表并排序
        LegendItems = new ObservableCollection<ChartLegendItem>(
            _legendItemsMap.Values.SelectMany(dict => dict.Values).OrderBy(item => item.DeviceId));

        // 更新分组集合
        UpdateLegendGroups();

        return plotData;
    }

    /// <summary>
    /// 更新图例分组集合
    /// </summary>
    private void UpdateLegendGroups()
    {
        var groups = new List<ChartLegendGroupViewModel>();

        // 按设备ID排序后分组
        var orderedDevices = _legendItemsMap.Keys.OrderBy(id => id);

        foreach (var deviceId in orderedDevices)
        {
            var deviceLegends = _legendItemsMap[deviceId];

            // 查找现有分组以保持展开状态
            var existingGroup = LegendGroups.FirstOrDefault(g => g.DeviceId == deviceId);

            var group = new ChartLegendGroupViewModel
            {
                DeviceId = deviceId,
                Items = new ObservableCollection<ChartLegendItem>(deviceLegends.Values),
                IsExpanded = existingGroup?.IsExpanded ?? false, // 保持现有展开状态，新建默认关闭
                IsVisible = existingGroup?.IsVisible ?? true // 保持现有可见状态，新建默认可见
            };

            // 监听 IsVisible 属性变化
            group.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(ChartLegendGroupViewModel.IsVisible))
                {
                    UpdateDeviceVisibility(deviceId, group.IsVisible);
                }
            };

            groups.Add(group);
        }

        LegendGroups = new ObservableCollection<ChartLegendGroupViewModel>(groups);
    }

    /// <summary>
    /// 更新设备的可见性
    /// </summary>
    private void UpdateDeviceVisibility(string deviceId, bool isVisible)
    {
        try
        {
            if (_seriesMap.TryGetValue(deviceId, out var deviceSeries))
            {
                foreach (var plotData in deviceSeries.Values)
                {
                    plotData.Plot.IsVisible = isVisible;
                }

                _chart?.Refresh();
                _logService.LogInfo($"已{(isVisible ? "显示" : "隐藏")}设备 [{deviceId}] 的所有曲线");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "更新设备可见性失败");
        }
    }

    /// <summary>
    /// 生成默认颜色
    /// </summary>
    private ScottPlot.Color GenerateDefaultColor(string deviceId, int metricIndex)
    {
        // 获取或分配设备的基础颜色
        if (!_deviceBaseColors.ContainsKey(deviceId))
        {
            var hue = _baseHues[_nextDeviceColorIndex % _baseHues.Count];
            _deviceBaseColors[deviceId] = ScottPlot.Color.FromHSL((float)hue, (float)0.8, (float)0.6);
            _nextDeviceColorIndex++;
            _logService.LogInfo($"为设备 [{deviceId}] 分配颜色，色调: {hue}°");
        }

        var baseColor = _deviceBaseColors[deviceId];

        // 为同一设备的不同指标创建颜色变体
        return CreateColorVariant(baseColor, metricIndex);
    }

    /// <summary>
    /// 创建颜色变体
    /// </summary>
    private ScottPlot.Color CreateColorVariant(ScottPlot.Color baseColor, int index)
    {
        var (h, s, l) = baseColor.ToHSL();

        // 调整色调、饱和度和亮度
        var hueOffset = (index * 10) % 20 - 10;
        var newHue = (h + hueOffset + 360) % 360;

        var saturation = index % 2 == 0
            ? Math.Max(0.6, Math.Min(1.0, s - index * 0.05))
            : Math.Max(0.5, Math.Min(0.9, s + index * 0.05));

        var lightness = index % 2 == 0
            ? Math.Max(0.5, Math.Min(0.7, l + index * 0.03))
            : Math.Max(0.4, Math.Min(0.6, l - index * 0.03));

        return ScottPlot.Color.FromHSL((float)newHue, (float)saturation, (float)lightness);
    }

    /// <summary>
    /// 更新系列偏移量
    /// </summary>
    private void UpdateSeriesOffset(string deviceId, string metricName)
    {
        try
        {
            if (_seriesMap.TryGetValue(deviceId, out var deviceSeries) &&
                deviceSeries.TryGetValue(metricName, out var plotData) &&
                _legendItemsMap.TryGetValue(deviceId, out var legendItems) &&
                legendItems.TryGetValue(metricName, out var legendItem))
            {
                var offset = legendItem.Offset;

                // 重新计算所有数据点
                plotData.YData.Clear();
                foreach (var originalValue in plotData.OriginalYData)
                {
                    plotData.YData.Add(originalValue + offset);
                }



                // 立即刷新
                _chart?.Refresh();
                _logService.LogInfo($"已更新 [{deviceId} - {metricName}] 的偏移量为: {offset}");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "更新偏移量失败");
        }
    }

    /// <summary>
    /// 更新系列颜色
    /// </summary>
    private void UpdateSeriesColor(string deviceId, string metricName)
    {
        try
        {
            if (_seriesMap.TryGetValue(deviceId, out var deviceSeries) &&
                deviceSeries.TryGetValue(metricName, out var plotData) &&
                _legendItemsMap.TryGetValue(deviceId, out var legendItems) &&
                legendItems.TryGetValue(metricName, out var legendItem))
            {
                var colorHex = legendItem.ColorHex;
                if (colorHex.StartsWith("#") && colorHex.Length == 7)
                {
                    try
                    {
                        plotData.Plot.Color = ScottPlot.Color.FromHex(colorHex);
                        _chart?.Refresh();
                        _logService.LogInfo($"已更新 [{deviceId} - {metricName}] 的颜色为: {colorHex}");
                    }
                    catch (Exception ex)
                    {
                        _logService.LogException(ex, $"解析颜色失败: {colorHex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "更新颜色失败");
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveConfig()
    {
        try
        {
            _configService.SaveConfig(LegendItems.ToList());
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存图例配置时发生异常");
        }
    }

    /// <summary>
    /// 清空数据
    /// </summary>
    private void OnDataCleared()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_chart != null)
            {
                _chart.Plot.Clear();
                _chart.Refresh();
            }

            _seriesMap.Clear();
            _legendItemsMap.Clear();
            LegendItems.Clear();
            LegendGroups.Clear();
            _deviceBaseColors.Clear();
            _nextDeviceColorIndex = 0;
            _logService.LogInfo("图表数据已清空");
        });
    }

    /// <summary>
    /// 设置鼠标跟踪功能
    /// </summary>
    private void SetupMouseTracking()
    {
        if (_chart == null)
            return;

        _chart.MouseMove += (s, e) =>
        {
            if (_crosshair == null || _hoverLabel == null)
                return;

            try
            {
                // 获取鼠标像素位置
                var mousePixel = e.GetPosition(_chart);

                // 查找所有可见折线中最近的数据点（基于像素距离）
                string? nearestDeviceId = null;
                string? nearestMetricName = null;
                int nearestIndex = -1;
                double minDistance = double.MaxValue;
                double nearestX = 0;
                double nearestY = 0;

                foreach (var kvp in _seriesMap)
                {
                    var deviceId = kvp.Key;
                    var deviceSeries = kvp.Value;

                    foreach (var metricKvp in deviceSeries)
                    {
                        var metricName = metricKvp.Key;
                        var plotData = metricKvp.Value;

                        // 跳过不可见的折线
                        if (!plotData.Plot.IsVisible)
                            continue;

                        // 查找最近的点（基于像素距离）
                        for (int i = 0; i < plotData.XData.Count; i++)
                        {
                            // 将数据点坐标转换为像素坐标
                            var dataCoord = new Coordinates(plotData.XData[i], plotData.YData[i]);
                            var pixelCoord = _chart.Plot.GetPixel(dataCoord);

                            // 计算像素距离（欧氏距离）
                            double dx = pixelCoord.X - mousePixel.X;
                            double dy = pixelCoord.Y - mousePixel.Y;
                            double distance = Math.Sqrt(dx * dx + dy * dy);

                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                nearestDeviceId = deviceId;
                                nearestMetricName = metricName;
                                nearestIndex = i;
                                nearestX = plotData.XData[i];
                                nearestY = plotData.YData[i];
                            }
                        }
                    }
                }

                // 如果找到了最近的点，显示Crosshair和标签
                if (nearestDeviceId != null && nearestMetricName != null && nearestIndex >= 0)
                {
                    // 更新Crosshair位置
                    _crosshair.Position = new Coordinates(nearestX, nearestY);
                    _crosshair.IsVisible = true;

                    // 获取原始值（不含偏移量）
                    var plotData = _seriesMap[nearestDeviceId][nearestMetricName];
                    var originalValue = plotData.OriginalYData[nearestIndex];
                    var offset = nearestY - originalValue;

                    // 转换时间
                    var time = DateTime.FromOADate(nearestX);

                    // 更新标签
                    var labelText = $"设备: {nearestDeviceId}\n" +
                                  $"测点: {nearestMetricName}\n" +
                                  $"时间: {time:HH:mm:ss}\n" +
                                  $"数值: {originalValue:F2}";

                    if (Math.Abs(offset) > 0.001)
                    {
                        labelText += $"\n偏移: {offset:+0.##;-0.##;0}";
                    }

                    _hoverLabel.LabelText = labelText;
                    _hoverLabel.Location = new Coordinates(nearestX, nearestY);
                    _hoverLabel.OffsetY = -50; // 向上偏移，避免遮挡数据点
                    _hoverLabel.IsVisible = true;

                    _chart.Refresh();
                }
                else
                {
                    // 没有找到数据点，隐藏
                    _crosshair.IsVisible = false;
                    _hoverLabel.IsVisible = false;
                    _chart.Refresh();
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, "鼠标跟踪失败");
            }
        };

        _chart.MouseLeave += (s, e) =>
        {
            if (_crosshair != null)
                _crosshair.IsVisible = false;
            if (_hoverLabel != null)
                _hoverLabel.IsVisible = false;
            _chart?.Refresh();
        };
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
