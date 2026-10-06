param([switch]$PluginOnly,[switch]$VerifyOnly,[string]$GameRoot=$PSScriptRoot)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
$manifest=Join-Path $root 'zhhant-install-manifest.json'
if(Get-Process -Name AsburyPines -ErrorAction SilentlyContinue){throw 'Exit Asbury Pines before uninstalling.'}
if(-not(Test-Path -LiteralPath $manifest)){throw 'Installation manifest is missing. No files were removed.'}
$data=Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
$pluginPrefix='BepInEx\plugins\AsburyPines.ZhHant\'
$pluginDir=Join-Path $root $pluginPrefix
$others=@(Get-ChildItem -LiteralPath (Join-Path $root 'BepInEx\plugins') -File -Recurse -Filter '*.dll' -ErrorAction SilentlyContinue | Where-Object {-not $_.FullName.StartsWith($pluginDir,[StringComparison]::OrdinalIgnoreCase)})
if($others.Count -gt 0 -and -not $PluginOnly){throw 'Other plugins use BepInEx. Run with -PluginOnly to keep the shared loader.'}
$targets=@()
foreach($entry in $data.Files){
 if($PluginOnly -and -not $entry.Path.StartsWith($pluginPrefix,[StringComparison]::OrdinalIgnoreCase)){continue}
 $path=[IO.Path]::GetFullPath((Join-Path $root $entry.Path))
 if(-not $path.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Unsafe manifest path: $($entry.Path)"}
 $safeRelative=$path.Substring($root.Length+1)
 $allowed=($safeRelative -in @('winhttp.dll','doorstop_config.ini','.doorstop_version')) -or $safeRelative.StartsWith('BepInEx\core\',[StringComparison]::OrdinalIgnoreCase) -or $safeRelative.StartsWith($pluginPrefix,[StringComparison]::OrdinalIgnoreCase)
 if(-not $allowed){throw "Manifest path is not a permitted plugin or loader file: $safeRelative"}
 if(Test-Path -LiteralPath $path){
  $hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
  if($hash -ne $entry.Hash){Write-Warning "Preserving modified file: $($entry.Path)";continue}
  $targets+=$path
 }
}
if($VerifyOnly){$targets;Write-Output "Would remove $($targets.Count) unchanged, manifest-owned files.";return}
# No recursive deletion. Only individually verified files from this installation.
foreach($path in $targets){Remove-Item -LiteralPath $path}
Write-Output "Removed $($targets.Count) installed files. Original game files, saves, research, backups and unlisted files were preserved."
if(Test-Path -LiteralPath (Join-Path $root 'BepInEx\plugins\AsburyPines.ZhHant\AsburyPines.ZhHant.dll')){Write-Warning 'The plugin DLL was modified and preserved. The plugin may still be active.'}
if(-not $PluginOnly -and (Test-Path -LiteralPath (Join-Path $root 'winhttp.dll'))){Write-Warning 'The loader DLL was modified and preserved. Inspect it before considering the loader uninstalled.'}
