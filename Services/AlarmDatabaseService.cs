using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MqttMonitor.Models;

namespace MqttMonitor.Services;

/// <summary>
/// 告警记录SQLite数据库服务
/// </summary>
public class AlarmDatabaseService : IDisposable
{
    private readonly LogService _logService;
    private readonly string _databasePath;
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

    public AlarmDatabaseService(LogService logService)
    {
        _logService = logService;

        // 数据库文件放在程序目录下
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _databasePath = Path.Combine(exeDirectory, "alarm_records.db");

        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库（创建表和索引）
    /// </summary>
    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS alarm_records (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    record_id TEXT NOT NULL UNIQUE,
                    device_id TEXT NOT NULL,
                    metric_name TEXT NOT NULL,
                    trigger_time TEXT NOT NULL,
                    alarm_type INTEGER NOT NULL,
                    trigger_value REAL NOT NULL,
                    threshold_value REAL NOT NULL,
                    unit TEXT,
                    recovered_time TEXT,
                    status INTEGER NOT NULL,
                    acknowledged INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_device_metric ON alarm_records(device_id, metric_name);
                CREATE INDEX IF NOT EXISTS idx_trigger_time ON alarm_records(trigger_time);
                CREATE INDEX IF NOT EXISTS idx_status ON alarm_records(status);
                CREATE INDEX IF NOT EXISTS idx_record_id ON alarm_records(record_id);
            ";

            using var command = new SqliteCommand(createTableSql, connection);
            command.ExecuteNonQuery();

            _logService.LogInfo($"告警数据库初始化完成: {_databasePath}");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "初始化告警数据库失败");
        }
    }

    /// <summary>
    /// 保存或更新告警记录
    /// </summary>
    public async Task SaveAlarmRecordAsync(AlarmRecord record)
    {
        if (record == null)
            return;

        await _writeSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            // 使用 REPLACE 实现插入或更新
            var sql = @"
                INSERT OR REPLACE INTO alarm_records
                (record_id, device_id, metric_name, trigger_time, alarm_type,
                 trigger_value, threshold_value, unit, recovered_time, status, acknowledged, created_at)
                VALUES
                (@recordId, @deviceId, @metricName, @triggerTime, @alarmType,
                 @triggerValue, @thresholdValue, @unit, @recoveredTime, @status, @acknowledged, @createdAt)
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@recordId", record.RecordId);
            command.Parameters.AddWithValue("@deviceId", record.DeviceId);
            command.Parameters.AddWithValue("@metricName", record.MetricName);
            command.Parameters.AddWithValue("@triggerTime", record.TriggerTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@alarmType", (int)record.AlarmType);
            command.Parameters.AddWithValue("@triggerValue", record.TriggerValue);
            command.Parameters.AddWithValue("@thresholdValue", record.ThresholdValue);
            command.Parameters.AddWithValue("@unit", record.Unit ?? "");
            command.Parameters.AddWithValue("@recoveredTime", record.RecoveredTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", (int)record.Status);
            command.Parameters.AddWithValue("@acknowledged", record.Acknowledged ? 1 : 0);
            command.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存告警记录到数据库失败");
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
        await _writeSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var record in records)
                {
                    var sql = @"
                        INSERT OR REPLACE INTO alarm_records
                        (record_id, device_id, metric_name, trigger_time, alarm_type,
                         trigger_value, threshold_value, unit, recovered_time, status, acknowledged, created_at)
                        VALUES
                        (@recordId, @deviceId, @metricName, @triggerTime, @alarmType,
                         @triggerValue, @thresholdValue, @unit, @recoveredTime, @status, @acknowledged, @createdAt)
                    ";

                    using var command = new SqliteCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("@recordId", record.RecordId);
                    command.Parameters.AddWithValue("@deviceId", record.DeviceId);
                    command.Parameters.AddWithValue("@metricName", record.MetricName);
                    command.Parameters.AddWithValue("@triggerTime", record.TriggerTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@alarmType", (int)record.AlarmType);
                    command.Parameters.AddWithValue("@triggerValue", record.TriggerValue);
                    command.Parameters.AddWithValue("@thresholdValue", record.ThresholdValue);
                    command.Parameters.AddWithValue("@unit", record.Unit ?? "");
                    command.Parameters.AddWithValue("@recoveredTime", record.RecoveredTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@status", (int)record.Status);
                    command.Parameters.AddWithValue("@acknowledged", record.Acknowledged ? 1 : 0);
                    command.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "批量保存告警记录失败");
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// 获取最近的告警记录
    /// </summary>
    public async Task<List<AlarmRecord>> GetRecentAlarmsAsync(int limit = 50)
    {
        var records = new List<AlarmRecord>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = @"
                SELECT * FROM alarm_records
                WHERE status = @status
                ORDER BY trigger_time DESC
                LIMIT @limit
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@status", (int)AlarmStatus.Recovered);
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(ReadAlarmRecord(reader));
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "从数据库读取最近告警记录失败");
        }

        return records;
    }

    /// <summary>
    /// 按时间范围查询告警记录
    /// </summary>
    public async Task<List<AlarmRecord>> GetAlarmsByDateRangeAsync(DateTime startTime, DateTime endTime, string? deviceId = null, string? metricName = null)
    {
        var records = new List<AlarmRecord>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = @"
                SELECT * FROM alarm_records
                WHERE trigger_time >= @startTime AND trigger_time <= @endTime
            ";

            if (!string.IsNullOrEmpty(deviceId))
            {
                sql += " AND device_id = @deviceId";
            }

            if (!string.IsNullOrEmpty(metricName))
            {
                sql += " AND metric_name = @metricName";
            }

            sql += " ORDER BY trigger_time DESC";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@startTime", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endTime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

            if (!string.IsNullOrEmpty(deviceId))
            {
                command.Parameters.AddWithValue("@deviceId", deviceId);
            }

            if (!string.IsNullOrEmpty(metricName))
            {
                command.Parameters.AddWithValue("@metricName", metricName);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(ReadAlarmRecord(reader));
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "按时间范围查询告警记录失败");
        }

        return records;
    }

    /// <summary>
    /// 按条件查询告警记录（支持告警类型筛选）
    /// </summary>
    public async Task<List<AlarmRecord>> QueryAlarmsAsync(
        DateTime startTime,
        DateTime endTime,
        string? deviceId = null,
        AlarmType? alarmType = null)
    {
        var records = new List<AlarmRecord>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = @"
                SELECT * FROM alarm_records
                WHERE trigger_time >= @startTime AND trigger_time <= @endTime
            ";

            if (!string.IsNullOrEmpty(deviceId))
            {
                sql += " AND device_id = @deviceId";
            }

            if (alarmType.HasValue)
            {
                sql += " AND alarm_type = @alarmType";
            }

            sql += " ORDER BY trigger_time DESC";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@startTime", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endTime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

            if (!string.IsNullOrEmpty(deviceId))
            {
                command.Parameters.AddWithValue("@deviceId", deviceId);
            }

            if (alarmType.HasValue)
            {
                command.Parameters.AddWithValue("@alarmType", (int)alarmType.Value);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(ReadAlarmRecord(reader));
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "查询告警记录失败");
        }

        return records;
    }

    /// <summary>
    /// 获取所有不同的设备ID
    /// </summary>
    public async Task<List<string>> GetAllDeviceIdsAsync()
    {
        var deviceIds = new List<string>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = "SELECT DISTINCT device_id FROM alarm_records ORDER BY device_id";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                deviceIds.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "获取设备列表失败");
        }

        return deviceIds;
    }

    /// <summary>
    /// 获取告警统计数据
    /// </summary>
    public async Task<Dictionary<string, int>> GetAlarmStatisticsAsync(DateTime startTime, DateTime endTime)
    {
        var statistics = new Dictionary<string, int>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = @"
                SELECT device_id || '_' || metric_name as key, COUNT(*) as count
                FROM alarm_records
                WHERE trigger_time >= @startTime AND trigger_time <= @endTime
                GROUP BY device_id, metric_name
                ORDER BY count DESC
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@startTime", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@endTime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var key = reader.GetString(0);
                var count = reader.GetInt32(1);
                statistics[key] = count;
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "获取告警统计数据失败");
        }

        return statistics;
    }

    /// <summary>
    /// 删除指定时间之前的记录
    /// </summary>
    public async Task DeleteOldRecordsAsync(DateTime beforeDate)
    {
        await _writeSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = "DELETE FROM alarm_records WHERE trigger_time < @beforeDate";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@beforeDate", beforeDate.ToString("yyyy-MM-dd HH:mm:ss"));

            var deletedCount = await command.ExecuteNonQueryAsync();
            _logService.LogInfo($"已删除 {deletedCount} 条旧告警记录");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "删除旧告警记录失败");
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// 从SqliteDataReader读取告警记录
    /// </summary>
    private AlarmRecord ReadAlarmRecord(SqliteDataReader reader)
    {
        return new AlarmRecord
        {
            RecordId = reader.GetString(reader.GetOrdinal("record_id")),
            DeviceId = reader.GetString(reader.GetOrdinal("device_id")),
            MetricName = reader.GetString(reader.GetOrdinal("metric_name")),
            TriggerTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("trigger_time"))),
            AlarmType = (AlarmType)reader.GetInt32(reader.GetOrdinal("alarm_type")),
            TriggerValue = reader.GetDouble(reader.GetOrdinal("trigger_value")),
            ThresholdValue = reader.GetDouble(reader.GetOrdinal("threshold_value")),
            Unit = reader.GetString(reader.GetOrdinal("unit")),
            RecoveredTime = reader.IsDBNull(reader.GetOrdinal("recovered_time"))
                ? null
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("recovered_time"))),
            Status = (AlarmStatus)reader.GetInt32(reader.GetOrdinal("status")),
            Acknowledged = reader.GetInt32(reader.GetOrdinal("acknowledged")) == 1
        };
    }

    public void Dispose()
    {
        _writeSemaphore?.Dispose();
    }
}
