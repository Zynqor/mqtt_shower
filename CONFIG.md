# 配置文件详细说明

MQTT Monitor 使用模块化的配置文件系统，每个配置文件负责管理特定功能的设置。所有配置文件位于 `configs/` 目录下，程序首次运行时会自动创建。

## 配置文件概览

| 配置文件 | 用途 | 自动创建 | 支持迁移 |
|---------|------|----------|----------|
| `mqtt_config.json` | MQTT连接设置 | ✅ | ✅ |
| `chart_config.json` | 图表显示配置 | ✅ | ✅ |
| `company_info.json` | 公司信息 | ✅ | ✅ |
| `layout_settings.json` | 窗口布局 | ✅ | ✅ |
| `alarm_config.json` | 告警规则 | ✅ | ✅ |
| `alert_settings.json` | 提醒设置 | ✅ | ✅ |
| `chart_legend_config.json` | 图例配置 | ✅ | ❌ |
| `commands.json` | 命令模板 | ✅ | ❌ |

## 1. MQTT连接配置 (mqtt_config.json)

### 文件位置
`configs/mqtt_config.json`

### 配置示例
```json
{
  "Title": "Mqtt Monitor",
  "Server": "mqtt.example.com",
  "Port": 1883,
  "Username": "admin",
  "Password": "encrypted_password_here",
  "ClientId": null,
  "BaseTopic": "iot/devices",
  "SubscribedTopics": [
    "iot/devices/+/datas",
    "iot/devices/sensor001/status"
  ],
  "UseTls": false,
  "CaCertificatePath": null,
  "ClientCertificatePath": null,
  "ClientKeyPath": null,
  "IgnoreCertificateErrors": false
}
```

### 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| `Title` | string | "Mqtt Monitor" | 应用程序标题 |
| `Server` | string | "localhost" | MQTT Broker 服务器地址 |
| `Port` | int | 1883 | MQTT Broker 端口（TLS通常为8883） |
| `Username` | string | null | MQTT 用户名（可选） |
| `Password` | string | null | MQTT 密码（自动加密存储） |
| `ClientId` | string | null | 客户端ID（null则自动生成UUID） |
| `BaseTopic` | string | "iot/devices" | 基础主题（用于命令发送） |
| `SubscribedTopics` | array | [] | 启动时自动订阅的主题列表 |
| `UseTls` | bool | false | 是否启用TLS/SSL加密 |
| `CaCertificatePath` | string | null | CA证书路径（验证服务器） |
| `ClientCertificatePath` | string | null | 客户端证书路径（双向认证） |
| `ClientKeyPath` | string | null | 客户端私钥路径（双向认证） |
| `IgnoreCertificateErrors` | bool | false | 忽略证书错误（仅测试用） |

### 注意事项
- **密码安全**: 密码会自动使用AES加密存储
- **主题通配符**: 支持MQTT标准通配符（`+` 和 `#`）
- **TLS配置**: 生产环境强烈建议启用TLS
- **证书路径**: 使用绝对路径或相对于程序目录的路径

---

## 2. 图表配置 (chart_config.json)

### 文件位置
`configs/chart_config.json`

### 配置示例
```json
{
  "MaxChartDataPoints": 1000,
  "ChartUpdateInterval": 800
}
```

### 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| `MaxChartDataPoints` | int | 1000 | 图表保留的最大数据点数 |
| `ChartUpdateInterval` | int | 800 | 图表刷新间隔（毫秒） |

### 性能调优建议

**MaxChartDataPoints（数据点数量）**:
- **少量设备（1-5个）**: 1000-2000 点
- **中等数量（5-20个）**: 500-1000 点
- **大量设备（>20个）**: 200-500 点

**ChartUpdateInterval（刷新间隔）**:
- **高刷新率**: 500-800 毫秒（实时性要求高）
- **平衡模式**: 800-1200 毫秒（推荐，性能与流畅度平衡）
- **低刷新率**: 1500-3000 毫秒（节省CPU资源）

---

## 3. 公司信息配置 (company_info.json)

### 文件位置
`configs/company_info.json`

### 配置示例
```json
{
  "CompanyName": "您的公司名称",
  "CompanyAddress": "公司地址",
  "CompanyPhone": "联系电话",
  "CompanyEmail": "contact@company.com"
}
```

### 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| `CompanyName` | string | "您的公司名称" | 公司/组织名称 |
| `CompanyAddress` | string | "您的公司地址" | 公司地址 |
| `CompanyPhone` | string | "联系电话" | 联系电话 |
| `CompanyEmail` | string | "contact@company.com" | 联系邮箱 |

### 用途
此配置用于"联系我们"窗口，显示公司联系信息。

---

## 4. 布局设置 (layout_settings.json)

### 文件位置
`configs/layout_settings.json`

### 配置示例
```json
{
  "WindowWidth": 1200.0,
  "WindowHeight": 800.0,
  "WindowLeft": 100.0,
  "WindowTop": 100.0,
  "WindowState": "Maximized",
  "AlarmViewLeftPanelWidth": 1.0,
  "AlarmViewRightPanelWidth": 3.0,
  "ActiveAlarmHeight": 1.0,
  "HistoryAlarmHeight": 1.0
}
```

### 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| `WindowWidth` | double | 1000.0 | 主窗口宽度（像素） |
| `WindowHeight` | double | 600.0 | 主窗口高度（像素） |
| `WindowLeft` | double | NaN | 窗口X坐标（NaN为居中） |
| `WindowTop` | double | NaN | 窗口Y坐标（NaN为居中） |
| `WindowState` | string | "Normal" | 窗口状态（Normal/Maximized/Minimized） |
| `AlarmViewLeftPanelWidth` | double | 1.0 | 告警页面左侧面板相对宽度 |
| `AlarmViewRightPanelWidth` | double | 3.0 | 告警页面右侧面板相对宽度 |
| `ActiveAlarmHeight` | double | 1.0 | 活动告警区域相对高度 |
| `HistoryAlarmHeight` | double | 1.0 | 历史告警区域相对高度 |

### 注意事项
- 此文件通常由程序自动维护，无需手动编辑
- 程序退出时会自动保存当前窗口状态
- 相对宽度/高度使用比例关系（如1:3表示左侧占25%，右侧占75%）

---

## 5. 告警规则配置 (alarm_config.json)

### 文件位置
`configs/alarm_config.json`

### 配置示例
```json
{
  "AlarmRules": [
    {
      "DeviceId": "sensor001",
      "MetricName": "temperature",
      "UpperLimit": 35.0,
      "LowerLimit": 10.0,
      "IsEnabled": true
    },
    {
      "DeviceId": "sensor001",
      "MetricName": "humidity",
      "UpperLimit": 80.0,
      "LowerLimit": 30.0,
      "IsEnabled": true
    }
  ]
}
```

### 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| `DeviceId` | string | - | 设备ID（必须与MQTT消息中的deviceId匹配） |
| `MetricName` | string | - | 测点名称（必须与payload中的name匹配） |
| `UpperLimit` | double | - | 上限阈值（超过触发上限告警） |
| `LowerLimit` | double | - | 下限阈值（低于触发下限告警） |
| `IsEnabled` | bool | true | 是否启用此告警规则 |

### 使用建议
- 通过界面的"告警配置"按钮配置，无需手动编辑
- 支持为同一设备的不同测点配置不同的告警规则
- 可以只设置上限或下限，不需要的阈值设为null

---

## 6. 提醒设置 (alert_settings.json)

### 文件位置
`configs/alert_settings.json`

### 配置示例
```json
{
  "EnableSound": true,
  "EnableNotification": true,
  "SoundFilePath": "Sounds/alarm.wav"
}
```

### 参数说明

| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| `EnableSound` | bool | true | 启用声音提醒 |
| `EnableNotification` | bool | true | 启用系统通知提醒 |
| `SoundFilePath` | string | "Sounds/alarm.wav" | 告警音频文件路径 |

### 自定义音频
1. 将音频文件（.wav格式）放入 `Sounds/` 目录
2. 修改 `SoundFilePath` 指向新文件
3. 支持相对路径或绝对路径

---

## 7. 图例配置 (chart_legend_config.json)

### 文件位置
`configs/chart_legend_config.json`

### 配置示例
```json
{
  "LegendGroups": [
    {
      "GroupName": "sensor001",
      "Items": [
        {
          "OriginalName": "sensor001_temperature",
          "DisplayName": "温度传感器1",
          "IsVisible": true
        },
        {
          "OriginalName": "sensor001_humidity",
          "DisplayName": "湿度传感器1",
          "IsVisible": true
        }
      ]
    }
  ]
}
```

### 参数说明

**LegendGroups** - 图例分组数组

每个分组包含：
- `GroupName`: 分组名称（通常为设备ID）
- `Items`: 图例项数组

**图例项参数**:
| 参数 | 类型 | 默认值 | 说明 |
|-----|------|--------|------|
| `OriginalName` | string | - | 原始名称（设备ID_测点名） |
| `DisplayName` | string | - | 显示名称（可自定义） |
| `IsVisible` | bool | true | 是否显示此数据线 |

### 使用建议
- 通过图表界面右键菜单配置，无需手动编辑
- 可以为每条数据线设置友好的显示名称
- 可以隐藏不需要的数据线

---

## 8. 命令模板配置 (commands.json)

### 文件位置
`configs/commands.json`

### 配置示例
```json
[
  {
    "Name": "setThreshold",
    "Description": "设置设备阈值参数",
    "Parameters": [
      {
        "Name": "metricName",
        "Type": "string",
        "Description": "测点名称",
        "Value": "temperature"
      },
      {
        "Name": "thresholdValue",
        "Type": "double",
        "Description": "阈值",
        "Value": "30.0"
      }
    ]
  },
  {
    "Name": "restart",
    "Description": "重启设备",
    "Parameters": []
  }
]
```

### 参数说明

**命令对象**:
| 参数 | 类型 | 说明 |
|-----|------|------|
| `Name` | string | 命令名称（发送到设备的commandName） |
| `Description` | string | 命令描述（界面显示用） |
| `Parameters` | array | 命令参数列表 |

**参数对象**:
| 字段 | 类型 | 说明 |
|-----|------|------|
| `Name` | string | 参数名称 |
| `Type` | string | 参数类型（string/int/double/bool） |
| `Description` | string | 参数描述 |
| `Value` | string | 默认值 |

### 使用说明
1. 在命令发送界面选择预配置的命令
2. 填写必要的参数
3. 选择目标设备
4. 点击发送按钮

---

## 配置文件自动迁移

程序支持从旧版 `config.json` 自动迁移到新的模块化配置文件。

### 迁移过程

1. **首次运行检测**
   - 程序启动时检查 `configs/` 目录
   - 如果不存在，自动创建

2. **旧配置检测**
   - 检查根目录是否存在旧版 `config.json`
   - 如果存在且新配置文件不存在，触发自动迁移

3. **迁移执行**
   - 读取旧配置文件
   - 按功能拆分到对应的新配置文件
   - 保留原 `config.json` 作为备份

4. **迁移日志**
   - 迁移过程会记录到日志文件
   - 可以通过日志标签页查看迁移状态

### 迁移映射

```
旧 config.json 字段 → 新配置文件
═══════════════════════════════════
Server, Port, Username...      → mqtt_config.json
WindowWidth, WindowHeight...   → layout_settings.json
MaxChartDataPoints...          → chart_config.json
CompanyName, CompanyEmail...   → company_info.json
```

---

## 配置文件路径管理

程序使用 `PathManager` 统一管理所有路径：

```csharp
// 配置文件路径
PathManager.ConfigsDir           // configs/
PathManager.MqttConfigFile      // configs/mqtt_config.json
PathManager.ChartConfigFile     // configs/chart_config.json
PathManager.CompanyInfoFile     // configs/company_info.json
PathManager.LayoutSettingsFile  // configs/layout_settings.json
...

// 数据文件路径  
PathManager.DataDir             // data/
PathManager.CsvDataDir          // data/csv/
PathManager.AlarmDatabaseFile   // data/alarm_records.db

// 日志文件路径
PathManager.LogsDir             // logs/
```

---

## 最佳实践

### 1. 配置管理
- ✅ 通过界面配置，避免手动编辑JSON文件
- ✅ 定期备份 `configs/` 目录
- ✅ 版本控制时排除包含敏感信息的配置文件

### 2. 安全性
- ✅ 生产环境启用TLS加密
- ✅ 使用强密码（程序会自动加密）
- ✅ 不要使用"忽略证书错误"选项
- ✅ 妥善保管证书文件

### 3. 性能优化
- ✅ 根据设备数量调整图表数据点
- ✅ 根据CPU性能调整刷新间隔
- ✅ 合理配置告警规则，避免过度告警

### 4. 数据管理
- ✅ 定期备份 `data/` 目录
- ✅ 监控磁盘空间使用情况
- ✅ 定期清理过期的CSV文件和告警记录

---

## 故障排查

### 配置文件损坏
1. 程序会自动尝试修复或使用默认值
2. 如果无法修复，删除损坏的配置文件，程序会重新生成
3. 查看日志文件获取详细错误信息

### 配置不生效
1. 确认配置文件格式正确（有效的JSON）
2. 检查文件权限是否正确
3. 重启程序使配置生效
4. 查看日志中的配置加载信息

### 找不到配置文件
1. 确认程序具有读写权限
2. 检查 `configs/` 目录是否存在
3. 尝试删除 `configs/` 目录，让程序重新生成
4. 查看日志中的路径信息

---

**提示**: 对配置文件的任何修改建议先备份原文件，避免配置丢失。
