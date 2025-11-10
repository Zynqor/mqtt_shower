# MQTT Monitor - 专业级IoT设备监控系统

一个基于 .NET 8.0 和 WPF 开发的企业级 MQTT 监控桌面应用程序，用于实时监控和管理物联网设备的时序数据。

## 项目简介

MQTT Monitor 是一个功能完善的专业级 IoT 设备数据监控工具，支持通过 MQTT 协议连接到物联网设备，实时接收、展示和存储设备的时序数据。该工具采用现代化的 MVVM 架构设计，提供了友好的图形化界面，支持多设备、多测点的数据可视化和智能告警管理。

## ✨ 核心特性

### 🔌 MQTT连接管理
- ✅ 支持 MQTT 3.1.1 / 5.0 协议
- ✅ TLS/SSL 加密连接（支持单向和双向认证）
- ✅ 自动重连机制
- ✅ 连接状态实时显示
- ✅ 用户名/密码认证
- ✅ 密码加密存储（AES加密）
- ✅ 多主题订阅管理
- ✅ QoS级别配置

### 📊 数据可视化
- **实时图表**
  - 使用 ScottPlot 绘制实时时序数据曲线
  - 支持多设备、多测点同时展示
  - 智能颜色分配（同设备相似色系，不同设备不同色系）
  - 支持鼠标缩放和平移
  - 可配置的数据点数量限制（默认1000点）
  - 可配置的图表更新间隔（默认800ms）
  - 自定义图例管理（显示/隐藏、重命名）

- **数据表格**
  - 实时更新设备状态
  - 显示测点名称、数值和单位
  - 支持多设备并行显示

- **日志视图**
  - 分级日志（Info/Warning/Error）
  - 实时日志显示
  - 异常详细记录
  - 日志文件持久化

### 🚨 智能告警系统
- **告警检测**
  - 上下限阈值检测
  - 实时告警触发
  - 支持按设备、测点配置告警规则

- **告警管理**
  - 活动告警实时显示
  - 告警历史记录（SQLite数据库）
  - 告警确认功能
  - 告警自动恢复检测

- **告警提醒**
  - 声音提醒（可配置音频文件）
  - 系统通知提醒
  - 提醒方式灵活配置

- **告警统计**
  - 按设备统计告警次数（柱状图）
  - 告警趋势分析（折线图）
  - 告警类型分布（饼图）
  - 统计数据可视化展示

### 📜 历史数据查询
- **CSV历史查询**
  - 按设备查询历史数据
  - 按时间范围查询（天级精度）
  - 查询结果表格展示
  - 支持导出为CSV文件

- **告警历史查询**
  - 按设备查询告警记录
  - 按时间范围查询（秒级精度）
  - 按告警类型筛选（上限/下限）
  - 查询结果导出

### 📤 命令下发
- 预配置命令管理（通过commands.json）
- 自定义命令下发
- 支持带参数的命令
- 命令响应监听和显示
- Topic订阅管理

### 💾 数据存储
- **CSV存储**
  - 自动按设备和测点分别存储
  - 批量写入优化（每5秒或100条刷新）
  - 支持后台异步写入
  - 数据文件自动管理
  - 程序退出时自动刷新缓存

- **SQLite数据库**
  - 告警记录持久化
  - 高效索引查询
  - 自动资源释放

### ⚙️ 配置管理
- **模块化配置文件**（支持自动迁移）
  - `mqtt_config.json` - MQTT连接配置
  - `chart_config.json` - 图表样式配置
  - `company_info.json` - 公司信息配置
  - `layout_settings.json` - 布局设置
  - `alarm_config.json` - 告警规则配置
  - `alert_settings.json` - 提醒设置
  - `chart_legend_config.json` - 图例配置
  - `commands.json` - 命令模板配置

- **配置自动迁移**
  - 支持从旧版config.json自动迁移
  - 配置文件版本兼容

### 🎨 现代化UI设计
- 统一的按钮样式（蓝/绿/红/灰/浅色）
- 优化的DatePicker和输入框
- 可拖动分隔条（支持布局保存）
- 响应式布局
- 图标和emoji支持
- 窗口位置和大小自动保存

### 🔧 资源管理
- 完善的Dispose模式
- 程序退出时优雅释放所有资源
- MQTT连接自动断开
- CSV缓存自动刷新
- 数据库连接自动释放
- Timer资源自动清理

## 技术栈

- **框架**: .NET 8.0 Windows
- **UI框架**: WPF (Windows Presentation Foundation)
- **架构模式**: MVVM
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **MVVM工具包**: CommunityToolkit.Mvvm 8.4.0
- **MQTT客户端**: MQTTnet 4.3.7
- **图表库**: ScottPlot 5.1.57
- **JSON序列化**: Newtonsoft.Json 13.0.4
- **数据库**: Microsoft.Data.Sqlite 9.0.1

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
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ../publish
```

## 配置说明

### 配置文件结构

程序首次运行时会自动创建以下目录结构：

```
项目根目录/
├── configs/                    # 配置文件目录（自动创建）
│   ├── mqtt_config.json       # MQTT连接配置
│   ├── chart_config.json      # 图表样式配置
│   ├── company_info.json      # 公司信息配置
│   ├── layout_settings.json   # 布局设置
│   ├── alarm_config.json      # 告警规则配置
│   ├── alert_settings.json    # 提醒设置
│   ├── chart_legend_config.json # 图例配置
│   └── commands.json          # 命令模板配置
├── data/                       # 数据文件目录（自动创建）
│   ├── alarm_records.db       # 告警记录数据库
│   └── csv/                   # CSV数据目录
│       ├── Device_A_temperature_20250101.csv
│       └── Device_B_humidity_20250101.csv
├── logs/                       # 日志文件目录（自动创建）
│   └── app_20250101.log
└── Sounds/                     # 声音文件目录
    ├── alarm.wav
    └── notification.wav
```

详细的配置文件说明请参考 [CONFIG.md](CONFIG.md)

## 使用方法

### 首次启动和连接

1. **配置MQTT连接**
   - 点击主界面右下角的"设置"按钮
   - 填入MQTT Broker地址、端口、用户名和密码
   - （可选）配置TLS/SSL加密
   - 点击"保存"按钮

2. **启动程序并连接**
   - 点击界面上的"连接"按钮
   - 等待状态显示为"已连接"（绿色）

### 订阅主题

- **通过界面添加**:
  1. 在右侧"Topic订阅管理"区域的输入框中输入主题
  2. 点击"订阅"按钮
  3. 订阅的主题会自动保存到配置文件

- **通过配置文件**:
  编辑 `configs/mqtt_config.json` 的 `SubscribedTopics` 数组

### 查看实时数据

- **图表视图**: 切换到"图表"标签页，查看实时数据曲线
  - 可以使用鼠标滚轮缩放
  - 右键拖动可以平移视图

- **表格视图**: 切换到"表格"标签页，查看最新设备状态

- **日志视图**: 切换到"日志"标签页，查看系统运行日志

### 配置告警规则

1. 点击主界面右下角的"告警配置"按钮
2. 为设备的特定测点配置上下限阈值
3. 点击"保存"按钮
4. 告警会自动检测并记录

### 查看告警

- **活动告警**: 切换到"告警"标签页的左侧区域
- **告警历史**: 切换到"告警"标签页的右侧区域
- **告警统计**: 切换到"告警统计"标签页

### 查询历史数据

1. **查询CSV历史数据**:
   - 点击"历史查询"按钮
   - 选择设备和时间范围
   - 点击"查询"按钮
   - 可导出查询结果为CSV

2. **查询告警历史**:
   - 点击"告警历史查询"按钮
   - 选择设备、时间范围和告警类型
   - 点击"查询"按钮
   - 可导出查询结果

### 发送命令

1. 切换到"命令发送"标签页
2. 选择目标设备
3. 选择命令类型（或自定义命令）
4. 填写命令参数
5. 点击"发送"按钮

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
│   ├── AlarmConfig.cs        # 告警配置
│   ├── AlarmRecord.cs        # 告警记录
│   ├── AlertSettings.cs      # 提醒设置
│   ├── ChartConfig.cs        # 图表配置
│   ├── ChartLegendConfig.cs  # 图例配置
│   ├── ChartLegendItem.cs    # 图例项
│   ├── CommandData.cs        # 命令数据
│   ├── CompanyInfo.cs        # 公司信息
│   ├── DeviceState.cs        # 设备状态
│   ├── LayoutSettings.cs     # 布局设置
│   ├── MqttSettings.cs       # MQTT设置
│   └── UpstreamData.cs       # 上行数据
├── Services/                 # 业务服务
│   ├── AlarmConfigService.cs        # 告警配置服务
│   ├── AlarmDatabaseService.cs      # 告警数据库服务
│   ├── AlarmDetectionService.cs     # 告警检测服务
│   ├── AlarmHistoryStorageService.cs # 告警历史存储服务
│   ├── ChartConfigService.cs        # 图表配置服务
│   ├── ChartLegendConfigService.cs  # 图例配置服务
│   ├── CompanyInfoService.cs        # 公司信息服务
│   ├── CsvDataStorageService.cs     # CSV数据存储服务
│   ├── DataProcessingService.cs     # 数据处理服务
│   ├── EncryptionService.cs         # 加密服务
│   ├── LayoutSettingsService.cs     # 布局设置服务
│   ├── LogService.cs                # 日志服务
│   ├── MqttService.cs               # MQTT服务
│   ├── PathManager.cs               # 路径管理器
│   └── SoundPlayerService.cs        # 声音播放服务
├── ViewModels/               # 视图模型
│   ├── AlarmConfigViewModel.cs      # 告警配置视图模型
│   ├── AlarmHistoryQueryViewModel.cs # 告警历史查询视图模型
│   ├── AlarmStatisticsViewModel.cs  # 告警统计视图模型
│   ├── AlarmViewModel.cs            # 告警视图模型
│   ├── AlertSettingsViewModel.cs    # 提醒设置视图模型
│   ├── ChartLegendGroupViewModel.cs # 图例分组视图模型
│   ├── ChartSettingsViewModel.cs    # 图表设置视图模型
│   ├── ChartViewModel.cs            # 图表视图模型
│   ├── CommandSenderViewModel.cs    # 命令发送视图模型
│   ├── HistoryQueryViewModel.cs     # 历史查询视图模型
│   ├── LogViewModel.cs              # 日志视图模型
│   ├── MainViewModel.cs             # 主窗口视图模型
│   ├── SettingsViewModel.cs         # 设置视图模型
│   └── TableViewModel.cs            # 表格视图模型
├── Views/                    # 视图
│   ├── AlarmConfigWindow.xaml       # 告警配置窗口
│   ├── AlarmHistoryQueryWindow.xaml # 告警历史查询窗口
│   ├── AlarmStatisticsView.xaml     # 告警统计视图
│   ├── AlarmView.xaml               # 告警视图
│   ├── AlertSettingsWindow.xaml     # 提醒设置窗口
│   ├── ChartSettingsWindow.xaml     # 图表设置窗口
│   ├── ChartView.xaml               # 图表视图
│   ├── CommandSenderView.xaml       # 命令发送视图
│   ├── ContactUsWindow.xaml         # 联系我们窗口
│   ├── HistoryQueryWindow.xaml      # 历史查询窗口
│   ├── LogView.xaml                 # 日志视图
│   ├── SettingsWindow.xaml          # 设置窗口
│   └── TableView.xaml               # 表格视图
├── doc/                      # 文档
│   ├── MQTT规则.md           # MQTT协议规范
│   ├── MQTT_TEST_README.md   # 测试工具说明
│   └── mqtt_test_sender.py   # Python测试脚本
├── App.xaml                  # WPF应用程序定义
├── App.xaml.cs               # 应用程序入口和依赖注入配置
├── MainWindow.xaml           # 主窗口界面
├── MainWindow.xaml.cs        # 主窗口代码
├── MqttMonitor.csproj        # 项目文件
├── README.md                 # 本文档
├── CONFIG.md                 # 配置文件详细说明
└── FEATURE_REVIEW.md         # 功能完善度评估报告
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
- 如果使用TLS，检查证书文件路径
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
- 程序退出时会自动刷新所有CSV缓存

### 4. 告警不触发
- 检查告警规则是否正确配置
- 确认设备ID和测点名称与实际数据匹配
- 查看日志中的告警检测信息
- 检查提醒设置是否启用

### 5. 程序崩溃或异常
- 查看应用程序目录下的 `logs/` 目录中的日志文件
- 检查配置文件格式是否正确
- 查看 `shutdown_error.log` 文件（如果存在）
- 尝试删除 `configs/` 目录，让程序重新生成默认配置

## 性能优化建议

### 图表配置
- **MaxChartDataPoints**: 根据设备数量调整，默认 1000 点
  - 少量设备（1-5个）: 1000-2000 点
  - 中等数量（5-20个）: 500-1000 点
  - 大量设备（>20个）: 200-500 点

- **ChartUpdateInterval**: 图表更新间隔（毫秒）
  - 高刷新率: 500-800 毫秒
  - 平衡模式: 800-1200 毫秒（推荐）
  - 低刷新率: 1500-3000 毫秒

### CSV存储优化
- 程序默认采用批量写入策略（每5秒或100条数据刷新一次）
- 大量数据场景下可以调整刷新间隔
- 程序退出时会自动刷新所有缓存，确保数据完整性

## 安全建议

1. **密码保护**: 程序会自动加密存储MQTT密码，但仍建议使用强密码
2. **TLS加密**: 生产环境建议启用TLS/SSL加密连接
3. **证书验证**: 不要在生产环境使用"忽略证书错误"选项
4. **配置文件**: 妥善保管 `configs/` 目录下的配置文件
5. **定期备份**: 建议定期备份 `data/` 目录下的数据和数据库文件

## 许可证

本项目采用 [MIT License](LICENSE)（请根据实际情况修改）。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 更新日志

### v2.0.0 (2025-01-10)
- ✨ 新增告警系统（检测、记录、统计、提醒）
- ✨ 新增历史数据查询功能（CSV和告警历史）
- ✨ 新增告警统计和可视化分析
- ✨ 新增配置文件模块化拆分
- ✨ 新增TLS/SSL加密连接支持
- ✨ 新增声音和系统通知提醒
- ⚡ 优化CSV写入性能（批量写入）
- ⚡ 优化资源管理（完善的Dispose模式）
- 🎨 优化UI设计（统一按钮样式、DatePicker）
- 🐛 修复多项已知问题

### v1.0.0
- 初始版本发布
- 支持 MQTT 连接和主题订阅
- 实现图表、表格、日志三种视图
- 支持命令发送功能
- 支持 CSV 数据存储
- 支持图例配置管理

---

**注意**:
- 首次运行前请通过界面的"设置"按钮配置 MQTT 连接信息
- 生产环境使用时请修改默认密码并启用TLS加密
- 建议定期备份 `data` 目录下的数据文件和数据库
- 配置文件支持从旧版本自动迁移，无需手动修改
