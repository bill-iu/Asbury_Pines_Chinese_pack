# Asbury Pines 繁體中文 0.1.14-pack.2

將目前使用的 0.1.14 漢化補丁整理為離線簡易安裝版。插件、詞庫及載入器內容保持一致；新增圖形化的安裝／更新、卸載及語言設定入口。

- EXE：下載 `Asbury_Pines_TC_0.1.14-pack.2_Setup.exe`，雙擊後選擇含有 `AsburyPines.exe` 的遊戲資料夾並安裝。
- ZIP：完整解壓 `Asbury_Pines_TC_0.1.14-pack.2_Portable.zip`，雙擊 `Install.cmd`；卸載使用 `Uninstall.cmd`。
- 適用 Windows Steam build 24231082／[EA] 3.01.001。遊戲更新後若版本不符會拒絕安裝。
- 安裝器保留版本檢查、檔案擁有權檢查、更新備份和失敗回復；卸載保留自行修改的檔案及其他插件。
- F8 切換繁中／英文，F9 重新載入詞庫。
- `SHA256SUMS.txt` 提供下載檔校驗碼；EXE 未簽章。

封裝只包含補丁、載入器、來源碼與第三方授權，不包含遊戲本體、素材或存檔。EXE 解壓、中文路徑、安裝／更新、檔案保護、語言設定與卸載測試均已通過。完整方法及範圍見 repo 的 `docs/PACKAGING-VERIFICATION.md`。

另附簡中版 EXE／ZIP：Asbury_Pines_SC_0.1.14-pack.2_Setup.exe 與 Asbury_Pines_SC_0.1.14-pack.2_Portable.zip。簡中由現有詞庫及動態文字以 OpenCC tw2s 轉換；兩版每次使用其中一版，執行另一版安裝器即可互換。簡中詞庫、動態規則與安裝互換已驗證，尚未重新完成全量遊戲內視覺檢查。
