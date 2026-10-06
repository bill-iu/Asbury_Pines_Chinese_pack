# Asbury Pines 繁體中文插件

這是 Windows Steam 版 Asbury Pines 的外掛式繁體中文翻譯。使用 BepInEx 5.4.23.5 x86 載入，於介面繪製與文字尺寸計算期間替換顯示內容，不覆寫遊戲 DLL、資源包或存檔。

已提供主線故事、社群事件、自然筆記、介面與動態數值文字的繁體中文詞庫。版本及驗證範圍見同目錄的 STATUS.md 與 RELEASE-VERIFICATION.md。

## 相容版本

- Windows、Steam App 2212790，已驗證 build 24231082，遊戲顯示 `[EA] 3.01.001`。
- Unity 6000.2.8f1，Mono，32 位元遊戲。即使 Windows 是64位元，也使用 x86 載入器。
- 目前僅接受 Assembly-CSharp.dll SHA-256：`D4F7976FDE49B59CD8B441CBD156537060EDC55E9F7676E693080B9B4CC1E15F`。
- 預設使用系統已安裝的 Microsoft JhengHei（微軟正黑體），不隨補丁散佈字型。亦支援設定為已安裝的 Noto Sans TC 等中文字型。

遊戲更新後若程式檔雜湊不符，插件保留英文並停止安裝執行期掛鉤。請取得經驗證的新插件版本；不要直接略過版本檢查。

## 安裝後使用

安裝完成後，直接由 Steam 啟動即可。開發診斷預設關閉，不需要開啟它來遊玩。

- **F8**：本次遊戲期間切換繁體中文／原始英文，同時還原字型。
- **F9**：重新載入詞庫，供自行調整翻譯時使用。
- 遊戲的「切換易讀字型」功能可繼續使用。
- 固定高度故事欄位若因中文字型行高增加而溢出，會在繪製時調整字級；切回英文即恢復原版顯示，不改原始文字或欄位尺寸。

永久停用：關閉遊戲，在遊戲目錄開啟 PowerShell，執行：

```powershell
.\Set-TraditionalChinese.ps1 -Mode Off
```

重新啟用使用 `-Mode On`，然後重新啟動遊戲。這個設定保存在 `BepInEx\config\local.asburypines.zhhant.cfg` 的 `Enabled`。

## 安裝與更新封裝

發佈封裝包含 `Install-TraditionalChinese.ps1`、`Set-TraditionalChinese.ps1`、`Uninstall-TraditionalChinese.ps1` 及 `payload`。解壓到遊戲目錄之外的資料夾，關閉遊戲後，在安裝包根目錄的 PowerShell 執行：

```powershell
.\Install-TraditionalChinese.ps1 -GameRoot 'D:\Steam\steamapps\common\Asbury Pines' -VerifyOnly
.\Install-TraditionalChinese.ps1 -GameRoot 'D:\Steam\steamapps\common\Asbury Pines'
```

請改為你的實際 Steam 安裝路徑。安裝器先核對版本、清單及所有檔案碰撞，才開始寫入；更新只覆蓋先前安裝清單擁有且未經自行修改的檔案。自行修改的翻譯、既有其他載入器或無法辨認的同名檔案會使安裝停止，避免直接覆蓋。更新前的檔案保留於遊戲目錄下的 `zhhant-install-backup-*`。

請使用發佈 ZIP 中的完整 `payload` 及授權檔，不要只複製單一插件 DLL。

請保留解壓資料夾，停用／啟用與卸載工具放在這裡，不會複製到遊戲目錄。從解壓資料夾操作時，一律指定遊戲路徑，例如：

```powershell
.\Set-TraditionalChinese.ps1 -GameRoot 'D:\Steam\steamapps\common\Asbury Pines' -Mode Off
.\Uninstall-TraditionalChinese.ps1 -GameRoot 'D:\Steam\steamapps\common\Asbury Pines' -VerifyOnly
.\Uninstall-TraditionalChinese.ps1 -GameRoot 'D:\Steam\steamapps\common\Asbury Pines'
```

下面的簡短指令適用於本機開發安裝，工具已另放在遊戲目錄的情況。

## 卸載

關閉遊戲，在遊戲目錄執行：

```powershell
.\Uninstall-TraditionalChinese.ps1 -VerifyOnly
.\Uninstall-TraditionalChinese.ps1
```

第一個指令只列出將移除的檔案。卸載僅刪除安裝清單中的檔案，且要求目前 SHA-256 與安裝時相同。修改過的檔案會保留並顯示警告；請閱讀結果，確認插件 DLL 與 `winhttp.dll` 是否仍存在。

若還有其他 BepInEx 插件，完整卸載會拒絕移除共用載入器。此時使用 `-PluginOnly` 只移除繁中插件。設定、記錄、翻譯備份、開發資料與原始遊戲檔案不會被遞迴刪除。

## 遇到問題

- 沒有中文：先看 `BepInEx\LogOutput.log`，確認插件載入、版本相容及系統字型可用。
- 更新後變英文：通常是版本檢查保留了原版，日誌會列出目前程式雜湊。
- 文字缺字：在插件設定中的 `Font` 指定已安裝的繁中文字型，再重啟遊戲。
- 翻譯或排版問題：記下故事／介面名稱、解析度、是否開啟易讀模式與截圖；不要傳送存檔才能重現的一般介面問題。

實際測試紀錄以 RELEASE-VERIFICATION.md 為準。Steam 外部動態狀態、內部代碼、品牌、玩家名稱、虛構月份及語言教學中的原文會依用途保留，並非一律替換所有拉丁字母。插件僅支援上述已驗證遊戲版本。

## 0.1.1 效能更新

減少隱藏文字的字型處理，快取顯示與故事排版，並在背景讀取預算結果。預算快取只在系統字型雜湊、文字及尺寸相符時使用，其他情境保留即時計算。實測比較、測試方法及尚存差異見 PERFORMANCE.zh-TW.md。

## 0.1.2 排版修正

修正自願證人陳述表及同類警察報告的標籤與正文重疊。保留 0.1.1 的快取與延後字型處理；中英切換仍會還原原版內容與字型。

## 0.1.3 表格邊界修正

警察報告的文字會在下一條固定橫線之前排完，避免末行被表格線穿過；保留正文內容與段落。

## 0.1.4 短摘要排版修正

修正「照常一團糟」等短句被表格橫線穿過的問題。只有一行空間時，短句依標籤實際寬度排在右側；長文繼續分行並遵守表格邊界。

## 0.1.5 問卷與退出修正

學校問卷的回答保留標題留白，年齡標籤依數字欄前的可用寬度顯示。退出時停止多餘的 UI 還原與重建。原生存檔寫入仍完整保留；本機實測存檔約 17 毫秒，剩餘數秒主要在 Unity 退出清理，詳見 EXIT-PERFORMANCE.zh-TW.md。

## 0.1.6 尋人啟事修正

修正最後現身日期與人物特徵、聯絡提示與聯絡人資料的重疊；完整保留日期、特徵及聯絡方式。

0.1.7 修正年輕艾爾文故事便箋正文超出紙張，以及過長人名在名牌內換行重疊的問題。

0.1.8 修正主選單標題、製作署名及版本資訊重疊，保留原有陰影與完整內容。

0.1.9 修正共用選人清單的姓名／技能等級重疊及人物資訊表頭；「新生的蘿蜜娜」統一顯示為「覺醒的蘿蜜娜」。

0.1.10 修正便箋故事正文超出紙張的問題，依圖片實際顯示底邊調整字級，完整保留正文。

0.1.11 修正薇薇安夫人許可執照的姓名、酒館名稱與正文重疊，保留所有證書資訊。


## 0.1.12 全資訊圖排版修正

已完成 421 個資訊圖版型的四組解析度／字型檢查，修正正文行距、短欄位、註腳及表格重疊。詳細驗證及適用範圍見 [全量排版檢查報告](FULL-LAYOUT-AUDIT.zh-TW.md)。
