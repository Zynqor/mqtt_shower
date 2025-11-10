using System;
using System.IO;

namespace MqttMonitor.Services;

/// <summary>
/// 路径管理服务 - 统一管理所有配置、数据和资源文件的路径
/// </summary>
public static class PathManager
{
    /// <summary>
    /// 应用程序根目录
    /// </summary>
    public static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;

    // ==================== 目录 ====================

    /// <summary>
    /// 配置文件目录
    /// </summary>
    public static string ConfigDirectory => EnsureDirectory(Path.Combine(AppDirectory, "configs"));

    /// <summary>
    /// 数据文件目录
    /// </summary>
    public static string DataDirectory => EnsureDirectory(Path.Combine(AppDirectory, "data"));

    /// <summary>
    /// CSV数据目录
    /// </summary>
    public static string CsvDataDirectory => EnsureDirectory(Path.Combine(DataDirectory, "csv"));

    /// <summary>
    /// 日志文件目录
    /// </summary>
    public static string LogDirectory => EnsureDirectory(Path.Combine(AppDirectory, "logs"));

    /// <summary>
    /// 声音文件目录
    /// </summary>
    public static string SoundsDirectory => EnsureDirectory(Path.Combine(AppDirectory, "Sounds"));

    // ==================== 配置文件 ====================

    /// <summary>
    /// MQTT连接配置文件
    /// </summary>
    public static string MqttConfigFile => Path.Combine(ConfigDirectory, "mqtt_config.json");

    /// <summary>
    /// 告警配置文件
    /// </summary>
    public static string AlarmConfigFile => Path.Combine(ConfigDirectory, "alarm_config.json");

    /// <summary>
    /// 图表配置文件
    /// </summary>
    public static string ChartConfigFile => Path.Combine(ConfigDirectory, "chart_config.json");

    /// <summary>
    /// 图表图例配置文件
    /// </summary>
    public static string ChartLegendConfigFile => Path.Combine(ConfigDirectory, "chart_legend_config.json");

    /// <summary>
    /// 布局设置文件
    /// </summary>
    public static string LayoutConfigFile => Path.Combine(ConfigDirectory, "layout_config.json");

    /// <summary>
    /// 提醒设置文件
    /// </summary>
    public static string AlertSettingsFile => Path.Combine(ConfigDirectory, "alert_settings.json");

    /// <summary>
    /// 命令配置文件
    /// </summary>
    public static string CommandsConfigFile => Path.Combine(ConfigDirectory, "commands.json");

    // ==================== 数据文件 ====================

    /// <summary>
    /// 告警记录数据库文件
    /// </summary>
    public static string AlarmDatabaseFile => Path.Combine(DataDirectory, "alarm_records.db");

    /// <summary>
    /// 告警历史存储文件
    /// </summary>
    public static string AlarmHistoryFile => Path.Combine(DataDirectory, "alarm_history.db");

    // ==================== 兼容性：旧路径映射 ====================

    /// <summary>
    /// 获取兼容路径（用于迁移旧配置）
    /// </summary>
    public static string GetLegacyPath(string newPath)
    {
        // 映射新路径到旧路径
        if (newPath == MqttConfigFile)
            return Path.Combine(AppDirectory, "config.json");

        if (newPath == CommandsConfigFile)
            return Path.Combine(AppDirectory, "commands.json");

        if (newPath == LayoutConfigFile)
            return Path.Combine(AppDirectory, "layout_settings.json");

        if (newPath == ChartLegendConfigFile)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "MqttMonitor", "chart_legend_config.json");
        }

        return newPath;
    }

    /// <summary>
    /// 迁移旧配置文件到新位置
    /// </summary>
    public static void MigrateOldConfig(string newPath)
    {
        var oldPath = GetLegacyPath(newPath);

        // 如果新文件已存在，不迁移
        if (File.Exists(newPath))
            return;

        // 如果旧文件存在，迁移到新位置
        if (File.Exists(oldPath))
        {
            try
            {
                File.Copy(oldPath, newPath, false);
                Console.WriteLine($"已迁移配置文件: {oldPath} -> {newPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"迁移配置文件失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private static string EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    /// <summary>
    /// 初始化所有目录和配置迁移
    /// </summary>
    public static void Initialize()
    {
        // 确保所有目录存在
        _ = ConfigDirectory;
        _ = DataDirectory;
        _ = CsvDataDirectory;
        _ = LogDirectory;
        _ = SoundsDirectory;

        // 迁移旧配置文件
        MigrateOldConfig(MqttConfigFile);
        MigrateOldConfig(CommandsConfigFile);
        MigrateOldConfig(LayoutConfigFile);
        MigrateOldConfig(ChartLegendConfigFile);
    }
}
