# MQTT Monitor - IoT设备监控工具

一个基于 .NET 8.0 和 WPF 开发的 MQTT 监控桌面应用程序，用于实时监控和管理物联网设备的时序数据。

## 项目简介

MQTT Monitor 是一个功能强大的 IoT 设备数据监控工具，支持通过 MQTT 协议连接到物联网设备，实时接收、展示和存储设备的时序数据。该工具采用现代化的 MVVM 架构设计，提供了友好的图形化界面，支持多设备、多测点的数据可视化。

## 主要功能

### 核心功能
- **MQTT 连接管理**
  - 支持连接/断开 MQTT Broker
  - 自动重连机制
  - 连接状态实时显示
  - 支持用户名/密码认证

- **主题订阅管理**
  - 动态订阅/取消订阅 MQTT 主题
  - 支持多主题同时订阅
  - 订阅列表持久化保存
  - 主题列表排序显示

- **数据可视化**
  - **图表视图**: 使用 ScottPlot 实时绘制时序数据曲线
    - 支持多设备、多测点同时展示
    - 智能颜色分配（同设备相似色系，不同设备不同色系）
    - 支持鼠标缩放和平移
    - 可配置的数据点数量限制
    - 可配置的图表更新间隔
  - **表格视图**: 以表格形式展示设备最新数据
    - 实时更新设备状态
    - 显示测点名称、数值和单位
  - **日志视图**: 显示系统运行日志和调试信息

- **命令发送**
  - 支持向设备发送控制命令
  - 可配置的命令模板（通过 commands.json）
  - 支持带参数的命令
  - 命令响应监听和显示

- **数据存储**
  - 自动将接收到的数据保存为 CSV 文件
  - 按设备和测点分别存储
  - 支持后台异步写入
  - 数据文件自动管理

- **图例配置**
  - 支持自定义图表图例的显示/隐藏
  - 图例配置持久化保存
  - 灵活的图例分组管理

## 技术栈

- **框架**: .NET 8.0 Windows
- **UI框架**: WPF (Windows Presentation Foundation)
- **架构模式**: MVVM
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **MVVM工具包**: CommunityToolkit.Mvvm 8.4.0
- **MQTT客户端**: MQTTnet 4.3.7
- **图表库**: ScottPlot 5.1.57
- **JSON序列化**: Newtonsoft.Json 13.0.4

## 系统要求

### 运行环境
- **操作系统**: Windows 10/11 (x64)
- **运行时**: .NET 8.0 Desktop Runtime

### 开发环境
- **IDE**: Visual Studio 2022 或 JetBrains Rider
- **SDK**: .NET 8.0 SDK
- **操作系统**: Windows 10/11

## 安装与构建

### 1. 克隆项目
```bash
git clone <repository-url>
cd mqtt_shower
```

### 2. 安装 .NET 8.0 SDK
从 [Microsoft 官网](https://dotnet.microsoft.com/download/dotnet/8.0) 下载并安装 .NET 8.0 SDK。

### 3. 恢复依赖
```bash
dotnet restore
```

### 4. 编译项目
```bash
dotnet build
```

### 5. 运行程序
```bash
dotnet run
```

或者在 Visual Studio 中直接按 F5 运行。

### 6. 发布可执行文件
```bash
# 发布为单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\publish
```

## 配置说明

### config.json - 主配置文件

```json
{
  "Title": "Mqtt Monitor",              // 应用程序标题
  "Server": "119.45.181.86",            // MQTT Broker 地址
  "Port": 1883,                         // MQTT Broker 端口
  "Username": "admin",                  // MQTT 用户名
  "Password": "your_strong_password",   // MQTT 密码
  "ClientId": null,                     // 客户端ID（null则自动生成）
  "BaseTopic": "iot/devices",           // 基础主题（用于命令发送）
  "SubscribedTopics": [                 // 启动时自动订阅的主题列表
    "iot/devices/1111/datas",
    "iot/devices/2222/datas"
  ],
  "WindowWidth": 1000.0,                // 窗口宽度
  "WindowHeight": 600.0,                // 窗口高度
  "WindowLeft": "NaN",                  // 窗口X位置
  "WindowTop": "NaN",                   // 窗口Y位置
  "WindowState": "Maximized",           // 窗口状态
  "MaxChartDataPoints": 1000,           // 图表最大数据点数
  "ChartUpdateInterval": 800            // 图表更新间隔（毫秒）
}
```

### commands.json - 命令模板配置

```json
[
  {
    "Name": "setThreshold",              // 命令名称
    "Description": "设置设备阈值参数",     // 命令描述
    "Parameters": [                      // 命令参数列表
      {
        "Name": "metricName",
        "Type": "string",
        "Description": "测点名称",
        "Value": "temperature"           // 默认值
      }
    ]
  }
]
```

## 使用方法

### 启动和连接

1. **修改配置文件**
   - 编辑 `config.json`，填入你的 MQTT Broker 地址、端口、用户名和密码

2. **启动程序**
   - 双击运行可执行文件或使用 `dotnet run`

3. **连接到 MQTT Broker**
   - 点击界面上的"连接"按钮
   - 等待状态显示为"已连接"

### 订阅主题

- **通过配置文件**: 在 `config.json` 的 `SubscribedTopics` 中添加主题
- **通过界面**:
  1. 在右侧"Topic订阅管理"区域的输入框中输入主题
  2. 点击"订阅"按钮
  3. 订阅的主题会自动保存到配置文件

### 查看数据

- **图表视图**: 切换到"图表"标签页，查看实时数据曲线
- **表格视图**: 切换到"表格"标签页，查看最新设备状态
- **日志视图**: 切换到"日志"标签页，查看系统运行日志

### 发送命令

1. 切换到"命令发送"标签页（如果有）
2. 选择目标设备
3. 选择命令类型
4. 填写命令参数
5. 点击"发送"按钮

### 数据存储

- 数据会自动保存到 `data` 目录下的 CSV 文件中
- 文件命名格式: `{设备ID}_{测点名称}.csv`
- 包含时间戳、数值和单位三列

## 数据协议

本应用遵循标准的 IoT MQTT 数据协议，详细规范请参考 [`doc/MQTT规则.md`](doc/MQTT规则.md)。

### 上行数据格式（设备 → 云端）

**主题**: `.../{deviceId}/datas`

```json
{
  "deviceId": "ENV-MON-001",
  "timestamp": 1730489461521,
  "payload": [
    {
      "name": "temperature",
      "value": 22.8,
      "unit": "°C"
    },
    {
      "name": "humidity",
      "value": 65.5,
      "unit": "%"
    }
  ]
}
```

### 下行命令格式（云端 → 设备）

**命令请求主题**: `.../{deviceId}/command/request`
**命令响应主题**: `.../{deviceId}/command/response`

```json
{
  "commandId": "cmd-12345",
  "commandName": "setThreshold",
  "timestamp": 1730491500000,
  "params": {
    "metricName": "temperature",
    "thresholdValue": 30
  }
}
```

## 项目结构

```
mqtt_shower/
├── Assets/                    # 资源文件
│   └── icon.ico              # 应用程序图标
├── Converters/               # 数据转换器
│   └── InvertedBooleanToVisibilityConverter.cs
├── Models/                   # 数据模型
│   ├── ChartLegendConfig.cs  # 图例配置
│   ├── ChartLegendItem.cs    # 图例项
│   ├── CommandData.cs        # 命令数据
│   ├── DeviceState.cs        # 设备状态
│   ├── MqttSettings.cs       # MQTT设置
│   └── UpstreamData.cs       # 上行数据
├── Services/                 # 业务服务
│   ├── ChartLegendConfigService.cs  # 图例配置服务
│   ├── CsvDataStorageService.cs     # CSV数据存储服务
│   ├── DataProcessingService.cs     # 数据处理服务
│   ├── LogService.cs                # 日志服务
│   └── MqttService.cs               # MQTT服务
├── ViewModels/               # 视图模型
│   ├── ChartLegendGroupViewModel.cs
│   ├── ChartViewModel.cs     # 图表视图模型
│   ├── CommandSenderViewModel.cs    # 命令发送视图模型
│   ├── LogViewModel.cs       # 日志视图模型
│   ├── MainViewModel.cs      # 主窗口视图模型
│   ├── SettingsViewModel.cs  # 设置视图模型
│   └── TableViewModel.cs     # 表格视图模型
├── Views/                    # 视图
│   ├── ChartView.xaml        # 图表视图
│   ├── CommandSenderView.xaml       # 命令发送视图
│   ├── LogView.xaml          # 日志视图
│   ├── SettingsWindow.xaml   # 设置窗口
│   └── TableView.xaml        # 表格视图
├── doc/                      # 文档
│   ├── MQTT规则.md           # MQTT协议规范
│   ├── MQTT_TEST_README.md   # 测试工具说明
│   └── mqtt_test_sender.py   # Python测试脚本
├── App.xaml                  # WPF应用程序定义
├── App.xaml.cs               # 应用程序入口和依赖注入配置
├── MainWindow.xaml           # 主窗口界面
├── MainWindow.xaml.cs        # 主窗口代码
├── MqttMonitor.csproj        # 项目文件
├── config.json               # 配置文件
└── commands.json             # 命令模板配置
```

## 开发指南

### MVVM 架构

本项目严格遵循 MVVM (Model-View-ViewModel) 架构模式：

- **Model**: 位于 `Models/` 目录，定义数据结构
- **View**: 位于 `Views/` 目录和根目录的 XAML 文件，定义用户界面
- **ViewModel**: 位于 `ViewModels/` 目录，实现业务逻辑和数据绑定

### 依赖注入

应用程序使用 Microsoft.Extensions.DependencyInjection 进行依赖注入，所有服务和 ViewModel 在 `App.xaml.cs` 的 `ConfigureServices` 方法中注册。

### 添加新功能

1. **添加新的数据模型**: 在 `Models/` 目录创建新的类
2. **添加新的服务**: 在 `Services/` 目录创建服务类，并在 `App.xaml.cs` 中注册
3. **添加新的视图**: 在 `Views/` 目录创建 XAML 和对应的代码文件
4. **添加新的 ViewModel**: 在 `ViewModels/` 目录创建 ViewModel 类

### 测试数据发送

项目包含一个 Python 测试脚本 `doc/mqtt_test_sender.py`，用于模拟 IoT 设备发送数据：

```bash
# 安装依赖
pip install paho-mqtt

# 运行测试脚本
cd doc
python mqtt_test_sender.py
```

详细说明请参考 [`doc/MQTT_TEST_README.md`](doc/MQTT_TEST_README.md)。

## 常见问题

### 1. 无法连接到 MQTT Broker
- 检查网络连接
- 确认 MQTT Broker 地址和端口是否正确
- 检查用户名和密码是否正确
- 查看日志标签页的错误信息

### 2. 图表不显示数据
- 确认已成功订阅相关主题
- 检查接收到的数据格式是否符合协议规范
- 查看日志标签页是否有数据解析错误
- 确认时间戳格式为 Unix 毫秒级时间戳

### 3. CSV 文件无法保存
- 检查程序是否有写入权限
- 确认磁盘空间是否充足
- 查看日志中的错误信息

### 4. 程序崩溃或异常
- 查看应用程序目录下的 `error.log` 文件
- 检查 `config.json` 格式是否正确
- 尝试删除配置文件，使用默认配置

## 配置优化建议

### 性能优化
- **MaxChartDataPoints**: 根据设备数量调整，默认 1000 点
  - 少量设备（1-5个）: 1000-2000 点
  - 中等数量（5-20个）: 500-1000 点
  - 大量设备（>20个）: 200-500 点

- **ChartUpdateInterval**: 图表更新间隔（毫秒）
  - 高刷新率: 500-800 毫秒
  - 平衡模式: 800-1200 毫秒
  - 低刷新率: 1500-3000 毫秒

## 许可证

本项目采用 [MIT License](LICENSE)（如果有的话，请根据实际情况修改）。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 更新日志

### v1.0.0
- 初始版本发布
- 支持 MQTT 连接和主题订阅
- 实现图表、表格、日志三种视图
- 支持命令发送功能
- 支持 CSV 数据存储
- 支持图例配置管理

---

**注意**:
- 首次运行前请务必修改 `config.json` 中的 MQTT 连接信息
- 生产环境使用时请修改默认密码
- 建议定期备份 `data` 目录下的数据文件
