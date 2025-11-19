using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MqttMonitor.Services;

/// <summary>
/// 语言切换服务
/// </summary>
public class LanguageService : INotifyPropertyChanged
{
    private const string LanguageConfigFile = "language.config";
    private string _currentLanguage = "zh-CN";

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 当前语言
    /// </summary>
    public string CurrentLanguage
    {
        get => _currentLanguage;
        private set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 初始化语言服务
    /// </summary>
    public void Initialize()
    {
        // 从配置文件加载语言设置
        LoadLanguageConfig();

        // 应用语言
        SwitchLanguage(CurrentLanguage);
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    /// <param name="language">语言代码 (zh-CN 或 en-US)</param>
    public void SwitchLanguage(string language)
    {
        if (language != "zh-CN" && language != "en-US")
        {
            language = "zh-CN"; // 默认中文
        }

        CurrentLanguage = language;

        // 构建语言资源文件路径
        var languageDict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Resources/Languages/{language}.xaml", UriKind.Absolute)
        };

        // 移除旧的语言资源
        var oldDict = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Resources/Languages/"));

        if (oldDict != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(oldDict);
        }

        // 添加新的语言资源
        Application.Current.Resources.MergedDictionaries.Add(languageDict);

        // 保存语言设置
        SaveLanguageConfig();

        // 设置 CultureInfo（影响日期、数字格式等）
        var culture = new CultureInfo(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>
    /// 加载语言配置
    /// </summary>
    private void LoadLanguageConfig()
    {
        try
        {
            if (File.Exists(LanguageConfigFile))
            {
                var language = File.ReadAllText(LanguageConfigFile).Trim();
                if (!string.IsNullOrWhiteSpace(language))
                {
                    CurrentLanguage = language;
                }
            }
        }
        catch
        {
            // 加载失败使用默认语言
            CurrentLanguage = "zh-CN";
        }
    }

    /// <summary>
    /// 保存语言配置
    /// </summary>
    private void SaveLanguageConfig()
    {
        try
        {
            File.WriteAllText(LanguageConfigFile, CurrentLanguage);
        }
        catch
        {
            // 保存失败忽略
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
