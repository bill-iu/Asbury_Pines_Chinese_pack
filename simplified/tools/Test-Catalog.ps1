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
 @('MAIN MENU','主选单'),
 @('New Romina','觉醒的萝蜜娜'),
 @('Awakened Romina','觉醒的萝蜜娜'),
 @("BONUS`nFROM`nPERKS",'项目加成'),
 @('WORM83','蠕虫83'),
 @('Mayute 12, Year 27 After Founding','建城后27年 Mayute 12日'),
 @('Worm83 changes the world. ','蠕虫83 改变世界。'),
 @("NATHANIEL ASBURY`nLINE 2`n<color=`"#B6FFCD`">COMPLETED</color>","纳撒尼尔・阿斯伯里`n第2行`n<color=`"#B6FFCD`">已完成</color>"),
 @('3 OF 12','3／12'),
 @('3 OF ?','3／?'),
 @('3 of 12','3／12'),
 @('2.0 <size=12>s</size>   5.0 <size=12>TIME</size>   ','2.0 <size=12>秒</size>   5.0 <size=12>时间</size>'),
 @('-3.5 <size=15> MLS</size>','-3.5 <size=15>餐</size>'),
 @('<ERROR - KLN1>','〈错误：KLN1〉'),
 @('46% RESEARCHED (5,047 / 7,900 PTS)','研究进度 46%（5,047／7,900 点数）'),
 @('46% RESEARCHED (5,047 / 7,900 XP)','研究进度 46%（5,047／7,900 经验）'),
 @('∞ (N/A <size=12>XP</size>)','∞ (不适用 <size=12>经验</size>)'),
 @('N/A (∞ <size=12>XP</size>)','不适用 (∞ <size=12>经验</size>)'),
 @('3<size=12>M</size> 9<size=12>S</size> (N/A <size=12>XP</size>)','3<size=12>分</size> 9<size=12>秒</size> (不适用 <size=12>经验</size>)'),
 @('2 <size=12>SEC</size> 5 <size=12>XP</size> (VARIES)','2<size=12>秒</size> 5<size=12>经验</size>（浮动）'),
 @('2-Ren','二次复兴'),
 @('N/A','不适用'),
 @('CURRENT ERA (Worm 83)','目前时代（蠕虫83）'),
 @('Unlocked new era, <i>Worm 83</i>.','已解锁新时代：<i>蠕虫83</i>。'),
 @('New [Error: Unknown Story] story unlocked.','已解锁[错误：未知故事]的新故事。'),
 @('New [Error: Unknown Community Event] community event unlocked.','已解锁[错误：未知社群事件]的新社群事件。'),
 @('ROMINA ZAPPA','萝蜜娜・扎帕'),
 @('100 EXP','100 经验'),
 @('<color="#FFA87E">PEOPLE</color>','<color="#FFA87E">人物</color>'),
 @('Aug 28, 2030','2030年8月28日'),
 @('Aug 6 , 1978','1978年8月6日'),
 @('Mar 28, 894','894年3月28日'),
 @('Jun  17, 899','899年6月17日'),
 @('Kajuli 14, 899','Kajuli 14, 899'),
 @('Feb 30, 899','Feb 30, 899'),
 @('MAR 28, 0894 BC','公元前894年3月28日'),
 @('Kajuli 14, 899 BC','Kajuli 14, 899 BC'),
 @("MAXIMUM OF:`n<color=`"#FFAB83`">-20.0%</color> FROM CATHOLICISM`n","取下列最大值：`n<color=`"#FFAB83`">-20.0%</color> 来自 天主教`n"),
 @('3<size=12>M</size> 9<size=12>S</size> (25 <size=12>XP</size>)','3<size=12>分</size> 9<size=12>秒</size> (25 <size=12>经验</size>)'),
 @('25<size=12>HR</size> 3<size=12>M</size>','25<size=12>时</size> 3<size=12>分</size>'),
 @('UNKNOWN ORIGINAL TEXT','UNKNOWN ORIGINAL TEXT'),
 @('VICTORY 12.34%','完成度 12.34%'),
 @('500 LOGS  <size=15>+5 IN 12</size><size=12>S</size>','500 根木材  <size=15>12秒后 +5</size><size=12></size>'),
 @('s-0001','s-0001'),
 @('A note about 100 FOOD and a character.','A note about 100 FOOD and a character.'),
 @('Kajuli 14, 103 ToS','Kajuli 14, 103 ToS'),
 @('th',''),
 @('the','the'),
 @('CURRENT VALUE: <color="#AA44CC">+4.50%</color>','目前数值：<color="#AA44CC">+4.50%</color>'),
 @('CURRENT ACTION LENGTH: <color="#FFAB83">13.8</color><size=14> s</size>','目前行动时间：<color="#FFAB83">13.8</color><size=14> 秒</size>'),
 @('CURRENT ACTION LENGTH: <color="#FFAB83">???</color><size=14> s</size>','目前行动时间：<color="#FFAB83">???</color><size=14> 秒</size>'),
 @('24.7K EXP REMAINING','剩余 24.7K 经验'),
 @('-355 EXP REMAINING','剩余 -355 经验'),
 @('0 EXP REMAINING','剩余 0 经验'),
 @('27% EXAMINED','已检视 27%'),
 @('12.5% REMAINING','剩余 12.5%'),
 @('BUILDING (67%) ...','建造中（67%） ...'),
 @('IDLE (4%)','闲置（4%）'),
 @('YOU HAVE <color="#FFAB83">9.123M</color> PRAYERS.','你拥有 <color="#FFAB83">9.123M</color> 祈祷。'),
 @('You were gone for at least 12 hours.','你离开了至少 12 小时。'),
 @('You were gone for less than a minute','你离开了不到一分钟。'),
 @('You were gone for 1 hour and 1 minute.','你离开了约 1 小时又 1 分钟。'),
 @('You were gone for 3 hours and 24 minutes.','你离开了约 3 小时又 24 分钟。'),
 @('You were gone for 1 hour.','你离开了约 1 小时。'),
 @('You were gone for 24 minutes.','你离开了约 24 分钟。'),
 @('<b>3</b>  Unlocked and  <b>19</b>  Completed','已解锁 <b>3</b>，已完成 <b>19</b>'),
 @('<b>9</b>  Unlocked','已解锁 <b>9</b>'),
 @('<b>17</b>  Completed','已完成 <b>17</b>'),
 @('The sign said 27% EXAMINED.','The sign said 27% EXAMINED.'),
 @('You were gone for a long time.','You were gone for a long time.'),
 @('37% WORSHIPPED (1,200 / 3,200 XP)','崇拜进度 37%（1,200／3,200 经验）'),
 @('49% RESEARCHED (2,001 / 4,100 XP)','研究进度 49%（2,001／4,100 经验）'),
 @('61% EXAMINED (6,100 / 10,000 XP)','检视进度 61%（6,100／10,000 经验）'),
 @('18% GOV. LIFE CYCLE COMPLETE (180 / 1,000 XP)','政体生命周期完成 18%（180／1,000 经验）'),
 @('3<size=12>M</size> 8<size=12>S</size>','3<size=12>分</size> 8<size=12>秒</size>'),
 @('2<size=12>HR</size> 49<size=12>M</size>','2<size=12>时</size> 49<size=12>分</size>'),
 @('WORSHIPPING...','崇拜中...'),
 @('RESEARCHING . . .','研究中 . . .'),
 @('WORKING . .','工作中 . .'),
 @('GOVERNING.','治理中.'),
 @('EXAMINING...','检视中...'),
 @('IDLE ...','闲置 ...'),
 @('VERSION 3.01.001','版本 3.01.001'),
 @('AGE 27','27 岁'),
 @('7 HRS','7 小时'),
 @('YOU HAVE <color="#FFAB83">15.3K</color> WOOD','持有：<color="#FFAB83">15.3K</color> 木材'),
 @('YOU HAVE <color="#FFAB83">-$15.3K</color>','持有：<color="#FFAB83">-$15.3K</color>'),
 @("CURRENT ERA (COLONIAL)`n <color=`"#FFAB83`">5</color> WOOD SPENT`n <color=`"#FFAB83`">+12</color> WOOD PRODUCED","目前时代（殖民时代）`n已消耗：<color=`"#FFAB83`">5</color> 木材`n已生产：<color=`"#FFAB83`">+12</color> 木材"),
 @('PREVIOUS ERA (NEOLITHIC)','先前时代（新石器时代）'),
 @('<color="#FFAB83">+5</color> WOOD = <color="#FFD27F">50%</color> X <color="#FFD27F">10</color> WOOD (FROM PAST WOOD)','<color="#FFAB83">+5</color> 木材 = <color="#FFD27F">50%</color> × <color="#FFD27F">10</color> 木材（来自过去的木材）'),
 @('CURRENT ERA (UNKNOWN ERA)','CURRENT ERA (UNKNOWN ERA)'),
 @('YOU HAVE <color="#FFAB83">15</color> UNOBTAINIUM','你拥有<color="#FFAB83">15</color> UNOBTAINIUM'),
 @('Unlocked new era, <i>The Colonial Era</i>.','已解锁新时代：<i>殖民时代</i>。'),
 @('A new religion (Catholicism) has been unlocked.','已解锁新的宗教：天主教。'),
 @('Finished worshipping Catholicism.','已完成崇拜：天主教。'),
 @('Finn Sloan unlocked!','已解锁：芬恩・斯隆！'),
 @('New Finn Sloan story unlocked.','已解锁芬恩・斯隆的新故事。'),
 @('A new religion (Unknown religion) has been unlocked.','A new religion (Unknown religion) has been unlocked.'),
 @('Someone said: Finn Sloan unlocked!','Someone said: Finn Sloan unlocked!'),
 @('3.7 <size=12>S</size>','3.7 <size=12>秒</size>'),
 @('4.9<size=11>s</size>','4.9 <size=11>秒</size>'),
 @('6 <size=12>SEC</size>','6 <size=12>秒</size>'),
 @('8 <size=11>MINUTES</size>','8 <size=11>分钟</size>'),
 @('8 <size=12>SEC</size> 47 <size=12>PTS</size>','8 <size=12>秒</size> 47 <size=12>点数</size>'),
 @('17 <size=12>M</size> 2 <size=12>s</size>','17 <size=12>分</size> 2 <size=12>秒</size>'),
 @('9% <size="13">of</size> GOV. LIFE CYCLE COMPLETE (901 / 10,000 PTS)','9% <size="13">政体周期完成</size>（901／10,000 点数）'),
 @('+7 FROM CATHOLICISM','+7 来自 天主教'),
 @("+10 FROM BLOOD-FOR-DEBT DIVINE MANDATE`n+1 FROM CATHOLICISM","+10 来自 以血偿债神谕`n+1 来自 天主教"),
 @('<color="#FFAB83">+15%</color> FROM CATHOLICISM','<color="#FFAB83">+15%</color> 来自 天主教'),
 @('+1 FROM AN UNKNOWN PERK','+1 FROM AN UNKNOWN PERK'),
 @("+1 FROM CATHOLICISM`nAn unknown sentence.","+1 FROM CATHOLICISM`nAn unknown sentence."),
 @('SCAVENGING (LVL 3) - 42% (2,106/5,000 XP)','拾荒（等级 3）－42%（2,106／5,000 经验）'),
 @('+9 SCIENCE - 35% (350/1,000 XP)','+9 科学－35%（350／1,000 经验）'),
 @('FAKE (LVL 3) - 42% (2,106/5,000 XP)','FAKE (LVL 3) - 42% (2,106/5,000 XP)'),
 @('42.7M LOGS +12 IN 53s','42.7M 根木材，53 秒后 +12'),
 @('42.7M UNKNOWN +12 IN 53s','42.7M UNKNOWN +12 IN 53s'),
 @('BASE ACTION LENGTH: <color="#FFAB83">9.4</color><size=14> s</size>','基础行动时间：<color="#FFAB83">9.4</color><size=14> 秒</size>'),
 @("BASE VALUE: <color=`"#FFAB83`">10</color>`nPRODUCTION BOOST(S)`n<color=`"#FFAB83`">+2</color> FROM CATHOLICISM","基础数值：<color=`"#FFAB83`">10</color>`n产量加成`n<color=`"#FFAB83`">+2</color> 来自 天主教"),
 @("BASE VALUE: <color=`"#FFAB83`">10</color>`r`nPRODUCTION BOOST(S)`r`n+2 FROM CATHOLICISM","基础数值：<color=`"#FFAB83`">10</color>`r`n产量加成`r`n+2 来自 天主教")
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