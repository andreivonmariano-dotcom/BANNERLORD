using System;
using System.IO;
using System.Reflection;
using System.Globalization;
class Probe {
 static int Main(string[] a) {
 CultureInfo.DefaultThreadCurrentCulture=CultureInfo.InvariantCulture;
 foreach(var p in Directory.GetFiles(a[0],"TaleWorlds*.dll"))try{Assembly.LoadFrom(p);}catch{}
 var c=Assembly.LoadFrom(Path.Combine(a[0],"TaleWorlds.CampaignSystem.dll"));
 var n=c.GetType("TaleWorlds.CampaignSystem.ExplainedNumber",true);
 var ctor=n.GetConstructor(new[]{typeof(float),typeof(bool),c.GetType("TaleWorlds.Localization.TextObject")??Assembly.LoadFrom(Path.Combine(a[0],"TaleWorlds.Localization.dll")).GetType("TaleWorlds.Localization.TextObject")});
 var add=n.GetMethod("AddFactor");var result=n.GetProperty("ResultNumber");var sum=n.GetProperty("SumOfFactors");
 using(var w=new StreamWriter(new FileStream(a[1],FileMode.CreateNew))){
 w.WriteLine("base,prior,sea,caravan,sum,result");
 foreach(float b in new[]{0f,1f,100f,-100f,123.5f,9876.25f})
 foreach(float f in new[]{0f,.125f,-.125f,.3f,-.3f})
 foreach(bool sea in new[]{false,true})
 foreach(bool caravan in new[]{false,true}){
 object x=ctor.Invoke(new object[]{b,false,null});
 add.Invoke(x,new object[]{f,null});
 if(sea){add.Invoke(x,new object[]{-.2f,null});if(caravan)add.Invoke(x,new object[]{-.8f,null});}
 w.WriteLine(b.ToString("R")+","+f.ToString("R")+","+(sea?1:0)+","+(caravan?1:0)+","+((float)sum.GetValue(x,null)).ToString("R")+","+((float)result.GetValue(x,null)).ToString("R"));
 }}
 Console.WriteLine("FACTOR_ROWS 120");return 0;
 }}