# Asbury Pines 繁體中文漢化包

目前插件版本 **0.1.14**，安裝包版本 **0.1.14-pack.2**。同時提供繁體及簡體中文版本。

| 版本 | EXE | ZIP |
| --- | --- | --- |
| 繁體中文 | `Asbury_Pines_TC_0.1.14-pack.2_Setup.exe` | `Asbury_Pines_TC_0.1.14-pack.2_Portable.zip` |
| 簡體中文 | `Asbury_Pines_SC_0.1.14-pack.2_Setup.exe` | `Asbury_Pines_SC_0.1.14-pack.2_Portable.zip` |

檔案皆在 [Releases](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/latest)。繁中資料位於 repo 根目錄，簡中版位於 [`simplified/`](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/simplified/README.md)。

兩版共用同一插件位置，**每次使用其中一版**。切換版本時，關閉遊戲後執行另一版的安裝器即可；安裝器會核對舊檔並備份。F8 切換目前安裝的中文版本與英文。

簡中版由繁中詞庫以 OpenCC `tw2s` 轉換，同時轉換插件動態文字與安裝介面；英文比對鍵、正則規則、富文字標籤及排版空行保持原樣。繁中預算排版快取未移入簡中版，簡中版使用原有的即時量測及執行期快取。簡中版尚未重新完成繁中版的 421 個版型遊戲內視覺檢查，詳細驗證範圍見 [簡中說明](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/simplified/README.md)。

這是 Windows Steam 版 Asbury Pines 的非官方繁體中文補丁，包含外掛、詞庫及所需的 BepInEx x86 載入器。使用系統的微軟正黑體，不散佈遊戲本體、遊戲 DLL、素材、字型或存檔。

## 最簡單：EXE 安裝器

1. 從 [Releases](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/latest) 下載所需版本的 `Setup.exe`。
2. 關閉遊戲，雙擊安裝器。
3. 選擇含有 `AsburyPines.exe` 的遊戲資料夾，選「安裝／更新繁體中文補丁」，按「執行」。安裝器會嘗試填入預設 Steam 路徑；其他 Steam 遊戲庫請自行選擇。
4. 安裝完成後，由 Steam 正常啟動遊戲。

不需要額外下載依賴、Python 或開發工具。安裝器使用 Windows 內建的 .NET Framework 與 Windows PowerShell；安裝器未簽章。Windows 若提示來源或簽章資訊，請先核對下載來源及本版 SHA-256。

## ZIP 版本

下載所需版本的 `Portable.zip`，**完整解壓縮**到任意可寫入的資料夾，再雙擊 `Install.cmd`。操作方式與 EXE 相同。不要只複製單一 DLL，也不要直接將 `payload` 強制覆蓋到已有插件的遊戲目錄；安裝器會先檢查版本和檔案，再建立安裝清單及備份。

需要手動操作時，可在解壓目錄執行：

```powershell
.\Install-TraditionalChinese.ps1 -GameRoot '你的遊戲資料夾' -VerifyOnly
.\Install-TraditionalChinese.ps1 -GameRoot '你的遊戲資料夾'
```

## 卸載及中英切換

- **F8**：本次遊戲中切換繁中／英文。
- **F9**：重新載入詞庫。
- 永久啟用／停用：關閉遊戲，重新開啟 EXE 或 `Install.cmd`，選擇對應操作。需先啟動遊戲一次產生設定。
- 卸載：關閉遊戲，重新開啟 EXE，或雙擊 ZIP 內的 `Uninstall.cmd`，選遊戲資料夾並執行。若還有其他 BepInEx 插件，請選「只卸載繁中插件」。

請保留 EXE 或解壓資料夾供日後管理。卸載只移除清單中雜湊相符的檔案；自行修改的檔案會保留並在結果中列出。原始遊戲檔、存檔、設定及更新備份均保留。

## 相容版本

已驗證 **Windows Steam build 24231082／[EA] 3.01.001**，Unity 6000.2.8f1、Mono、x86。64 位元 Windows 仍使用此 x86 載入器。

| 檔案 | 已驗證 SHA-256 |
| --- | --- |
| `AsburyPines_Data/Managed/Assembly-CSharp.dll` | `D4F7976FDE49B59CD8B441CBD156537060EDC55E9F7676E693080B9B4CC1E15F` |
| `MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll` | `473D59FE1F1A2CBBF37340517CA6F9C6A48330D44DC609E5C3209ADF63792FCF` |

遊戲更新後若版本不符，安裝器會拒絕安裝；插件亦會停止掛鉤並保留英文。請使用經重新驗證的版本，不要略過檢查。更新只覆蓋先前安裝器擁有且沒有自行修改的檔案，備份在遊戲目錄的 `zhhant-install-backup-*`。

## 補丁內容與驗證

包含主線故事、社群事件、自然筆記、介面及動態文字翻譯，以及中文排版、插圖留白和退出相容性修正。詞庫統計及遊戲內驗證範圍見 [版本狀態](docs/STATUS.md) 與 [驗證紀錄](docs/RELEASE-VERIFICATION.md)。遊戲內驗證紀錄來自原有補丁；本次封裝的檢查見 [打包驗證](docs/PACKAGING-VERIFICATION.md)。

遊戲更新、特殊字型或其他插件組合可能需要另外驗證。沒有中文時，查看遊戲目錄的 `BepInEx/LogOutput.log`，確認版本及字型；回報排版問題時，附上故事／介面名稱、解析度及截圖。

## 來源與重建

`source/` 保留插件原始碼、排版快取、MonoMod 原檔與相容性修補腳本；`installer/` 是本次 EXE 外殼原始碼。`payload/` 僅包含可散佈的補丁及載入器。第三方授權、來源和 Doorstop 對應來源壓縮檔在 `licenses/`。

在 Windows PowerShell 執行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-Pack.ps1` 可從本 repo 重新建立 ZIP 與 EXE，輸出在 `artifacts/`。此命令重新打包已驗證插件，不重新編譯遊戲插件。

簡中版可直接執行 `simplified/tools/Build-Pack.ps1` 重建安裝包。若要從繁中重新產生簡中詞庫及插件，需要建置端 Python／OpenCC `tw2s`，執行 `tools/Generate-Simplified.py`，再執行 `tools/Build-Simplified-Plugin.ps1 -GameRoot '遊戲資料夾'`。已有 `simplified/` 時產生器會停止，請先保留舊資料。已發佈安裝包不需 Python 或 OpenCC。

Asbury Pines 及原有劇情、名稱與素材屬於各自權利人；翻譯用於另外安裝的遊戲。各第三方授權僅適用於對應元件，詳見 [第三方聲明](licenses/THIRD-PARTY-NOTICES.md)。

## 授權範圍

本專案有權授權的原創程式碼及原創程式碼貢獻採用 [MIT License](LICENSE)，包括繁中／簡中插件的原創實作、安裝器、建置與轉換工具及驗證腳本。

MIT 授權不涵蓋遊戲原文、劇情、素材、翻譯詞庫、插件內嵌的遊戲翻譯文字及翻譯／排版快取資料。翻譯內容的使用與散佈仍須符合原作權利、適用許可及法律；本專案的 MIT 授權不表示已取得遊戲開發者的授權。第三方元件保留各自授權，UnityDoorstop 仍依其 LGPL 2.1 授權散佈。完整範圍見 [LICENSE-SCOPE.md](LICENSE-SCOPE.md)。
