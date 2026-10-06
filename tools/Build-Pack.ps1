param([string]$Version='0.1.14-pack.2')
$ErrorActionPreference='Stop'
if($Version -notmatch '^[0-9A-Za-z][0-9A-Za-z.-]{0,60}$'){throw 'Invalid release version.'}
$repo=Split-Path $PSScriptRoot -Parent
$out=Join-Path $repo 'artifacts'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$zipPath=Join-Path $out ('Asbury_Pines_TC_'+$Version+'_Portable.zip')
$exePath=Join-Path $out ('Asbury_Pines_TC_'+$Version+'_Setup.exe')
if((Test-Path -LiteralPath $zipPath) -or (Test-Path -LiteralPath $exePath)){throw 'Release artifacts already exist; use a new version or preserve the existing artifacts first.'}
$rootFiles=@('LICENSE','LICENSE-SCOPE.md','README.md','Easy-Setup.ps1','Install.cmd','Uninstall.cmd','Install-TraditionalChinese.ps1','Set-TraditionalChinese.ps1','Uninstall-TraditionalChinese.ps1')
$files=@(foreach($name in $rootFiles){Get-Item -LiteralPath (Join-Path $repo $name)};foreach($name in @('payload','source','licenses','docs','installer','tools')){Get-ChildItem -LiteralPath (Join-Path $repo $name) -Recurse -File | Where-Object {$_.FullName -notmatch '[\\/]__pycache__[\\/]'}})
foreach($file in $files){
 $relative=$file.FullName.Substring($repo.Length+1)
 if($relative -match '(^|\\)(config|cache|diagnostics|backup|test-work|\.git)(\\|$)|Assembly-CSharp\.dll|UnityEngine.*\.dll|Newtonsoft\.Json\.dll|UnityPlayer\.dll|AsburyPines\.exe|\.log$'){throw "Forbidden package file: $relative"}
}
$records=@(foreach($file in ($files|Sort-Object FullName)){[pscustomobject]@{Path=$file.FullName.Substring($repo.Length+1).Replace('\','/');SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;Bytes=$file.Length}})
$hashManifest=Join-Path $repo 'SHA256.json'
[IO.File]::WriteAllText($hashManifest,($records|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open($zipPath,[IO.Compression.ZipArchiveMode]::Create)
try{
 foreach($file in @($files)+(Get-Item -LiteralPath $hashManifest)){
  $name=$file.FullName.Substring($repo.Length+1).Replace('\','/')
  [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,$name,[IO.Compression.CompressionLevel]::Optimal)
 }
}finally{$zip.Dispose()}
$hashPath=Join-Path $out 'embedded-package-sha256.txt'
[IO.File]::WriteAllText($hashPath,(Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash,[Text.UTF8Encoding]::new($false))
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){throw 'The .NET Framework C# compiler is required to build the EXE.'}
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll ('/resource:'+$zipPath+',AsburyPines.Package') ('/resource:'+$hashPath+',AsburyPines.PackageSHA256') ('/out:'+$exePath) (Join-Path $repo 'installer\Bootstrap.cs')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed.'}
$checksums=@(foreach($file in @($zipPath,$exePath)){(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($file)})
[IO.File]::WriteAllLines((Join-Path $out 'SHA256SUMS.txt'),$checksums,[Text.UTF8Encoding]::new($false))
Get-Item -LiteralPath $zipPath,$exePath | Select-Object Name,Length
