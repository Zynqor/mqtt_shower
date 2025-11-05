### 物联网设备MQTT通信协议精简版 (v1.0)

本文档定义了物联网设备与云端之间基于MQTT协议的数据交换规范，包含**上行时序数据**和**下行控制命令**两部分。

---

### 1. 上行时序数据协议

用于设备向云端上报采集的各类时序数据。

`.../{deviceId}/datas`

#### **1.1. 核心结构**

上行消息采用JSON格式，包含一个顶层对象，其中含有**元数据**和一个**数据负载 (payload)** 数组。数据负载中每个元素代表一个独立的测点。

```json
{
  "deviceId": "设备的唯一标识",
  "timestamp": 1730489461521,
  "payload": [
    {
      "name": "测点名称",
      "value": 22.8,
      "unit": "单位"
    }
  ]
}
```

#### **1.2. 关键字段定义**

| 字段层级     | 字段名         | 数据类型   | 描述与约束                            |
| -------- | ----------- | ------ | -------------------------------- |
| **元数据**  | `deviceId`  | String | **（必须）** 设备的全局唯一标识符，例如MAC地址或序列号。 |
|          | `timestamp` | Number | **（必须）** 数据采集时的UTC毫秒级Unix时间戳。    |
| **数据负载** | `payload`   | Array  | **（必须）** 包含一个或多个“测点对象”的数组。       |
| 测点对象     | `name`      | String | **（必须）** 测点名称，例如 "温度"、"CPU占用率"。  |
|          | `value`     | Number | **（必须）** 测量到的具体数值。               |
|          | `unit`      | String | （可选）数值的单位，例如 "C"、"%"、"V"。        |



### 2. 下行控制命令协议

用于云端向指定设备下发控制指令，并接收执行回执，采用请求/响应模式。

#### **2.1. 交互流程**

1.  **命令下发**: 云端向特定主题 `.../{deviceId}/command/request` 发布命令请求。
2.  **命令响应**: 设备执行后，向 `.../{deviceId}/command/response` 主题发布响应结果。

#### **2.2. 命令请求 (云端 -> 设备)**

| 字段名 | 数据类型 | 描述与约束 |
|---|---|---|
| `commandId` | String | **（必须）** 命令的唯一标识符，建议使用UUID，用于关联响应。 |
| `commandName` | String | **（必须）** 命令的功能名称，例如 `rebootDevice`、`setUploadInterval`。 |
| `timestamp` | Number | **（必须）** 命令下发时的UTC毫秒级Unix时间戳。 |
| `params` | Object | （可选）命令所需的参数，以键值对形式提供。 |

**请求示例**:
```json
{
  "commandId": "cmd-set-interval-e2d8a4f1",
  "commandName": "setUploadInterval",
  "timestamp": 1730491500000,
  "params": {
    "interval": 600
  }
}
```

#### **2.3. 命令响应 (设备 -> 云端)**

| 字段名 | 数据类型 | 描述与约束 |
|---|---|---|
| `commandId` | String | **（必须）** 必须与收到的命令请求中的 `commandId` 完全一致。 |
| `status` | String | **（必须）** 执行状态，必须是 `"success"` 或 `"error"` 之一。 |
| `timestamp` | Number | **（必须）** 生成此响应时的UTC毫秒级Unix时间戳。 |
| `errorMessage`| String | （可选）当 `status` 为 `"error"` 时，提供详细的错误信息。 |
| `result` | Object | （可选）当命令为查询类操作时，在此字段中返回结果。 |

**响应示例 (成功)**:
```json
{
  "commandId": "cmd-set-interval-e2d8a4f1",
  "status": "success",
  "timestamp": 1730491501800
}
```

**响应示例 (失败)**:
```json
{
  "commandId": "cmd-set-interval-e2d8a4f1",
  "status": "error",
  "timestamp": 1730491501800,
  "errorMessage": "Interval value out of range"
}
```