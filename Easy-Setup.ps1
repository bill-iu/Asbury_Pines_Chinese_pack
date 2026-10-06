param(
 [string]$InitialGameRoot='',
 [ValidateSet('Install','Uninstall','PluginOnly','On','Off','Verify')][string]$Action='Install',
 [string]$GameRoot='',
 [switch]$NonInteractive
)
$ErrorActionPreference='Stop'

function Invoke-PackAction([string]$SelectedAction,[string]$Root){
 if([string]::IsNullOrWhiteSpace($Root)){throw '請選擇含有 AsburyPines.exe 的遊戲資料夾。'}
 switch($SelectedAction){
  'Install' {& (Join-Path $PSScriptRoot 'Install-TraditionalChinese.ps1') -GameRoot $Root}
  'Verify' {& (Join-Path $PSScriptRoot 'Install-TraditionalChinese.ps1') -GameRoot $Root -VerifyOnly}
  'Uninstall' {& (Join-Path $PSScriptRoot 'Uninstall-TraditionalChinese.ps1') -GameRoot $Root}
  'PluginOnly' {& (Join-Path $PSScriptRoot 'Uninstall-TraditionalChinese.ps1') -GameRoot $Root -PluginOnly}
  'On' {& (Join-Path $PSScriptRoot 'Set-TraditionalChinese.ps1') -GameRoot $Root -Mode On}
  'Off' {& (Join-Path $PSScriptRoot 'Set-TraditionalChinese.ps1') -GameRoot $Root -Mode Off}
 }
}

if($NonInteractive){Invoke-PackAction $Action $GameRoot;return}
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
[Windows.Forms.Application]::EnableVisualStyles()
$form=New-Object Windows.Forms.Form
$form.Text='Asbury Pines 繁體中文補丁 0.1.14'
$form.ClientSize=New-Object Drawing.Size(660,470)
$form.StartPosition='CenterScreen'
$form.FormBorderStyle='FixedDialog'
$form.MaximizeBox=$false
$form.Font=New-Object Drawing.Font('Microsoft JhengHei',10)
$title=New-Object Windows.Forms.Label
$title.Text="安裝／更新繁體中文補丁`r`n請先關閉遊戲，再選擇含有 AsburyPines.exe 的資料夾。"
$title.SetBounds(20,18,620,52)
$form.Controls.Add($title)
$path=New-Object Windows.Forms.TextBox
$path.SetBounds(20,85,514,28)
$candidates=@($GameRoot,$InitialGameRoot)
try{
 $steam=(Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -ErrorAction Stop).SteamPath
 $candidates+=Join-Path $steam 'steamapps\common\Asbury Pines'
}catch{}
foreach($candidate in $candidates){
 if($candidate -and (Test-Path -LiteralPath (Join-Path $candidate 'AsburyPines.exe'))){$path.Text=$candidate;break}
}
$form.Controls.Add($path)
$browse=New-Object Windows.Forms.Button
$browse.Text='選擇資料夾'
$browse.SetBounds(544,83,96,32)
$browse.Add_Click({
 $dialog=New-Object Windows.Forms.FolderBrowserDialog
 $dialog.Description='選擇 Asbury Pines 的遊戲根目錄（含 AsburyPines.exe）'
 $dialog.ShowNewFolderButton=$false
 if(Test-Path -LiteralPath $path.Text -PathType Container){$dialog.SelectedPath=$path.Text}
 try{if($dialog.ShowDialog($form) -eq 'OK'){$path.Text=$dialog.SelectedPath}}finally{$dialog.Dispose()}
})
$form.Controls.Add($browse)
$options=New-Object Windows.Forms.ComboBox
$options.DropDownStyle='DropDownList'
$options.SetBounds(20,133,450,30)
$labels=@('安裝／更新繁體中文補丁','檢查版本及安裝條件（不寫入檔案）','卸載補丁及其載入器','只卸載繁中插件（保留其他插件的載入器）','永久啟用中文（需先啟動遊戲一次）','永久停用中文（需先啟動遊戲一次）')
$actions=@('Install','Verify','Uninstall','PluginOnly','On','Off')
foreach($label in $labels){[void]$options.Items.Add($label)}
$options.SelectedIndex=[Array]::IndexOf($actions,$Action)
$form.Controls.Add($options)
$run=New-Object Windows.Forms.Button
$run.Text='執行'
$run.SetBounds(490,130,150,35)
$form.Controls.Add($run)
$log=New-Object Windows.Forms.TextBox
$log.Multiline=$true
$log.ReadOnly=$true
$log.ScrollBars='Vertical'
$log.SetBounds(20,185,620,190)
$log.Text="適用 Windows Steam [EA] 3.01.001（build 24231082）。`r`n安裝前會檢查遊戲與 Mono 版本；不覆蓋無法辨認或自行修改的檔案。`r`n更新備份保留在遊戲目錄的 zhhant-install-backup-*。"
$form.Controls.Add($log)
$note=New-Object Windows.Forms.Label
$note.Text="安裝後由 Steam 啟動遊戲；F8 切換中英，F9 重載詞庫。`r`n請保留本安裝器或解壓資料夾，以便日後卸載。"
$note.SetBounds(20,394,620,52)
$form.Controls.Add($note)
$run.Add_Click({
 $selected=$actions[$options.SelectedIndex]
 if($selected -in @('Uninstall','PluginOnly')){
  $answer=[Windows.Forms.MessageBox]::Show($form,'將只移除安裝清單擁有且未修改的檔案。確定執行卸載？','卸載繁體中文補丁','YesNo','Question')
  if($answer -ne 'Yes'){return}
 }
 $run.Enabled=$false;$browse.Enabled=$false;$options.Enabled=$false;$path.Enabled=$false
 $form.UseWaitCursor=$true
 try{
  $log.Text='正在處理，請稍候…';$form.Refresh()
  $result=Invoke-PackAction $selected $path.Text 3>&1 | Out-String
  $log.Text=$result
  $message=if($selected -eq 'Install'){'安裝完成。請由 Steam 啟動遊戲。'}elseif($selected -eq 'Verify'){'版本及安裝條件檢查通過，沒有寫入檔案。'}else{'操作完成。請查看結果；中文設定需重啟遊戲才會生效。'}
  [void][Windows.Forms.MessageBox]::Show($form,$message,'Asbury Pines 繁體中文補丁','OK','Information')
 }catch{
  $log.Text=$_.Exception.Message
  [void][Windows.Forms.MessageBox]::Show($form,$_.Exception.Message,'無法完成操作','OK','Error')
 }finally{$run.Enabled=$true;$browse.Enabled=$true;$options.Enabled=$true;$path.Enabled=$true;$form.UseWaitCursor=$false}
})
[void]$form.ShowDialog()
$form.Dispose()
