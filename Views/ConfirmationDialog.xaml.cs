using System.Windows;

namespace MqttMonitor.Views;

/// <summary>
/// 确认对话框
/// </summary>
public partial class ConfirmationDialog : Window
{
    public bool Result { get; private set; }

    public ConfirmationDialog(string message)
    {
        InitializeComponent();
        MessageTextBlock.Text = message;
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
