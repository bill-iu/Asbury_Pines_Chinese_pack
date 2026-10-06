# Asbury Pines 简体中文汉化包

插件版本 **0.1.14**，安装包版本 **0.1.14-pack.2**。适用 Windows Steam build 24231082／[EA] 3.01.001（Unity 6000.2.8f1、Mono、x86）。

## 安装

1. 在 [Releases](https://github.com/bill-iu/Asbury_Pines_TC_pack/releases/latest) 下载 `Asbury_Pines_SC_0.1.14-pack.2_Setup.exe`。
2. 关闭游戏，双击安装器，选择包含 `AsburyPines.exe` 的游戏文件夹。
3. 选择“安装／更新简体中文补丁”并执行，然后从 Steam 启动游戏。

也可以完整解压 `Asbury_Pines_SC_0.1.14-pack.2_Portable.zip`，双击 `Install.cmd`。安装包离线可用，不需要 Python 或 OpenCC；使用 Windows 内置的 .NET Framework 与 Windows PowerShell。EXE 未签名，下载文件的校验码见 Release 中的 `SHA256SUMS.txt`。

安装器保留游戏及 Mono 版本检查、文件所有权检查、更新备份和失败回滚，不改动游戏本体或存档。备份在游戏目录的 `zhhant-install-backup-*`。修改过的文件不会强制覆盖，不兼容的游戏版本会拒绝安装。

## 切换和卸载

- F8：在简体中文和原始英文之间切换。
- F9：重新加载词库。
- 永久启用／停用：重新打开安装器选择相应操作；需先启动游戏一次生成配置。
- 卸载：关闭游戏，重新打开 EXE 选择卸载，或双击 ZIP 中的 `Uninstall.cmd`。有其他 BepInEx 插件时，选择“只卸载简中插件”。

请保留安装器或解压文件夹。卸载只移除清单中未修改的文件，保留本地修改、配置及备份。

繁中和简中共用一个插件位置，不能同时加载。安装另一版即可替换现有的、未修改的中文补丁；不会改变当前存档。此共用位置仍沿用原来的 `AsburyPines.ZhHant` 路径及配置名称，以支持安全更新。

## 简体转换与验证范围

以繁中版为来源，通过 OpenCC `tw2s` 转换 **4,237 条词库记录**及 **47 条动态规则**的显示文字，并转换插件中的动态提示、安装器界面和中文说明。保留英文匹配键、正则表达式、富文本标记、数值占位符及换行结构；不是重新翻译全部剧情。

简中版沿用排版修正及实时量测，未加载繁中预先量测的排版快取。默认仍使用系统 Microsoft JhengHei，以延续原有的排版行为；可通过配置选用已安装的 CJK 字体。安装包不附带字体。

已验证编译词库、动态文字、EXE／ZIP 解压及校验、中文路径、安装／更新、繁简互换、修改文件保护和卸载。简中版未重新完成繁中版 421 个故事版型的游戏内视觉检查；遇到漏译、缺字或排版问题，请提供故事／界面名称、分辨率和截图。游戏更新后请使用重新验证的补丁。

## 来源和重建

`source/` 包含简中插件源代码和载入器相容性修补来源；`licenses/` 保留第三方授权和 Doorstop 对应来源。游戏本体、素材、存档和字体不在包内。

在本文件夹执行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-Pack.ps1
pwsh.exe -NoProfile -File .\tools\Test-Catalog.ps1 -GameRoot '兼容游戏文件夹'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-Pack.ps1 -GameRoot '兼容游戏文件夹'
```

输出在 `artifacts/`。已存在相同版本的发布文件时，重建会停止以保护现有文件。测试使用独立副本，不改动传入的游戏安装。
