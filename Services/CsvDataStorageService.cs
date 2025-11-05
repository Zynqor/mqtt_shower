using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MqttMonitor.Models;

namespace MqttMonitor.Services;

/// <summary>
/// CSV数据存储服务
/// </summary>
public class CsvDataStorageService : IDisposable
{
    private readonly LogService _logService;
    private readonly string _dataDirectory;
    private readonly ConcurrentDictionary<string, StreamWriter> _fileWriters = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _deviceParameters = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;
    private bool _isEnabled = false;

    public CsvDataStorageService(LogService logService)
    {
        _logService = logService;

        // 获取exe所在目录
        var exePath = AppDomain.CurrentDomain.BaseDirectory;
        _dataDirectory = Path.Combine(exePath, "datas");

        // 创建数据目录
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
            _logService.LogInfo($"已创建数据目录: {_dataDirectory}");
        }
        else
        {
            _logService.LogInfo($"数据目录已存在: {_dataDirectory}");
        }
    }

    /// <summary>
    /// 启用CSV数据存储（连接时调用）
    /// </summary>
    public async Task<bool> EnableAsync()
    {
        try
        {
            await _writeLock.WaitAsync();

            if (_isEnabled)
            {
                _logService.LogWarning("CSV数据存储已经启用");
                return true;
            }

            // 测试是否可以在数据目录中创建文件
            var testFilePath = Path.Combine(_dataDirectory, $"_test_{Guid.NewGuid()}.tmp");
            try
            {
                await File.WriteAllTextAsync(testFilePath, "test");
                File.Delete(testFilePath);
            }
            catch (Exception ex)
            {
                _logService.LogException(ex, $"无法访问数据目录: {_dataDirectory}");
                return false;
            }

            _isEnabled = true;
            _logService.LogInfo("CSV数据存储已启用");
            return true;
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "启用CSV数据存储失败");
            return false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 禁用CSV数据存储（断开连接时调用）
    /// </summary>
    public async Task DisableAsync()
    {
        try
        {
            await _writeLock.WaitAsync();

            if (!_isEnabled)
            {
                _logService.LogInfo("CSV数据存储已经禁用");
                return;
            }

            // 关闭所有文件写入器
            foreach (var kvp in _fileWriters)
            {
                try
                {
                    await kvp.Value.FlushAsync();
                    kvp.Value.Dispose();
                    _logService.LogInfo($"已关闭文件写入器: {kvp.Key}");
                }
                catch (Exception ex)
                {
                    _logService.LogException(ex, $"关闭文件写入器失败: {kvp.Key}");
                }
            }

            _fileWriters.Clear();
            _isEnabled = false;
            _logService.LogInfo("CSV数据存储已禁用，所有文件已关闭");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "禁用CSV数据存储失败");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 保存上行数据到CSV文件
    /// </summary>
    public async Task SaveDataAsync(UpstreamDataPacket dataPacket)
    {
        if (!_isEnabled)
        {
            // 未启用时不保存数据
            return;
        }

        if (dataPacket.Payload == null || dataPacket.Payload.Count == 0)
            return;

        try
        {
            await _writeLock.WaitAsync();

            var deviceId = dataPacket.DeviceId;
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(dataPacket.Timestamp).ToLocalTime();
            var dateKey = timestamp.ToString("yyyyMMdd");
            var fileKey = $"{deviceId}_{dateKey}";
            var fileName = $"{fileKey}.csv";
            var filePath = Path.Combine(_dataDirectory, fileName);

            // 获取当前设备的所有参数名称
            var parameterNames = dataPacket.Payload.Select(p => p.Name).ToList();

            // 检查是否需要创建新文件或更新表头
            var needsHeader = !File.Exists(filePath);

            // 如果设备参数发生变化，记录并可能需要更新表头
            if (!_deviceParameters.ContainsKey(deviceId))
            {
                _deviceParameters[deviceId] = new HashSet<string>(parameterNames);
            }
            else
            {
                var existingParams = _deviceParameters[deviceId];
                var newParams = parameterNames.Except(existingParams).ToList();
                if (newParams.Any())
                {
                    // 有新参数加入
                    foreach (var param in newParams)
                    {
                        existingParams.Add(param);
                    }
                    _logService.LogInfo($"设备 [{deviceId}] 检测到新参数: {string.Join(", ", newParams)}");
                }
            }

            // 获取或创建文件写入器
            if (!_fileWriters.TryGetValue(fileKey, out var writer) || writer.BaseStream == null)
            {
                // 关闭旧的写入器（如果存在）
                if (_fileWriters.TryRemove(fileKey, out var oldWriter))
                {
                    await oldWriter.FlushAsync();
                    oldWriter.Dispose();
                }

                // 创建新的写入器
                writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
                _fileWriters[fileKey] = writer;

                // 写入表头
                if (needsHeader)
                {
                    var header = "时间," + string.Join(",", _deviceParameters[deviceId].OrderBy(p => p));
                    await writer.WriteLineAsync(header);
                    await writer.FlushAsync();
                    _logService.LogInfo($"已创建CSV文件: {fileName}");
                }
            }

            // 构建数据行
            var timeString = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var dataDict = dataPacket.Payload.ToDictionary(p => p.Name, p => p.Value);
            var orderedParams = _deviceParameters[deviceId].OrderBy(p => p);

            var values = new List<string> { timeString };
            foreach (var paramName in orderedParams)
            {
                if (dataDict.TryGetValue(paramName, out var value))
                {
                    values.Add(value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    values.Add(""); // 参数不存在时留空
                }
            }

            var line = string.Join(",", values);
            await writer.WriteLineAsync(line);
            await writer.FlushAsync(); // 立即刷新到磁盘，防止数据丢失
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "保存CSV数据失败");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 清理过期的文件写入器
    /// </summary>
    public async Task CleanupOldWritersAsync()
    {
        try
        {
            await _writeLock.WaitAsync();

            var today = DateTime.Now.ToString("yyyyMMdd");
            var keysToRemove = _fileWriters.Keys.Where(key => !key.EndsWith(today)).ToList();

            foreach (var key in keysToRemove)
            {
                if (_fileWriters.TryRemove(key, out var writer))
                {
                    await writer.FlushAsync();
                    writer.Dispose();
                    _logService.LogInfo($"已关闭过期文件写入器: {key}");
                }
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "清理文件写入器失败");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // 关闭所有文件写入器
        foreach (var writer in _fileWriters.Values)
        {
            try
            {
                writer.Flush();
                writer.Dispose();
            }
            catch
            {
                // 忽略关闭时的异常
            }
        }

        _fileWriters.Clear();
        _writeLock.Dispose();

        _logService.LogInfo("CSV数据存储服务已关闭");
    }
}
