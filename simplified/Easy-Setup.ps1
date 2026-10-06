param(
 [string]$InitialGameRoot='',
 [ValidateSet('Install','Uninstall','PluginOnly','On','Off','Verify')][string]$Action='Install',
 [string]$GameRoot='',
 [switch]$NonInteractive
)
$ErrorActionPreference='Stop'

function Invoke-PackAction([string]$SelectedAction,[string]$Root){
 if([string]::IsNullOrWhiteSpace($Root)){throw '请选择含有 AsburyPines.exe 的游戏资料夹。'}
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
$form.Text='Asbury Pines 简体中文补丁 0.1.14'
$form.ClientSize=New-Object Drawing.Size(660,470)
$form.StartPosition='CenterScreen'
$form.FormBorderStyle='FixedDialog'
$form.MaximizeBox=$false
$form.Font=New-Object Drawing.Font('Microsoft JhengHei',10)
$title=New-Object Windows.Forms.Label
$title.Text="安装／更新简体中文补丁`r`n请先关闭游戏，再选择含有 AsburyPines.exe 的资料夹。"
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
$browse.Text='选择资料夹'
$browse.SetBounds(544,83,96,32)
$browse.Add_Click({
 $dialog=New-Object Windows.Forms.FolderBrowserDialog
 $dialog.Description='选择 Asbury Pines 的游戏根目录（含 AsburyPines.exe）'
 $dialog.ShowNewFolderButton=$false
 if(Test-Path -LiteralPath $path.Text -PathType Container){$dialog.SelectedPath=$path.Text}
 try{if($dialog.ShowDialog($form) -eq 'OK'){$path.Text=$dialog.SelectedPath}}finally{$dialog.Dispose()}
})
$form.Controls.Add($browse)
$options=New-Object Windows.Forms.ComboBox
$options.DropDownStyle='DropDownList'
$options.SetBounds(20,133,450,30)
$labels=@('安装／更新简体中文补丁','检查版本及安装条件（不写入档案）','卸载补丁及其载入器','只卸载简中插件（保留其他插件的载入器）','永久启用中文（需先启动游戏一次）','永久停用中文（需先启动游戏一次）')
$actions=@('Install','Verify','Uninstall','PluginOnly','On','Off')
foreach($label in $labels){[void]$options.Items.Add($label)}
$options.SelectedIndex=[Array]::IndexOf($actions,$Action)
$form.Controls.Add($options)
$run=New-Object Windows.Forms.Button
$run.Text='执行'
$run.SetBounds(490,130,150,35)
$form.Controls.Add($run)
$log=New-Object Windows.Forms.TextBox
$log.Multiline=$true
$log.ReadOnly=$true
$log.ScrollBars='Vertical'
$log.SetBounds(20,185,620,190)
$log.Text="适用 Windows Steam [EA] 3.01.001（build 24231082）。`r`n安装前会检查游戏与 Mono 版本；不覆盖无法辨认或自行修改的档案。`r`n更新备份保留在游戏目录的 zhhant-install-backup-*。"
$form.Controls.Add($log)
$note=New-Object Windows.Forms.Label
$note.Text="安装后由 Steam 启动游戏；F8 切换中英，F9 重载词库。`r`n请保留本安装器或解压资料夹，以便日后卸载。"
$note.SetBounds(20,394,620,52)
$form.Controls.Add($note)
$run.Add_Click({
 $selected=$actions[$options.SelectedIndex]
 if($selected -in @('Uninstall','PluginOnly')){
  $answer=[Windows.Forms.MessageBox]::Show($form,'将只移除安装清单拥有且未修改的档案。确定执行卸载？','卸载简体中文补丁','YesNo','Question')
  if($answer -ne 'Yes'){return}
 }
 $run.Enabled=$false;$browse.Enabled=$false;$options.Enabled=$false;$path.Enabled=$false
 $form.UseWaitCursor=$true
 try{
  $log.Text='正在处理，请稍候…';$form.Refresh()
  $result=Invoke-PackAction $selected $path.Text 3>&1 | Out-String
  $log.Text=$result
  $message=if($selected -eq 'Install'){'安装完成。请由 Steam 启动游戏。'}elseif($selected -eq 'Verify'){'版本及安装条件检查通过，没有写入档案。'}else{'操作完成。请查看结果；中文设定需重启游戏才会生效。'}
  [void][Windows.Forms.MessageBox]::Show($form,$message,'Asbury Pines 简体中文补丁','OK','Information')
 }catch{
  $log.Text=$_.Exception.Message
  [void][Windows.Forms.MessageBox]::Show($form,$_.Exception.Message,'无法完成操作','OK','Error')
 }finally{$run.Enabled=$true;$browse.Enabled=$true;$options.Enabled=$true;$path.Enabled=$true;$form.UseWaitCursor=$false}
})
[void]$form.ShowDialog()
$form.Dispose()
