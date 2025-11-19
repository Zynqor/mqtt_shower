using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Data;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// TableView.xaml 的交互逻辑
/// </summary>
public partial class TableView : UserControl
{
    private readonly TableViewModel _viewModel;

    public TableView(TableViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // 监听列名集合变化，动态生成列
        _viewModel.ColumnNames.CollectionChanged += OnColumnNamesChanged;

        // 初始化列
        UpdateColumns();
    }

    /// <summary>
    /// 当列名集合变化时
    /// </summary>
    private void OnColumnNamesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateColumns();
    }

    /// <summary>
    /// 更新 DataGrid 列
    /// </summary>
    private void UpdateColumns()
    {
        DataGrid.Columns.Clear();

        // 添加设备ID列
        var deviceIdHeader = System.Windows.Application.Current?.TryFindResource("Table.DeviceId") as string ?? "设备ID";
        DataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = deviceIdHeader,
            Binding = new Binding("DeviceId"),
            Width = new DataGridLength(150)
        });

        // 添加测点列
        foreach (var columnName in _viewModel.ColumnNames)
        {
            if (columnName == "设备ID") continue;

            DataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = columnName,
                Binding = new Binding($"[{columnName}]"),
                Width = new DataGridLength(120)
            });
        }
    }
}
