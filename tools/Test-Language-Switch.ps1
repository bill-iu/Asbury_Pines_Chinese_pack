param([Parameter(Mandatory=$true)][string]$GameRoot,[string]$Version='0.1.14-pack.2')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$fixture=Join-Path $repo ('test-work\switch-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $fixture 'AsburyPines_Data\Managed'),(Join-Path $fixture 'MonoBleedingEdge\EmbedRuntime') -Force | Out-Null
foreach($relative in @('AsburyPines_Data\Managed\Assembly-CSharp.dll','MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll')){
 Copy-Item -LiteralPath (Join-Path $GameRoot $relative) -Destination (Join-Path $fixture $relative)
}
[IO.File]::WriteAllText((Join-Path $fixture 'AsburyPines.exe'),'Fixture only; not executable.')
$tc=Join-Path $repo ('artifacts\Asbury_Pines_TC_'+$Version+'_Setup.exe')
$sc=Join-Path $repo ('simplified\artifacts\Asbury_Pines_SC_'+$Version+'_Setup.exe')
$counter=0
foreach($step in @(@{Name='TC';Exe=$tc;Root=$repo},@{Name='SC';Exe=$sc;Root=(Join-Path $repo 'simplified')},@{Name='TC';Exe=$tc;Root=$repo})){
 $counter++
 $extract=Join-Path $fixture ('installer-'+$counter)
 $arguments=@('--install','"'+$fixture+'"','--work-dir','"'+$extract+'"')
 $process=Start-Process -FilePath $step.Exe -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $extract 'setup-result.log');throw "Language switch failed: $($step.Name)"}
 $manifest=Get-Content -Raw -LiteralPath (Join-Path $fixture 'zhhant-install-manifest.json') | ConvertFrom-Json
 foreach($file in $manifest.Files){
  $target=Join-Path $fixture $file.Path
  $source=Join-Path $step.Root ('payload\'+$file.Path)
  if((Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $source).Hash){throw "Language switch content mismatch: $($step.Name): $($file.Path)"}
 }
 Write-Output "PASS: switched to $($step.Name), $($manifest.Files.Count) matching files."
}
& (Join-Path $repo 'Uninstall-TraditionalChinese.ps1') -GameRoot $fixture
if(Test-Path -LiteralPath (Join-Path $fixture 'winhttp.dll')){throw 'Switch fixture uninstall failed.'}
Write-Output 'PASS: TC -> SC -> TC through the published EXEs; safe final uninstall.'
