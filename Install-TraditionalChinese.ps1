param([Parameter(Mandatory=$true)][string]$GameRoot,[switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($GameRoot).TrimEnd('\')
$payload=Join-Path $PSScriptRoot 'payload'
$manifestPath=Join-Path $root 'zhhant-install-manifest.json'
if(Get-Process -Name AsburyPines -ErrorAction SilentlyContinue){throw 'Exit Asbury Pines before installing.'}
$assembly=Join-Path $root 'AsburyPines_Data\Managed\Assembly-CSharp.dll'
if(-not(Test-Path -LiteralPath (Join-Path $root 'AsburyPines.exe')) -or -not(Test-Path -LiteralPath $assembly)){throw 'Select the Asbury Pines game directory containing AsburyPines.exe.'}
if((Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash -ne 'D4F7976FDE49B59CD8B441CBD156537060EDC55E9F7676E693080B9B4CC1E15F'){throw 'Unsupported game build. No files changed.'}
$mono=Join-Path $root 'MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll'
if(-not(Test-Path -LiteralPath $mono) -or (Get-FileHash -LiteralPath $mono -Algorithm SHA256).Hash -ne '473D59FE1F1A2CBBF37340517CA6F9C6A48330D44DC609E5C3209ADF63792FCF'){throw 'Unsupported Mono runtime for the x86 loader compatibility fix. No files changed.'}
if(-not(Test-Path -LiteralPath $payload)){throw 'Package payload is missing.'}
$previous=@{}
if(Test-Path -LiteralPath $manifestPath){
 $old=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
 foreach($entry in $old.Files){$previous[$entry.Path]=$entry.Hash}
}
$files=@(foreach($file in Get-ChildItem -LiteralPath $payload -File -Recurse){
 $relative=$file.FullName.Substring($payload.Length+1)
 $allowed=($relative -in @('winhttp.dll','doorstop_config.ini','.doorstop_version')) -or $relative.StartsWith('BepInEx\core\',[StringComparison]::OrdinalIgnoreCase) -or $relative.StartsWith('BepInEx\plugins\AsburyPines.ZhHant\',[StringComparison]::OrdinalIgnoreCase)
 if(-not $allowed){throw "Unexpected package path: $relative"}
 $target=[IO.Path]::GetFullPath((Join-Path $root $relative))
 if(-not $target.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw "Unsafe package path: $relative"}
 $hash=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
 if(Test-Path -LiteralPath $target){
  if(-not $previous.ContainsKey($relative)){throw "Unowned file already exists: $relative. No files changed."}
  if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $previous[$relative]){throw "Locally modified file: $relative. Preserve your edits before updating. No files changed."}
 }
 [pscustomobject]@{Source=$file.FullName;Path=$relative;Target=$target;Hash=$hash}
})
if(-not($files.Path -contains 'BepInEx\plugins\AsburyPines.ZhHant\AsburyPines.ZhHant.dll')){throw 'Plugin DLL is missing from package.'}
if(-not($files.Path -contains 'winhttp.dll')){throw 'Loader is missing from package.'}
if($VerifyOnly){Write-Output "Verified game build and $($files.Count) payload paths. No files changed.";return}
# All game-version and collision checks finish before the first copy.
$rollback=Join-Path $root ('zhhant-install-backup-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $rollback | Out-Null
$written=@()
try {
 foreach($file in $files){
  $backup=Join-Path $rollback $file.Path
  $existed=Test-Path -LiteralPath $file.Target
  if($existed){New-Item -ItemType Directory -Path (Split-Path $backup -Parent) -Force | Out-Null;Copy-Item -LiteralPath $file.Target -Destination $backup}
  New-Item -ItemType Directory -Path (Split-Path $file.Target -Parent) -Force | Out-Null
  $written+=@{Target=$file.Target;Backup=$backup;Existed=$existed}
  Copy-Item -LiteralPath $file.Source -Destination $file.Target
  if((Get-FileHash -LiteralPath $file.Target -Algorithm SHA256).Hash -ne $file.Hash){throw "Copy verification failed: $($file.Path)"}
 }
 if(Test-Path -LiteralPath $manifestPath){Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $rollback 'zhhant-install-manifest.json')}
 $index=@{};foreach($key in $previous.Keys){$index[$key]=$previous[$key]};foreach($file in $files){$index[$file.Path]=$file.Hash}
 $manifest=@{InstalledAt=(Get-Date).ToString('o');Loader='BepInEx 5.4.23.5 x86';Files=@(foreach($key in ($index.Keys|Sort-Object)){@{Path=$key;Hash=$index[$key]}})}
 $pendingManifest=Join-Path $rollback 'new-install-manifest.json'
 $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $pendingManifest -Encoding utf8
 Move-Item -LiteralPath $pendingManifest -Destination $manifestPath -Force
} catch {
 $failure=$_
 $rollbackErrors=@()
 for($i=$written.Count-1;$i -ge 0;$i--){
  $entry=$written[$i]
  try {
   if($entry.Existed){
    # A failed copy may leave the original intact but locked against writes.
    $unchanged=(Test-Path -LiteralPath $entry.Target) -and ((Get-FileHash -LiteralPath $entry.Target -Algorithm SHA256).Hash -eq (Get-FileHash -LiteralPath $entry.Backup -Algorithm SHA256).Hash)
    if(-not $unchanged){Copy-Item -LiteralPath $entry.Backup -Destination $entry.Target -Force}
   }elseif(Test-Path -LiteralPath $entry.Target){Remove-Item -LiteralPath $entry.Target}
  } catch {$rollbackErrors+=$_.Exception.Message}
 }
 $oldManifest=Join-Path $rollback 'zhhant-install-manifest.json'
 if(Test-Path -LiteralPath $oldManifest){Copy-Item -LiteralPath $oldManifest -Destination $manifestPath -Force}
 if($rollbackErrors.Count){throw "Installation failed: $($failure.Exception.Message). Some files could not be restored: $($rollbackErrors -join '; '). Backups: $rollback"}
 throw $failure
}
Write-Output "Installed $($files.Count) verified files. Original game files and saves were not changed. Update backups: $rollback"
