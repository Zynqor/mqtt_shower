using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using MqttMonitor.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;

namespace MqttMonitor.Views;

public partial class AlarmStatisticsView : UserControl
{
    private AlarmStatisticsViewModel? _viewModel;
    private Crosshair? _trendChartCrosshair;
    private Text? _trendChartLabel;

    public AlarmStatisticsView(AlarmStatisticsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        Loaded += async (s, e) =>
        {
            if (_viewModel != null)
            {
                // 订阅数据变化
                _viewModel.AlarmCounts.CollectionChanged += OnAlarmCountsChanged;
                _viewModel.AlarmTrends.CollectionChanged += OnAlarmTrendsChanged;
                _viewModel.AlarmTypeDistributions.CollectionChanged += OnAlarmTypeDistributionsChanged;

                // 初始化图表
                InitializeCharts();

                // 为折线图添加鼠标悬浮功能
                SetupTrendChartMouseTracking();

                // 加载今天的数据
                await _viewModel.LoadStatisticsAsync();

                // 绘制初始数据
                UpdateAlarmCountChart();
                UpdateAlarmTrendChart();
                UpdateAlarmTypeChart();
            }
        };
    }

    private void InitializeCharts()
    {
        // 设置中文字体
        var fontName = "Microsoft YaHei UI";

        // 设置柱状图样式
        AlarmCountChart.Plot.Font.Automatic();
        AlarmCountChart.Plot.Title("设备/测点告警次数统计（Top 20）");
        AlarmCountChart.Plot.YLabel("告警次数");
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.UpperCenter;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.FontName = fontName;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Bold = true;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#333333");
        AlarmCountChart.Plot.Axes.Left.TickLabelStyle.FontName = fontName;

        // 设置折线图样式
        AlarmTrendChart.Plot.Font.Automatic();
        AlarmTrendChart.Plot.Title("告警趋势");
        AlarmTrendChart.Plot.XLabel("时间");
        AlarmTrendChart.Plot.YLabel("告警次数");
        AlarmTrendChart.Plot.Axes.Bottom.TickLabelStyle.FontName = fontName;
        AlarmTrendChart.Plot.Axes.Left.TickLabelStyle.FontName = fontName;

        // 设置饼图样式
        AlarmTypeChart.Plot.Font.Automatic();
        AlarmTypeChart.Plot.Title("告警类型分布");
    }

    private void OnAlarmCountsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateAlarmCountChart();
    }

    private void OnAlarmTrendsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateAlarmTrendChart();
    }

    private void OnAlarmTypeDistributionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateAlarmTypeChart();
    }

    /// <summary>
    /// 更新告警次数柱状图
    /// </summary>
    private void UpdateAlarmCountChart()
    {
        if (_viewModel == null)
            return;

        AlarmCountChart.Plot.Clear();

        var data = _viewModel.AlarmCounts.ToList();
        if (data.Count == 0)
        {
            AlarmCountChart.Plot.Axes.AutoScale();
            AlarmCountChart.Refresh();
            return;
        }

        // 准备数据
        var positions = Enumerable.Range(0, data.Count).Select(x => (double)x).ToArray();
        var values = data.Select(x => (double)x.Count).ToArray();
        var labels = data.Select(x => x.Label).ToArray();

        // 添加柱状图
        var bars = AlarmCountChart.Plot.Add.Bars(positions, values);
        bars.Color = Colors.Red.WithAlpha(0.7);

        // 在每个柱子上方添加数值标签
        var fontName = "Microsoft YaHei UI";
        for (int i = 0; i < positions.Length; i++)
        {
            var text = AlarmCountChart.Plot.Add.Text(values[i].ToString("F0"), positions[i], values[i]);
            text.LabelStyle.FontSize = 11;
            text.LabelStyle.FontName = fontName;
            text.LabelStyle.Bold = true;
            text.LabelStyle.ForeColor = ScottPlot.Color.FromHex("#333333");
            text.OffsetY = 10; // 向上偏移，显示在柱子上方
        }

        // 设置X轴标签（横向显示）
        AlarmCountChart.Plot.Axes.Bottom.SetTicks(positions, labels);
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.UpperCenter;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Bold = true;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromHex("#333333");

        // 设置Y轴从0开始
        AlarmCountChart.Plot.Axes.SetLimits(bottom: 0);

        // 自动调整尺度
        AlarmCountChart.Plot.Axes.AutoScale();

        AlarmCountChart.Refresh();
    }

    /// <summary>
    /// 更新告警趋势折线图
    /// </summary>
    private void UpdateAlarmTrendChart()
    {
        if (_viewModel == null)
            return;

        AlarmTrendChart.Plot.Clear();

        var data = _viewModel.AlarmTrends.ToList();
        if (data.Count == 0)
        {
            // 清空Crosshair
            _trendChartCrosshair = null;
            _trendChartLabel = null;
            AlarmTrendChart.Plot.Axes.AutoScale();
            AlarmTrendChart.Refresh();
            return;
        }

        // 准备数据
        var times = data.Select(x => x.Time.ToOADate()).ToArray();
        var counts = data.Select(x => (double)x.Count).ToArray();

        // 添加折线图
        var linePlot = AlarmTrendChart.Plot.Add.Scatter(times, counts);
        linePlot.Color = Colors.Red;
        linePlot.LineWidth = 2;
        linePlot.MarkerSize = 8;
        linePlot.LinePattern = LinePattern.Solid;

        // 添加Crosshair用于鼠标悬浮显示
        _trendChartCrosshair = AlarmTrendChart.Plot.Add.Crosshair(0, 0);
        _trendChartCrosshair.IsVisible = false;
        _trendChartCrosshair.LineColor = ScottPlot.Color.FromHex("#666666");
        _trendChartCrosshair.LineWidth = 1;

        // 添加数值标签（初始不可见）
        _trendChartLabel = AlarmTrendChart.Plot.Add.Text("", 0, 0);
        _trendChartLabel.LabelStyle.FontSize = 12;
        _trendChartLabel.LabelStyle.FontName = "Microsoft YaHei UI";
        _trendChartLabel.LabelStyle.Bold = true;
        _trendChartLabel.LabelStyle.ForeColor = ScottPlot.Color.FromHex("#333333");
        _trendChartLabel.LabelStyle.BackColor = ScottPlot.Color.FromHex("#FFFFFF").WithAlpha(0.9);
        _trendChartLabel.LabelStyle.BorderColor = ScottPlot.Color.FromHex("#666666");
        _trendChartLabel.LabelStyle.BorderWidth = 1;
        _trendChartLabel.LabelStyle.Padding = 5;
        _trendChartLabel.IsVisible = false;

        // 设置X轴为日期时间
        AlarmTrendChart.Plot.Axes.DateTimeTicksBottom();

        // 设置Y轴从0开始
        AlarmTrendChart.Plot.Axes.SetLimits(bottom: 0);

        // 自动调整尺度
        AlarmTrendChart.Plot.Axes.AutoScale();

        AlarmTrendChart.Refresh();
    }

    /// <summary>
    /// 更新告警类型饼图
    /// </summary>
    private void UpdateAlarmTypeChart()
    {
        if (_viewModel == null)
            return;

        AlarmTypeChart.Plot.Clear();

        var data = _viewModel.AlarmTypeDistributions.ToList();
        if (data.Count == 0)
        {
            AlarmTypeChart.Plot.Axes.AutoScale();
            AlarmTypeChart.Refresh();
            return;
        }

        // 准备数据
        var values = data.Select(x => (double)x.Count).ToArray();
        var labels = data.Select(x => $"{x.TypeName}\n{x.Count}次 ({x.Percentage:F1}%)").ToArray();

        // 添加饼图
        var pie = AlarmTypeChart.Plot.Add.Pie(values);

        // 设置第一个切片（上限告警）
        if (pie.Slices.Count > 0)
        {
            pie.Slices[0].FillColor = Colors.Red.WithAlpha(0.8);
            pie.Slices[0].LabelStyle.Text = labels[0];
            pie.Slices[0].LabelStyle.FontSize = 12;
            pie.Slices[0].LabelStyle.FontName = "Microsoft YaHei UI";
        }

        // 设置第二个切片（下限告警）
        if (pie.Slices.Count > 1)
        {
            pie.Slices[1].FillColor = Colors.Blue.WithAlpha(0.8);
            pie.Slices[1].LabelStyle.Text = labels[1];
            pie.Slices[1].LabelStyle.FontSize = 12;
            pie.Slices[1].LabelStyle.FontName = "Microsoft YaHei UI";
        }

        // 自动调整尺度
        AlarmTypeChart.Plot.Axes.AutoScale();

        AlarmTypeChart.Refresh();
    }

    /// <summary>
    /// 设置折线图鼠标跟踪功能
    /// </summary>
    private void SetupTrendChartMouseTracking()
    {
        AlarmTrendChart.MouseMove += (s, e) =>
        {
            if (_viewModel == null || _trendChartCrosshair == null || _trendChartLabel == null)
                return;

            var data = _viewModel.AlarmTrends.ToList();
            if (data.Count == 0)
                return;

            // 获取鼠标位置对应的坐标
            var mousePixel = e.GetPosition(AlarmTrendChart);
            var mouseCoordinate = AlarmTrendChart.Plot.GetCoordinates((float)mousePixel.X, (float)mousePixel.Y);

            // 查找最近的数据点
            var times = data.Select(x => x.Time.ToOADate()).ToArray();
            var counts = data.Select(x => (double)x.Count).ToArray();

            int nearestIndex = -1;
            double minDistance = double.MaxValue;

            for (int i = 0; i < times.Length; i++)
            {
                double distance = System.Math.Abs(times[i] - mouseCoordinate.X);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }

            if (nearestIndex >= 0 && nearestIndex < data.Count)
            {
                // 更新Crosshair位置
                _trendChartCrosshair.Position = new Coordinates(times[nearestIndex], counts[nearestIndex]);
                _trendChartCrosshair.IsVisible = true;

                // 更新标签
                var time = data[nearestIndex].Time;
                var count = data[nearestIndex].Count;
                _trendChartLabel.LabelText = $"{time:yyyy-MM-dd HH:mm}\n告警次数: {count}";
                _trendChartLabel.Location = new Coordinates(times[nearestIndex], counts[nearestIndex]);
                _trendChartLabel.OffsetY = -40; // 向上偏移，避免遮挡数据点
                _trendChartLabel.IsVisible = true;

                AlarmTrendChart.Refresh();
            }
        };

        AlarmTrendChart.MouseLeave += (s, e) =>
        {
            if (_trendChartCrosshair != null)
                _trendChartCrosshair.IsVisible = false;
            if (_trendChartLabel != null)
                _trendChartLabel.IsVisible = false;
            AlarmTrendChart.Refresh();
        };
    }
}
