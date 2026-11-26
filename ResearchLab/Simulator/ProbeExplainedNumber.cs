using System;
using System.IO;
using System.Reflection;
using System.Globalization;
class FactorProbe {
 static int Main(string[] a) {
  CultureInfo.DefaultThreadCurrentCulture=CultureInfo.InvariantCulture;
  foreach(var p in Directory.GetFiles(a[0],"TaleWorlds*.dll"))try{Assembly.LoadFrom(p);}catch{}
  var c=Assembly.LoadFrom(Path.Combine(a[0],"TaleWorlds.CampaignSystem.dll"));
  var n=c.GetType("TaleWorlds.CampaignSystem.ExplainedNumber",true);
  var ctor=n.GetConstructors()[0];
  var add=n.GetMethod("AddFactor");
  var result=n.GetProperty("ResultNumber");
  var sum=n.GetProperty("SumOfFactors");
  foreach(float b in new[]{100f,-100f,123.5f,0f}) {
   foreach(float[] factors in new[]{new float[0],new[]{-.2f},new[]{-.2f,-.8f},new[]{-.1f}}) {
    object x=ctor.Invoke(new object[]{b,false,null});
    foreach(float f in factors)add.Invoke(x,new object[]{f,null});
    Console.WriteLine("FACTOR base="+b.ToString("R")+" count="+factors.Length+" sum="+((float)sum.GetValue(x,null)).ToString("R")+" result="+((float)result.GetValue(x,null)).ToString("R"));
   }
  }
  return 0;
 }
}