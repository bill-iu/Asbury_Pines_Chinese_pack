using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace AsburyPines.ZhHant {
 [BepInPlugin("local.asburypines.zhhant", "Asbury Pines Traditional Chinese", "0.1.14")]
 [BepInProcess("AsburyPines.exe")]
 public sealed class Plugin : BaseUnityPlugin {
  internal static Plugin Self;
  internal static readonly FieldInfo TextField=AccessTools.Field(typeof(Text),"m_Text");
  internal static Font Chinese;
  internal static bool Active;
  static bool changingFont;
  static readonly Dictionary<Text,Font> Originals=new Dictionary<Text,Font>();
  static readonly HashSet<string> Missing=new HashSet<string>();
  static readonly HashSet<Text> PendingFonts=new HashSet<Text>();
  static readonly Dictionary<Graphic,bool> DropCapOriginals=new Dictionary<Graphic,bool>();
  ConfigEntry<bool> enabledSetting;
  ConfigEntry<bool> logMissing;
  ConfigEntry<bool> testCommands;
  string folder;
  Harmony harmony;
  float nextFlush;
  float nextTest;
  string lastCommand;
  GameObject diagnosticPreview;
  bool quitting;
  internal static Catalog Words;
  void Awake() {
   try { Initialize(); }
   catch(Exception e) {
    Active=false;
    if(harmony!=null)harmony.UnpatchSelf();
    Logger.LogError("Localization startup failed; original English retained. "+e);
   }
  }
  void Initialize() {
   Self=this;
   folder=Path.GetDirectoryName(Info.Location);
   enabledSetting=Config.Bind("General","Enabled",true,"Enable Traditional Chinese. F8 toggles for this session; F9 reloads translations.");
   logMissing=Config.Bind("Diagnostics","LogMissing",false,"Write deduplicated untranslated visible strings to the plugin directory.");
   testCommands=Config.Bind("Diagnostics","EnableTestCommands",false,"Development only: allow local diagnostic requests in this plugin's test-request.json.");
   var fontName=Config.Bind("General","Font","Microsoft JhengHei","Installed CJK font family. Default uses Windows Traditional Chinese font.");
   if(!enabledSetting.Value) { Logger.LogInfo("Disabled by configuration. No runtime patches installed.");return; }
   Words=Catalog.Load(Path.Combine(folder,"translations"));
   string gameAssembly=Path.Combine(Paths.GameRootPath,"AsburyPines_Data","Managed","Assembly-CSharp.dll");
   using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(gameAssembly)) {
    string hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
    if(hash!="D4F7976FDE49B59CD8B441CBD156537060EDC55E9F7676E693080B9B4CC1E15F") {
     Logger.LogWarning("Unverified game build. Original English retained; no runtime patches installed. Assembly SHA256="+hash);return;
    }
   }
   var available=Font.GetOSInstalledFontNames();
   string selected=new[]{fontName.Value,"Microsoft JhengHei","Microsoft JhengHei UI","Noto Sans CJK TC","Noto Sans TC","Microsoft YaHei"}.FirstOrDefault(x=>available.Contains(x));
   if(selected==null) {Logger.LogError("No supported CJK font installed. Keeping original English; no patches installed.");return;}
   Chinese=Font.CreateDynamicFontFromOSFont(selected,20);
   if(Chinese==null) {Logger.LogError("Could not create CJK font. Keeping original English.");return;}
   DontDestroyOnLoad(Chinese);
   if(selected=="Microsoft JhengHei")layoutCacheTask=System.Threading.Tasks.Task.Factory.StartNew<LayoutCacheFile>(LoadLayoutCache);
   Active=true;
   harmony=new Harmony("local.asburypines.zhhant");
   foreach(string name in new[]{"OnPopulateMesh","get_preferredWidth","get_preferredHeight"}) {
    var method=AccessTools.Method(typeof(Text),name,name=="OnPopulateMesh"?new Type[]{typeof(VertexHelper)}:Type.EmptyTypes);
    if(method==null) throw new MissingMethodException("UnityEngine.UI.Text",name);
    harmony.Patch(method,new HarmonyMethod(typeof(Plugin),"BeforeRender"),null,null,new HarmonyMethod(typeof(Plugin),"AfterRender"),null);
   }
   harmony.Patch(AccessTools.PropertySetter(typeof(Text),"font"),new HarmonyMethod(typeof(Plugin),"BeforeFont"));
   harmony.Patch(AccessTools.PropertySetter(typeof(Text),"text"),postfix:new HarmonyMethod(typeof(Plugin),"PrepareFont"));
   harmony.Patch(AccessTools.Method(typeof(Text),"OnEnable"),postfix:new HarmonyMethod(typeof(Plugin),"PrepareFont"));
   // Inactive stories can receive the CJK font before their controller's first Start.
   // Let the game cache its genuine default, then restore the display font.
   var readable=AccessTools.TypeByName("ReadableFontController");
   if(readable!=null)harmony.Patch(AccessTools.Method(readable,"Start"),new HarmonyMethod(typeof(Plugin),"BeforeReadableStart"),null,null,new HarmonyMethod(typeof(Plugin),"AfterReadableStart"),null);
   // Geometry calculations must measure the same translated text that the renderer uses.
   var page=AccessTools.TypeByName("PageManagement");
   if(page!=null) foreach(string name in new[]{"CalculateHeightOfText","CalculateWidthOfText"}) {
    var method=AccessTools.Method(page,name);
    if(method!=null) harmony.Patch(method,new HarmonyMethod(typeof(Plugin),"BeforeMeasure"));
   }
   var notification=AccessTools.TypeByName("NotificationTop");
   if(notification!=null)harmony.Patch(AccessTools.Method(notification,"CalculateWidthOfText"),new HarmonyMethod(typeof(Plugin),"BeforeNotificationMeasure"));
   foreach(string typeName in new[]{"SpeedTooltip","ResourceBreakdownTooltip"}) {
    var type=AccessTools.TypeByName(typeName);
    harmony.Patch(AccessTools.Method(type,"CalculateHeightOfText"),new HarmonyMethod(typeof(Plugin),"BeforeTooltipHeight"));
    harmony.Patch(AccessTools.Method(type,"CalculateWidthOfText"),new HarmonyMethod(typeof(Plugin),"BeforeTooltipWidth"));
   }
   SceneManager.sceneLoaded+=SceneLoaded;
   Logger.LogInfo("Ready: "+Words.Count+" translations; font="+selected+"; Unity="+Application.unityVersion+". Game text values remain English; translation occurs only during UI rendering/measurement.");
  }
  struct RenderState {public string source;public FontData data;public bool rich;public int size;public TextAnchor alignment;public HorizontalWrapMode wrap;public float spacing;}
  static readonly FieldInfo FontDataField=AccessTools.Field(typeof(Text),"m_FontData");
  static bool IsNamePlaque(Text text) {return text.transform.parent!=null&&text.transform.parent.name=="Name Plaque"&&(text.name=="Name"||text.name=="Name Shadow"||text.name=="Subtitle"||text.name=="Subtitle Shadow");}
  static void BeforeRender(Text __instance, out RenderState __state) {
   __state=default(RenderState);
   if(!Active||__instance==null||Words==null)return;
   string original=__instance.text, translated;
   if(Words.TryTranslate(original,out translated)) {
    // Unity forbids registering a graphic rebuild from inside its rebuild loop.
    if(__instance.font!=Chinese){PendingFonts.Add(__instance);return;}
    __state=new RenderState{source=original};
    int headerSize=FixedFieldFontSize(__instance,translated);
    string rendered=RenderTranslation(__instance,translated);
    if(IsNamePlaque(__instance)||headerSize>0||StoryRoot(__instance)!=null) {
     __state.data=(FontData)FontDataField.GetValue(__instance);__state.rich=__state.data.richText;__state.size=__state.data.fontSize;__state.alignment=__state.data.alignment;
     __state.wrap=__state.data.horizontalOverflow;
     __state.spacing=__state.data.lineSpacing;
     if(StoryRoot(__instance)!=null){__state.data.horizontalOverflow=HorizontalWrapMode.Wrap;__state.data.lineSpacing=Mathf.Max(1,__state.spacing);}
     if(IsNamePlaque(__instance))__state.data.richText=true;
     if(headerSize>0)__state.data.fontSize=headerSize;
     if(IsSelectorLabel(__instance))__state.data.alignment=TextAnchor.UpperLeft;
    }
    TextField.SetValue(__instance,rendered);
   } else if(Self.logMissing.Value&&!string.IsNullOrWhiteSpace(original)&&Regex.IsMatch(original,"[A-Za-z]{3}")) Missing.Add(original);
  }
  static Exception AfterRender(Text __instance,RenderState __state,Exception __exception) {
   if(__state.source!=null) {
    if(__instance!=null)TextField.SetValue(__instance,__state.source);
    if(__state.data!=null){__state.data.richText=__state.rich;__state.data.fontSize=__state.size;__state.data.alignment=__state.alignment;__state.data.horizontalOverflow=__state.wrap;__state.data.lineSpacing=__state.spacing;}
   }
   return __exception;
  }
  static readonly Dictionary<string,string> IllustratedLayouts=new Dictionary<string,string>();
  static TextGenerationSettings ChineseSettings(Text text,Vector2 size) {
   var settings=text.GetGenerationSettings(size);
   if(StoryRoot(text)!=null){settings.lineSpacing=Mathf.Max(1,settings.lineSpacing);settings.horizontalOverflow=HorizontalWrapMode.Wrap;}
   return settings;
  }
  sealed class HeaderFit {public string value;public Font font;public float scale,spacing,width,height;public int size,result;public FontStyle style;}
  static readonly Dictionary<Text,HeaderFit> HeaderFits=new Dictionary<Text,HeaderFit>();
  static readonly Dictionary<Text,HeaderFit> CaptionBudgets=new Dictionary<Text,HeaderFit>();
  static bool IsSelectorLabel(Text text) {
   var parent=text.transform.parent;
   return parent!=null&&(text.name=="CharName"||text.name=="SkillLevel")&&parent.Find("CharName")!=null&&parent.Find("SkillLevel")!=null;
  }
  static int FixedFieldFontSize(Text text,string translated) {
   var parent=text.transform.parent;
   if(parent==null)return -1;
   float width,height;bool storyCaption=false;
   if(IsSelectorLabel(text)) {
    var rect=parent as RectTransform;
    width=Mathf.Min(text.rectTransform.rect.width,rect.rect.width-text.rectTransform.anchoredPosition.x-4);
    height=text.name=="CharName"?16:18;
   } else if(parent.name=="Content"&&parent.parent!=null&&parent.parent.name=="Skills") {
    width=text.rectTransform.rect.width;height=20;
    if(text.name=="Skills Title")width=160;
    else if(text.name=="Level Title")width=53;
    else if(text.name=="Boost Title"){width=68;height=48;}
    else if(text.name!="Name"&&text.name!="Job"&&text.name!="Age")return -1;
   } else if(StoryRoot(text)!=null&&(text.text.Length<100||(StoryRoot(text).name=="s-0075"&&text.name=="Date"))&&!IsNamePlaque(text)&&FormBoundary(text)==null) {
    storyCaption=true;
    // Short captions need the same protection as paragraphs. Preserve the
    // original number of lines, rather than trusting oversized authoring boxes.
    Font original;if(!Originals.TryGetValue(text,out original)||original==null)return -1;
    width=text.rectTransform.rect.width;
    HeaderFit budget;
    if(CaptionBudgets.TryGetValue(text,out budget)&&budget.value==text.text&&budget.font==original&&budget.size==text.fontSize&&budget.spacing==text.lineSpacing&&budget.scale==text.pixelsPerUnit&&budget.width==width&&budget.style==text.fontStyle)height=budget.height;
    else {
     var measure=text.GetGenerationSettings(new Vector2(width,0));measure.font=original;
     using(var generator=new TextGenerator())height=generator.GetPreferredHeight(text.text,measure)/Mathf.Max(.001f,text.pixelsPerUnit);
     CaptionBudgets[text]=new HeaderFit{value=text.text,font=original,size=text.fontSize,spacing=text.lineSpacing,scale=text.pixelsPerUnit,width=width,style=text.fontStyle,height=height};
    }
    height=Mathf.Max(16,height);
    if(IsLicenseText(text)&&text.name=="LICENSE")height=Mathf.Min(height,62);
    if(StoryRoot(text).name=="s-0056"&&text.name=="Location")height=Mathf.Min(height,32);
    if((StoryRoot(text).name=="s-0181"||StoryRoot(text).name=="c-0027")&&text.name=="Title 1")height=90;
    if((StoryRoot(text).name=="c-0018"||StoryRoot(text).name=="c-0019")&&text.name=="SubTitle")height=Mathf.Min(height,16);
    if((StoryRoot(text).name=="c-0018"||StoryRoot(text).name=="c-0019")&&text.name=="Date")height=16;
    if(StoryRoot(text).name=="s-0019"&&(text.name=="Carvings"||text.name=="Carvings (2)"))height=Mathf.Min(height,22);
    if(StoryRoot(text).name=="s-0221"&&text.name=="Text Page 1")height=Mathf.Min(height,71);
    if(StoryRoot(text).name=="s-0075"&&text.name=="Date")height=50;
    if(parent.name=="square"&&text.name=="text")height=16;
    if(StoryRoot(text).name=="c-0027"&&text.name=="text"&&text.text.StartsWith("Noon,",StringComparison.Ordinal)) {
     var footer=parent.Find("subtitle (1)") as RectTransform;
     if(footer!=null)height=FormBoundaryGap(text,footer)-12;
    }
    translated=FitTemplateText(text,translated);
   } else {
   if(parent.name!="Header Panel"||parent.Find("By")==null||parent.Find("Version")==null)return -1;
   Transform page=parent;while(page!=null&&page.name!="Main Menu Page")page=page.parent;
   if(page==null)return -1;
   string name=text.name.Replace(" Shadow","");
   if(name=="Asbury Pines"){width=560;height=57;}
   else if(name=="Welcome To"){width=340;height=70;}
   else if(name=="By"){width=175;height=25;}
   else if(name=="Version"){width=630;height=25;}
   else return -1;
   }
   HeaderFit cached;
   if(HeaderFits.TryGetValue(text,out cached)&&cached.value==translated&&cached.font==text.font&&cached.scale==text.pixelsPerUnit&&cached.spacing==text.lineSpacing&&cached.size==text.fontSize&&cached.style==text.fontStyle&&cached.width==width&&cached.height==height)return cached.result;
   var settings=ChineseSettings(text,new Vector2(width,0));settings.resizeTextForBestFit=false;
   settings.horizontalOverflow=HorizontalWrapMode.Wrap;settings.verticalOverflow=VerticalWrapMode.Overflow;
   float scale=Mathf.Max(0.001f,text.pixelsPerUnit);int result=8;
   using(var generator=new TextGenerator())for(int size=text.fontSize;size>=8;size--) {
    settings.fontSize=size;
    if(generator.GetPreferredHeight(translated,settings)/scale<=height&&(storyCaption||generator.GetPreferredWidth(translated,settings)/scale<=width)){result=size;break;}
   }
   HeaderFits[text]=new HeaderFit{value=translated,font=text.font,scale=text.pixelsPerUnit,spacing=text.lineSpacing,size=text.fontSize,style=text.fontStyle,width=width,height=height,result=result};
   return result;
  }
  static readonly Dictionary<string,string> StoryLayouts=new Dictionary<string,string>();
  static System.Threading.Tasks.Task<LayoutCacheFile> layoutCacheTask;
  static LayoutCacheFile LoadLayoutCache() {
   var stream=typeof(Plugin).Assembly.GetManifestResourceStream("AsburyPines.LayoutCache");
   if(stream==null)return null;
    using(stream)using(var gzip=new System.IO.Compression.GZipStream(stream,System.IO.Compression.CompressionMode.Decompress))using(var reader=new StreamReader(gzip)) {
     var cache=JsonConvert.DeserializeObject<LayoutCacheFile>(reader.ReadToEnd());
     foreach(var pair in cache.fontHashes) {
      string file=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts),pair.Key);
      if(!File.Exists(file))return null;
      using(var sha=System.Security.Cryptography.SHA256.Create())using(var font=File.OpenRead(file))
       if(BitConverter.ToString(sha.ComputeHash(font)).Replace("-","")!=pair.Value)return null;
     }
     return cache;
    }
  }
  sealed class LayoutCacheFile {
   public Dictionary<string,string> fontHashes {get;set;}
   public Dictionary<string,string> stories {get;set;}
   public Dictionary<string,string> illustrated {get;set;}
  }
  sealed class RenderCache {
   public string source,translation,result;
   public Font font,original;
   public Rect rect;
   public Vector3 position;
   public float spacing,scale,available;
   public int size,min,max;
   public FontStyle style;
   public bool bestFit;
  }
  static readonly Dictionary<Text,RenderCache> Rendered=new Dictionary<Text,RenderCache>();
  static readonly Dictionary<Text,RectTransform> FormBoundaries=new Dictionary<Text,RectTransform>();
  static RectTransform FormBoundary(Text text) {
   RectTransform boundary;
   if(FormBoundaries.TryGetValue(text,out boundary))return boundary;
   var parent=text.transform.parent;
   var header=parent==null?null:parent.Find("Header");
   if((text.name=="Text"||text.name.StartsWith("Text (",StringComparison.Ordinal))&&header!=null&&parent.Find("Statement")!=null) {
    float nearest=float.MaxValue;
    foreach(Transform child in header) {
     var line=child as RectTransform;
     if(line==null||!line.name.StartsWith("Thin Wing Lines",StringComparison.Ordinal))continue;
     float gap=FormBoundaryGap(text,line);
     if(gap>0&&gap<nearest){nearest=gap;boundary=line;}
    }
   }
   FormBoundaries[text]=boundary;return boundary;
  }
  static float FormBoundaryGap(Text text,RectTransform line) {
   Vector3 top=line.TransformPoint(new Vector3(line.rect.xMin,line.rect.yMax,0));
   return text.rectTransform.rect.yMax-text.rectTransform.InverseTransformPoint(top).y;
  }
  static string RenderTranslation(Text text,string translated) {
   RenderCache cached;Font original;Originals.TryGetValue(text,out original);
   var rect=text.rectTransform.rect;var position=text.rectTransform.localPosition;
   float available=text.text.Length>=100||FormBoundary(text)!=null?(float)Math.Round(StoryAvailableHeight(text),2):rect.height;
   // Scroll movement belongs to the parent, so it does not invalidate text layout.
   if(Rendered.TryGetValue(text,out cached)&&cached.source==text.text&&cached.translation==translated&&cached.font==text.font&&cached.original==original&&cached.rect==rect&&cached.position==position&&cached.available==available&&cached.size==text.fontSize&&cached.spacing==text.lineSpacing&&cached.scale==text.pixelsPerUnit&&cached.style==text.fontStyle&&cached.bestFit==text.resizeTextForBestFit&&cached.min==text.resizeTextMinSize&&cached.max==text.resizeTextMaxSize)return cached.result;
   string result=FitNamePlaque(text,FitStoryText(text,FitTemplateText(text,FitLicenseText(text,FitQuestionnaireLabel(text,FitNewspaperIntro(text,FitMusterRow(text,FitIllustratedStory(text,translated))))))));
   Rendered[text]=new RenderCache{source=text.text,translation=translated,result=result,font=text.font,original=original,rect=rect,position=position,available=available,size=text.fontSize,spacing=text.lineSpacing,scale=text.pixelsPerUnit,style=text.fontStyle,bestFit=text.resizeTextForBestFit,min=text.resizeTextMinSize,max=text.resizeTextMaxSize};
   return result;
  }
  static readonly Dictionary<Text,Transform> StoryRoots=new Dictionary<Text,Transform>();
  static Transform StoryRoot(Text text) {
   Transform root;if(StoryRoots.TryGetValue(text,out root))return root;
   root=text.transform;while(root!=null&&root.parent!=null&&root.parent.name!="Story Objects Holder")root=root.parent;
   if(root==null||root.parent==null)root=null;
   StoryRoots[text]=root;return root;
  }
  static readonly Dictionary<Text,KeyValuePair<string,string>> TemplateLayouts=new Dictionary<Text,KeyValuePair<string,string>>();
  static string FitTemplateText(Text text,string translated) {
   if(StoryRoot(text)==null)return translated;
   string key=text.text+"\0"+translated;KeyValuePair<string,string> cached;
   if(TemplateLayouts.TryGetValue(text,out cached)&&cached.Key==key)return cached.Value;
   string result=BuildTemplateText(text,translated);TemplateLayouts[text]=new KeyValuePair<string,string>(key,result);return result;
  }
  static string BuildTemplateText(Text text,string translated) {
   var story=StoryRoot(text);if(story==null)return translated;
   string id=story.name,name=text.name;
   // The original Date accidentally duplicates the body, which is already
   // rendered by Text Page 1. Only the first date line belongs in this field.
   if(id=="s-0075"&&name=="Date")return translated.Split('\n')[0];
   if(FormBoundary(text)!=null&&translated.StartsWith("\u3000",StringComparison.Ordinal))translated="\u3000\u3000"+translated;
   if(id=="s-0181"||id=="c-0027") {
    if(name=="Title 1")return "泥沼地\n審判鬥坑";
    if(name=="Title 2"||name=="title small")return "";
   }
   if((id=="c-0018"||id=="c-0019")&&name=="Date")translated=Regex.Replace(translated,@"(\d+)\s*年\s*(\d+)\s*月\s*(\d+)\s*日","$1/$2/$3");
   if(id=="s-0224"||id=="s-0225") {
    if(name=="content")translated=id=="s-0225"?translated.Replace("天才","天才¹").Replace("玉米","玉米²"):translated.Replace("考古學","考古學¹");
    if(name=="content (2)")translated="¹ "+(id=="s-0225"?translated.Replace("\n\n","\n² "):translated);
    if(name=="content (1)")translated=translated.Replace("徵人\n>","徵人");
   }
   if(id=="s-0173"||id=="s-0291"||id=="s-0284"||id=="s-0097"||id=="s-0181"||id=="c-0027") {
    var prefix=Regex.Match(text.text.Replace("\r\n","\n"),@"^\n+").Value;
    if(prefix.Length>0&&!translated.StartsWith("\n",StringComparison.Ordinal))translated=prefix+translated;
   }
   if(id=="c-0012"&&(name=="Names"||(name=="Top"&&text.transform.parent.name=="Names")))translated=translated.Replace("\n\n","\n");
   if(id=="s-0097") {
    if(name=="memory"||name=="done")return "";
    if(name=="List_left (1)")translated=translated.Replace("痛——苦的","痛——苦的回憶").Replace("回想我們做過的","回想我們做過的一切");
   }
   if(id=="c-0027") {
    if(name=="heading")return "時間、罪行與對戰組合";
    if(name=="heading (1)"||name=="heading (2)")return "";
    if(name=="text") {
     if(!text.text.StartsWith("Noon,",StringComparison.Ordinal))return "";
     var columns=text.transform.parent.GetComponentsInChildren<Text>().Where(t=>t.name=="text").ToArray();
     var date=Regex.Split(translated.Trim(),@"\n{2,}");
     string matches="",crimes="";
     foreach(var column in columns) {
      string value;if(!Words.TryTranslate(column.text,out value))value=column.text;
      if(column.text.StartsWith("Skandrayk",StringComparison.Ordinal))matches=value;
      if(column.text.StartsWith("Unpaid",StringComparison.Ordinal))crimes=value;
     }
     var events=Regex.Split(matches.Trim(),@"\n{2,}");var offenses=Regex.Split(crimes.Trim(),@"\n{2,}");
     if(date.Length==4&&events.Length==4&&offenses.Length==4)return string.Join("\n\n",Enumerable.Range(0,4).Select(i=>date[i]+"；罪行："+offenses[i].Replace("\n"," ")+"\n"+events[i].Replace("\n"," ")).ToArray());
    }
   }
   if(id=="s-0221") {
    if(name=="content (3)"&&text.transform.parent.name=="Content")return "The² hot⁴¹ of¹ the² sun⁴ feels¹⁰² good⁴³⁸.";
    if(name=="content (5)"&&text.transform.parent.name=="Content")return "The²：標準用法，無強調\nHot⁴¹：溫和的熱，暖意\nOf¹：標準所有格\nThe²：標準用法，無強調\nSun⁴：春日正午的太陽\nFeels¹⁰²：皮膚的感受\nGood⁴³⁸：意料之外，卻欣然接受";
    if(name=="content (7)"&&text.transform.parent.name=="Content")return "";
   }
   return translated;
  }
  static float StoryAvailableHeight(Text text) {
   float available=text.rectTransform.rect.height;
   var scroll=text.GetComponentInParent<ScrollRect>(true);
   if(scroll!=null&&scroll.content!=null&&text.transform.IsChildOf(scroll.content)&&available>0) {
    var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
    float top=scroll.content.InverseTransformPoint(corners[1]).y;
    float bottom=scroll.content.InverseTransformPoint(corners[0]).y;
    float factor=(top-bottom)/available;
    if(factor>0.001f)available=Mathf.Min(available,(top-scroll.content.rect.yMin)/factor-2);
   }
   var boundary=FormBoundary(text);
   if(boundary!=null)available=Mathf.Min(available,Mathf.Max(0,FormBoundaryGap(text,boundary)-8));
   available=Mathf.Min(available,PaperAvailableHeight(text));
   if(IsLicenseText(text)&&text.name=="text")available=Mathf.Min(available,124);
   var story=StoryRoot(text);
   if(story!=null&&story.name=="s-0097"&&text.name=="List_left (1)") {
    var next=text.transform.parent.Find("List_left (2)") as RectTransform;
    if(next!=null)available=Mathf.Min(available,FormBoundaryGap(text,next)-8);
   }
   if(story!=null&&story.name=="s-0020"&&text.name=="Comment") {
    var votes=story.Find("upvotes") as RectTransform;
    if(votes!=null)available=Mathf.Min(available,FormBoundaryGap(text,votes)-10);
   }
   return available;
  }
  static readonly Dictionary<Text,Image[]> PaperImages=new Dictionary<Text,Image[]>();
  static bool IsLicenseText(Text text) {return text.transform.parent!=null&&text.transform.parent.name=="Content"&&text.transform.parent.parent!=null&&text.transform.parent.parent.name=="s-0115";}
  static string FitLicenseText(Text text,string translated) {
   if(!IsLicenseText(text))return translated;
   if(text.name=="text (1)"||text.name=="text (2)")return "";
   if(text.name!="text")return translated;
   var holder=text.transform.parent.Find("text (1)").GetComponent<Text>();
   var venue=text.transform.parent.Find("text (2)").GetComponent<Text>();
   string holderName,venueName;
   if(!Words.TryTranslate(holder.text,out holderName))holderName=holder.text;
   if(!Words.TryTranslate(venue.text,out venueName))venueName=venue.text;
   return "持照人："+holderName.Trim()+"\n營業場所："+venueName.Trim()+"\n\n"+translated;
  }
  static float PaperAvailableHeight(Text text) {
   Image[] images;
   if(!PaperImages.TryGetValue(text,out images)) {
    var parent=text.transform.parent;
    images=parent==null?new Image[0]:parent.Cast<Transform>().Where(t=>t.name.StartsWith("Note Paper Segment",StringComparison.Ordinal)).Select(t=>t.GetComponent<Image>()).Where(i=>i!=null).ToArray();
    // Page-based notes put the paper Image on the parent itself, not on
    // sibling segments. Its aspect-preserved drawing bounds can be much
    // shorter than the authored Text/RectTransform bounds.
    if(parent!=null&&Regex.IsMatch(parent.name,@"^Page \d+$")&&StoryRoot(text)!=null) {
     var paper=parent.GetComponent<Image>();
     if(paper!=null&&paper.sprite!=null)images=images.Concat(new[]{paper}).ToArray();
    }
    PaperImages[text]=images;
   }
   float lowest=float.PositiveInfinity;
   foreach(var image in images) {
    if(image==null||!image.isActiveAndEnabled)continue;
    var rect=image.GetPixelAdjustedRect();var sprite=image.overrideSprite;
    if(sprite!=null&&image.preserveAspect&&sprite.rect.width>0&&sprite.rect.height>0&&rect.width>0&&rect.height>0) {
     // Match Image's aspect-preserving drawing rectangle, not its oversized
     // RectTransform. The unused space below the sprite is not paper.
     float ratio=sprite.rect.width/sprite.rect.height;
     if(ratio>rect.width/rect.height) {
      float height=rect.width/ratio;rect.y+=(rect.height-height)*image.rectTransform.pivot.y;rect.height=height;
     } else {
      float width=rect.height*ratio;rect.x+=(rect.width-width)*image.rectTransform.pivot.x;rect.width=width;
     }
    }
    var bottom=image.rectTransform.TransformPoint(new Vector3(rect.center.x,rect.yMin,0));
    lowest=Mathf.Min(lowest,text.rectTransform.InverseTransformPoint(bottom).y);
   }
   return float.IsPositiveInfinity(lowest)?float.PositiveInfinity:Mathf.Max(0,text.rectTransform.rect.yMax-lowest-24);
  }
  static string FitQuestionnaireLabel(Text text,string translated) {
   if(text.name!="age label"||!text.supportRichText||text.transform.parent==null||text.transform.parent.Find("Question Label")==null)return translated;
   var age=text.transform.parent.Find("Age") as RectTransform;if(age==null)return translated;
   float width=age.anchoredPosition.x-text.rectTransform.anchoredPosition.x-8;
   if(width<=0)return translated;
   string label=translated.TrimEnd(':','：');
   var settings=ChineseSettings(text,new Vector2(1000,0));settings.resizeTextForBestFit=false;
   using(var generator=new TextGenerator())for(int size=text.fontSize;size>=12;size--) {
    string candidate="<size="+size.ToString(CultureInfo.InvariantCulture)+">"+label+"</size>";
    if(generator.GetPreferredWidth(candidate,settings)/Mathf.Max(0.001f,text.pixelsPerUnit)<=width)return candidate;
   }
   return translated;
  }
  static string FitNamePlaque(Text text,string translated) {
   var plaque=text.transform.parent as RectTransform;
   if(plaque==null||plaque.name!="Name Plaque")return translated;
   bool name=text.name=="Name"||text.name=="Name Shadow";
   bool subtitle=text.name=="Subtitle"||text.name=="Subtitle Shadow";
   if(!name&&!subtitle)return translated;
   // Measure the visible label and its shadow against the same inner bounds.
   var label=plaque.Find(name?"Name":"Subtitle") as RectTransform;
   if(label==null)return translated;
   float width=Mathf.Min(label.rect.width,plaque.rect.width-label.anchoredPosition.x-14);
   var settings=ChineseSettings(text,new Vector2(width,0));
   settings.richText=true;
   settings.resizeTextForBestFit=false;settings.horizontalOverflow=HorizontalWrapMode.Wrap;
   float scale=Mathf.Max(0.001f,text.pixelsPerUnit);
   using(var generator=new TextGenerator()) {
    // Unity retains the base font's line box for smaller rich-text glyphs.
    // Fit one line, rather than demanding a height below that native line box.
    float height=generator.GetPreferredHeight("字",settings)/scale;
    for(int size=text.fontSize;size>=10;size--) {
    string candidate="<size="+size.ToString(CultureInfo.InvariantCulture)+">"+translated+"</size>";
    if(generator.GetPreferredWidth(candidate,settings)/scale<=width&&generator.GetPreferredHeight(candidate,settings)/scale<=height+1)return candidate;
    }
   }
   return translated;
  }
  static string FitStoryText(Text text,string translated) {
   if(!text.supportRichText||(text.text.Length<100&&FormBoundary(text)==null))return translated;
   Transform story=text.transform;
   while(story!=null&&story.parent!=null&&story.parent.name!="Story Objects Holder")story=story.parent;
   if(story==null||story.parent==null)return translated;
   if(story.name=="s-0267"||story.name=="s-0198")return translated; // do not shrink the measured illustration gaps
   var rect=text.rectTransform.rect;
   if(rect.width<=0||rect.height<=0)return translated;
   float available=StoryAvailableHeight(text);
   Font original;if(!Originals.TryGetValue(text,out original)||original==null)return translated;
   var settings=ChineseSettings(text,new Vector2(rect.width,0));
   settings.horizontalOverflow=HorizontalWrapMode.Wrap;
   float scale=Mathf.Max(0.001f,text.pixelsPerUnit);
   string key=text.text+"|"+translated+"|"+JsonConvert.SerializeObject(new{width=Math.Round(rect.width,2),height=Math.Round(available,2),scale=scale,font=original.name,cjk=Chinese.name,size=text.fontSize,spacing=text.lineSpacing,style=text.fontStyle,bestFit=text.resizeTextForBestFit,min=text.resizeTextMinSize,max=text.resizeTextMaxSize});
   bool hardNote=!float.IsPositiveInfinity(PaperAvailableHeight(text))||(IsLicenseText(text)&&text.name=="text");
   key+="|all-fields-v4";
   if(IsLicenseText(text))key+="|license-fields-v1";
   if(hardNote)key+="|paper-visible-boundary-v2";
   if(FormBoundary(text)!=null)key+="|form-border-v2";
   string cached;if(StoryLayouts.TryGetValue(key,out cached))return cached;
   string result=translated;
   using(var generator=new TextGenerator()) {
    settings.font=original;settings.lineSpacing=text.lineSpacing;
    float english=generator.GetPreferredHeight(text.text,settings)/scale;
    settings.font=Chinese;settings.lineSpacing=Mathf.Max(1,text.lineSpacing);
    float height=generator.GetPreferredHeight(translated,settings)/scale;
    // Some original labels intentionally overflow (for example repeated date text).
    // Preserve that authored allowance; prevent the translation from adding overflow.
    // Police-form borders are hard limits, even when the English text overflows.
    float budget=FormBoundary(text)!=null||hardNote?available:Mathf.Max(16,english);
    if(story.name=="s-0097"||story.name=="s-0020")budget=Mathf.Min(budget,available);
    // A one-line form section cannot reserve a separate line for its heading.
    // Keep short answers beside the independent heading, with a measured indent.
    if(FormBoundary(text)!=null&&height>budget+2&&translated.StartsWith("\n",StringComparison.Ordinal)&&translated.IndexOf('\n',1)<0) {
     var headingTransform=text.transform.parent.Find(text.name.Replace("Text","Statement"));
     var heading=headingTransform==null?null:headingTransform.GetComponent<Text>();
     if(heading!=null) {
      string headingText;if(!Words.TryTranslate(heading.text,out headingText))headingText=heading.text;
      var headingSettings=ChineseSettings(heading,new Vector2(10000,0));headingSettings.font=Chinese;
      float headingWidth=generator.GetPreferredWidth(headingText,headingSettings)/Mathf.Max(0.001f,heading.pixelsPerUnit);
      float spaceWidth=generator.GetPreferredWidth("\u3000",settings)/scale;
      if(spaceWidth>0) {
       string inline=new string('\u3000',Mathf.CeilToInt((headingWidth+16)/spaceWidth))+translated.Substring(1);
       float inlineHeight=generator.GetPreferredHeight(inline,settings)/scale;
       if(inlineHeight<=budget+2){result=inline;height=inlineHeight;}
      }
     }
    }
    if(height>budget+2) {
     for(int size=text.fontSize-1;size>=12;size--) {
      float ratio=(float)size/text.fontSize;
      string inner=Regex.Replace(translated,@"<size=(\d+)>",m=>"<size="+Mathf.Max(12,Mathf.FloorToInt(int.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture)*ratio)).ToString(CultureInfo.InvariantCulture)+">");
      string candidate="<size="+size.ToString(CultureInfo.InvariantCulture)+">"+inner+"</size>";
      if(generator.GetPreferredHeight(candidate,settings)/scale<=budget) {result=candidate;break;}
     }
    }
   }
   if(StoryLayouts.Count>8192)StoryLayouts.Clear();StoryLayouts[key]=result;return result;
  }
  static string FitNewspaperIntro(Text text,string translated) {
   if(text.name!="Text Page 1 (6)"||!text.supportRichText)return translated;
   Transform story=text.transform;
   while(story!=null&&story.name!="s-0224"&&story.name!="s-0225"&&story.name!="c-0037")story=story.parent;
   if(story==null)return translated;
   var settings=ChineseSettings(text,text.rectTransform.rect.size);
   settings.resizeTextForBestFit=false;settings.verticalOverflow=VerticalWrapMode.Overflow;
   float available=text.rectTransform.rect.height-4;
   var next=text.transform.parent.Find("content") as RectTransform;
   if(next!=null) {
    float gap=text.rectTransform.anchoredPosition.y-next.anchoredPosition.y;
    if(gap>0)available=Mathf.Min(available,gap-8);
   }
   using(var generator=new TextGenerator())for(int size=text.fontSize;size>=12;size--) {
    string rendered="<size="+size.ToString(CultureInfo.InvariantCulture)+">"+translated.Replace("\r","").Replace("\n","")+"</size>";
    if(generator.GetPreferredHeight(rendered,settings)/Mathf.Max(0.001f,text.pixelsPerUnit)<=available)
     return rendered;
   }
   return translated;
  }
  static string FitMusterRow(Text text,string translated) {
   if(!text.name.StartsWith("person",StringComparison.Ordinal)||!text.supportRichText)return translated;
   Transform story=text.transform;while(story!=null&&story.name!="c-0024")story=story.parent;
   if(story==null)return translated;
   var parts=translated.Replace("\r","").Split(new[]{'\n'},2);
   if(parts.Length!=2)return translated;
   int split=parts[0].LastIndexOf('　');if(split<0)return translated;
   string name=parts[0].Substring(0,split),age=parts[0].Substring(split+1),reason=parts[1];
   var settings=ChineseSettings(text,text.rectTransform.rect.size);
   settings.resizeTextForBestFit=false;settings.horizontalOverflow=HorizontalWrapMode.Overflow;
   float scale=Mathf.Max(0.001f,text.pixelsPerUnit);
   using(var generator=new TextGenerator()) {
    for(int size=14;size>=8;size--) {
     settings.fontSize=size;
     float nameWidth=generator.GetPreferredWidth(name,settings)/scale;
     float ageWidth=generator.GetPreferredWidth(age,settings)/scale;
     float reasonWidth=generator.GetPreferredWidth(reason,settings)/scale;
     if(nameWidth>172||ageWidth>52||reasonWidth>text.rectTransform.rect.width-244)continue;
     float space=(generator.GetPreferredWidth("X X",settings)-generator.GetPreferredWidth("XX",settings))/scale;
     if(space<=0)return translated;
     string line=name+new string(' ',Mathf.Max(1,Mathf.RoundToInt((180-nameWidth)/space)))+age+new string(' ',Mathf.Max(1,Mathf.RoundToInt((60-ageWidth)/space)))+reason;
     return "<size="+size.ToString(CultureInfo.InvariantCulture)+">"+line+"</size>";
    }
   }
   return translated;
  }
  static string FitIllustratedStory(Text text,string translated) {
   var story=StoryRoot(text);
   bool shield=story!=null&&story.name=="s-0198"&&text.name=="Message";
   bool email=story!=null&&story.name=="s-0267"&&text.name=="text"&&text.transform.parent!=null&&text.transform.parent.name=="screen";
   if(!shield&&!email)return translated;
   var gaps=Regex.Matches(translated,@"\n{5,}");
   if(gaps.Count!=(shield?1:2))return translated;
   var settings=ChineseSettings(text,text.rectTransform.rect.size);
   settings.verticalOverflow=VerticalWrapMode.Overflow;
   settings.horizontalOverflow=HorizontalWrapMode.Wrap;
   settings.textAnchor=TextAnchor.UpperLeft;
   string key=translated+"|illustration-wrap-v4|"+story.name+"|"+JsonConvert.SerializeObject(new{width=Math.Round(text.rectTransform.rect.width,2),scale=text.pixelsPerUnit,font=text.font.name,size=text.fontSize,spacing=text.lineSpacing,style=text.fontStyle,bestFit=text.resizeTextForBestFit,min=text.resizeTextMinSize,max=text.resizeTextMaxSize});
   string cached;if(IllustratedLayouts.TryGetValue(key,out cached))return cached;
   string result="";int cursor=0;
   using(var generator=new TextGenerator()) {
    for(int i=0;i<gaps.Count;i++) {
     var gap=gaps[i];result+=translated.Substring(cursor,gap.Index-cursor);
     // Logical text offsets, including clearance below the authored illustration.
     // s-0198 needs a fixed visual gap even when font rasterization changes.
     float desired=shield?510f:i==0?1000f:1770f;
     string prefix=result+"\n\n";
     generator.Populate(prefix+"字",settings);
     if(generator.lineCount<2)return translated;
     var lines=generator.lines;float scale=Mathf.Max(0.001f,text.pixelsPerUnit);
     float current=-lines[lines.Count-1].topY/scale;
     float step=(lines[lines.Count-2].topY-lines[lines.Count-1].topY)/scale;
     if(step<=0)return translated;
     int extra=Mathf.Clamp(Mathf.CeilToInt((desired-current)/step),0,160);
     result=prefix+new string('\n',extra);cursor=gap.Index+gap.Length;
    }
   }
   result+=translated.Substring(cursor);
   if(IllustratedLayouts.Count>32)IllustratedLayouts.Clear();
   IllustratedLayouts[key]=result;return result;
  }
  static void BeforeFont(Text __instance,ref Font value) {
   if(changingFont||!Active||__instance==null||Chinese==null)return;
   bool tracked=Originals.ContainsKey(__instance);
   if(value!=Chinese&&tracked)Originals[__instance]=value;
   if(!__instance.isActiveAndEnabled)return;
   if(!tracked){PendingFonts.Add(__instance);return;}
   string translated;
   if(Words.TryTranslate(__instance.text,out translated)) {
    if(value!=Chinese)Originals[__instance]=value;
    value=Chinese;
   }
  }
  static void ApplyFont(Text text) {
   if(text.font==Chinese)return;
   Originals[text]=text.font;
   changingFont=true;
   try{text.font=Chinese;}finally{changingFont=false;}
  }
  static void PrepareFont(Text __instance) {
   if(Active&&__instance!=null&&__instance.isActiveAndEnabled)PendingFonts.Add(__instance);
  }
  static void PrepareVisibleFont(Text __instance) {
   string translated;
   if(Active&&__instance!=null&&__instance.isActiveAndEnabled&&Words.TryTranslate(__instance.text,out translated)) {
    ApplyFont(__instance);PrepareDecoration(__instance);
   }
  }
  static void PrepareDecoration(Text body) {
   var root=StoryRoot(body);
   if(root!=null&&(root.name=="s-0224"||root.name=="s-0225")&&body.name=="content") {
    foreach(Transform marker in body.transform.parent)if(marker.name.StartsWith("super",StringComparison.Ordinal)) {
     var label=marker.GetComponent<Text>();if(label==null)continue;
     if(!DropCapOriginals.ContainsKey(label))DropCapOriginals[label]=label.enabled;
     label.enabled=false;
    }
   }
   if(root!=null&&root.name=="s-0221"&&body.name=="content (5)") {
    // Word references are now attached to their words, so the old absolute
    // superscript positions must not be drawn over the translated worksheet.
    foreach(string path in new[]{"piecs","content (3)"}) {
     var group=body.transform.parent.Find(path);if(group==null)continue;
     foreach(var label in group.GetComponentsInChildren<Text>(true)) {
      if(label.transform==group)continue;
      if(!DropCapOriginals.ContainsKey(label))DropCapOriginals[label]=label.enabled;
      label.enabled=false;
     }
    }
   }
   if(IsLicenseText(body)&&body.name=="text") {
    foreach(string lineName in new[]{"horizontal line","horizontal line (1)"}) {
     var line=body.transform.parent.Find(lineName);var image=line==null?null:line.GetComponent<Image>();
     if(image!=null){if(!DropCapOriginals.ContainsKey(image))DropCapOriginals[image]=image.enabled;image.enabled=false;}
    }
   }
   string name=body.name;
   if(name!="content"&&!name.StartsWith("descriptionC",StringComparison.Ordinal))return;
   Transform story=body.transform;
   while(story!=null&&story.name!="s-0224"&&story.name!="s-0225"&&story.name!="c-0037"&&story.name!="c-0030")story=story.parent;
   if(story==null)return;
   var child=body.transform.parent.Find(name=="content"?"fancy f":name.Replace("descriptionC","dropC"));
   if(child==null)return;var picture=child.GetComponent<Image>();if(picture==null)return;
   if(!DropCapOriginals.ContainsKey(picture))DropCapOriginals[picture]=picture.enabled;
   picture.enabled=false;
  }
  static void BeforeMeasure(ref string bonus_text,Text popup_block_text) {
   string translated;
   if(Active&&Words.TryTranslate(bonus_text,out translated)) {
    bonus_text=translated;
    if(popup_block_text!=null)ApplyFont(popup_block_text);
   }
  }
  static void BeforeNotificationMeasure(object __instance,ref string __0) {
   string translated;
   if(!Active||!Words.TryTranslate(__0,out translated))return;
   __0=translated;
   var text=AccessTools.Field(__instance.GetType(),"NotificationText").GetValue(__instance) as Text;
   if(text!=null)ApplyFont(text);
  }
  static Text TooltipText(object instance,bool detail) {
   string field=detail?(instance.GetType().Name=="SpeedTooltip"?"tooltip_boost_text":"tooltip_breakdown_text"):"tooltip_value_text";
   return (Text)AccessTools.Field(instance.GetType(),field).GetValue(instance);
  }
  static void TranslateTooltipMeasure(object instance,bool detail,ref string source) {
   string translated;
   if(!Active||!Words.TryTranslate(source,out translated))return;
   source=translated;
   var text=TooltipText(instance,detail);
   if(text!=null)ApplyFont(text);
  }
  static void BeforeTooltipHeight(object __instance,ref string __0) {TranslateTooltipMeasure(__instance,true,ref __0);}
  static void BeforeTooltipWidth(object __instance,ref string __0,ref string __1) {
   TranslateTooltipMeasure(__instance,false,ref __0);TranslateTooltipMeasure(__instance,true,ref __1);
  }
  static void BeforeReadableStart(Component __instance) {
   var text=__instance.GetComponent<Text>();Font original;
   if(text==null||text.font!=Chinese||!Originals.TryGetValue(text,out original))return;
   changingFont=true;
   try{text.font=original;}finally{changingFont=false;}
  }
  static Exception AfterReadableStart(Component __instance,Exception __exception) {
   var text=__instance.GetComponent<Text>();
   if(text!=null)PrepareFont(text);
   return __exception;
  }
  // Text.OnEnable already queues new scene text. A global scene scan duplicates
  // that work and also touches thousands of hidden prefab/notification objects.
  void SceneLoaded(Scene scene,LoadSceneMode mode){Rendered.Clear();}
  void Refresh() {
   Rendered.Clear();
   TemplateLayouts.Clear();
   foreach(var pair in DropCapOriginals.ToArray()) {
    if(pair.Key==null){DropCapOriginals.Remove(pair.Key);continue;}
    pair.Key.enabled=pair.Value;
   }
   // Event-driven only: never scan thousands of inactive story objects every frame.
   changingFont=true;
   try {
    foreach(var pair in Originals.ToArray()) {
     if(pair.Key==null){Originals.Remove(pair.Key);continue;}
     if(!Active&&pair.Key.font==Chinese)pair.Key.font=pair.Value;
    }
   } finally {changingFont=false;}
   foreach(Text text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))if(text!=null&&text.isActiveAndEnabled){PrepareFont(text);text.SetAllDirty();}
   // Existing notifications need new geometry when F8/F9 changes their display text.
   foreach(string typeName in new[]{"SpeedTooltip","ResourceBreakdownTooltip"}) {
    var type=AccessTools.TypeByName(typeName);
    foreach(var component in Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>()) {
     if(component==null||!component.gameObject.scene.IsValid())continue;
     var rect=AccessTools.Field(type,"rt").GetValue(component) as RectTransform;
     if(rect==null||!rect.gameObject.activeInHierarchy)continue;
     var value=TooltipText(component,false);var detail=TooltipText(component,true);
     if(value!=null&&detail!=null)AccessTools.Method(type,"SetContent").Invoke(component,new object[]{value.text,detail.text});
    }
   }
   // SetText only sizes TextArea and reassigns the same English source string.
   var notification=AccessTools.TypeByName("NotificationTop");
   if(notification!=null) {
    var stored=AccessTools.Field(notification,"notification_text");
    var setText=AccessTools.Method(notification,"SetText");
    foreach(var component in Resources.FindObjectsOfTypeAll(notification).Cast<MonoBehaviour>()) {
     if(component==null||!component.gameObject.scene.IsValid())continue;
     var source=stored.GetValue(component) as string;
     if(!string.IsNullOrEmpty(source))setText.Invoke(component,new object[]{source});
    }
   }
  }
  void Update() {
   if(harmony==null)return;
   if(layoutCacheTask!=null&&layoutCacheTask.IsCompleted) {
    try {
     var cache=layoutCacheTask.Result;
     if(cache!=null) {
      foreach(var pair in cache.stories)StoryLayouts[pair.Key]=pair.Value;
      foreach(var pair in cache.illustrated)IllustratedLayouts[pair.Key]=pair.Value;
     }
    }catch(Exception error){Logger.LogWarning("Precomputed layout cache unavailable; measuring normally: "+error.Message);}
    finally{layoutCacheTask=null;}
   }
   if(Input.GetKeyDown(KeyCode.F8)) {Active=!Active;Refresh();Logger.LogInfo(Active?"Traditional Chinese enabled":"Original English enabled");}
   if(Input.GetKeyDown(KeyCode.F9)) {
    try{Words=Catalog.Load(Path.Combine(folder,"translations"));Refresh();Logger.LogInfo("Reloaded "+Words.Count+" translations.");}
    catch(Exception e){Logger.LogError("Translation reload failed; previous catalog retained. "+e.Message);}
   }
   if(Time.unscaledTime>=nextFlush) {
    nextFlush=Time.unscaledTime+30;
    foreach(Text dead in Originals.Keys.Where(x=>x==null).ToArray())Originals.Remove(dead);
    foreach(Text dead in Rendered.Keys.Where(x=>x==null).ToArray())Rendered.Remove(dead);
    foreach(Text dead in StoryRoots.Keys.Where(x=>x==null).ToArray())StoryRoots.Remove(dead);
    foreach(Text dead in CaptionBudgets.Keys.Where(x=>x==null).ToArray())CaptionBudgets.Remove(dead);
    foreach(Text dead in HeaderFits.Keys.Where(x=>x==null).ToArray())HeaderFits.Remove(dead);
    foreach(Text dead in TemplateLayouts.Keys.Where(x=>x==null).ToArray())TemplateLayouts.Remove(dead);
    foreach(Text dead in FormBoundaries.Keys.Where(x=>x==null).ToArray())FormBoundaries.Remove(dead);
    if(logMissing.Value&&Missing.Count>0)File.WriteAllText(Path.Combine(folder,"missing.json"),JsonConvert.SerializeObject(Missing,Formatting.Indented),new UTF8Encoding(false));
   }
   if(testCommands.Value&&Time.unscaledTime>=nextTest) {
    nextTest=Time.unscaledTime+2;
    string path=Path.Combine(folder,"test-request.json");
    if(File.Exists(path)) {
     string request=File.ReadAllText(path);
     if(request!=lastCommand){lastCommand=request;try{RunTest(JsonConvert.DeserializeObject<Dictionary<string,string>>(request));}catch(Exception e){Logger.LogError("Diagnostic request failed: "+e);}}
    }
   }
  }
  void LateUpdate() {
   if(!Active){PendingFonts.Clear();return;}
   // Initialization briefly enables many prefab texts before hiding their pages.
   // Wait until the frame's final visibility is known before tracking a CJK font.
   foreach(var text in PendingFonts)if(text!=null)PrepareVisibleFont(text);
   PendingFonts.Clear();
  }
  void RunTest(Dictionary<string,string> request) {
   string command=request["command"];
   if(command=="resolution") {
    int width=int.Parse(request["width"],CultureInfo.InvariantCulture),height=int.Parse(request["height"],CultureInfo.InvariantCulture);
    if(!((width==1280&&height==720)||(width==1920&&height==1080)))throw new InvalidDataException("Unsupported diagnostic resolution.");
    Screen.SetResolution(width,height,false);
   }
   if(command=="audit-story-layout"){StartCoroutine(AuditStoryLayout(request["id"]));return;}
   if(command=="audit-note-layout"){StartCoroutine(AuditStoryLayout(request["id"],true));return;}
   if(command=="audit-all-fields"){StartCoroutine(AuditAllFields(request["id"]));return;}
   if(command=="preview-variable-tooltip") {
    if(diagnosticPreview!=null)Destroy(diagnosticPreview);
    string typeName=request["type"];
    if(typeName!="SpeedTooltip"&&typeName!="ResourceBreakdownTooltip")throw new InvalidDataException("Unsupported variable tooltip.");
    Active=request["language"]!="en";Refresh();
    var type=AccessTools.TypeByName(typeName);
    var source=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
    var sourceObject=(GameObject)AccessTools.Field(type,"Tooltip").GetValue(source);
    var canvas=Resources.FindObjectsOfTypeAll<Canvas>().First(x=>x.gameObject.scene.IsValid()&&x.name=="Canvas L2");
    diagnosticPreview=new GameObject("Localization Variable Tooltip",typeof(RectTransform),typeof(CanvasGroup));
    diagnosticPreview.SetActive(false);
    var root=(RectTransform)diagnosticPreview.transform;root.SetParent(canvas.transform,false);
    root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
    diagnosticPreview.GetComponent<CanvasGroup>().blocksRaycasts=false;
    var clone=Instantiate(sourceObject,root,false);RestorePreviewFonts(sourceObject,clone);
    var oldFields=new Dictionary<System.Reflection.FieldInfo,object>();
    try {
     foreach(string name in new[]{"rt","tooltip_value_text",typeName=="SpeedTooltip"?"tooltip_boost_text":"tooltip_breakdown_text","horizontal_line"}) {
      if(name=="horizontal_line"&&typeName!="SpeedTooltip")continue;
      var field=AccessTools.Field(type,name);if(field==null)continue;
      object old=field.GetValue(source);oldFields[field]=old;
      if(name=="rt")field.SetValue(source,clone.GetComponent<RectTransform>());
      else {
       var component=old as Component;
       if(component==null)throw new InvalidOperationException("Missing tooltip field "+name);
       var originals=sourceObject.GetComponentsInChildren(component.GetType(),true);
       var copies=clone.GetComponentsInChildren(component.GetType(),true);
       int index=Array.IndexOf(originals,component);
       if(index<0||index>=copies.Length)throw new InvalidOperationException("Tooltip field outside clone "+name);
       field.SetValue(source,copies[index]);
      }
     }
     // The inspected method only measures, sizes, assigns text and toggles a line.
     // Temporarily redirect those UI fields to the inactive clone; never call Open/Update.
     AccessTools.Method(type,"SetContent").Invoke(source,new object[]{request["value"],request["detail"]});
    }finally{foreach(var pair in oldFields)pair.Key.SetValue(source,pair.Value);}
    foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
     if(behaviour!=null&&behaviour.GetType().Assembly.GetName().Name=="Assembly-CSharp")DestroyImmediate(behaviour);
    foreach(var selectable in clone.GetComponentsInChildren<Selectable>(true))selectable.interactable=false;
    var rect=(RectTransform)clone.transform;
    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=Vector2.zero;
    clone.SetActive(true);diagnosticPreview.SetActive(true);Refresh();
   }
   if(command=="close-preview-ui"&&diagnosticPreview!=null)Destroy(diagnosticPreview);
   if(command=="preview-card"||command=="preview-tooltip") {
    if(diagnosticPreview!=null)Destroy(diagnosticPreview);
    string typeName=request.ContainsKey("card-type")?request["card-type"]:"ScavengeCard";
    var allowed=command=="preview-card"?new[]{"ScavengeCard","WorkLocationCard","NatureLocationCard","ArtifactCard","GovernmentCard","ReligionCard","ScienceCard","CharacterPeriodCard","CharacterSelectorButton"}:new[]{"HumanActionPlateHoverTooltip","PlantActionPlateHoverTooltip","AnimalActionPlateHoverTooltip","SpeedTooltip"};
    if(!allowed.Contains(typeName))throw new InvalidDataException("Unsupported preview type.");
    var cardType=AccessTools.TypeByName(typeName);
    var source=Resources.FindObjectsOfTypeAll(cardType).Cast<MonoBehaviour>().FirstOrDefault(x=>x.gameObject.scene.IsValid()&&(command=="preview-tooltip"||x.name==request["card"]));
    GameObject sourceObject;
    if(command=="preview-tooltip")sourceObject=source==null?null:(GameObject)AccessTools.Field(cardType,"Tooltip").GetValue(source);
    else sourceObject=source==null?(typeName=="CharacterPeriodCard"?Resources.Load<GameObject>("Character Pages/Character Period Card"):typeName=="CharacterSelectorButton"?Resources.Load<GameObject>("Common Elements/Character Button"):null):source.gameObject;
    if(sourceObject==null)throw new InvalidDataException("No native preview source for "+typeName);
    var canvas=Resources.FindObjectsOfTypeAll<Canvas>().First(x=>x.gameObject.scene.IsValid()&&x.name=="Canvas L2");
    diagnosticPreview=new GameObject("Localization Card Preview",typeof(RectTransform),typeof(CanvasGroup));
    diagnosticPreview.SetActive(false);
    var root=(RectTransform)diagnosticPreview.transform;root.SetParent(canvas.transform,false);
    root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
    diagnosticPreview.GetComponent<CanvasGroup>().blocksRaycasts=false;
    var clone=Instantiate(sourceObject,root,false);
    RestorePreviewFonts(sourceObject,clone);
    foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
     if(behaviour!=null&&behaviour.GetType().Assembly.GetName().Name=="Assembly-CSharp")DestroyImmediate(behaviour);
    foreach(var selectable in clone.GetComponentsInChildren<Selectable>(true))selectable.interactable=false;
    var rect=(RectTransform)clone.transform;
    var dimensions=((RectTransform)sourceObject.transform).rect.size;
    rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=dimensions;
    if(typeName=="CharacterPeriodCard") {
     // Timeline rows are wider than a viewport; show their left-hand name panel.
     rect.pivot=new Vector2(0,0.5f);rect.anchoredPosition=new Vector2(-((RectTransform)canvas.transform).rect.width/2+300,0);
    }
    if(typeName=="CharacterSelectorButton") {
     // The prefab's placeholder overlay is normally cleared by Init. Avoid
     // gameplay initialization while making its native text rows visible.
     foreach(var selectable in clone.GetComponentsInChildren<Selectable>(true))if(selectable.targetGraphic!=null)selectable.targetGraphic.enabled=false;
     foreach(string childName in new[]{"HiddenBanner","TaskView"}) {
      var child=clone.transform.Find(childName);if(child!=null)child.gameObject.SetActive(false);
     }
     var background=clone.GetComponent<Image>()??clone.AddComponent<Image>();background.color=new Color(0.44f,0.30f,0.24f,1);background.raycastTarget=false;
    }
    if(request.ContainsKey("title"))clone.transform.Find("Title").GetComponent<Text>().text=request["title"];
    if(request.ContainsKey("description")) {
     var popup=clone.transform.Find("Image Mask/Popup Text");popup.gameObject.SetActive(true);
     popup.Find("Text").GetComponent<Text>().text=request["description"];
    }
    if(request.ContainsKey("values"))foreach(var pair in JsonConvert.DeserializeObject<Dictionary<string,string>>(request["values"])) {
     var child=clone.transform.Find(pair.Key);
     if(child==null||child.GetComponent<Text>()==null)throw new InvalidDataException("Unknown preview Text path: "+pair.Key);
     child.GetComponent<Text>().text=pair.Value;
    }
    clone.SetActive(true);diagnosticPreview.SetActive(true);Refresh();
   }
   if(command=="preview-notification") {
    if(diagnosticPreview!=null)Destroy(diagnosticPreview);
    var type=AccessTools.TypeByName("NotificationTop");
    var source=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
    var canvas=source.GetComponentInParent<Canvas>(true);
    diagnosticPreview=new GameObject("Localization Diagnostic Preview",typeof(RectTransform),typeof(CanvasGroup));
    diagnosticPreview.SetActive(false);
    var root=(RectTransform)diagnosticPreview.transform;root.SetParent(canvas.transform,false);
    root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=Vector2.zero;root.offsetMax=Vector2.zero;
    diagnosticPreview.GetComponent<CanvasGroup>().blocksRaycasts=false;
    // Instantiate under an inactive parent: game behaviours never enter Awake/OnEnable.
    var clone=Instantiate(source.gameObject,root,false);
    RestorePreviewFonts(source.gameObject,clone);
    var component=clone.GetComponent(type);
    AccessTools.Method(type,"SetText").Invoke(component,new object[]{request["text"]});
    foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
     if(behaviour!=null&&behaviour.GetType().Assembly.GetName().Name=="Assembly-CSharp")DestroyImmediate(behaviour);
    foreach(var selectable in clone.GetComponentsInChildren<Selectable>(true))selectable.interactable=false;
    var rect=(RectTransform)clone.transform;
    rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.anchoredPosition=Vector2.zero;
    clone.SetActive(true);diagnosticPreview.SetActive(true);Refresh();
   }
   if(command=="measure-notification") {
    var type=AccessTools.TypeByName("NotificationTop");
    var instance=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
    var method=AccessTools.Method(type,"CalculateWidthOfText");
    var label=(Text)AccessTools.Field(type,"NotificationText").GetValue(instance);
    string originalLabel=label.text;
    bool wasActive=Active;
    var results=new List<object>();
    try {
     foreach(string source in new[]{"New Everly Praetis story unlocked.","New Romina Zappa story unlocked.","UNKNOWN ORIGINAL TEXT"}) {
      string translated;bool hit=Words.TryTranslate(source,out translated);
      Active=true;
      float patched=Convert.ToSingle(method.Invoke(instance,new object[]{source}));
      Active=false;
      float expected=Convert.ToSingle(method.Invoke(instance,new object[]{hit?translated:source}));
      if(float.IsNaN(patched)||Math.Abs(patched-expected)>0.01f||label.text!=originalLabel)throw new InvalidOperationException("Notification measurement or source preservation failed.");
      results.Add(new{source=source,display=hit?translated:source,patchedWidth=patched,expectedWidth=expected,sourcePreserved=label.text==originalLabel});
     }
    }finally{Active=wasActive;Refresh();}
    string path=Path.Combine(folder,"diagnostics");Directory.CreateDirectory(path);
    File.WriteAllText(Path.Combine(path,Regex.Replace(request["id"],"[^a-zA-Z0-9_-]","")+"-measurement.json"),JsonConvert.SerializeObject(results,Formatting.Indented),new UTF8Encoding(false));
   }
   if(command=="quit"){Application.Quit();return;}
   if(command=="measure-tooltips") {
    var results=new List<object>();bool wasActive=Active;
    try {
     foreach(string typeName in new[]{"SpeedTooltip","ResourceBreakdownTooltip"}) {
      var type=AccessTools.TypeByName(typeName);
      var instance=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
      var value=TooltipText(instance,false);var detail=TooltipText(instance,true);
      string originalValue=value.text,originalDetail=detail.text;
      foreach(string source in new[]{"SPEED BOOST(S)","CURRENT ERA (Worm 83)","UNKNOWN ORIGINAL TEXT",""}) {
       string translated;bool hit=Words.TryTranslate(source,out translated);
       foreach(string methodName in new[]{"CalculateHeightOfText","CalculateWidthOfText"}) {
        var method=AccessTools.Method(type,methodName);
        Active=true;
        float patched=Convert.ToSingle(method.Invoke(instance,methodName=="CalculateHeightOfText"?new object[]{source}:new object[]{source,source}));
        Active=false;
        string display=hit?translated:source;
        float expected=Convert.ToSingle(method.Invoke(instance,methodName=="CalculateHeightOfText"?new object[]{display}:new object[]{display,display}));
        bool preserved=value.text==originalValue&&detail.text==originalDetail;
        if(float.IsNaN(patched)||Math.Abs(patched-expected)>0.01f||!preserved)throw new InvalidOperationException("Tooltip measurement or source preservation failed.");
        results.Add(new{type=typeName,method=methodName,source=source,display=display,patched=patched,expected=expected,sourcePreserved=preserved});
       }
      }
     }
    }finally{Active=wasActive;Refresh();}
    string path=Path.Combine(folder,"diagnostics");Directory.CreateDirectory(path);
    File.WriteAllText(Path.Combine(path,Regex.Replace(request["id"],"[^a-zA-Z0-9_-]","")+"-measurement.json"),JsonConvert.SerializeObject(results,Formatting.Indented),new UTF8Encoding(false));
   }
   if(command=="toggle"){Active=!Active;Refresh();}
   if(command=="reload"){Words=Catalog.Load(Path.Combine(folder,"translations"));Refresh();}
   if(command=="scroll-preview") {
    var type=AccessTools.TypeByName("PageManagement");
    var manager=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
    var popup=(GameObject)AccessTools.Field(type,"StoryAndSkillsPopup").GetValue(manager);
    if(!popup.activeInHierarchy)throw new InvalidOperationException("Open a story preview before scrolling.");
    float position=float.Parse(request["position"],CultureInfo.InvariantCulture);
    if(float.IsNaN(position)||float.IsInfinity(position)||position<0||position>1)throw new InvalidDataException("Scroll position must be between 0 and 1.");
    Canvas.ForceUpdateCanvases();
    foreach(var scroll in popup.GetComponentsInChildren<ScrollRect>()) {
     scroll.StopMovement();scroll.verticalNormalizedPosition=position;
    }
   }
   if(command=="preview-skills") {
    var type=AccessTools.TypeByName("PageManagement");
    var manager=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
    var popup=(GameObject)AccessTools.Field(type,"StoryAndSkillsPopup").GetValue(manager);
    var holder=(Transform)AccessTools.Field(type,"StoryObjectsHolder").GetValue(manager);
    foreach(Transform child in holder)child.gameObject.SetActive(false);
    popup.SetActive(true);
    var skills=(GameObject)AccessTools.Field(type,"SkillsPopup").GetValue(manager);skills.SetActive(true);
    // Native placeholders contain both ordinary and zero-state rows. Real game
    // logic selects one; this display-only preview shows the ordinary rows.
    foreach(Transform child in skills.transform.Find("Content"))if(child.name.EndsWith(" Zero",StringComparison.Ordinal))child.gameObject.SetActive(false);
    if(request.ContainsKey("values"))foreach(var pair in JsonConvert.DeserializeObject<Dictionary<string,string>>(request["values"])) {
     var child=skills.transform.Find(pair.Key);
     if(child==null||child.GetComponent<Text>()==null)throw new InvalidDataException("Unknown skills Text path: "+pair.Key);
     child.GetComponent<Text>().text=pair.Value;
    }
    Active=true;Refresh();
   }
   if(command=="preview-story"||command=="close-preview") {
    var type=AccessTools.TypeByName("PageManagement");
    var manager=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
    var popup=(GameObject)AccessTools.Field(type,"StoryAndSkillsPopup").GetValue(manager);
    if(command=="close-preview")popup.SetActive(false);
    else {
     // Display-only QA: deliberately do NOT call ShowEvent, mark stories read,
     // change unlock flags, load a save, grant achievements, or advance gameplay.
     var holder=(Transform)AccessTools.Field(type,"StoryObjectsHolder").GetValue(manager);
     var story=holder.Find(request["story"]);
     if(story==null)throw new InvalidDataException("Unknown story object");
     foreach(Transform child in holder)child.gameObject.SetActive(false);
     popup.SetActive(true);
     ((GameObject)AccessTools.Field(type,"SkillsPopup").GetValue(manager)).SetActive(false);
     story.gameObject.SetActive(true);
     foreach(string name in new[]{"Name","Subtitle"}) {
      var label=story.Find("Name Plaque/"+name);var shadow=story.Find("Name Plaque/"+name+" Shadow");
      if(label!=null&&shadow!=null)label.GetComponent<Text>().text=shadow.GetComponent<Text>().text;
     }
     foreach(string name in new[]{"Name","Subtitle"})if(request.ContainsKey(name)) {
      string value=request[name],translated;
      if(!Words.TryTranslate(value,out translated))throw new InvalidDataException("Preview plaque must use a catalog source.");
      foreach(string suffix in new[]{""," Shadow"}) {
       var label=story.Find("Name Plaque/"+name+suffix);if(label!=null)label.GetComponent<Text>().text=value;
      }
     }
     Active=true;Refresh();
    }
   }
   if(command=="readable") {
    var field=AccessTools.Field(AccessTools.TypeByName("ReadableFontController"),"readable_mode_on");
    field.SetValue(null,!(bool)field.GetValue(null));Refresh();
   }
   if(command=="page") {
    string name=request["page"];
    if(name!="Settings"&&name!="HelpManual"&&name!="MainMenu")throw new InvalidOperationException("Only non-gameplay pages are allowed for this diagnostic.");
    var type=AccessTools.TypeByName("PageManagement");
    var manager=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
    AccessTools.Method(type,"SelectPage").Invoke(manager,new object[]{Enum.Parse(AccessTools.TypeByName("Page"),name)});
   }
   StartCoroutine(Snapshot(request["id"]));
  }
  static void RestorePreviewFonts(GameObject source,GameObject clone) {
   // Native Instantiate copies the current CJK font but cannot copy our dictionary.
   // Seed a diagnostic clone with each corresponding genuine source font first.
   var originals=source.GetComponentsInChildren<Text>(true);
   var copies=clone.GetComponentsInChildren<Text>(true);
   if(originals.Length!=copies.Length)throw new InvalidOperationException("Preview text hierarchy mismatch.");
   changingFont=true;
   try {
    for(int i=0;i<copies.Length;i++) {
     Font font;
     if(!Originals.TryGetValue(originals[i],out font))font=originals[i].font;
     if(font==Chinese)throw new InvalidOperationException("Preview source has no recorded original font.");
     copies[i].font=font;
    }
   }finally{changingFont=false;}
  }
  System.Collections.IEnumerator AuditStoryLayout(string id,bool notesOnly=false) {
   var type=AccessTools.TypeByName("PageManagement");
   var manager=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
   var popup=(GameObject)AccessTools.Field(type,"StoryAndSkillsPopup").GetValue(manager);
   var holder=(Transform)AccessTools.Field(type,"StoryObjectsHolder").GetValue(manager);
   var skills=(GameObject)AccessTools.Field(type,"SkillsPopup").GetValue(manager);
   var states=holder.Cast<Transform>().ToDictionary(x=>x,x=>x.gameObject.activeSelf);
   bool oldPopup=popup.activeSelf,oldSkills=skills.activeSelf,oldActive=Active;
   var rows=new List<object>();int stories=0,flags=0;
   try {
    Active=true;popup.SetActive(true);skills.SetActive(false);Refresh();
    foreach(var pair in states)pair.Key.gameObject.SetActive(false);
    foreach(var pair in states) {
     if(notesOnly&&!pair.Key.GetComponentsInChildren<Image>(true).Any(i=>i.name.StartsWith("Note Paper Segment",StringComparison.Ordinal)||(i.sprite!=null&&Regex.IsMatch(i.name,@"^Page \d+$"))))continue;
     var story=pair.Key;story.gameObject.SetActive(true);
     yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
     stories++;
     foreach(var label in story.GetComponentsInChildren<Text>()) {
      string translated;
      if((label.text.Length<100&&FormBoundary(label)==null)||!Words.TryTranslate(label.text,out translated))continue;
      translated=RenderTranslation(label,translated);
      var rect=label.rectTransform.rect;if(rect.width<=0||rect.height<=0)continue;
      Font original;if(!Originals.TryGetValue(label,out original))original=label.font;
      var settings=label.GetGenerationSettings(new Vector2(rect.width,0));
      settings.font=original;
      float english=new TextGenerator().GetPreferredHeight(label.text,settings)/label.pixelsPerUnit;
      settings.font=Chinese;settings.lineSpacing=Mathf.Max(1,label.lineSpacing);settings.horizontalOverflow=HorizontalWrapMode.Wrap;
      float chinese=new TextGenerator().GetPreferredHeight(translated,settings)/label.pixelsPerUnit;
      float available=StoryAvailableHeight(label);
      bool formBorder=FormBoundary(label)!=null;
      bool paperBoundary=!float.IsPositiveInfinity(PaperAvailableHeight(label));
      bool addedOverflow=chinese>available+2&&(formBorder||paperBoundary||chinese>english+2);
      if(addedOverflow)flags++;
      string path=label.name;for(Transform p=label.transform.parent;p!=null&&p!=holder;p=p.parent)path=p.name+"/"+path;
      rows.Add(new{story=story.name,path=path,sourceLength=label.text.Length,width=rect.width,height=rect.height,availableHeight=available,englishHeight=english,chineseHeight=chinese,formBorder=formBorder,overflow=label.verticalOverflow.ToString(),bestFit=label.resizeTextForBestFit,addedOverflow=addedOverflow});
     }
     story.gameObject.SetActive(false);
    }
   }finally{
    foreach(var pair in states)pair.Key.gameObject.SetActive(pair.Value);
    popup.SetActive(oldPopup);skills.SetActive(oldSkills);Active=oldActive;Refresh();
   }
   string folderPath=Path.Combine(folder,"diagnostics");Directory.CreateDirectory(folderPath);
   File.WriteAllText(Path.Combine(folderPath,Regex.Replace(id,"[^a-zA-Z0-9_-]","")+"-layout.json"),JsonConvert.SerializeObject(new{stories=stories,flags=flags,texts=rows,scope="Native story objects activated without ShowEvent or save loading; compare translated preferred height against available and original height. Flags require inspection."},Formatting.Indented),new UTF8Encoding(false));
   File.WriteAllText(Path.Combine(folderPath,Regex.Replace(id,"[^a-zA-Z0-9_-]","")+"-cache.json"),JsonConvert.SerializeObject(new{stories=StoryLayouts,illustrated=IllustratedLayouts}),new UTF8Encoding(false));
   Logger.LogInfo("Story layout audit complete: "+stories+" stories, "+rows.Count+" texts, "+flags+" flags.");
  }
  static float[][] AuditInk(Text label,Transform root,bool chinese) {
   string value=label.text,translation;
   bool hit=Words.TryTranslate(value,out translation);
   if(chinese&&hit)value=RenderTranslation(label,translation);
   var settings=label.GetGenerationSettings(label.rectTransform.rect.size);
   Font original;settings.font=chinese?label.font:Originals.TryGetValue(label,out original)?original:label.font;
   if(chinese&&hit) {
    settings.horizontalOverflow=HorizontalWrapMode.Wrap;
    settings.lineSpacing=Mathf.Max(1,label.lineSpacing);
    int size=FixedFieldFontSize(label,translation);if(size>0)settings.fontSize=size;
    if(IsNamePlaque(label))settings.richText=true;
    if(IsSelectorLabel(label))settings.textAnchor=TextAnchor.UpperLeft;
   }
   var boxes=new List<float[]>();float scale=Mathf.Max(0.001f,label.pixelsPerUnit);
   using(var generator=new TextGenerator()) {
    generator.Populate(value,settings);var vertices=generator.verts;
    for(int i=0;i+3<vertices.Count;i+=4) {
     float quadWidth=Mathf.Abs(vertices[i+2].position.x-vertices[i].position.x),quadHeight=Mathf.Abs(vertices[i+2].position.y-vertices[i].position.y);
     if(quadWidth<=2.01f&&quadHeight<=2.01f)continue;
     float left=float.PositiveInfinity,bottom=float.PositiveInfinity,right=float.NegativeInfinity,top=float.NegativeInfinity;
     for(int j=0;j<4;j++) {
      var p=root.InverseTransformPoint(label.transform.TransformPoint(vertices[i+j].position/scale));
      left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);bottom=Mathf.Min(bottom,p.y);top=Mathf.Max(top,p.y);
     }
     // Unity emits a tiny blank atlas quad for some whitespace glyphs.
     if(right-left>0.1f&&top-bottom>0.1f&&(right-left>2.01f||top-bottom>2.01f))boxes.Add(new[]{left,bottom,right,top});
    }
   }
   return boxes.ToArray();
  }
  System.Collections.IEnumerator AuditAllFields(string id) {
   var type=AccessTools.TypeByName("PageManagement");
   while(!Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().Any(x=>x.gameObject.scene.IsValid()))yield return null;
   var manager=Resources.FindObjectsOfTypeAll(type).Cast<MonoBehaviour>().First(x=>x.gameObject.scene.IsValid());
   var popup=(GameObject)AccessTools.Field(type,"StoryAndSkillsPopup").GetValue(manager);
   var holder=(Transform)AccessTools.Field(type,"StoryObjectsHolder").GetValue(manager);
   var skills=(GameObject)AccessTools.Field(type,"SkillsPopup").GetValue(manager);
   var states=holder.Cast<Transform>().ToDictionary(x=>x,x=>x.gameObject.activeSelf);
   bool oldPopup=popup.activeSelf,oldSkills=skills.activeSelf,oldActive=Active;
   var rows=new List<object>();int count=0;
   try {
    Active=true;popup.SetActive(true);skills.SetActive(false);Refresh();
    foreach(var pair in states)pair.Key.gameObject.SetActive(false);
    foreach(var pair in states) {
     var story=pair.Key;story.gameObject.SetActive(true);
     yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();count++;
     foreach(var label in story.GetComponentsInChildren<Text>()) {
      if(!label.isActiveAndEnabled||string.IsNullOrWhiteSpace(label.text))continue;
      string translated;bool hit=Words.TryTranslate(label.text,out translated);
      string path=label.name;for(Transform p=label.transform.parent;p!=null&&p!=story;p=p.parent)path=p.name+"/"+path;
      rows.Add(new{story=story.name,path=path,source=label.text,display=hit?RenderTranslation(label,translated):label.text,width=label.rectTransform.rect.width,height=label.rectTransform.rect.height,preferredHeight=label.preferredHeight,availableHeight=StoryAvailableHeight(label),ink=AuditInk(label,story,true),englishInk=AuditInk(label,story,false)});
     }
     story.gameObject.SetActive(false);
    }
   }finally{
    foreach(var pair in states)pair.Key.gameObject.SetActive(pair.Value);
    popup.SetActive(oldPopup);skills.SetActive(oldSkills);Active=oldActive;Refresh();
   }
   string pathOut=Path.Combine(folder,"diagnostics",Regex.Replace(id,"[^a-zA-Z0-9_-]","")+"-fields.json.gz");
   using(var file=File.Create(pathOut))using(var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionMode.Compress))using(var writer=new StreamWriter(gzip,new UTF8Encoding(false)))writer.Write(JsonConvert.SerializeObject(new{stories=count,texts=rows}));
   Logger.LogInfo("Full field audit complete: "+count+" stories, "+rows.Count+" fields.");
  }
  System.Collections.IEnumerator Snapshot(string id) {
   yield return null;
   yield return new WaitForEndOfFrame();
   string safe=Regex.Replace(id,"[^a-zA-Z0-9_-]","");
   string path=Path.Combine(folder,"diagnostics");Directory.CreateDirectory(path);
   ScreenCapture.CaptureScreenshot(Path.Combine(path,safe+".png"));
   var rows=new List<object>();
   foreach(Text text in Resources.FindObjectsOfTypeAll<Text>())if(text!=null&&text.gameObject.scene.IsValid()&&text.gameObject.activeInHierarchy) {
    string translated;bool hit=Words.TryTranslate(text.text,out translated);
    if(Active&&hit)translated=RenderTranslation(text,translated);
    string hierarchy=text.name;for(Transform parent=text.transform.parent;parent!=null;parent=parent.parent)hierarchy=parent.name+"/"+hierarchy;
    var lines=text.text.Length>500?text.cachedTextGenerator.lines.Select(line=>new{start=line.startCharIdx,height=line.height,top=line.topY}).ToArray():null;
    rows.Add(new {name=text.name,hierarchy=hierarchy,source=text.text,display=Active&&hit?translated:text.text,font=text.font==null?null:text.font.name,size=text.fontSize,lineSpacing=text.lineSpacing,wrap=text.horizontalOverflow.ToString(),width=text.rectTransform.rect.width,height=text.rectTransform.rect.height,preferredHeight=text.preferredHeight,preferredWidth=text.preferredWidth,lines=lines});
   }
   var scrolls=new List<object>();
   foreach(var scroll in Resources.FindObjectsOfTypeAll<ScrollRect>())if(scroll!=null&&scroll.gameObject.scene.IsValid()&&scroll.gameObject.activeInHierarchy) {
    scrolls.Add(new{name=scroll.name,position=scroll.verticalNormalizedPosition,contentHeight=scroll.content==null?0:scroll.content.rect.height,viewportHeight=scroll.viewport==null?0:scroll.viewport.rect.height});
   }
   var decorativeInitials=DropCapOriginals.Where(pair=>pair.Key!=null&&pair.Key.gameObject.activeInHierarchy).Select(pair=>new{name=pair.Key.name,enabled=pair.Key.enabled,originalEnabled=pair.Value}).ToArray();
   var notifications=new List<object>();
   var notificationType=AccessTools.TypeByName("NotificationTop");
   foreach(var component in Resources.FindObjectsOfTypeAll(notificationType).Cast<MonoBehaviour>())if(component!=null&&component.gameObject.scene.IsValid()) {
    var source=AccessTools.Field(notificationType,"notification_text").GetValue(component) as string;
    if(string.IsNullOrEmpty(source))continue;
    var label=(Text)AccessTools.Field(notificationType,"NotificationText").GetValue(component);
    var area=(RectTransform)AccessTools.Field(notificationType,"TextAreaRectTransform").GetValue(component);
    float measured=Convert.ToSingle(AccessTools.Method(notificationType,"CalculateWidthOfText").Invoke(component,new object[]{source}));
    notifications.Add(new{id=component.GetInstanceID(),source=source,labelSource=label.text,areaWidth=area.sizeDelta.x,measuredWidth=measured,font=label.font.name});
   }
   File.WriteAllText(Path.Combine(path,safe+".json"),JsonConvert.SerializeObject(new{enabled=Active,count=Words.Count,texts=rows,scrolls=scrolls,decorativeInitials=decorativeInitials,notifications=notifications},Formatting.Indented),new UTF8Encoding(false));
   Logger.LogInfo("Diagnostic snapshot: "+safe+"; active text components="+rows.Count);
  }
  void OnApplicationQuit(){quitting=true;Active=false;PendingFonts.Clear();}
  void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;Active=false;if(!quitting&&harmony!=null){Refresh();harmony.UnpatchSelf();}}
 }
 public sealed class Translation {public string source; public string target;}
 public sealed class Rule {public string pattern; public string replacement;}
 public sealed class Catalog {
  readonly Dictionary<string,string> exact=new Dictionary<string,string>(StringComparer.Ordinal);
  readonly Dictionary<string,string> folded=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  readonly HashSet<string> ambiguous=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  readonly Dictionary<string,string> cache=new Dictionary<string,string>(StringComparer.Ordinal);
  readonly List<KeyValuePair<Regex,string>> rules=new List<KeyValuePair<Regex,string>>();
  static readonly Regex Spaces=new Regex(@"\s+",RegexOptions.Compiled);
  static readonly Regex Tags=new Regex(@"(<[^>]+>)",RegexOptions.Compiled);
  static readonly Regex Letters=new Regex(@"[A-Za-z]+",RegexOptions.Compiled);
  static readonly Regex BonusLine=new Regex(@"^(?<amount>(?:<color=""#[0-9A-Fa-f]{6,8}"">[+\-]?[0-9.,]+%?</color>|[+\-]?[0-9.,]+%?)) FROM (?<name>[^<>\r\n]+)$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
  static readonly Regex SkillProgress=new Regex(@"^(?<skill>LIFE|WORK|NATURE|SCIENCE|RELIGION|SCAVENGING) \(LVL (?<level>\d+)\) - (?<percent>[0-9.,]+)% \((?<current>[0-9.,]+)/(?<total>[0-9.,]+) XP\)$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
  static readonly Regex SkillBonusProgress=new Regex(@"^\+(?<level>\d+) (?<skill>LIFE|WORK|NATURE|SCIENCE|RELIGION|SCAVENGING) - (?<percent>[0-9.,]+)% \((?<current>[0-9.,]+)/(?<total>[0-9.,]+) XP\)$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
  static readonly Regex ResourceCountdown=new Regex(@"^(?<amount>[0-9.,KMBT]+) (?<unit>FOOD|WOOD|CERAMICS|PLASTICS|OIL|ELECTRICITY|STONE|MONEY|HIDES|PRAYERS|KNOWLEDGE|COMPUTERS|IRON|STEEL|SNAILS|GREEN OPALS|MEALS|LOGS|PIECES|BARRELS|IDEAS) \+(?<gain>[0-9.,KMBT]+) IN (?<seconds>[0-9.,]+)s$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
  static readonly HashSet<string> NumericWords=new HashSet<string>(("K M B T S OF MAX LVL LEVEL EXP XP FOOD WOOD CERAMICS PLASTICS OIL ELECTRICITY STONE MONEY HIDES PRAYERS KNOWLEDGE COMPUTERS IRON STEEL SNAILS GREEN OPALS MEALS LOGS PIECES BARRELS IDEAS MLS LGS PCS LBS BAR MW HDS PRY KNW SNL OPL").Split(' '),StringComparer.OrdinalIgnoreCase);
  public int Count {get{return exact.Count;}}
  static string Normalize(string x){return Spaces.Replace(x??""," ").Trim();}
  public static Catalog Load(string folder) {
   var result=new Catalog();
   if(!Directory.Exists(folder))return result;
   foreach(string file in Directory.GetFiles(folder,"*.json").OrderBy(x=>x,StringComparer.Ordinal)) {
    if(Path.GetFileName(file)=="rules.json") {
     foreach(var rule in JsonConvert.DeserializeObject<List<Rule>>(File.ReadAllText(file,Encoding.UTF8)))result.rules.Add(new KeyValuePair<Regex,string>(new Regex(rule.pattern,RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20)),rule.replacement));
    } else {
     var rows=JsonConvert.DeserializeObject<List<Translation>>(File.ReadAllText(file,Encoding.UTF8));
     // An explicit empty target suppresses a redundant layout fragment (e.g. English ordinal suffix).
     // A missing/null target remains invalid and is ignored.
     foreach(var row in rows)if(!string.IsNullOrWhiteSpace(row.source)&&row.target!=null) {
      var key=Normalize(row.source);string existing;
      if(result.exact.TryGetValue(key,out existing)&&existing!=row.target)throw new InvalidDataException("Conflicting translation: "+key);
      result.exact[key]=row.target;
      if(!result.ambiguous.Contains(key)) {
       if(result.folded.TryGetValue(key,out existing)&&existing!=row.target){result.folded.Remove(key);result.ambiguous.Add(key);}
       else result.folded[key]=row.target;
      }
     }
    }
   }
   return result;
  }
  public bool TryTranslate(string source,out string target) {
   target=source;if(string.IsNullOrEmpty(source))return false;
   if(cache.TryGetValue(source,out target))return target!=source;
   string key=Normalize(source);
   if(!Lookup(key,out target)) {
    target=source;
    DateTime date;
    string[] dateFormats={"MMM d, yyyy","MMM d yyyy","MMMM d, yyyy","MMMM d yyyy","d MMM yyyy","d MMMM yyyy","MMM-dd-yyyy"};
    string dateKey=Regex.Replace(key,@"\s*,\s*",", ");
    bool beforeCommonEra=dateKey.EndsWith(" BC",StringComparison.Ordinal);
    if(beforeCommonEra)dateKey=dateKey.Substring(0,dateKey.Length-3);
    if(Regex.IsMatch(dateKey,@"^(?:[A-Za-z]{3,9}[ -]\d{1,2},?[ -]\d{3,4}|\d{1,2} [A-Za-z]{3,9} \d{3,4})$")) {
     dateKey=Regex.Replace(dateKey,@"(?<!\d)\d{3}$",m=>"0"+m.Value);
     if(DateTime.TryParseExact(dateKey,dateFormats,CultureInfo.InvariantCulture,DateTimeStyles.None,out date))target=(beforeCommonEra?"公元前":"")+date.Year.ToString(CultureInfo.InvariantCulture)+date.ToString("年M月d日",CultureInfo.InvariantCulture);
    }
    foreach(var rule in rules) {
     if(rule.Key.IsMatch(source)){target=rule.Key.Replace(source,rule.Value);break;}
    }
    string structured;
    if(target==source&&TryStructured(source,out structured))target=structured;
    {
     var pieces=Tags.Split(target);bool any=false;
     for(int i=0;i<pieces.Length;i+=2){
      string value;
      if(Lookup(Normalize(pieces[i]),out value)){pieces[i]=value;any=true;}
      else if(Regex.IsMatch(pieces[i],@"^\s*(?:[+-]?\$?\d|(?:LEVEL|LVL|MAX)\s+\d)")&&Regex.IsMatch(pieces[i],@"^[0-9A-Za-z.,+%:/ ()$\-\r\n]+$")&&Letters.Matches(pieces[i]).Cast<Match>().All(m=>NumericWords.Contains(m.Value))) {
       pieces[i]=Letters.Replace(pieces[i],m=>m.Value.Equals("S",StringComparison.OrdinalIgnoreCase)?"秒":exact.TryGetValue(m.Value.ToUpperInvariant(),out value)?value:m.Value);any=true;
      }
     }
     if(any)target=string.Concat(pieces);
    }
   }
   if(cache.Count>8192)cache.Clear();
   cache[source]=target;
   return target!=source;
  }
  // These are display templates confirmed in the game IL, not substring replacements.
  // Require every nonblank line to be known before accepting a composite tooltip.
  bool TryStructured(string source,out string target) {
   target=source;
   if(source.Length>4096)return false;
   var lines=Regex.Split(source,@"(\r\n|\n|\r)");bool changed=false;
   for(int i=0;i<lines.Length;i+=2) {
    if(string.IsNullOrWhiteSpace(lines[i]))continue;
    if(lines[i].Length>512)return false;
    string value;
    if(!TryDynamicLine(lines[i],out value)&&!Lookup(Normalize(lines[i]),out value)) {
     bool matched=false;
     foreach(var rule in rules)if(rule.Key.IsMatch(lines[i])){value=rule.Key.Replace(lines[i],rule.Value);matched=true;break;}
     if(!matched)return false;
    }
    // Keep the source line separators; standalone heading translations may contain them.
    value=value.Trim('\r','\n');changed|=value!=lines[i];lines[i]=value;
   }
   if(changed)target=string.Concat(lines);
   return changed;
  }
  static readonly string[][] Notifications={
   new[]{"Unlocked new era, <i>","</i>.","已解鎖新時代：<i>","</i>。"},
   new[]{"New "," story unlocked.","已解鎖", "的新故事。"},
   new[]{"New "," community event unlocked.","已解鎖", "的新社群事件。"},
   new[]{"A new scavenging zone (",") has been unlocked.","已解鎖新的拾荒區域：", "。"},
   new[]{"A new nature location (",") has been unlocked.","已解鎖新的自然地點：", "。"},
   new[]{"A new artifact (",") has been unlocked.","已解鎖新的文物：", "。"},
   new[]{"A new religion (",") has been unlocked.","已解鎖新的宗教：", "。"},
   new[]{"A new government (",") has been unlocked.","已解鎖新的政體：", "。"},
   new[]{"A new science (",") has been unlocked.","已解鎖新的科學：", "。"},
   new[]{"Finished scavenging ",".","已完成拾荒：", "。"},
   new[]{"Finished building ",".","已完成建造：", "。"},
   new[]{"Finished exploring ",".","已完成探索：", "。"},
   new[]{"Finished examing artifact (",").","已完成文物檢視：", "。"},
   new[]{"Finished worshipping ",".","已完成崇拜：", "。"},
   new[]{"Finished government ",".","已完成政體：", "。"},
   new[]{"Finished researching ",".","已完成研究：", "。"},
   new[]{"Obtained Perk (",").","獲得加成：", "。"},
   new[]{""," unlocked!","已解鎖：", "！"},
   new[]{""," has been unlocked.","已解鎖：", "。"}
  };
  bool TryDynamicLine(string source,out string target) {
   target=source;string label;
   var duration=Regex.Match(source,@"^(?<time>∞|N/A|[0-9]+<size=12>M</size> [0-9]+<size=12>S</size>|[0-9]+<size=12>HR</size> [0-9]+<size=12>M</size>) \((?<xp>[0-9.,]+|∞|N/A) <size=12>XP</size>\)$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(20));
   if(duration.Success) {
    string time=duration.Groups["time"].Value,translatedTime;
    if(TryTranslate(time,out translatedTime))time=translatedTime;
    string xp=duration.Groups["xp"].Value;
    target=time+" ("+(xp=="N/A"?"不適用":xp)+" <size=12>經驗</size>)";return true;
   }
   if(TryResourceFlow(source,out target))return true;
   foreach(var template in Notifications) {
    if(source.Length<=template[0].Length+template[1].Length||!source.StartsWith(template[0],StringComparison.Ordinal)||!source.EndsWith(template[1],StringComparison.Ordinal))continue;
    string name=source.Substring(template[0].Length,source.Length-template[0].Length-template[1].Length);
    if(Lookup(Normalize(name),out label)){target=template[2]+label+template[3];return true;}
   }
   var match=BonusLine.Match(source);
   if(match.Success&&Lookup(Normalize(match.Groups["name"].Value),out label)) {
    target=match.Groups["amount"].Value+" 來自 "+label;return true;
   }
   match=SkillProgress.Match(source);
   if(match.Success&&Lookup(match.Groups["skill"].Value,out label)) {
    target=label+"（等級 "+match.Groups["level"].Value+"）－"+match.Groups["percent"].Value+"%（"+match.Groups["current"].Value+"／"+match.Groups["total"].Value+" 經驗）";return true;
   }
   match=SkillBonusProgress.Match(source);
   if(match.Success&&Lookup(match.Groups["skill"].Value,out label)) {
    target="+"+match.Groups["level"].Value+" "+label+"－"+match.Groups["percent"].Value+"%（"+match.Groups["current"].Value+"／"+match.Groups["total"].Value+" 經驗）";return true;
   }
   match=ResourceCountdown.Match(source);
   if(match.Success&&Lookup(match.Groups["unit"].Value,out label)) {
    target=match.Groups["amount"].Value+" "+label+"，"+match.Groups["seconds"].Value+" 秒後 +"+match.Groups["gain"].Value;return true;
   }
   return false;
  }
  const string ResourceNames="EXP|EXPERIENCE|FOOD|WOOD|CERAMICS|PLASTICS|OIL|ELECTRICITY|STONE|MONEY|HIDES|PRAYERS|KNOWLEDGE|COMPUTERS|IRON|STEEL|SNAILS|GREEN OPALS";
  static readonly Regex ResourceAmount=new Regex("^(?<amount><color=\"#[0-9A-Fa-f]{6}\">[+-]?\\$?[0-9][0-9.,]*[KMBT]?</color>)(?: (?<unit>"+ResourceNames+"))?$",RegexOptions.CultureInvariant);
  static readonly Regex ResourceFlow=new Regex("^ ?(?<received>.+) = (?<percent><color=\"#FFD27F\">[0-9.,]+%</color>) X (?<past>.+) \\(FROM PAST (?<unit>"+ResourceNames+")\\)$",RegexOptions.CultureInvariant);
  bool TryResourceAmount(string source,out string target) {
   target=source;var match=ResourceAmount.Match(source);string unit;
   if(!match.Success)return false;
   if(!match.Groups["unit"].Success)return match.Groups["amount"].Value.Contains("$");
   if(!Lookup(match.Groups["unit"].Value,out unit))return false;
   target=match.Groups["amount"].Value+" "+unit;return true;
  }
  bool TryResourceFlow(string source,out string target) {
   target=source;string value;
   foreach(string heading in new[]{"CURRENT ERA (","PREVIOUS ERA ("}) {
    if(source.StartsWith(heading,StringComparison.Ordinal)&&source.EndsWith(")",StringComparison.Ordinal)&&Lookup(Normalize(source.Substring(heading.Length,source.Length-heading.Length-1)),out value)) {
     target=(heading.StartsWith("CURRENT",StringComparison.Ordinal)?"目前時代（":"先前時代（")+value+"）";return true;
    }
   }
   if(source.StartsWith("YOU HAVE ",StringComparison.Ordinal)&&TryResourceAmount(source.Substring(9),out value)){target="持有："+value;return true;}
   foreach(string suffix in new[]{" SPENT"," PRODUCED"}) {
    if(source.EndsWith(suffix,StringComparison.Ordinal)&&TryResourceAmount(source.Substring(0,source.Length-suffix.Length).TrimStart(),out value)) {
     target=(suffix==" SPENT"?"已消耗：":"已生產：")+value;return true;
    }
   }
   var match=ResourceFlow.Match(source);string received,past,unit;
   if(match.Success&&TryResourceAmount(match.Groups["received"].Value,out received)&&TryResourceAmount(match.Groups["past"].Value,out past)&&Lookup(match.Groups["unit"].Value,out unit)) {
    target=received+" = "+match.Groups["percent"].Value+" × "+past+"（來自過去的"+unit+"）";return true;
   }
   return false;
  }
  bool Lookup(string key,out string value){return exact.TryGetValue(key,out value)||folded.TryGetValue(key,out value);}
 }
}




