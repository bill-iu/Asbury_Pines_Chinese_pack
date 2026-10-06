# Asbury Pines 中文汉化包

[繁體中文](README.md) | [简体中文](README.zh-CN.md)

Windows Steam 版 Asbury Pines 的非官方中文补丁，提供 **繁体中文（TC）**及 **简体中文（SC）**，包含插件、词库及所需的 BepInEx x86 加载器。使用系统已安装的中文字体，正常游玩不需 Python 或开发工具。

目前插件版本 **0.1.14**，安装包版本 **0.1.14-pack.2**。已验证游戏版本为 **Steam build 24231082／[EA] 3.01.001**。

## 下载

| 版本 | EXE 安装器 | ZIP 安装包 |
| --- | --- | --- |
| 繁体中文（TC） | [下载 EXE](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/Asbury_Pines_TC_0.1.14-pack.2_Setup.exe) | [下载 ZIP](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/Asbury_Pines_TC_0.1.14-pack.2_Portable.zip) |
| 简体中文（SC） | [下载 EXE](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/Asbury_Pines_SC_0.1.14-pack.2_Setup.exe) | [下载 ZIP](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/Asbury_Pines_SC_0.1.14-pack.2_Portable.zip) |

[完整 Release 说明](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/tag/v0.1.14-pack.2) · [SHA-256 校验码](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/SHA256SUMS.txt)

两版共用同一插件位置，**每次使用其中一版**。切换繁中／简中时，关闭游戏后执行另一版的安装器即可；安装器会检查旧文件并建立备份。F8 切换目前安装的中文版本与英文。

## 安装

### EXE：双击安装

1. 下载所需语言的 `Setup.exe`，先关闭游戏。
2. 双击安装器，选择含有 `AsburyPines.exe` 的游戏文件夹。
3. 选择「安装／更新繁体中文补丁」或「安装／更新简体中文补丁」，按「执行」。
4. 完成后由 Steam 正常启动游戏。

安装器会尝试填入默认 Steam 路径；若游戏装在其他游戏库，请自行选择。安装器使用 Windows 内置的 .NET Framework 与 Windows PowerShell，离线可用。EXE 未签名，可用上方校验码核对下载文件。

### ZIP：完整解压后安装

下载所需语言的 `Portable.zip`，完整解压到可写入的文件夹，再双击 `Install.cmd`，依上面的步骤选择游戏目录并安装。

请使用完整安装包。不要只复制单一 DLL，或直接强制覆盖 `payload` 到已有插件的游戏目录。安装器会先核对版本和文件，再写入安装清单与备份。

需要手动操作时，在解压文件夹执行以下指令，并改成实际游戏路径。两版沿用相同的管理脚本名称：

```powershell
.\Install-TraditionalChinese.ps1 -GameRoot '你的游戏文件夹' -VerifyOnly
.\Install-TraditionalChinese.ps1 -GameRoot '你的游戏文件夹'
```

## 使用、切换与卸载

- **F8**：在目前安装的中文版本和英文之间切换，仅影响本次游戏。
- **F9**：重新载入词库。
- **永久启用／停用**：关闭游戏，重新开启 EXE 或 `Install.cmd`，选择对应操作。需先启动游戏一次产生设定。
- **卸载**：关闭游戏，重新开启 EXE 选择卸载，或双击 ZIP 内的 `Uninstall.cmd`。若还有其他 BepInEx 插件，选择「只卸载繁中插件」或「只卸载简中插件」，保留共用加载器。

请保留 EXE 或解压文件夹供日后管理。更新只覆盖安装清单拥有且未经自行修改的文件，备份留在游戏目录的 `zhhant-install-backup-*`。卸载只删除清单中杂凑相符的文件；自行修改的文件、设定及备份会保留。

## 相容性

- Windows Steam build **24231082**，游戏显示 **[EA] 3.01.001**。
- Unity **6000.2.8f1**、Mono、**x86**；64 位元 Windows 仍使用这个 x86 加载器。
- 预设使用系统 Microsoft JhengHei（微软正黑体），可设定为其他已安装的 CJK 字体。安装包不附带字体。

| 文件 | 已验证 SHA-256 |
| --- | --- |
| `AsburyPines_Data/Managed/Assembly-CSharp.dll` | `D4F7976FDE49B59CD8B441CBD156537060EDC55E9F7676E693080B9B4CC1E15F` |
| `MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll` | `473D59FE1F1A2CBBF37340517CA6F9C6A48330D44DC609E5C3209ADF63792FCF` |

游戏更新后若版本不符，安装器会拒绝安装，插件亦会停止挂钩并保留英文。请等待经重新验证的补丁，不要略过版本检查。补丁不覆写游戏本体、资源包或存档。

## 翻译内容与验证范围

涵盖主线故事、社群事件、自然笔记、介面及动态文字，并包含中文排版、插图留白和退出相容性修正。词库有 **4,237 条记录**及 **47 条动态规则**；这些数字是词库统计，不代表已穷举所有游戏进度。

简中版由繁中词库以 OpenCC `tw2s` 转换，同时转换插件动态文字及安装介面。英文匹配键、正则表达式、富文字标签、占位符和换行结构保持一致。简中版沿用即时排版测量与运行时缓存，未载入繁中预先测量的排版缓存。

已通过编译词库输出、119 个动态回归测试案例、EXE 解压与校验、中文路径、安装／更新／卸载、修改文件保护及繁中→简中→繁中互换测试。**简中版尚未重新完成繁中版全部 421 个故事版型的游戏内视觉检查。**

- [版本状态](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/docs/STATUS.md)
- [繁中游戏内验证纪录](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/docs/RELEASE-VERIFICATION.md)
- [打包验证](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/docs/PACKAGING-VERIFICATION.md)
- [简中专用说明](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/simplified/README.md)

## 问题回报

没有中文时，先查看游戏目录的 `BepInEx/LogOutput.log`，确认插件载入、版本相容及系统字体可用。游戏更新、特殊字体或其他插件组合可能需要另外验证。

回报翻译、缺字或排版问题时，请附上游戏版本、故事／介面名称、解析度、是否开启易读模式及截图。可使用 repo 的 [Issues](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/issues) 回报。

## 来源与重建

repo 根目录包含繁中资料，`simplified/` 包含简中资料。各版的 `source/` 保留插件源代码及加载器相容性修补来源，`installer/` 是 EXE 外壳源代码，`licenses/` 保留第三方授权及 Doorstop 对应来源。繁中另保留已验证的预先测量的排版缓存。

在 repo 根目录重建繁中安装包：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-Pack.ps1
```

重建简中安装包：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\simplified\tools\Build-Pack.ps1
```

输出分别位于 `artifacts/` 与 `simplified/artifacts/`。已有同名版本文件时，构建会停止，保留已发布档。

若要从繁中重新产生简中词库及插件，构建端需要 Python／OpenCC `tw2s`。先保留既有 `simplified/`，执行 `tools/Generate-Simplified.py`，再执行 `tools/Build-Simplified-Plugin.ps1 -GameRoot '游戏文件夹'`。已有 `simplified/` 时产生器会停止。已发布安装包不需 Python 或 OpenCC。

## 授权

本专案有权授权的原创程式码及原创程式码贡献采用 [MIT License](LICENSE)，包括繁中／简中插件的原创实作、安装器、构建与转换工具及验证脚本。

MIT 授权不涵盖游戏原文、剧情、素材、翻译词库、插件内嵌的游戏翻译文字及翻译／排版缓存资料。翻译内容的使用与分发仍须符合原作权利、适用许可及法律；本专案的 MIT 授权不表示已取得游戏开发者的授权。

第三方元件保留各自授权，UnityDoorstop 仍依其 LGPL 2.1 授权分发。详见 [授权范围](LICENSE-SCOPE.md) 与 [第三方声明](licenses/THIRD-PARTY-NOTICES.md)。
