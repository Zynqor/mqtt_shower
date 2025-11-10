using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MqttMonitor.Models;

namespace MqttMonitor.Services;

/// <summary>
/// 历史数据SQLite存储服务
/// 替代CSV存储，解决文件锁定问题
/// </summary>
public class HistoryDataStorageService : IDisposable
{
    private readonly LogService _logService;
    private readonly string _databasePath;
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    private readonly List<HistoryDataRow> _writeBuffer = new();
    private readonly Timer _flushTimer;
    private readonly object _bufferLock = new();

    private const int BUFFER_SIZE = 500;  // 缓冲区大小
    private const int FLUSH_INTERVAL_SECONDS = 10;  // 刷新间隔（秒）
    private bool _isDisposed = false;

    public HistoryDataStorageService(LogService logService)
    {
        _logService = logService;
        _databasePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "history_data.db");

        try
        {
            InitializeDatabase();
            _logService.LogInfo($"历史数据存储服务已初始化，数据库路径: {_databasePath}");

            // 定时刷新缓冲区
            _flushTimer = new Timer(
                OnFlushTimerCallback,
                null,
                TimeSpan.FromSeconds(FLUSH_INTERVAL_SECONDS),
                TimeSpan.FromSeconds(FLUSH_INTERVAL_SECONDS));
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "初始化历史数据存储服务失败");
            throw;
        }
    }

    /// <summary>
    /// 初始化数据库和表结构
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS history_data (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                device_id TEXT NOT NULL,
                metric_name TEXT NOT NULL,
                timestamp INTEGER NOT NULL,
                value REAL NOT NULL,
                unit TEXT,
                created_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_device_time
                ON history_data(device_id, timestamp DESC);

            CREATE INDEX IF NOT EXISTS idx_device_metric_time
                ON history_data(device_id, metric_name, timestamp DESC);

            CREATE INDEX IF NOT EXISTS idx_timestamp
                ON history_data(timestamp);

            -- 启用WAL模式提升并发性能
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=NORMAL;
        ";

        using var command = new SqliteCommand(createTableSql, connection);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 保存MQTT数据包到缓冲区
    /// </summary>
    public Task SaveDataAsync(UpstreamDataPacket packet)
    {
        if (_isDisposed) return Task.CompletedTask;

        try
        {
            lock (_bufferLock)
            {
                // 将每个测点数据添加到缓冲区
                foreach (var payload in packet.Payload)
                {
                    _writeBuffer.Add(new HistoryDataRow
                    {
                        DeviceId = packet.DeviceId,
                        MetricName = payload.Name,
                        Timestamp = packet.Timestamp,
                        Value = payload.Value,
                        Unit = payload.Unit ?? ""
                    });
                }

                // 缓冲区满时立即刷新
                if (_writeBuffer.Count >= BUFFER_SIZE)
                {
                    _ = Task.Run(FlushBufferAsync);
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存历史数据到缓冲区失败");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 定时器回调：刷新缓冲区
    /// </summary>
    private async void OnFlushTimerCallback(object? state)
    {
        try
        {
            await FlushBufferAsync();
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "定时刷新历史数据缓冲区失败");
        }
    }

    /// <summary>
    /// 批量写入数据库
    /// </summary>
    private async Task FlushBufferAsync()
    {
        List<HistoryDataRow> rowsToWrite;

        lock (_bufferLock)
        {
            if (_writeBuffer.Count == 0) return;

            rowsToWrite = _writeBuffer.ToList();
            _writeBuffer.Clear();
        }

        await _writeSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            var insertSql = @"
                INSERT INTO history_data
                (device_id, metric_name, timestamp, value, unit, created_at)
                VALUES (@deviceId, @metricName, @timestamp, @value, @unit, @createdAt)
            ";

            foreach (var row in rowsToWrite)
            {
                using var command = new SqliteCommand(insertSql, connection, transaction);
                command.Parameters.AddWithValue("@deviceId", row.DeviceId);
                command.Parameters.AddWithValue("@metricName", row.MetricName);
                command.Parameters.AddWithValue("@timestamp", row.Timestamp);
                command.Parameters.AddWithValue("@value", row.Value);
                command.Parameters.AddWithValue("@unit", row.Unit);
                command.Parameters.AddWithValue("@createdAt",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();

            _logService.LogInfo($"批量写入 {rowsToWrite.Count} 条历史数据到SQLite");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "批量写入历史数据失败");
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// 查询历史数据
    /// </summary>
    public async Task<List<HistoryDataPoint>> QueryDataAsync(
        string deviceId,
        string? metricName,
        long startTimestamp,
        long endTimestamp,
        int? limit = null)
    {
        var result = new List<HistoryDataPoint>();

        await _writeSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = @"
                SELECT metric_name, timestamp, value, unit
                FROM history_data
                WHERE device_id = @deviceId
                    AND timestamp >= @startTime
                    AND timestamp <= @endTime
            ";

            if (!string.IsNullOrEmpty(metricName))
            {
                sql += " AND metric_name = @metricName";
            }

            sql += " ORDER BY timestamp ASC";

            if (limit.HasValue)
            {
                sql += $" LIMIT {limit.Value}";
            }

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@deviceId", deviceId);
            command.Parameters.AddWithValue("@startTime", startTimestamp);
            command.Parameters.AddWithValue("@endTime", endTimestamp);

            if (!string.IsNullOrEmpty(metricName))
            {
                command.Parameters.AddWithValue("@metricName", metricName);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new HistoryDataPoint
                {
                    MetricName = reader.GetString(0),
                    Timestamp = reader.GetInt64(1),
                    Value = reader.GetDouble(2),
                    Unit = reader.IsDBNull(3) ? "" : reader.GetString(3)
                });
            }

            _logService.LogInfo($"查询历史数据: 设备={deviceId}, 测点={metricName ?? "全部"}, 结果={result.Count}条");

            return result;
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "查询历史数据失败");
            return result;
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// 获取可用的设备列表
    /// </summary>
    public async Task<List<string>> GetAvailableDevicesAsync()
    {
        var devices = new List<string>();

        await _writeSemaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = "SELECT DISTINCT device_id FROM history_data ORDER BY device_id";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                devices.Add(reader.GetString(0));
            }

            return devices;
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "获取设备列表失败");
            return devices;
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    /// <summary>
    /// 清理旧数据（保留指定天数）
    /// </summary>
    public async Task CleanOldDataAsync(int daysToKeep = 90)
    {
        await _writeSemaphore.WaitAsync();
        try
        {
            var cutoffTimestamp = DateTimeOffset.Now.AddDays(-daysToKeep).ToUnixTimeMilliseconds();

            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var sql = "DELETE FROM history_data WHERE timestamp < @cutoffTimestamp";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@cutoffTimestamp", cutoffTimestamp);

            var deletedRows = await command.ExecuteNonQueryAsync();

            _logService.LogInfo($"清理旧历史数据: 删除 {deletedRows} 条记录（保留{daysToKeep}天）");

            // 优化数据库
            using var vacuumCommand = new SqliteCommand("VACUUM", connection);
            await vacuumCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "清理旧历史数据失败");
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _flushTimer?.Dispose();
            FlushBufferAsync().Wait(TimeSpan.FromSeconds(5));
            _writeSemaphore?.Dispose();

            _logService.LogInfo("历史数据存储服务已释放资源");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "释放历史数据存储服务资源时出错");
        }
    }
}

/// <summary>
/// 历史数据行（用于缓冲区）
/// </summary>
internal class HistoryDataRow
{
    public string DeviceId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// 历史数据点（查询结果）
/// </summary>
public class HistoryDataPoint
{
    public string MetricName { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}
