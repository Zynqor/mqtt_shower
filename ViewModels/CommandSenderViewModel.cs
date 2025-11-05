using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using MqttMonitor.Models;
using MqttMonitor.Services;

namespace MqttMonitor.ViewModels;

/// <summary>
/// 命令发送视图 ViewModel
/// </summary>
public class CommandSenderViewModel : INotifyPropertyChanged
{
    private readonly DataProcessingService _dataProcessingService;
    private readonly MqttService _mqttService;
    private readonly LogService _logService;
    private readonly Dictionary<string, CommandState> _pendingCommands = new();
    private readonly System.Timers.Timer _timeoutTimer;
    private readonly HashSet<string> _autoSubscribedTopics = new(); // 记录自动订阅的topic

    private const int CommandTimeoutSeconds = 10;
    private string _selectedDeviceId = string.Empty;
    private string _commandName = string.Empty;
    private string _commandParams = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 命令历史记录
    /// </summary>
    public ObservableCollection<CommandHistoryItem> CommandHistory { get; } = new();

    /// <summary>
    /// 选中的设备ID
    /// </summary>
    public string SelectedDeviceId
    {
        get => _selectedDeviceId;
        set
        {
            _selectedDeviceId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 命令名称
    /// </summary>
    public string CommandName
    {
        get => _commandName;
        set
        {
            _commandName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 命令参数（纯文本字符串）
    /// </summary>
    public string CommandParams
    {
        get => _commandParams;
        set
        {
            _commandParams = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 发送命令
    /// </summary>
    public ICommand SendCommandCommand { get; }

    /// <summary>
    /// 清空历史记录
    /// </summary>
    public ICommand ClearHistoryCommand { get; }

    public CommandSenderViewModel(
        DataProcessingService dataProcessingService,
        MqttService mqttService,
        LogService logService)
    {
        _dataProcessingService = dataProcessingService;
        _mqttService = mqttService;
        _logService = logService;

        // 按钮始终可点击，不检查状态
        SendCommandCommand = new RelayCommand(OnSendCommand);
        ClearHistoryCommand = new RelayCommand(OnClearHistory);

        // 订阅命令响应事件
        _dataProcessingService.OnCommandResponseParsed += OnCommandResponseReceived;

        // 创建超时定时器（每秒检查一次）
        _timeoutTimer = new System.Timers.Timer(1000);
        _timeoutTimer.Elapsed += CheckCommandTimeouts;
        _timeoutTimer.Start();
    }

    /// <summary>
    /// 发送命令
    /// </summary>
    private async void OnSendCommand()
    {
        // 检查必填字段
        if (string.IsNullOrWhiteSpace(SelectedDeviceId))
        {
            var message = "请输入目标设备ID";
            _logService.LogWarning(message);
            MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(CommandName))
        {
            var message = "请输入命令名称";
            _logService.LogWarning(message);
            MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 检查MQTT连接状态
        if (_mqttService.CurrentState != ConnectionState.Connected)
        {
            var message = $"MQTT未连接，当前状态：{_mqttService.CurrentState}。请先连接MQTT服务器。";
            _logService.LogWarning(message);
            MessageBox.Show(message, "无法发送命令", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // 自动订阅响应Topic（如果还未订阅）
            var responseTopic = $"iot/devices/{SelectedDeviceId}/command/response";
            if (!_autoSubscribedTopics.Contains(responseTopic))
            {
                try
                {
                    await _mqttService.SubscribeAsync(responseTopic);
                    _autoSubscribedTopics.Add(responseTopic);
                    _logService.LogInfo($"自动订阅命令响应 Topic: {responseTopic}");
                }
                catch (Exception ex)
                {
                    _logService.LogWarning($"自动订阅响应Topic失败: {ex.Message}，但仍会尝试发送命令");
                }
            }

            // 构建命令请求
            var commandRequest = new CommandRequest
            {
                CommandId = Guid.NewGuid().ToString(),
                CommandName = CommandName.Trim(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Params = string.IsNullOrWhiteSpace(CommandParams) ? null : CommandParams.Trim()
            };

            // 记录待处理命令
            var commandState = new CommandState
            {
                CommandId = commandRequest.CommandId,
                CommandName = commandRequest.CommandName,
                DeviceId = SelectedDeviceId,
                SentTime = DateTime.Now,
                Status = "待响应"
            };

            lock (_pendingCommands)
            {
                _pendingCommands[commandRequest.CommandId] = commandState;
            }

            // 发布到 MQTT
            var topic = $"iot/devices/{SelectedDeviceId}/command/request";
            var payload = JsonConvert.SerializeObject(commandRequest);

            await _mqttService.PublishAsync(topic, payload);

            // 添加到历史记录
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var historyItem = new CommandHistoryItem
                {
                    Time = DateTime.Now,
                    DeviceId = SelectedDeviceId,
                    CommandName = CommandName.Trim(),
                    CommandId = commandRequest.CommandId,
                    Status = "待响应"
                };
                CommandHistory.Insert(0, historyItem);

                // 限制历史记录数量
                while (CommandHistory.Count > 100)
                {
                    CommandHistory.RemoveAt(CommandHistory.Count - 1);
                }
            });

            _logService.LogInfo($"已发送命令 [{CommandName}] 到设备 [{SelectedDeviceId}]，命令ID: {commandRequest.CommandId}");
        }
        catch (Exception ex)
        {
            var message = $"发送命令失败：{ex.Message}";
            _logService.LogException(ex, "发送命令失败");
            MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    /// <summary>
    /// 收到命令响应
    /// </summary>
    private void OnCommandResponseReceived(CommandResponse response)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            CommandState? commandState = null;

            lock (_pendingCommands)
            {
                if (_pendingCommands.TryGetValue(response.CommandId, out commandState))
                {
                    _pendingCommands.Remove(response.CommandId);
                }
            }

            if (commandState != null)
            {
                var elapsed = (DateTime.Now - commandState.SentTime).TotalSeconds;
                var statusText = response.Status == "success" ? "成功" : $"失败: {response.ErrorMessage}";

                _logService.LogInfo($"命令 [{commandState.CommandName}] 响应: {statusText}，耗时: {elapsed:F2}秒");

                // 更新历史记录
                var historyItem = CommandHistory.FirstOrDefault(h => h.CommandId == response.CommandId);
                if (historyItem != null)
                {
                    historyItem.Status = statusText;
                    historyItem.ResponseTime = DateTime.Now;
                }
            }
        });
    }

    /// <summary>
    /// 检查命令超时
    /// </summary>
    private void CheckCommandTimeouts(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var now = DateTime.Now;
        var timedOutCommands = new List<CommandState>();

        lock (_pendingCommands)
        {
            var commandsToRemove = _pendingCommands
                .Where(kvp => (now - kvp.Value.SentTime).TotalSeconds > CommandTimeoutSeconds)
                .ToList();

            foreach (var kvp in commandsToRemove)
            {
                timedOutCommands.Add(kvp.Value);
                _pendingCommands.Remove(kvp.Key);
            }
        }

        if (timedOutCommands.Count > 0)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var commandState in timedOutCommands)
                {
                    _logService.LogWarning($"命令 [{commandState.CommandName}] 超时未响应，命令ID: {commandState.CommandId}");

                    // 更新历史记录
                    var historyItem = CommandHistory.FirstOrDefault(h => h.CommandId == commandState.CommandId);
                    if (historyItem != null)
                    {
                        historyItem.Status = "超时";
                    }
                }
            });
        }
    }

    /// <summary>
    /// 清空历史记录
    /// </summary>
    private void OnClearHistory()
    {
        CommandHistory.Clear();
        _logService.LogInfo("已清空命令历史记录");
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 命令状态（用于超时跟踪）
/// </summary>
public class CommandState
{
    public string CommandId { get; set; } = string.Empty;
    public string CommandName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public DateTime SentTime { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 命令历史记录项
/// </summary>
public class CommandHistoryItem : INotifyPropertyChanged
{
    private string _status = string.Empty;
    private DateTime? _responseTime;

    public DateTime Time { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string CommandName { get; set; } = string.Empty;
    public string CommandId { get; set; } = string.Empty;

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public DateTime? ResponseTime
    {
        get => _responseTime;
        set
        {
            _responseTime = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
