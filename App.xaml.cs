using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using MqttMonitor.Models;
using MqttMonitor.Services;
using MqttMonitor.ViewModels;
using MqttMonitor.Views;
using Newtonsoft.Json; // Add this using statement
using System.IO;

namespace MqttMonitor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public App()
    {
        // Subscribe to the global exception handler
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure dependency injection
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        ServiceProvider = serviceCollection.BuildServiceProvider();

        // 初始化告警检测服务（需要在启动时订阅事件）
        var alarmDetectionService = ServiceProvider.GetRequiredService<AlarmDetectionService>();

        // Show main window
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register MqttSettings as a singleton
        services.AddSingleton<MqttSettings>(sp =>
        {
            var logService = sp.GetRequiredService<LogService>();
            var encryptionService = sp.GetRequiredService<EncryptionService>();
            var configFilePath = "config.json";
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    var settings = JsonConvert.DeserializeObject<MqttSettings>(json) ?? new MqttSettings();

                    // 解密密码（如果已加密）
                    if (!string.IsNullOrEmpty(settings.Password))
                    {
                        try
                        {
                            settings.Password = encryptionService.Decrypt(settings.Password);
                        }
                        catch (Exception decryptEx)
                        {
                            logService.LogException(decryptEx, "解密密码失败，可能需要重新设置密码");
                            // 解密失败时清空密码，用户需要重新输入
                            settings.Password = null;
                        }
                    }

                    return settings;
                }
            }
            catch (Exception ex)
            {
                logService.LogException(ex, "加载 MQTT 设置失败");
            }
            return new MqttSettings(); // Return default settings on error or if file not found
        });

        // Register MainWindow
        services.AddSingleton<MainWindow>();

        // Register AlertSettings as a singleton
        services.AddSingleton<AlertSettings>(sp =>
        {
            var alarmConfigService = sp.GetRequiredService<AlarmConfigService>();
            return alarmConfigService.LoadAlertSettings();
        });

        // Register Services
        services.AddSingleton<LogService>();
        services.AddSingleton<EncryptionService>();
        services.AddSingleton<CsvDataStorageService>();
        services.AddSingleton<MqttService>();
        services.AddSingleton<DataProcessingService>();
        services.AddSingleton<ChartLegendConfigService>();
        services.AddSingleton<AlarmConfigService>();
        services.AddSingleton<SoundPlayerService>();
        services.AddSingleton<AlarmHistoryStorageService>();
        services.AddSingleton<AlarmDetectionService>();

        // Register ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LogViewModel>();
        services.AddSingleton<TableViewModel>();
        services.AddSingleton<ChartViewModel>();
        services.AddSingleton<CommandSenderViewModel>();
        services.AddSingleton<AlarmViewModel>();
        services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(
            sp.GetRequiredService<LogService>(),
            sp.GetRequiredService<EncryptionService>(),
            sp.GetRequiredService<MqttSettings>())); // Transient for new instance each time
        services.AddTransient<ChartSettingsViewModel>(sp => new ChartSettingsViewModel(
            sp.GetRequiredService<LogService>(),
            sp.GetRequiredService<MqttSettings>(),
            sp.GetRequiredService<EncryptionService>())); // Transient for new instance each time
        services.AddTransient<AlarmConfigViewModel>();
        services.AddTransient<AlertSettingsViewModel>();

        // Register Views
        services.AddSingleton<LogView>();
        services.AddSingleton<TableView>();
        services.AddSingleton<ChartView>();
        services.AddSingleton<CommandSenderView>();
        services.AddSingleton<AlarmView>();
        services.AddTransient<SettingsWindow>(); // Transient for new instance each time
        services.AddTransient<ChartSettingsWindow>(); // Transient for new instance each time
        services.AddTransient<ContactUsWindow>(); // Transient for new instance each time
        services.AddTransient<AlarmConfigWindow>(); // Transient for new instance each time
        services.AddTransient<AlertSettingsWindow>(); // Transient for new instance each time
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanup CsvDataStorageService
        var csvStorage = ServiceProvider?.GetService<CsvDataStorageService>();
        csvStorage?.Dispose();

        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Log the exception using the LogService
        var logService = ServiceProvider?.GetService<LogService>();
        if (logService != null)
        {
            logService.LogException(e.Exception, "应用程序发生未处理的异常");
        }
        else
        {
            // Fallback if LogService is not available yet
            System.IO.File.AppendAllText("error.log", $"[{DateTime.Now}] Unhandled exception: {e.Exception.Message}\n{e.Exception.StackTrace}\n");
        }

        // Prevent the application from crashing
        e.Handled = true;

        MessageBox.Show($"应用程序发生未处理的错误：{e.Exception.Message}\n错误详情已记录到日志文件。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
