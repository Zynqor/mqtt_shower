using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using MqttMonitor.ViewModels;

namespace MqttMonitor.Views;

/// <summary>
/// ChartView.xaml 的交互逻辑
/// </summary>
public partial class ChartView : UserControl
{
    public ChartView(ChartViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // 将 WpfPlot 控件传递给 ViewModel
        viewModel.InitializeChart(ChartControl);
    }

    /// <summary>
    /// 处理文本框按键事件（回车键触发更新）
    /// </summary>
    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // 获取TextBox
            if (sender is TextBox textBox)
            {
                // 强制更新绑定源
                var bindingExpression = textBox.GetBindingExpression(TextBox.TextProperty);
                bindingExpression?.UpdateSource();

                // 移除焦点，触发LostFocus
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }

            e.Handled = true;
        }
    }
}
