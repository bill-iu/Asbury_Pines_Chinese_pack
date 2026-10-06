# Reproducible x86 compatibility correction for this package's pinned Unity Mono.
param([string]$GameRoot=(Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),[string]$SourceDll=(Join-Path $PSScriptRoot 'MonoMod.Utils.original.dll'),[string]$OutputDll=(Join-Path $PSScriptRoot 'MonoMod.Utils.dll'))
$ErrorActionPreference='Stop'
$game=[IO.Path]::GetFullPath($GameRoot)
$source=[IO.Path]::GetFullPath($SourceDll)
if((Get-FileHash $source).Hash -ne '9D1495F147AC93C4F81F84538C1A326E8F8A6AEFC78D6289D798F3CE1162C5E9'){throw 'Unexpected MonoMod input'}
if((Get-FileHash (Join-Path $game 'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll')).Hash -ne '473D59FE1F1A2CBBF37340517CA6F9C6A48330D44DC609E5C3209ADF63792FCF'){throw 'Unverified Mono runtime'}
Add-Type -Path (Join-Path $game 'BepInEx/core/Mono.Cecil.dll')
$a=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($source)
try {
 $t=$a.MainModule.Types | Where-Object FullName -eq 'MonoMod.Utils.Extensions'
 $m=$t.Methods | Where-Object Name -eq 'SetMonoCorlibInternal'
 $i=@($m.Body.Instructions | Where-Object {$_.Offset -eq 0x13f -and $_.OpCode.Code -eq 'Ldc_I4_S' -and $_.Operand -eq 20})
 if($i.Count -ne 1){throw 'Unexpected offset calculation'}
 # MonoAssemblyName is four bytes larger in this pinned Unity x86 runtime.
 # Official matching PDB: corlib_internal=83, friend_assembly_names=76.
 # The previous sum was 79, corrupting the pointer's most significant byte.
 $i[0].Operand=[sbyte]24
 $out=[IO.Path]::GetFullPath($OutputDll)
 $a.Write($out)
 Get-FileHash $out | ConvertTo-Json
} finally {$a.Dispose()}
