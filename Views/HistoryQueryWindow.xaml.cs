using System.Windows;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// HistoryQueryWindow.xaml 的交互逻辑
/// </summary>
public partial class HistoryQueryWindow : Window
{
    public HistoryQueryWindow(HistoryQueryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // 数据加载后动态生成列
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(HistoryQueryViewModel.HistoryData) && viewModel.HistoryData.Count > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    // 清除除时间列外的所有列
                    while (HistoryDataGrid.Columns.Count > 1)
                    {
                        HistoryDataGrid.Columns.RemoveAt(1);
                    }

                    // 收集所有列名
                    var allColumns = new System.Collections.Generic.HashSet<string>();
                    foreach (var row in viewModel.HistoryData)
                    {
                        foreach (var key in row.Data.Keys)
                        {
                            allColumns.Add(key);
                        }
                    }

                    // 添加数据列
                    foreach (var column in allColumns.OrderBy(c => c))
                    {
                        var dataColumn = new System.Windows.Controls.DataGridTextColumn
                        {
                            Header = column,
                            Binding = new System.Windows.Data.Binding($"Data[{column}]"),
                            Width = new System.Windows.Controls.DataGridLength(120)
                        };
                        HistoryDataGrid.Columns.Add(dataColumn);
                    }
                });
            }
        };
    }
}
