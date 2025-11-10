# 配置文件拆分迁移总结

## 拆分方案

### 原 config.json 内容分配：
- **MQTT配置** → `mqtt_config.json` (MqttSettings)
- **窗口布局** → `layout_settings.json` (LayoutSettings)
- **图表配置** → `chart_config.json` (ChartConfig)
- **公司信息** → `company_info.json` (CompanyInfo)

## 需要更新的文件

### ✅ 已完成的模型和服务：
1. ✅ Models/ChartConfig.cs - 新建
2. ✅ Models/CompanyInfo.cs - 新建
3. ✅ Models/LayoutSettings.cs - 添加Window*属性
4. ✅ Models/MqttSettings.cs - 移除Window*, Chart*, Company*属性
5. ✅ Services/ChartConfigService.cs - 新建
6. ✅ Services/CompanyInfoService.cs - 新建
7. ✅ Services/LayoutSettingsService.cs - 添加从config.json迁移逻辑
8. ✅ App.xaml.cs - 注册新服务和配置，添加迁移逻辑

### ⏳ 需要更新的 ViewModels：
- [ ] MainViewModel.cs - RestoreWindowState/SaveWindowState使用LayoutSettings
- [ ] ChartViewModel.cs - MaxChartDataPoints使用ChartConfig
- [ ] ChartSettingsViewModel.cs - 使用ChartConfig而非MqttSettings
- [ ] ContactUsWindow.xaml.cs - 使用CompanyInfo

### ⏳ 需要更新的依赖注入：
- [ ] App.xaml.cs - 更新ViewModel构造函数传参
