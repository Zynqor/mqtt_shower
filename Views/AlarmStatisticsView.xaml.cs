using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using MqttMonitor.ViewModels;
using ScottPlot;

namespace MqttMonitor.Views;

public partial class AlarmStatisticsView : UserControl
{
    private AlarmStatisticsViewModel? _viewModel;

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
        var font = new ScottPlot.Fonts.FontFamily
        {
            Default = "Microsoft YaHei UI",
            Serif = "Microsoft YaHei UI",
            SansSerif = "Microsoft YaHei UI",
            Monospace = "Microsoft YaHei UI"
        };

        // 设置柱状图样式
        AlarmCountChart.Plot.Font.Set(font);
        AlarmCountChart.Plot.Title("设备/测点告警次数统计（Top 20）");
        AlarmCountChart.Plot.XLabel("设备/测点");
        AlarmCountChart.Plot.YLabel("告警次数");
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Rotation = 45;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

        // 设置折线图样式
        AlarmTrendChart.Plot.Font.Set(font);
        AlarmTrendChart.Plot.Title("告警趋势");
        AlarmTrendChart.Plot.XLabel("时间");
        AlarmTrendChart.Plot.YLabel("告警次数");

        // 设置饼图样式
        AlarmTypeChart.Plot.Font.Set(font);
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
            AlarmCountChart.Refresh();
            return;
        }

        // 准备数据
        var positions = Enumerable.Range(0, data.Count).Select(x => (double)x).ToArray();
        var values = data.Select(x => (double)x.Count).ToArray();
        var labels = data.Select(x => x.Label).ToArray();

        // 添加柱状图
        var barPlot = AlarmCountChart.Plot.Add.Bars(positions, values);
        barPlot.Color = Colors.Red.WithAlpha(0.7);

        // 设置X轴标签
        AlarmCountChart.Plot.Axes.Bottom.SetTicks(positions, labels);
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Rotation = 45;
        AlarmCountChart.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;

        // 设置Y轴从0开始
        AlarmCountChart.Plot.Axes.SetLimits(bottom: 0);

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
        linePlot.MarkerSize = 6;

        // 设置X轴为日期时间
        AlarmTrendChart.Plot.Axes.DateTimeTicksBottom();

        // 设置Y轴从0开始
        AlarmTrendChart.Plot.Axes.SetLimits(bottom: 0);

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

        AlarmTypeChart.Refresh();
    }
}
