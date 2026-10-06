param([Parameter(Mandatory=$true)][string]$GameRoot)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$edition=Join-Path $repo 'simplified'
$managed=Join-Path ([IO.Path]::GetFullPath($GameRoot)) 'AsburyPines_Data\Managed'
$core=Join-Path $edition 'payload\BepInEx\core'
$assembly=Join-Path $managed 'Assembly-CSharp.dll'
if((Get-FileHash -LiteralPath $assembly).Hash -ne 'D4F7976FDE49B59CD8B441CBD156537060EDC55E9F7676E693080B9B4CC1E15F'){throw 'Unverified game build.'}
$output=Join-Path $edition 'payload\BepInEx\plugins\AsburyPines.ZhHant\AsburyPines.ZhHant.dll'
$refs=@('mscorlib.dll','System.dll','System.Core.dll','netstandard.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.UI.dll','UnityEngine.UIModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.ScreenCaptureModule.dll','Newtonsoft.Json.dll') | ForEach-Object {'/reference:'+(Join-Path $managed $_)}
$refs+=@(('/reference:'+(Join-Path $core 'BepInEx.dll')),('/reference:'+(Join-Path $core '0Harmony.dll')))
# SC text uses the existing runtime measurement path, not cached TC measurements.
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
& $compiler /noconfig /nologo /target:library /nostdlib /optimize+ /utf8output $refs ('/out:'+$output) (Join-Path $edition 'source\Plugin.cs')
if($LASTEXITCODE -ne 0){throw 'Simplified plugin compilation failed.'}
Write-Output "Built simplified plugin: $output"
