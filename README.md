# HttpPing

HTTP/HTTPS 响应监控工具，参考 [vmPing](https://github.com/r-smith/vmPing) 设计。用于实时监控多个 Web 服务的可用性和响应时间。

## 功能特性

### 核心监控
- **实时检测**：定时检测 HTTP/HTTPS 服务的可用性
- **状态码显示**：显示 HTTP 响应状态码（200、301、404 等）
- **响应时间**：精确测量每次请求的响应时间
- **多主机管理**：同时监控多个主机，网格布局显示

### 状态可视化
- **状态圆点**：最近 N 次检测结果的可视化圆点（绿=正常，红=异常）
- **迷你折线图**：Sparkline 迷你折线图，实时展示响应时间趋势
- **颜色状态条**：底部颜色条直观显示当前状态

### 弹窗通知（vmPing 风格）
- **右下角弹窗**：主机断连/恢复时在屏幕右下角弹出通知窗口
- **单窗口模式**：所有状态变化追加到同一窗口的列表中
- **可配置消除时间**：1分钟、3分钟、5分钟、30分钟、1小时、3小时、永不消除
- **点击恢复**：点击弹窗可快速恢复主窗口

### 其他通知
- **气泡通知**：系统托盘气泡提示状态变化
- **声音提醒**：断连时播放警告音，恢复时播放提示音

### 状态历史
- **完整记录**：带时间戳的状态变化日志（断开连接、恢复连接、开始/停止监控）
- **过滤搜索**：按状态类型和文本关键词过滤
- **清晰标识**：`✔ 恢复连接`、`✗ 连接断开`、`● 已启动监控`、`● 已停止监控`

### 统计分析
- **可用率**：总成功率百分比
- **响应时间**：最小/最大/平均响应时间
- **检测计数**：总检测次数、成功/失败次数
- **最后失败时间**：最近一次失败的时间

### 系统托盘
- **最小化到托盘**：关闭窗口时最小化到系统托盘继续监控
- **托盘右键菜单**：快速显示窗口或退出程序
- **应用图标**：自定义 .ico 图标

### 收藏夹
- **保存/加载**：将当前主机列表保存为收藏夹，一键切换
- **管理**：删除不需要的收藏夹

### 右键菜单
- 开始/停止、查看统计、复制网址、在浏览器中打开、移除

## 系统要求

- Windows 10/11 或 Windows Server 2016+
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## 使用方法

```bash
# 编译
dotnet build -c Release

# 运行
dotnet run

# 发布（框架依赖，约 360KB，需要目标电脑安装 .NET 8 Runtime）
dotnet publish -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true -o publish

# 发布（自包含，约 147MB，无需安装 .NET Runtime）
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish_sc
```

## 操作指南

1. 点击菜单 **主机 → 添加** 输入 URL（如 `http://172.16.2.203`）
2. 点击播放按钮或 **主机 → 全部开始** 启动监控
3. 右键主机卡片：停止、查看统计、复制网址、浏览器打开
4. **选项** 菜单配置：检测间隔、超时、通知方式、消除时间

## 项目结构

```
HttpPing/
├── Controls/
│   ├── ProbeTile.xaml/.cs        # 主机卡片控件
│   └── SparklineControl.cs       # 迷你折线图控件
├── Converters/
│   └── InverseBoolToVisibilityConverter.cs
├── Models/
│   ├── ProbeConfig.cs            # 主机配置模型
│   ├── ProbeHistoryEntry.cs      # 检测历史记录
│   ├── ProbeStatistics.cs        # 统计数据模型
│   └── StatusHistoryEntry.cs     # 状态变化事件
├── Services/
│   ├── AlertService.cs           # 通知服务（弹窗、气泡、声音）
│   └── HttpProbeService.cs       # HTTP 探测服务
├── ViewModels/
│   └── ProbeViewModel.cs         # 主机视图模型（核心逻辑）
├── MainWindow.xaml/.cs           # 主窗口
├── AddHostDialog.xaml/.cs        # 添加主机对话框
├── OptionsWindow.xaml/.cs        # 选项设置窗口
├── StatisticsWindow.xaml/.cs     # 统计详情窗口
├── StatusHistoryWindow.xaml/.cs  # 状态历史窗口
├── NotificationWindow.xaml/.cs   # 弹窗通知窗口（vmPing 风格）
├── ConfigManager.cs              # 配置持久化
└── location_http_14327.ico       # 应用图标
```

## 配置文件

程序运行时在可执行文件目录生成 `HttpPing.xml`，保存上次会话的主机列表和选项设置。

## 技术栈

- C# / .NET 8 / WPF
- 零外部 NuGet 依赖
- MVVM 模式（手动实现）
- `System.Windows.Forms.NotifyIcon` 系统托盘
- 自定义 `SparklineControl`（OnRender 绘制）
- XML 序列化配置持久化

## 参考项目

- [vmPing](https://github.com/r-smith/vmPing) — Visual Multi Ping
