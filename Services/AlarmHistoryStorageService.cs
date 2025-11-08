using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MqttMonitor.Models;

namespace MqttMonitor.Services;

/// <summary>
/// 告警历史记录CSV存储服务
/// </summary>
public class AlarmHistoryStorageService : IDisposable
{
    private readonly LogService _logService;
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    private readonly string _baseDirectory = "alarm_history";

    public AlarmHistoryStorageService(LogService logService)
    {
        _logService = logService;

        // 确保目录存在
        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
            _logService.LogInfo($"已创建告警历史目录: {_baseDirectory}");
        }
    }

    /// <summary>
    /// 保存告警记录到CSV
    /// </summary>
    public async Task SaveAlarmRecordAsync(AlarmRecord record)
    {
        if (record == null || record.Status != AlarmStatus.Recovered)
            return;

        await _writeSemaphore.WaitAsync();
        try
        {
            // 按天创建文件：alarm_history/2025-01-15.csv
            var date = record.TriggerTime.ToString("yyyy-MM-dd");
            var fileName = $"{date}.csv";
            var filePath = Path.Combine(_baseDirectory, fileName);

            var isNewFile = !File.Exists(filePath);

            // 追加模式写入
            using var writer = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8);

            // 如果是新文件，写入表头
            if (isNewFile)
            {
                await writer.WriteLineAsync("触发时间,设备ID,测点名称,告警类型,触发值,阈值,单位,恢复时间,持续时长,是否已确认");
            }

            // 写入数据行
            var alarmTypeText = record.AlarmType == AlarmType.UpperLimit ? "上限告警" : "下限告警";
            var acknowledgedText = record.Acknowledged ? "是" : "否";
            var recoveredTimeText = record.RecoveredTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

            var line = $"{record.TriggerTime:yyyy-MM-dd HH:mm:ss}," +
                       $"{EscapeCsv(record.DeviceId)}," +
                       $"{EscapeCsv(record.MetricName)}," +
                       $"{alarmTypeText}," +
                       $"{record.TriggerValue:F2}," +
                       $"{record.ThresholdValue:F2}," +
                       $"{EscapeCsv(record.Unit)}," +
                       $"{recoveredTimeText}," +
                       $"{EscapeCsv(record.Duration)}," +
                       $"{acknowledgedText}";

            await writer.WriteLineAsync(line);
            await writer.FlushAsync();

            _logService.LogInfo($"告警记录已保存: {record.DeviceId} - {record.MetricName}");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存告警记录失败");
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// 批量保存告警记录
    /// </summary>
    public async Task SaveAlarmRecordsAsync(IEnumerable<AlarmRecord> records)
    {
        foreach (var record in records)
        {
            await SaveAlarmRecordAsync(record);
        }
    }

    /// <summary>
    /// CSV字段转义（处理逗号和引号）
    /// </summary>
    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // 如果包含逗号、引号或换行符，需要用引号包裹并转义内部引号
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// 获取历史文件列表
    /// </summary>
    public List<string> GetHistoryFiles()
    {
        try
        {
            var files = Directory.GetFiles(_baseDirectory, "*.csv");
            return new List<string>(files);
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "获取历史文件列表失败");
            return new List<string>();
        }
    }

    /// <summary>
    /// 清理旧的历史文件（可选，保留最近N天）
    /// </summary>
    public async Task CleanupOldFilesAsync(int keepDays = 90)
    {
        await Task.Run(() =>
        {
            try
            {
                var files = Directory.GetFiles(_baseDirectory, "*.csv");
                var cutoffDate = DateTime.Now.AddDays(-keepDays);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                        _logService.LogInfo($"已删除旧告警历史文件: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, "清理旧告警历史文件失败");
            }
        });
    }

    public void Dispose()
    {
        _writeSemaphore?.Dispose();
    }
}
