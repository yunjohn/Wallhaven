# Wallhaven Desktop

Windows 10/11 x64 壁纸应用，以 Wallhaven 为图片源。C# / WPF / .NET 10，业务代码没有第三方 NuGet 依赖。

参考本机拾光壁纸的界面研究，实现整窗大图、双层 0.55 秒淡入、左下 14-DIP 圆角信息卡、462-DIP 设置侧栏，并增加双列浏览列表。侧栏和信息卡使用半透明着色，没有宣称与 UWP 原生 Acrylic 或所有运行时交互像素一致。设置侧栏采用带图标的折叠卡片；在 924 DIP 起并排显示，较窄窗口使用覆盖模式。透明 32-DIP 标题栏保留拖动与窗口按钮，右键菜单提供主要操作。

## 使用

发布包运行 `dist/Wallhaven.exe`，详见[使用说明](USER-GUIDE.md)。首次联网拉取真实图片；支持关键词搜索、筛选、分页、随机 seed、原图缓存、下载、收藏、桌面设置、定时随机更换、系统托盘、可选开机启动、快捷键、上次壁纸恢复和单实例唤醒。

设置、收藏和壁纸数据默认保存在程序所在目录，旧版 LocalAppData 数据首次自动复制，详见使用说明。默认使用公开 SFW 接口，无需账户。可填写 API Key 访问账户偏好、站内收藏（含私有收藏）及账户允许的内容；密钥由 Windows DPAPI 加密保存，请求仅使用 X-API-Key 头。站内收藏只读，本地收藏不上传。自动切换要求进程运行；设置壁纸使用 Windows `SystemParametersInfoW`。不包含锁屏设置、Store 上架、双向账户同步或后台系统服务。

## 开发与构建

安装 .NET 10 SDK（Windows），运行：

```powershell
dotnet run --project src/Wallhaven.App
dotnet run --project tests/Wallhaven.Tests
dotnet run --project tests/Wallhaven.Tests -- --live
./build.ps1 -LiveTests
```

`build.ps1` 运行检查后生成自包含的单文件 EXE 和 ZIP。无需依赖研究工具；`tools/UiResourceExtractor` 仅用于之前的资源研究，不属于解决方案或发布包。

## 性能

1.1.0 使用有容量上限的解码缓存、文件校验复用、相邻本地图片预解码、双列列表虚拟化和视口缩略图加载。毛玻璃使用静态取样，设置侧栏带展开收起过渡。

本机对同一张 3,728,435 字节的 PNG 做 7 次“校验＋预览解码”比较，后 6 次的中位数由旧版本约 36.04 ms 降到 0.046 ms；这是图片准备路径的热缓存测量，不包括网络、布局和最终画面呈现，不能视为整窗帧率。WPF 诊断中的 1000 条列表在当时视口只生成 12 个控件，验证了滚动及跨视口定位。详情见 [验证记录](TESTING.md)。

`tools/PerformanceProbe` 可对保存的旧版 DLL 和当前 DLL 做本地图片对比；测试图片、输出路径由命令行提供。解码缓存的 96 MiB 限额只统计缓存条目，不代表应用全部内存。

## 结构

- `src/Wallhaven.Core`：API、查询参数、下载与验证、原图缓存、本地存储、可测试调度器。
- `src/Wallhaven.App`：WPF 界面、桌面适配、托盘、窗口与键盘交互。
- `tests/Wallhaven.Tests`：无测试框架依赖的检查程序；`--live` 额外访问真实 API 并下载图片。
- `artifacts/qa`：应用自身 WPF 渲染截图与诊断结果。
- [UI 研究报告](analysis/shiguang/REPORT.md)、[提取控件索引](analysis/shiguang/INDEX.md)。

## 界面诊断

```powershell
./src/Wallhaven.App/bin/Release/net10.0-windows/Wallhaven.exe --diagnostics D:/dev/wallhaven/artifacts/qa
```

诊断使用独立的测试数据目录，访问真实图源，检查收藏持久化、分页、切换、窄窗口布局，渲染 PNG 和 `results.json`，然后退出。诊断模式只测试桌面图片转换，跳过系统壁纸 API，不改变用户桌面。此开关不是普通用户运行所需的选项。

## 图源文档

[Wallhaven API](https://wallhaven.cc/help/api)；[微软 WPF 文档](https://learn.microsoft.com/dotnet/desktop/wpf/)。图片版权归各自权利人所有，来源页通过图片编号构造，第三方原应用的品牌图标与字体没有打包进本应用。
