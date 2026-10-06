#requires -Version 7.0
param([Parameter(Mandatory=$true)][string]$GameRoot)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
[void][Reflection.Assembly]::LoadFrom((Join-Path ([IO.Path]::GetFullPath($GameRoot)) 'AsburyPines_Data\Managed\Newtonsoft.Json.dll'))
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo 'payload\BepInEx\plugins\AsburyPines.ZhHant\AsburyPines.ZhHant.dll'))
$type=$assembly.GetType('AsburyPines.ZhHant.Catalog')
$catalogPath=[string](Join-Path $repo 'payload\BepInEx\plugins\AsburyPines.ZhHant\translations')
$catalog=$type.GetMethod('Load').Invoke($null,[object[]]@($catalogPath))
$cases=@(
 @('MAIN MENU','主選單'),
 @('New Romina','覺醒的蘿蜜娜'),
 @('Awakened Romina','覺醒的蘿蜜娜'),
 @("BONUS`nFROM`nPERKS",'項目加成'),
 @('WORM83','蠕蟲83'),
 @('Mayute 12, Year 27 After Founding','建城後27年 Mayute 12日'),
 @('Worm83 changes the world. ','蠕蟲83 改變世界。'),
 @("NATHANIEL ASBURY`nLINE 2`n<color=`"#B6FFCD`">COMPLETED</color>","納撒尼爾・阿斯伯里`n第2行`n<color=`"#B6FFCD`">已完成</color>"),
 @('3 OF 12','3／12'),
 @('3 OF ?','3／?'),
 @('3 of 12','3／12'),
 @('2.0 <size=12>s</size>   5.0 <size=12>TIME</size>   ','2.0 <size=12>秒</size>   5.0 <size=12>時間</size>'),
 @('-3.5 <size=15> MLS</size>','-3.5 <size=15>餐</size>'),
 @('<ERROR - KLN1>','〈錯誤：KLN1〉'),
 @('46% RESEARCHED (5,047 / 7,900 PTS)','研究進度 46%（5,047／7,900 點數）'),
 @('46% RESEARCHED (5,047 / 7,900 XP)','研究進度 46%（5,047／7,900 經驗）'),
 @('∞ (N/A <size=12>XP</size>)','∞ (不適用 <size=12>經驗</size>)'),
 @('N/A (∞ <size=12>XP</size>)','不適用 (∞ <size=12>經驗</size>)'),
 @('3<size=12>M</size> 9<size=12>S</size> (N/A <size=12>XP</size>)','3<size=12>分</size> 9<size=12>秒</size> (不適用 <size=12>經驗</size>)'),
 @('2 <size=12>SEC</size> 5 <size=12>XP</size> (VARIES)','2<size=12>秒</size> 5<size=12>經驗</size>（浮動）'),
 @('2-Ren','二次復興'),
 @('N/A','不適用'),
 @('CURRENT ERA (Worm 83)','目前時代（蠕蟲83）'),
 @('Unlocked new era, <i>Worm 83</i>.','已解鎖新時代：<i>蠕蟲83</i>。'),
 @('New [Error: Unknown Story] story unlocked.','已解鎖[錯誤：未知故事]的新故事。'),
 @('New [Error: Unknown Community Event] community event unlocked.','已解鎖[錯誤：未知社群事件]的新社群事件。'),
 @('ROMINA ZAPPA','蘿蜜娜・扎帕'),
 @('100 EXP','100 經驗'),
 @('<color="#FFA87E">PEOPLE</color>','<color="#FFA87E">人物</color>'),
 @('Aug 28, 2030','2030年8月28日'),
 @('Aug 6 , 1978','1978年8月6日'),
 @('Mar 28, 894','894年3月28日'),
 @('Jun  17, 899','899年6月17日'),
 @('Kajuli 14, 899','Kajuli 14, 899'),
 @('Feb 30, 899','Feb 30, 899'),
 @('MAR 28, 0894 BC','公元前894年3月28日'),
 @('Kajuli 14, 899 BC','Kajuli 14, 899 BC'),
 @("MAXIMUM OF:`n<color=`"#FFAB83`">-20.0%</color> FROM CATHOLICISM`n","取下列最大值：`n<color=`"#FFAB83`">-20.0%</color> 來自 天主教`n"),
 @('3<size=12>M</size> 9<size=12>S</size> (25 <size=12>XP</size>)','3<size=12>分</size> 9<size=12>秒</size> (25 <size=12>經驗</size>)'),
 @('25<size=12>HR</size> 3<size=12>M</size>','25<size=12>時</size> 3<size=12>分</size>'),
 @('UNKNOWN ORIGINAL TEXT','UNKNOWN ORIGINAL TEXT'),
 @('VICTORY 12.34%','完成度 12.34%'),
 @('500 LOGS  <size=15>+5 IN 12</size><size=12>S</size>','500 根木材  <size=15>12秒後 +5</size><size=12></size>'),
 @('s-0001','s-0001'),
 @('A note about 100 FOOD and a character.','A note about 100 FOOD and a character.'),
 @('Kajuli 14, 103 ToS','Kajuli 14, 103 ToS'),
 @('th',''),
 @('the','the'),
 @('CURRENT VALUE: <color="#AA44CC">+4.50%</color>','目前數值：<color="#AA44CC">+4.50%</color>'),
 @('CURRENT ACTION LENGTH: <color="#FFAB83">13.8</color><size=14> s</size>','目前行動時間：<color="#FFAB83">13.8</color><size=14> 秒</size>'),
 @('CURRENT ACTION LENGTH: <color="#FFAB83">???</color><size=14> s</size>','目前行動時間：<color="#FFAB83">???</color><size=14> 秒</size>'),
 @('24.7K EXP REMAINING','剩餘 24.7K 經驗'),
 @('-355 EXP REMAINING','剩餘 -355 經驗'),
 @('0 EXP REMAINING','剩餘 0 經驗'),
 @('27% EXAMINED','已檢視 27%'),
 @('12.5% REMAINING','剩餘 12.5%'),
 @('BUILDING (67%) ...','建造中（67%） ...'),
 @('IDLE (4%)','閒置（4%）'),
 @('YOU HAVE <color="#FFAB83">9.123M</color> PRAYERS.','你擁有 <color="#FFAB83">9.123M</color> 祈禱。'),
 @('You were gone for at least 12 hours.','你離開了至少 12 小時。'),
 @('You were gone for less than a minute','你離開了不到一分鐘。'),
 @('You were gone for 1 hour and 1 minute.','你離開了約 1 小時又 1 分鐘。'),
 @('You were gone for 3 hours and 24 minutes.','你離開了約 3 小時又 24 分鐘。'),
 @('You were gone for 1 hour.','你離開了約 1 小時。'),
 @('You were gone for 24 minutes.','你離開了約 24 分鐘。'),
 @('<b>3</b>  Unlocked and  <b>19</b>  Completed','已解鎖 <b>3</b>，已完成 <b>19</b>'),
 @('<b>9</b>  Unlocked','已解鎖 <b>9</b>'),
 @('<b>17</b>  Completed','已完成 <b>17</b>'),
 @('The sign said 27% EXAMINED.','The sign said 27% EXAMINED.'),
 @('You were gone for a long time.','You were gone for a long time.'),
 @('37% WORSHIPPED (1,200 / 3,200 XP)','崇拜進度 37%（1,200／3,200 經驗）'),
 @('49% RESEARCHED (2,001 / 4,100 XP)','研究進度 49%（2,001／4,100 經驗）'),
 @('61% EXAMINED (6,100 / 10,000 XP)','檢視進度 61%（6,100／10,000 經驗）'),
 @('18% GOV. LIFE CYCLE COMPLETE (180 / 1,000 XP)','政體生命週期完成 18%（180／1,000 經驗）'),
 @('3<size=12>M</size> 8<size=12>S</size>','3<size=12>分</size> 8<size=12>秒</size>'),
 @('2<size=12>HR</size> 49<size=12>M</size>','2<size=12>時</size> 49<size=12>分</size>'),
 @('WORSHIPPING...','崇拜中...'),
 @('RESEARCHING . . .','研究中 . . .'),
 @('WORKING . .','工作中 . .'),
 @('GOVERNING.','治理中.'),
 @('EXAMINING...','檢視中...'),
 @('IDLE ...','閒置 ...'),
 @('VERSION 3.01.001','版本 3.01.001'),
 @('AGE 27','27 歲'),
 @('7 HRS','7 小時'),
 @('YOU HAVE <color="#FFAB83">15.3K</color> WOOD','持有：<color="#FFAB83">15.3K</color> 木材'),
 @('YOU HAVE <color="#FFAB83">-$15.3K</color>','持有：<color="#FFAB83">-$15.3K</color>'),
 @("CURRENT ERA (COLONIAL)`n <color=`"#FFAB83`">5</color> WOOD SPENT`n <color=`"#FFAB83`">+12</color> WOOD PRODUCED","目前時代（殖民時代）`n已消耗：<color=`"#FFAB83`">5</color> 木材`n已生產：<color=`"#FFAB83`">+12</color> 木材"),
 @('PREVIOUS ERA (NEOLITHIC)','先前時代（新石器時代）'),
 @('<color="#FFAB83">+5</color> WOOD = <color="#FFD27F">50%</color> X <color="#FFD27F">10</color> WOOD (FROM PAST WOOD)','<color="#FFAB83">+5</color> 木材 = <color="#FFD27F">50%</color> × <color="#FFD27F">10</color> 木材（來自過去的木材）'),
 @('CURRENT ERA (UNKNOWN ERA)','CURRENT ERA (UNKNOWN ERA)'),
 @('YOU HAVE <color="#FFAB83">15</color> UNOBTAINIUM','你擁有<color="#FFAB83">15</color> UNOBTAINIUM'),
 @('Unlocked new era, <i>The Colonial Era</i>.','已解鎖新時代：<i>殖民時代</i>。'),
 @('A new religion (Catholicism) has been unlocked.','已解鎖新的宗教：天主教。'),
 @('Finished worshipping Catholicism.','已完成崇拜：天主教。'),
 @('Finn Sloan unlocked!','已解鎖：芬恩・斯隆！'),
 @('New Finn Sloan story unlocked.','已解鎖芬恩・斯隆的新故事。'),
 @('A new religion (Unknown religion) has been unlocked.','A new religion (Unknown religion) has been unlocked.'),
 @('Someone said: Finn Sloan unlocked!','Someone said: Finn Sloan unlocked!'),
 @('3.7 <size=12>S</size>','3.7 <size=12>秒</size>'),
 @('4.9<size=11>s</size>','4.9 <size=11>秒</size>'),
 @('6 <size=12>SEC</size>','6 <size=12>秒</size>'),
 @('8 <size=11>MINUTES</size>','8 <size=11>分鐘</size>'),
 @('8 <size=12>SEC</size> 47 <size=12>PTS</size>','8 <size=12>秒</size> 47 <size=12>點數</size>'),
 @('17 <size=12>M</size> 2 <size=12>s</size>','17 <size=12>分</size> 2 <size=12>秒</size>'),
 @('9% <size="13">of</size> GOV. LIFE CYCLE COMPLETE (901 / 10,000 PTS)','9% <size="13">政體週期完成</size>（901／10,000 點數）'),
 @('+7 FROM CATHOLICISM','+7 來自 天主教'),
 @("+10 FROM BLOOD-FOR-DEBT DIVINE MANDATE`n+1 FROM CATHOLICISM","+10 來自 以血償債神諭`n+1 來自 天主教"),
 @('<color="#FFAB83">+15%</color> FROM CATHOLICISM','<color="#FFAB83">+15%</color> 來自 天主教'),
 @('+1 FROM AN UNKNOWN PERK','+1 FROM AN UNKNOWN PERK'),
 @("+1 FROM CATHOLICISM`nAn unknown sentence.","+1 FROM CATHOLICISM`nAn unknown sentence."),
 @('SCAVENGING (LVL 3) - 42% (2,106/5,000 XP)','拾荒（等級 3）－42%（2,106／5,000 經驗）'),
 @('+9 SCIENCE - 35% (350/1,000 XP)','+9 科學－35%（350／1,000 經驗）'),
 @('FAKE (LVL 3) - 42% (2,106/5,000 XP)','FAKE (LVL 3) - 42% (2,106/5,000 XP)'),
 @('42.7M LOGS +12 IN 53s','42.7M 根木材，53 秒後 +12'),
 @('42.7M UNKNOWN +12 IN 53s','42.7M UNKNOWN +12 IN 53s'),
 @('BASE ACTION LENGTH: <color="#FFAB83">9.4</color><size=14> s</size>','基礎行動時間：<color="#FFAB83">9.4</color><size=14> 秒</size>'),
 @("BASE VALUE: <color=`"#FFAB83`">10</color>`nPRODUCTION BOOST(S)`n<color=`"#FFAB83`">+2</color> FROM CATHOLICISM","基礎數值：<color=`"#FFAB83`">10</color>`n產量加成`n<color=`"#FFAB83`">+2</color> 來自 天主教"),
 @("BASE VALUE: <color=`"#FFAB83`">10</color>`r`nPRODUCTION BOOST(S)`r`n+2 FROM CATHOLICISM","基礎數值：<color=`"#FFAB83`">10</color>`r`n產量加成`r`n+2 來自 天主教")
)
foreach($case in $cases){
 $argsForCall=[object[]]@([string]$case[0],$null)
 $null=$type.GetMethod('TryTranslate').Invoke($catalog,$argsForCall)
 if($argsForCall[1] -cne $case[1]){throw "Translation regression: $($case[0]) -> $($argsForCall[1]); expected $($case[1])"}
}
Write-Output "PASS: $($cases.Count) compiled-catalog cases, including rich text, dynamic resources, unknown-text fallback and identifiers."

$allRows=0
foreach($file in Get-ChildItem -LiteralPath $catalogPath -Filter '*.json' -File){
 if($file.Name -eq 'rules.json'){continue}
 foreach($row in (Get-Content -Raw -LiteralPath $file.FullName | ConvertFrom-Json)){
  $check=[object[]]@([string]$row.source,$null)
  $null=$type.GetMethod('TryTranslate').Invoke($catalog,$check)
  if($check[1] -cne $row.target){throw "Compiled row mismatch: $($file.Name): $($row.source)"}
  $allRows++
 }
}
Write-Output "PASS: $allRows compiled translation rows match the packaged catalog."