#!/usr/bin/env python3
"""
MQTT测试数据发送脚本
用于向MQTT服务器发送模拟的设备数据
"""

import json
import time
import random
from datetime import datetime
import paho.mqtt.client as mqtt

# MQTT配置（从config.json读取的配置）
MQTT_BROKER = "119.45.181.86"
MQTT_PORT = 1883
MQTT_USERNAME = "admin"
MQTT_PASSWORD = "your_strong_password"
MQTT_TOPIC = "test"  # 发送数据的topic

# 设备数据模板
DEVICE_DATA = {
    "deviceId": "ENV-MON-002",
    "timestamp": 0,  # 将自动生成
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

# 回调函数
def on_connect(client, userdata, flags, rc):
    if rc == 0:
        print("✅ 成功连接到MQTT服务器")
    else:
        print(f"❌ 连接失败，返回码: {rc}")

def on_publish(client, userdata, mid):
    print(f"📤 消息已发布 (ID: {mid})")

def generate_device_data(device_id="ENV-MON-002"):
    """
    生成设备数据，包含当前时间戳和随机波动的数值
    """
    # 获取当前时间戳（毫秒）
    timestamp = int(time.time() * 1000)

    # 生成带随机波动的数据
    data = {
        "deviceId": device_id,
        "timestamp": timestamp,
        "payload": [
            {
                "name": "Humidity",
                "value": round(60 + random.uniform(-5, 10), 1),  # 55-70之间
                "unit": "%"
            },
            {
                "name": "CO2_Level",
                "value": round(400 + random.uniform(-50, 150)),  # 350-550之间
                "unit": "ppm"
            },
            {
                "name": "Light_Intensity",
                "value": round(800 + random.uniform(-200, 300))  # 600-1100之间
            }
        ]
    }
    return data

def send_mqtt_data(client, topic, data):
    """
    发送数据到MQTT
    """
    payload = json.dumps(data, ensure_ascii=False)
    result = client.publish(topic, payload, qos=1)

    # 打印发送的数据
    timestamp_str = datetime.fromtimestamp(data['timestamp'] / 1000).strftime('%Y-%m-%d %H:%M:%S')
    print(f"\n📊 [{timestamp_str}] 设备: {data['deviceId']}")
    for metric in data['payload']:
        unit = metric.get('unit', '')
        print(f"   - {metric['name']}: {metric['value']} {unit}")

    return result

def main():
    print("=" * 60)
    print("MQTT 测试数据发送器")
    print("=" * 60)
    print(f"MQTT服务器: {MQTT_BROKER}:{MQTT_PORT}")
    print(f"发送Topic: {MQTT_TOPIC}")
    print(f"用户名: {MQTT_USERNAME}")
    print("=" * 60)

    # 创建MQTT客户端
    client = mqtt.Client(client_id=f"mqtt_test_sender_{int(time.time())}")
    client.username_pw_set(MQTT_USERNAME, MQTT_PASSWORD)
    client.on_connect = on_connect
    client.on_publish = on_publish

    try:
        # 连接到MQTT服务器
        print(f"\n🔄 正在连接到 {MQTT_BROKER}:{MQTT_PORT}...")
        client.connect(MQTT_BROKER, MQTT_PORT, 60)
        client.loop_start()
        time.sleep(2)  # 等待连接完成

        # 询问用户发送模式
        print("\n请选择发送模式:")
        print("1. 发送单条数据")
        print("2. 持续发送（每2秒一次）")
        print("3. 发送指定次数")

        mode = input("\n请输入选项 (1/2/3): ").strip()

        if mode == "1":
            # 发送单条数据
            data = generate_device_data()
            send_mqtt_data(client, MQTT_TOPIC, data)

        elif mode == "2":
            # 持续发送
            print("\n⏰ 开始持续发送数据（按 Ctrl+C 停止）...")
            try:
                while True:
                    data = generate_device_data()
                    send_mqtt_data(client, MQTT_TOPIC, data)
                    time.sleep(2)  # 每2秒发送一次
            except KeyboardInterrupt:
                print("\n\n⏹️ 停止发送")

        elif mode == "3":
            # 发送指定次数
            count = int(input("\n请输入发送次数: "))
            print(f"\n⏰ 将发送 {count} 条数据...")
            for i in range(count):
                data = generate_device_data()
                send_mqtt_data(client, MQTT_TOPIC, data)
                if i < count - 1:  # 最后一条不延迟
                    time.sleep(2)
        else:
            print("❌ 无效选项")

        time.sleep(1)  # 确保最后一条消息发送完成

    except KeyboardInterrupt:
        print("\n\n⏹️ 程序中断")
    except Exception as e:
        print(f"\n❌ 错误: {e}")
    finally:
        client.loop_stop()
        client.disconnect()
        print("\n✅ 已断开连接")
        print("=" * 60)

if __name__ == "__main__":
    main()
