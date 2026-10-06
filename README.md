# Asbury Pines 中文漢化包

[繁體中文](README.md) | [简体中文](README.zh-CN.md)

Windows Steam 版 Asbury Pines 的非官方中文補丁，提供 **繁體中文（TC）**及 **簡體中文（SC）**，包含插件、詞庫及所需的 BepInEx x86 載入器。使用系統已安裝的中文字型，正常遊玩不需 Python 或開發工具。

目前插件版本 **0.1.14**，安裝包版本 **0.1.14-pack.2**。已驗證遊戲版本為 **Steam build 24231082／[EA] 3.01.001**。

## 下載

| 版本 | EXE 安裝器 | ZIP 安裝包 |
| --- | --- | --- |
| 繁體中文（TC） | [下載 EXE](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/Asbury_Pines_TC_0.1.14-pack.2_Setup.exe) | [下載 ZIP](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/Asbury_Pines_TC_0.1.14-pack.2_Portable.zip) |
| 簡體中文（SC） | [下載 EXE](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/Asbury_Pines_SC_0.1.14-pack.2_Setup.exe) | [下載 ZIP](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/Asbury_Pines_SC_0.1.14-pack.2_Portable.zip) |

[完整 Release 說明](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/tag/v0.1.14-pack.2) · [SHA-256 校驗碼](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/releases/download/v0.1.14-pack.2/SHA256SUMS.txt)

兩版共用同一插件位置，**每次使用其中一版**。切換繁中／簡中時，關閉遊戲後執行另一版的安裝器即可；安裝器會檢查舊檔並建立備份。F8 切換目前安裝的中文版本與英文。

## 安裝

### EXE：雙擊安裝

1. 下載所需語言的 `Setup.exe`，先關閉遊戲。
2. 雙擊安裝器，選擇含有 `AsburyPines.exe` 的遊戲資料夾。
3. 選擇「安裝／更新繁體中文補丁」或「安裝／更新簡體中文補丁」，按「執行」。
4. 完成後由 Steam 正常啟動遊戲。

安裝器會嘗試填入預設 Steam 路徑；若遊戲裝在其他遊戲庫，請自行選擇。安裝器使用 Windows 內建的 .NET Framework 與 Windows PowerShell，離線可用。EXE 未簽章，可用上方校驗碼核對下載檔。

### ZIP：完整解壓後安裝

下載所需語言的 `Portable.zip`，完整解壓到可寫入的資料夾，再雙擊 `Install.cmd`，依上面的步驟選擇遊戲目錄並安裝。

請使用完整安裝包。不要只複製單一 DLL，或直接強制覆蓋 `payload` 到已有插件的遊戲目錄。安裝器會先核對版本和檔案，再寫入安裝清單與備份。

需要手動操作時，在解壓資料夾執行以下指令，並改成實際遊戲路徑。兩版沿用相同的管理腳本名稱：

```powershell
.\Install-TraditionalChinese.ps1 -GameRoot '你的遊戲資料夾' -VerifyOnly
.\Install-TraditionalChinese.ps1 -GameRoot '你的遊戲資料夾'
```

## 使用、切換與卸載

- **F8**：在目前安裝的中文版本和英文之間切換，僅影響本次遊戲。
- **F9**：重新載入詞庫。
- **永久啟用／停用**：關閉遊戲，重新開啟 EXE 或 `Install.cmd`，選擇對應操作。需先啟動遊戲一次產生設定。
- **卸載**：關閉遊戲，重新開啟 EXE 選擇卸載，或雙擊 ZIP 內的 `Uninstall.cmd`。若還有其他 BepInEx 插件，選擇「只卸載繁中插件」或「只卸載簡中插件」，保留共用載入器。

請保留 EXE 或解壓資料夾供日後管理。更新只覆蓋安裝清單擁有且未經自行修改的檔案，備份留在遊戲目錄的 `zhhant-install-backup-*`。卸載只刪除清單中雜湊相符的檔案；自行修改的檔案、設定及備份會保留。

## 相容性

- Windows Steam build **24231082**，遊戲顯示 **[EA] 3.01.001**。
- Unity **6000.2.8f1**、Mono、**x86**；64 位元 Windows 仍使用這個 x86 載入器。
- 預設使用系統 Microsoft JhengHei（微軟正黑體），可設定為其他已安裝的 CJK 字型。安裝包不附帶字型。

| 檔案 | 已驗證 SHA-256 |
| --- | --- |
| `AsburyPines_Data/Managed/Assembly-CSharp.dll` | `D4F7976FDE49B59CD8B441CBD156537060EDC55E9F7676E693080B9B4CC1E15F` |
| `MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll` | `473D59FE1F1A2CBBF37340517CA6F9C6A48330D44DC609E5C3209ADF63792FCF` |

遊戲更新後若版本不符，安裝器會拒絕安裝，插件亦會停止掛鉤並保留英文。請等待經重新驗證的補丁，不要略過版本檢查。補丁不覆寫遊戲本體、資源包或存檔。

## 翻譯內容與驗證範圍

涵蓋主線故事、社群事件、自然筆記、介面及動態文字，並包含中文排版、插圖留白和退出相容性修正。詞庫有 **4,237 條記錄**及 **47 條動態規則**；這些數字是詞庫統計，不代表已窮舉所有遊戲進度。

簡中版由繁中詞庫以 OpenCC `tw2s` 轉換，同時轉換插件動態文字及安裝介面。英文匹配鍵、正則表達式、富文字標籤、佔位符和換行結構保持一致。簡中版沿用即時排版量測與執行期快取，未載入繁中預先量測的排版快取。

已通過編譯詞庫輸出、119 個動態回歸案例、EXE 解壓與校驗、中文路徑、安裝／更新／卸載、修改檔案保護及繁中→簡中→繁中互換測試。**簡中版尚未重新完成繁中版全部 421 個故事版型的遊戲內視覺檢查。**

- [版本狀態](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/docs/STATUS.md)
- [繁中遊戲內驗證紀錄](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/docs/RELEASE-VERIFICATION.md)
- [打包驗證](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/docs/PACKAGING-VERIFICATION.md)
- [簡中專用說明](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/blob/main/simplified/README.md)

## 問題回報

沒有中文時，先查看遊戲目錄的 `BepInEx/LogOutput.log`，確認插件載入、版本相容及系統字型可用。遊戲更新、特殊字型或其他插件組合可能需要另外驗證。

回報翻譯、缺字或排版問題時，請附上遊戲版本、故事／介面名稱、解析度、是否開啟易讀模式及截圖。可使用 repo 的 [Issues](https://github.com/bill-iu/Asbury_Pines_Chinese_pack/issues) 回報。

## 來源與重建

repo 根目錄包含繁中資料，`simplified/` 包含簡中資料。各版的 `source/` 保留插件原始碼及載入器相容性修補來源，`installer/` 是 EXE 外殼原始碼，`licenses/` 保留第三方授權及 Doorstop 對應來源。繁中另保留已驗證的預先量測的排版快取。

在 repo 根目錄重建繁中安裝包：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-Pack.ps1
```

重建簡中安裝包：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\simplified\tools\Build-Pack.ps1
```

輸出分別位於 `artifacts/` 與 `simplified/artifacts/`。已有同名版本檔案時，建置會停止，保留已發佈檔。

若要從繁中重新產生簡中詞庫及插件，建置端需要 Python／OpenCC `tw2s`。先保留既有 `simplified/`，執行 `tools/Generate-Simplified.py`，再執行 `tools/Build-Simplified-Plugin.ps1 -GameRoot '遊戲資料夾'`。已有 `simplified/` 時產生器會停止。已發佈安裝包不需 Python 或 OpenCC。

## 授權

本專案有權授權的原創程式碼及原創程式碼貢獻採用 [MIT License](LICENSE)，包括繁中／簡中插件的原創實作、安裝器、建置與轉換工具及驗證腳本。

MIT 授權不涵蓋遊戲原文、劇情、素材、翻譯詞庫、插件內嵌的遊戲翻譯文字及翻譯／排版快取資料。翻譯內容的使用與散佈仍須符合原作權利、適用許可及法律；本專案的 MIT 授權不表示已取得遊戲開發者的授權。

第三方元件保留各自授權，UnityDoorstop 仍依其 LGPL 2.1 授權散佈。詳見 [授權範圍](LICENSE-SCOPE.md) 與 [第三方聲明](licenses/THIRD-PARTY-NOTICES.md)。
