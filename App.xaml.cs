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

        // 初始化路径管理器（创建目录结构并迁移旧配置）
        PathManager.Initialize();

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
            var configFilePath = PathManager.MqttConfigFile;
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

        // Register ChartConfig as a singleton (with migration from old config.json)
        services.AddSingleton<ChartConfig>(sp =>
        {
            var chartConfigService = sp.GetRequiredService<ChartConfigService>();
            var logService = sp.GetRequiredService<LogService>();
            var chartConfig = chartConfigService.LoadChartConfig();

            // 如果chart_config.json不存在，尝试从旧的config.json迁移
            if (!File.Exists("chart_config.json") && File.Exists("config.json"))
            {
                try
                {
                    var oldConfigJson = File.ReadAllText("config.json");
                    dynamic? oldConfig = JsonConvert.DeserializeObject(oldConfigJson);
                    if (oldConfig != null)
                    {
                        if (oldConfig.MaxChartDataPoints != null)
                            chartConfig.MaxChartDataPoints = (int)oldConfig.MaxChartDataPoints;
                        if (oldConfig.ChartUpdateInterval != null)
                            chartConfig.ChartUpdateInterval = (int)oldConfig.ChartUpdateInterval;

                        chartConfigService.SaveChartConfig(chartConfig);
                        logService.LogInfo("已从config.json迁移图表配置到chart_config.json");
                    }
                }
                catch (Exception ex)
                {
                    logService.LogException(ex, "从config.json迁移图表配置失败");
                }
            }

            return chartConfig;
        });

        // Register CompanyInfo as a singleton (with migration from old config.json)
        services.AddSingleton<CompanyInfo>(sp =>
        {
            var companyInfoService = sp.GetRequiredService<CompanyInfoService>();
            var logService = sp.GetRequiredService<LogService>();
            var companyInfo = companyInfoService.LoadCompanyInfo();

            // 如果company_info.json不存在，尝试从旧的config.json迁移
            if (!File.Exists("company_info.json") && File.Exists("config.json"))
            {
                try
                {
                    var oldConfigJson = File.ReadAllText("config.json");
                    dynamic? oldConfig = JsonConvert.DeserializeObject(oldConfigJson);
                    if (oldConfig != null)
                    {
                        if (oldConfig.CompanyName != null)
                            companyInfo.CompanyName = (string)oldConfig.CompanyName;
                        if (oldConfig.CompanyAddress != null)
                            companyInfo.CompanyAddress = (string)oldConfig.CompanyAddress;
                        if (oldConfig.CompanyPhone != null)
                            companyInfo.CompanyPhone = (string)oldConfig.CompanyPhone;
                        if (oldConfig.CompanyEmail != null)
                            companyInfo.CompanyEmail = (string)oldConfig.CompanyEmail;

                        companyInfoService.SaveCompanyInfo(companyInfo);
                        logService.LogInfo("已从config.json迁移公司信息到company_info.json");
                    }
                }
                catch (Exception ex)
                {
                    logService.LogException(ex, "从config.json迁移公司信息失败");
                }
            }

            return companyInfo;
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
        services.AddSingleton<AlarmDatabaseService>();
        services.AddSingleton<AlarmDetectionService>();
        services.AddSingleton<LayoutSettingsService>();
        services.AddSingleton<ChartConfigService>();
        services.AddSingleton<CompanyInfoService>();

        // Register ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LogViewModel>();
        services.AddSingleton<TableViewModel>();
        services.AddSingleton<ChartViewModel>();
        services.AddSingleton<CommandSenderViewModel>();
        services.AddSingleton<AlarmViewModel>();
        services.AddSingleton<AlarmStatisticsViewModel>();
        services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel(
            sp.GetRequiredService<LogService>(),
            sp.GetRequiredService<EncryptionService>(),
            sp.GetRequiredService<MqttSettings>())); // Transient for new instance each time
        services.AddTransient<ChartSettingsViewModel>(sp => new ChartSettingsViewModel(
            sp.GetRequiredService<LogService>(),
            sp.GetRequiredService<ChartConfig>(),
            sp.GetRequiredService<ChartConfigService>())); // Transient for new instance each time
        services.AddTransient<AlarmConfigViewModel>();
        services.AddTransient<AlertSettingsViewModel>();
        services.AddTransient<HistoryQueryViewModel>(); // Transient for new instance each time
        services.AddTransient<AlarmHistoryQueryViewModel>(); // Transient for new instance each time

        // Register Views
        services.AddSingleton<LogView>();
        services.AddSingleton<TableView>();
        services.AddSingleton<ChartView>();
        services.AddSingleton<CommandSenderView>();
        services.AddSingleton<AlarmStatisticsView>();
        services.AddSingleton<AlarmView>();
        services.AddTransient<SettingsWindow>(); // Transient for new instance each time
        services.AddTransient<ChartSettingsWindow>(); // Transient for new instance each time
        services.AddTransient<ContactUsWindow>(sp => new ContactUsWindow(
            sp.GetRequiredService<CompanyInfo>())); // Transient for new instance each time
        services.AddTransient<AlarmConfigWindow>(); // Transient for new instance each time
        services.AddTransient<AlertSettingsWindow>(); // Transient for new instance each time
        services.AddTransient<HistoryQueryWindow>(); // Transient for new instance each time
        services.AddTransient<AlarmHistoryQueryWindow>(); // Transient for new instance each time
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            // 1. 断开MQTT连接
            var mqttService = ServiceProvider?.GetService<MqttService>();
            if (mqttService != null && mqttService.CurrentState == Services.ConnectionState.Connected)
            {
                await mqttService.DisconnectAsync();
            }

            // 2. 释放CSV数据存储服务（会刷新所有缓存）
            var csvStorage = ServiceProvider?.GetService<CsvDataStorageService>();
            csvStorage?.Dispose();

            // 3. 释放告警数据库服务
            var alarmDatabase = ServiceProvider?.GetService<AlarmDatabaseService>();
            alarmDatabase?.Dispose();

            // 4. 释放告警历史存储服务
            var alarmHistoryStorage = ServiceProvider?.GetService<AlarmHistoryStorageService>();
            alarmHistoryStorage?.Dispose();

            // 5. 日志记录
            var logService = ServiceProvider?.GetService<LogService>();
            logService?.LogInfo("应用程序正常退出，所有资源已释放");
        }
        catch (Exception ex)
        {
            // 退出时发生错误，记录但不阻止退出
            System.IO.File.AppendAllText("shutdown_error.log",
                $"[{DateTime.Now}] Shutdown error: {ex.Message}\n{ex.StackTrace}\n");
        }

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
