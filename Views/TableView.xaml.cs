using System.Collections.Specialized;
using System.Windows.Controls;
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

        // 监听列名集合变化，更新显示
        _viewModel.ColumnNames.CollectionChanged += OnColumnNamesChanged;
        _viewModel.DataRows.CollectionChanged += OnDataRowsChanged;
    }

    /// <summary>
    /// 当列名集合变化时
    /// </summary>
    private void OnColumnNamesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _viewModel.UpdateDeviceDisplayItems();
    }

    /// <summary>
    /// 当数据行变化时
    /// </summary>
    private void OnDataRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _viewModel.UpdateDeviceDisplayItems();
    }
}
