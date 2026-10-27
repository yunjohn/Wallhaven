# 拾光壁纸 UI 逆向分析

分析日期：2026-10-26。对象：本机 Microsoft Store 安装包 `54311Bysyk.forWindows11`，版本 `28.0.1715.0`，x64。

## 已完成

直接只读解析安装包 `resources.pri`，导出 979 个资源候选，并将其中全部 39 个 XBF 转换为可阅读的 XAML。随后用独立 XML 解析器验证 39 个文件，索引 3443 个 XML 节点（包含属性元素，不等同于 3443 个可见控件）和 831 个 Resources 文本键。

39 份布局包括：应用资源 1 份、主页面和设置页 2 份、对话框 26 份、控件库及密度资源 10 份。第三方通用控件模板包含在总数内，不能把 39 份都称为应用页面。

当前输出是编译资源的重建文本，不是开发者原始源码，也没有恢复事件处理函数或运行时添加的控件。转换无异常仅表示工具能处理输入，不能证明输出与原始 XAML 完全相同或可直接编译。

## 文件入口

- [完整页面和控件索引](INDEX.md)
- [主页面布局](xaml/Files/Pages/MainPage.xaml)
- [设置页布局](xaml/Files/Pages/SettingsView.xaml)
- [节点结构与文本关联](ui-map.json)
- [文本资源](strings.json)
- [全部资源候选及转换状态](inventory.json)

## 技术结构

清单使用 `Windows.Universal`、入口 `Timeline.App`，依赖 `Microsoft.UI.Xaml.2.8` 和 `.NET Native 2.2`；界面根类型为 `Windows.UI.Xaml`。这确认它属于 UWP/XAML 应用，使用 WinUI 2 控件。C# 是高度可信的语言判断，但本次没有对本机代码进行反编译来独立证明源语言。

设置页使用 CommunityToolkit 的 `SettingsCard`、`SettingsExpander`、`Segmented` 和 `TokenView`；图标引用 Segoe Fluent Icons。许多颜色和字号来自系统主题资源，而非应用自定义的固定色值。因此复现时应使用对应主题资源，不能只从静态 XAML 推断所有最终颜色与字号。

## 主界面还原规格（静态资源可直接确认）

| 区域 | 结构与参数 | 证据 |
| --- | --- | --- |
| 壁纸背景 | RelativePanel 铺满父容器，两层 Image，`UniformToFill`，水平与垂直居中，初始 Opacity=0 | MainPage: ImgUhdPointer、ImgUhd1、ImgUhd2 |
| 图片过渡 | 两层图片均有 `ScalarTransition Duration=0:0:0.55` | MainPage: Image.OpacityTransition |
| 设置容器 | SplitView，初始关闭，默认 Overlay，默认宽 450 | MainPage: ViewSplit |
| 自适应设置容器 | 宽度触发器 604：Overlay、宽 462；924：Inline、宽 462 | MainPage: VisualState.Setters |
| 壁纸说明卡 | 左下对齐；外边距 30，内边距 `16,12`，最大宽 640，圆角 14 | MainPage: ViewBarPointer、ViewBar |
| 说明卡材质 | `AcrylicInAppFillColorDefaultBrush` | MainPage: ViewBar.Background |
| 标题与说明 | 标题 Subtitle；正文 Body；版权、日期、图片参数 Caption；各段上间距 8 | MainPage: TextTitle、TextDetail* |
| 加载状态 | 标题下面 ProgressBar，不定进度，顶间距 8 | MainPage: ProgressLoading |
| 额外说明 | ViewStory 及细节文本初始 Collapsed，运行时显示条件未知 | MainPage: ViewStory、TextDetail* |
| 提示区域 | 左上 InfoBar 容器，外边距 `29,44,29,0` | MainPage: ViewInfo |
| 跳转输入 | TextBox 最小宽 180、最大宽 320，Flyout 左下边缘对齐 | MainPage: FlyoutGo、BoxGo |

自适应触发器的实际选择顺序和窗口模式变化需要实机验证，以上是资源中存在的触发器与赋值。不能把触发器常量等同于已经测量的运行时行为。

## 设置侧栏

- 标题区域默认 Margin=`21,32,20,30`；604 宽度触发器设为 `27,32,26,30`。
- 内容为纵向 ScrollViewer，默认 Padding=`20,0,20,30`，604 触发器改为 `26,0,26,30`。
- 标题使用 `TitleLargeTextBlockStyle`；右侧是全屏、窄屏、宽屏三个分段按钮。
- 设置卡片组主要使用 SettingsExpander，组间距为 6；普通组常见子项内边距 `58,8,16,8`，来源筛选组常见 `16,8`。
- 主题、语言、图片缓存、功能开关等结构已恢复。部分来源组初始隐藏，其标题与子选项由代码填充。
- Provider、Topic、Device 等浮层内容宽度均为 374。
- 支持/订阅图标有 0.2 秒、缩放 1→1.4、自动往返动画。

## Wallhaven 专用 UI

设置页已经包含 `ExpanderWallhaven`，Tag=`wallhaven`，初始 Visibility=`Collapsed`，组间距 `0,6,0,0`。内部按顺序包含：

1. `SettingsOrder` → `SegmentedWallhavenOrder`，单选。
2. `SettingsCateHow` → `SegmentedWallhavenCateHow`，多选。
3. `SettingsCateWhat` → `SegmentedWallhavenCateWhat`，多选。

这些分段控件在 XAML 中没有项目。项目名称、选中值、与 API 参数的对应关系不在这些布局资源里，不能声称已经恢复。可以在自己的实现中按 Wallhaven 官方 API 独立接入。

## 菜单与交互的证据范围

文本资源确认包含下载、收藏、下一张、设置桌面、设置锁屏、自动桌面、自动锁屏、切换来源、填充/适应、设置等菜单文案；另有 `Right-click to open menu.` 提示。因此右键菜单是有依据的交互方向。

但主页面静态 XAML 没有对应完整菜单结构。菜单排列、图标、启用条件与自动隐藏行为尚未验证。键盘快捷键声明存在，但多数按键的具体功能需要事件处理逻辑或运行画面佐证，不能只根据键名推测。

## 尚未恢复

- .NET Native 本机代码中的事件处理、动态菜单、图源项目与运行时布局调整。
- UI 主题资源最终解析值、窗口标题栏集成、DPI 下的实际像素大小。
- 手势、悬停、右键菜单、说明卡展开收起等运行时效果。
- 完整中文资源。当前 PRI 的 Resources 主要是英文；不能将它们描述成已恢复的中文 UI。
- 实际画面比对：此前窗口截图接口超时，本轮检查时应用没有打开的目标窗口，因此本报告没有声称完成截图验证。

下一阶段可按上面已确认的尺寸和容器关系独立实现大图浏览、左下信息卡、设置侧栏和 Wallhaven 筛选，再用运行画面对照调整。当前目录交付的是分析资料与提取工具，尚未生成可运行的壁纸软件。

## 工具与复现

解析使用开源 [PriTools](https://github.com/chausner/PriTools) 和 [XbfTools](https://github.com/chausner/XbfTools)，均为 Apache-2.0；上游源码与许可证保留在 `.tools/` 内。XbfTools 上游明确指出重建输出可能与原始内容不同。

当前目录运行：

```powershell
dotnet run --project tools/UiResourceExtractor -- 'C:\Program Files\WindowsApps\54311Bysyk.forWindows11_28.0.1715.0_x64__fp51msqsmzpvr\resources.pri' 'analysis/shiguang'
python tools/summarize_ui.py
```

所需依赖为 .NET 10、Python 3、`.tools/PriTools-master/PriFormat` 与 `.tools/XbfTools-master/XbfFormat`。Windows 更新应用后安装路径与版本可能改变。

SHA-256：

| 输入 | Hash |
| --- | --- |
| resources.pri | `3D62677E17139D7EE89D69599CBF97870DB1447A107F9714DE6676C979DC3D98` |
| PriTools 源码 ZIP | `F0BE4EDE12E76C5CE2FF511A4A0052A1D8A2584B3F3FD0122F4A8E314BA3AA72` |
| XbfTools 源码 ZIP | `78C85C4CB22389DAF9E9AB1CF2D41F3267DFB2E2180369450D62E32E7A8F9D27` |
