using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace MqttMonitor.Services;

/// <summary>
/// MQTT 连接状态枚举
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Error
}

/// <summary>
/// MQTT 服务，负责 MQTT 连接、订阅和发布
/// </summary>
public class MqttService : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private IManagedMqttClient? _mqttClient;
    private ConnectionState _currentState = ConnectionState.Disconnected;
    private readonly ConcurrentHashSet<string> _activeSubscriptions = new(); // Store manual subscriptions
    private readonly ConcurrentHashSet<string> _autoSubscriptions = new(); // Store auto subscriptions (managed by DeviceManagementService)
    private ObservableCollection<string> _sortedActiveSubscriptions = new();

    public ObservableCollection<string> SortedActiveSubscriptions
    {
        get => _sortedActiveSubscriptions;
        private set
        {
            if (_sortedActiveSubscriptions != value)
            {
                _sortedActiveSubscriptions = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<MqttApplicationMessageReceivedEventArgs>? OnMessageReceived;

    /// <summary>
    /// 当前连接状态
    /// </summary>
    public ConnectionState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState != value)
            {
                _currentState = value;
                OnPropertyChanged();
                _logService.LogInfo($"MQTT 连接状态变更为: {value}");
            }
        }
    }

    public MqttService(LogService logService)
    {
        _logService = logService;
        UpdateSortedSubscriptions(); // Initialize sorted subscriptions
    }

    /// <summary>
    /// 连接到 MQTT 服务器
    /// </summary>
    public async Task ConnectAsync(
        string server,
        int port,
        string? username = null,
        string? password = null,
        string? clientId = null,
        bool useTls = false,
        string? caCertPath = null,
        string? clientCertPath = null,
        string? clientKeyPath = null,
        bool ignoreCertErrors = false)
    {
        try
        {
            CurrentState = ConnectionState.Connecting;
            var protocol = useTls ? "mqtts" : "mqtt";
            _logService.LogInfo($"正在连接到 MQTT 服务器 {protocol}://{server}:{port}...");

            // 创建 MQTT 客户端
            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            // 配置客户端选项
            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(server, port)
                .WithClientId(clientId ?? Guid.NewGuid().ToString())
                .WithCleanSession();

            if (!string.IsNullOrEmpty(username))
            {
                clientOptions.WithCredentials(username, password);
            }

            // 配置 TLS/SSL
            if (useTls)
            {
                _logService.LogInfo("启用 TLS/SSL 加密连接");

                var tlsOptions = new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    SslProtocol = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                };

                // 如果忽略证书错误（仅用于测试）
                if (ignoreCertErrors)
                {
                    _logService.LogWarning("警告：已启用忽略证书错误，这在生产环境中不安全！");
                    tlsOptions.CertificateValidationHandler = _ => true;
                }

                // 加载证书
                var certificates = new List<System.Security.Cryptography.X509Certificates.X509Certificate2>();

                // 加载 CA 证书
                if (!string.IsNullOrEmpty(caCertPath))
                {
                    if (!File.Exists(caCertPath))
                    {
                        throw new FileNotFoundException($"CA证书文件不存在: {caCertPath}");
                    }
                    _logService.LogInfo($"加载 CA 证书: {caCertPath}");
                    certificates.Add(new System.Security.Cryptography.X509Certificates.X509Certificate2(caCertPath));
                }

                // 加载客户端证书和私钥（双向认证）
                if (!string.IsNullOrEmpty(clientCertPath) && !string.IsNullOrEmpty(clientKeyPath))
                {
                    if (!File.Exists(clientCertPath))
                    {
                        throw new FileNotFoundException($"客户端证书文件不存在: {clientCertPath}");
                    }
                    if (!File.Exists(clientKeyPath))
                    {
                        throw new FileNotFoundException($"客户端私钥文件不存在: {clientKeyPath}");
                    }

                    _logService.LogInfo($"加载客户端证书: {clientCertPath}");
                    _logService.LogInfo($"加载客户端私钥: {clientKeyPath}");

                    // 读取证书和私钥并合并
                    var clientCert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPemFile(
                        clientCertPath,
                        clientKeyPath);
                    certificates.Add(clientCert);
                }

                if (certificates.Any())
                {
                    tlsOptions.Certificates = certificates;
                }

                clientOptions.WithTls(tlsOptions);
            }

            // 配置托管客户端选项（支持自动重连）
            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(clientOptions.Build())
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .Build();

            // 订阅事件
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
            _mqttClient.ConnectingFailedAsync += OnConnectingFailedAsync;

            // 启动连接
            await _mqttClient.StartAsync(managedOptions);
        }
        catch (Exception ex)
        {
            CurrentState = ConnectionState.Error;
            _logService.LogException(ex, "连接 MQTT 服务器失败");
            throw;
        }
    }

    /// <summary>
    /// 断开 MQTT 连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            if (_mqttClient != null)
            {
                _logService.LogInfo("正在断开 MQTT 连接...");

                // 取消事件订阅，防止内存泄漏
                _mqttClient.ConnectedAsync -= OnConnectedAsync;
                _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
                _mqttClient.ApplicationMessageReceivedAsync -= OnApplicationMessageReceivedAsync;
                _mqttClient.ConnectingFailedAsync -= OnConnectingFailedAsync;

                await _mqttClient.StopAsync();

                // 释放 MQTT 客户端资源
                _mqttClient.Dispose();
                _mqttClient = null;

                // 清空活动订阅列表，以便重新连接时能够重新订阅
                _activeSubscriptions.Clear();
                _autoSubscriptions.Clear();
                UpdateSortedSubscriptions(); // Update sorted list after clearing

                CurrentState = ConnectionState.Disconnected;
                _logService.LogInfo("已断开 MQTT 连接并释放资源");
            }
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "断开 MQTT 连接失败");
            throw;
        }
    }

    /// <summary>
    /// 订阅 MQTT 主题
    /// </summary>
    public async Task SubscribeAsync(string topic)
    {
        try
        {
            if (_mqttClient == null)
            {
                throw new InvalidOperationException("MQTT 客户端未初始化");
            }

            _logService.LogInfo($"正在订阅主题: {topic}");
            await _mqttClient.SubscribeAsync(topic);
            _activeSubscriptions.Add(topic); // Add to active subscriptions
            UpdateSortedSubscriptions(); // Update sorted list
            _logService.LogInfo($"成功订阅主题: {topic}");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, $"订阅主题失败: {topic}");
            throw;
        }
    }

    /// <summary>
    /// 批量订阅 MQTT 主题
    /// </summary>
    public async Task SubscribeTopicsAsync(IEnumerable<string> topics)
    {
        if (_mqttClient == null)
        {
            throw new InvalidOperationException("MQTT 客户端未初始化");
        }

        var tasks = new List<Task>();
        foreach (var topic in topics)
        {
            if (!_activeSubscriptions.Contains(topic))
            {
                tasks.Add(SubscribeAsync(topic));
            }
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 取消订阅 MQTT 主题
    /// </summary>
    public async Task UnsubscribeAsync(string topic)
    {
        try
        {
            if (_mqttClient == null)
            {
                throw new InvalidOperationException("MQTT 客户端未初始化");
            }

            _logService.LogInfo($"正在取消订阅主题: {topic}");
            await _mqttClient.UnsubscribeAsync(topic);
            _activeSubscriptions.TryRemove(topic); // Remove from active subscriptions
            UpdateSortedSubscriptions(); // Update sorted list
            _logService.LogInfo($"成功取消订阅主题: {topic}");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, $"取消订阅主题失败: {topic}");
            throw;
        }
    }

    /// <summary>
    /// 发布 MQTT 消息
    /// </summary>
    public async Task PublishAsync(string topic, string payload, bool retain = false)
    {
        try
        {
            if (_mqttClient == null)
            {
                throw new InvalidOperationException("MQTT 客户端未初始化");
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .Build();

            await _mqttClient.EnqueueAsync(message);
            _logService.LogInfo($"发布消息到主题: {topic}");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, $"发布消息失败: {topic}");
            throw;
        }
    }

    /// <summary>
    /// 自动订阅 MQTT 主题（由设备管理服务使用，不显示在手动订阅列表中）
    /// </summary>
    public async Task SubscribeAutoAsync(string topic)
    {
        try
        {
            if (_mqttClient == null)
            {
                throw new InvalidOperationException("MQTT 客户端未初始化");
            }

            _logService.LogInfo($"[自动] 正在订阅主题: {topic}");
            await _mqttClient.SubscribeAsync(topic);
            _autoSubscriptions.Add(topic);
            _logService.LogInfo($"[自动] 成功订阅主题: {topic}");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, $"[自动] 订阅主题失败: {topic}");
            throw;
        }
    }

    /// <summary>
    /// 取消自动订阅 MQTT 主题
    /// </summary>
    public async Task UnsubscribeAutoAsync(string topic)
    {
        try
        {
            if (_mqttClient == null)
            {
                throw new InvalidOperationException("MQTT 客户端未初始化");
            }

            _logService.LogInfo($"[自动] 正在取消订阅主题: {topic}");
            await _mqttClient.UnsubscribeAsync(topic);
            _autoSubscriptions.TryRemove(topic);
            _logService.LogInfo($"[自动] 成功取消订阅主题: {topic}");
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, $"[自动] 取消订阅主题失败: {topic}");
            throw;
        }
    }

    // 事件处理器
    private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        CurrentState = ConnectionState.Connected;
        _logService.LogInfo("MQTT 连接成功");

        // Re-subscribe to all manual subscriptions
        if (_activeSubscriptions.Any())
        {
            _logService.LogInfo($"正在重新订阅 {_activeSubscriptions.Count} 个手动主题...");
            await SubscribeTopicsAsync(_activeSubscriptions);
            UpdateSortedSubscriptions(); // Ensure sorted list is updated after re-subscription
        }

        // Re-subscribe to all auto subscriptions
        if (_autoSubscriptions.Any())
        {
            _logService.LogInfo($"正在重新订阅 {_autoSubscriptions.Count} 个自动主题...");
            var tasks = new List<Task>();
            foreach (var topic in _autoSubscriptions)
            {
                tasks.Add(_mqttClient!.SubscribeAsync(topic));
            }
            await Task.WhenAll(tasks);
        }
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        if (CurrentState == ConnectionState.Connected || CurrentState == ConnectionState.Reconnecting)
        {
            CurrentState = ConnectionState.Reconnecting;
            _logService.LogWarning("MQTT 连接断开，正在尝试重连...");
        }
        else if (CurrentState != ConnectionState.Disconnected)
        {
            CurrentState = ConnectionState.Disconnected;
        }
        return Task.CompletedTask;
    }

    private Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            OnMessageReceived?.Invoke(args);
        }
        catch (Exception ex)
        {
            _logService.LogException(ex, "处理接收到的 MQTT 消息时出错");
        }
        return Task.CompletedTask;
    }

    private Task OnConnectingFailedAsync(ConnectingFailedEventArgs args)
    {
        CurrentState = ConnectionState.Error;
        _logService.LogError($"MQTT 连接失败: {args.Exception?.Message}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 更新排序后的订阅列表
    /// </summary>
    private void UpdateSortedSubscriptions()
    {
        SortedActiveSubscriptions = new ObservableCollection<string>(_activeSubscriptions.OrderBy(t => t));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// A thread-safe hash set implementation.
    /// </summary>
    private class ConcurrentHashSet<T> : System.Collections.Generic.ICollection<T> where T : notnull
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<T, byte> _dictionary;

        public ConcurrentHashSet()
        {
            _dictionary = new System.Collections.Concurrent.ConcurrentDictionary<T, byte>();
        }

        public ConcurrentHashSet(System.Collections.Generic.IEqualityComparer<T> comparer)
        {
            _dictionary = new System.Collections.Concurrent.ConcurrentDictionary<T, byte>(comparer);
        }

        public bool Add(T item)
        {
            return _dictionary.TryAdd(item, 0);
        }

        public bool TryRemove(T item)
        {
            return _dictionary.TryRemove(item, out _);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(T item)
        {
            return _dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _dictionary.Keys.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _dictionary.TryRemove(item, out _);
        }

        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        void System.Collections.Generic.ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
        {
            return _dictionary.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
