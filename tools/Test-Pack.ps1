param([Parameter(Mandatory=$true)][string]$GameRoot,[string]$Version='0.1.14-pack.1')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $repo ('artifacts\Asbury_Pines_TC_'+$Version+'_Setup.exe')
$work=Join-Path $repo ('test-work\'+[guid]::NewGuid().ToString('N'))
$fixture=Join-Path $work '遊戲資料夾 (測試)'
New-Item -ItemType Directory -Path (Join-Path $fixture 'AsburyPines_Data\Managed'),(Join-Path $fixture 'MonoBleedingEdge\EmbedRuntime') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $GameRoot 'AsburyPines_Data\Managed\Assembly-CSharp.dll') -Destination (Join-Path $fixture 'AsburyPines_Data\Managed\Assembly-CSharp.dll')
Copy-Item -LiteralPath (Join-Path $GameRoot 'MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll') -Destination (Join-Path $fixture 'MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll')
[IO.File]::WriteAllText((Join-Path $fixture 'AsburyPines.exe'),'Fixture only; not executable.')
$case=0
function Run-Exe([string]$Action,[int]$Expected=0){
 $script:case++
 $extract=Join-Path $work ('解壓資料夾 (測試) '+$script:case)
 $arguments=@('--'+$Action,'"'+$fixture+'"','--work-dir','"'+$extract+'"')
 $process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne $Expected){
  $log=Join-Path $extract 'setup-result.log'
  if(Test-Path -LiteralPath $log){Get-Content -LiteralPath $log}
  throw "$Action returned $($process.ExitCode), expected $Expected."
 }
 return $extract
}
$assembly=Join-Path $fixture 'AsburyPines_Data\Managed\Assembly-CSharp.dll'
$mono=Join-Path $fixture 'MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll'
$assemblyHash=(Get-FileHash -LiteralPath $assembly).Hash
$monoHash=(Get-FileHash -LiteralPath $mono).Hash
$extracted=Run-Exe 'verify'
if(Test-Path -LiteralPath (Join-Path $fixture 'winhttp.dll')){throw 'Verify wrote game files.'}
$records=Get-Content -Raw -LiteralPath (Join-Path $extracted 'SHA256.json') | ConvertFrom-Json
foreach($entry in $records){if((Get-FileHash -LiteralPath (Join-Path $extracted $entry.Path)).Hash -ne $entry.SHA256){throw "Extraction checksum mismatch: $($entry.Path)"}}
$originalMono=[IO.File]::ReadAllBytes($mono)
try{
 [IO.File]::WriteAllText($mono,'unsupported runtime fixture')
 [void](Run-Exe 'install' 1)
 if(Test-Path -LiteralPath (Join-Path $fixture 'winhttp.dll')){throw 'Unsupported Mono wrote payload.'}
}finally{[IO.File]::WriteAllBytes($mono,$originalMono)}
$loader=Join-Path $fixture 'winhttp.dll'
[IO.File]::WriteAllText($loader,'unowned loader fixture')
$unownedHash=(Get-FileHash -LiteralPath $loader).Hash
[void](Run-Exe 'install' 1)
if((Get-FileHash -LiteralPath $loader).Hash -ne $unownedHash){throw 'Unowned loader was overwritten.'}
Remove-Item -LiteralPath $loader
[void](Run-Exe 'install')
$manifestPath=Join-Path $fixture 'zhhant-install-manifest.json'
$manifest=Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
foreach($entry in $manifest.Files){if((Get-FileHash -LiteralPath (Join-Path $fixture $entry.Path)).Hash -ne $entry.Hash){throw "Installed file mismatch: $($entry.Path)"}}
[void](Run-Exe 'install')
$config=Join-Path $fixture 'BepInEx\config\local.asburypines.zhhant.cfg'
New-Item -ItemType Directory -Path (Split-Path $config -Parent) -Force | Out-Null
[IO.File]::WriteAllText($config,"[General]`r`nEnabled = true`r`n",[Text.UTF8Encoding]::new($false))
& (Join-Path $repo 'Easy-Setup.ps1') -NonInteractive -Action Off -GameRoot $fixture
if([IO.File]::ReadAllText($config) -notmatch 'Enabled = false'){throw 'Off did not update the setting.'}
& (Join-Path $repo 'Easy-Setup.ps1') -NonInteractive -Action On -GameRoot $fixture
if([IO.File]::ReadAllText($config) -notmatch 'Enabled = true'){throw 'On did not update the setting.'}
$changed=Join-Path $fixture 'BepInEx\plugins\AsburyPines.ZhHant\translations\ui.json'
Add-Content -LiteralPath $changed -Value 'modified fixture'
$modifiedHash=(Get-FileHash -LiteralPath $changed).Hash
[void](Run-Exe 'install' 1)
if((Get-FileHash -LiteralPath $changed).Hash -ne $modifiedHash){throw 'Locally edited translation was overwritten.'}
$other=Join-Path $fixture 'BepInEx\plugins\OtherPlugin.dll'
[IO.File]::WriteAllText($other,'other plugin fixture')
[void](Run-Exe 'uninstall' 1)
& (Join-Path $repo 'Easy-Setup.ps1') -NonInteractive -Action PluginOnly -GameRoot $fixture
if(-not(Test-Path -LiteralPath $loader) -or -not(Test-Path -LiteralPath $other)){throw 'Plugin-only uninstall affected the shared loader or another plugin.'}
Remove-Item -LiteralPath $other
[void](Run-Exe 'uninstall')
if((Get-FileHash -LiteralPath $changed).Hash -ne $modifiedHash){throw 'Uninstall changed local edits.'}
if(Test-Path -LiteralPath $loader){throw 'Uninstall left the owned loader.'}
if(Test-Path -LiteralPath (Join-Path $fixture 'BepInEx\plugins\AsburyPines.ZhHant\AsburyPines.ZhHant.dll')){throw 'Uninstall left the owned plugin.'}
if((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash -or (Get-FileHash -LiteralPath $mono).Hash -ne $monoHash){throw 'Original game files changed.'}
$report=[ordered]@{Version=$Version;VerifiedAt=(Get-Date).ToString('o');ExtractedChecksums=$records.Count;FreshInstallFiles=$manifest.Files.Count;UnicodeAndSpaces=$true;VerifyDoesNotInstall=$true;UnsupportedMonoRejected=$true;UnownedLoaderPreserved=$true;UpdatePassed=$true;ModifiedTranslationPreserved=$true;PluginOnlyKeepsOtherMods=$true;LanguageTogglePassed=$true;UninstallPassed=$true;OriginalGameFilesPreserved=$true}
$report|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $repo 'artifacts\test-verification.json') -Encoding utf8
Write-Output 'PASS: EXE extraction, Unicode paths, verify-only, unsupported runtime, unowned loader, install/update, edited-file protection, language settings and shared-loader-safe uninstall.'
