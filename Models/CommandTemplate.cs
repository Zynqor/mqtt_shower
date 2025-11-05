using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MqttMonitor.Models;

/// <summary>
/// 命令模板参数
/// </summary>
public class CommandParameter : INotifyPropertyChanged
{
    private string _value = string.Empty;

    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型 (string, number, boolean)
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// 参数描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 参数值（绑定用）
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 命令模板
/// </summary>
public class CommandTemplate
{
    /// <summary>
    /// 命令名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 命令描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 命令参数列表
    /// </summary>
    public List<CommandParameter> Parameters { get; set; } = new List<CommandParameter>();

    /// <summary>
    /// 显示文本（用于 ComboBox 显示）
    /// </summary>
    public string DisplayText => $"{Name} - {Description}";
}
