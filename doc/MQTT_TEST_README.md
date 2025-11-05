# MQTT 测试数据发送器使用说明

## 快速开始

### 1. 安装Python依赖

```bash
pip install -r requirements.txt
```

或者直接安装：
```bash
pip install paho-mqtt
```

### 2. 运行脚本

```bash
python mqtt_test_sender.py
```

### 3. 选择发送模式

脚本提供三种发送模式：

- **模式1**: 发送单条数据
- **模式2**: 持续发送（每2秒一次，按 Ctrl+C 停止）
- **模式3**: 发送指定次数

## 配置说明

脚本中的MQTT配置默认从您的 `config.json` 读取：

```python
MQTT_BROKER = "119.45.181.86"
MQTT_PORT = 1883
MQTT_USERNAME = "admin"
MQTT_PASSWORD = "your_strong_password"
MQTT_TOPIC = "test"
```

如需修改，请编辑脚本顶部的配置项。

## 数据格式

脚本会自动生成以下格式的JSON数据：

```json
{
  "deviceId": "ENV-MON-002",
  "timestamp": 1730489502000,
  "payload": [
    {
      "name": "Humidity",
      "value": 62.5,
      "unit": "%"
    },
    {
      "name": "CO2_Level",
      "value": 450,
      "unit": "ppm"
    },
    {
      "name": "Light_Intensity",
      "value": 850
    }
  ]
}
```

### 数据特点

- ✅ **自动时间戳**: 每次发送都使用当前时间的Unix毫秒时间戳
- ✅ **随机波动**: 数值会在合理范围内随机波动，模拟真实传感器数据
  - Humidity: 55-70%
  - CO2_Level: 350-550 ppm
  - Light_Intensity: 600-1100

## 测试多设备

如需测试多设备，可以修改脚本中的 `deviceId`：

```python
# 发送设备1的数据
data1 = generate_device_data("ENV-MON-001")
send_mqtt_data(client, MQTT_TOPIC, data1)

# 发送设备2的数据
data2 = generate_device_data("ENV-MON-002")
send_mqtt_data(client, MQTT_TOPIC, data2)

# 发送设备3的数据
data3 = generate_device_data("TEMP-SENSOR-001")
send_mqtt_data(client, MQTT_TOPIC, data3)
```

## 图表颜色方案

程序已更新，现在图表使用智能颜色分配：

### 颜色逻辑

✅ **不同设备使用不同色号**
- ENV-MON-001: 红色系
- ENV-MON-002: 黄色系
- TEMP-SENSOR-001: 绿色系
- 等等...（10种基础色调循环使用）

✅ **同设备不同指标使用相似色号**
- 同一设备的所有指标都基于该设备的基础颜色
- 通过调整饱和度和亮度来区分不同指标
- 例如 ENV-MON-002：
  - Humidity: 黄色（基础色）
  - CO2_Level: 浅黄色
  - Light_Intensity: 深黄色

### 图表功能

- ✅ 实时显示最近 1000 个数据点
- ✅ 时间轴自动调整范围
- ✅ 支持鼠标缩放和平移
- ✅ 显示图例，标识每条线的设备和指标
- ✅ 网格线辅助读数

## 故障排查

### 问题1: 数据发送了但图表不显示

**可能原因**:
- MQTT客户端未连接
- 未订阅正确的Topic（需要订阅 "test"）

**解决方法**:
1. 确保MQTT状态栏显示"已连接"
2. 在右侧 Topic订阅管理中，确保已订阅 "test"
3. 查看日志标签页，确认收到消息

### 问题2: 图表显示但没有数据点

**可能原因**:
- 时间戳格式不对
- 数据格式不符合预期

**解决方法**:
1. 确保使用Unix毫秒时间戳（13位数字）
2. 确保JSON格式正确
3. 查看日志中是否有解析错误

### 问题3: 颜色不够明显

可以修改 `ChartViewModel.cs` 中的颜色参数：

```csharp
// 调整基础色调列表
private readonly List<double> _baseHues = new() { 0, 60, 120, 180, 240, 300 };

// 调整饱和度和亮度
var saturation = 0.9;  // 增加饱和度（0-1）
var value = 0.85;      // 调整亮度（0-1）
```

## 示例：测试不同设备

创建一个新的Python脚本 `test_multiple_devices.py`：

```python
import time
from mqtt_test_sender import *

def main():
    client = mqtt.Client(client_id=f"multi_device_test_{int(time.time())}")
    client.username_pw_set(MQTT_USERNAME, MQTT_PASSWORD)
    client.connect(MQTT_BROKER, MQTT_PORT, 60)
    client.loop_start()
    time.sleep(2)

    devices = ["ENV-MON-001", "ENV-MON-002", "TEMP-SENSOR-001", "HUMIDITY-PROBE-01"]

    for i in range(10):  # 发送10轮
        for device_id in devices:
            data = generate_device_data(device_id)
            send_mqtt_data(client, MQTT_TOPIC, data)
            time.sleep(0.5)
        time.sleep(1)

    client.loop_stop()
    client.disconnect()

if __name__ == "__main__":
    main()
```

这将测试4个设备，每个设备3个指标，共12条折线！
