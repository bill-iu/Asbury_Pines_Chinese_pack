param([ValidateSet('On','Off')][string]$Mode='On',[string]$GameRoot=$PSScriptRoot)
$ErrorActionPreference='Stop'
$config=Join-Path ([IO.Path]::GetFullPath($GameRoot)) 'BepInEx\config\local.asburypines.zhhant.cfg'
if(-not (Test-Path -LiteralPath $config)){throw 'Run the game once after installation to create its configuration.'}
$text=[IO.File]::ReadAllText($config)
if($text -notmatch '(?m)^Enabled\s*='){throw 'Missing Enabled setting; configuration was not changed.'}
$text=[regex]::Replace($text,'(?m)^Enabled\s*=.*$',('Enabled = '+($Mode -eq 'On').ToString().ToLowerInvariant()))
[IO.File]::WriteAllText($config,$text,[Text.UTF8Encoding]::new($false))
Write-Output "Simplified Chinese: $Mode. Restart the game for this setting to take effect. F8 switches language during the current session."
