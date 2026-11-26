// Actual native default price output, no reference arithmetic or campaign initialization.
using System;
using System.IO;
using System.Reflection;
using System.Globalization;
class NativePriceExport {
 static int Main(string[] args){
   if(args.Length!=2)return 2;
   string bin=Path.GetFullPath(args[0]);
   foreach(string p in Directory.GetFiles(bin,"TaleWorlds*.dll"))try{Assembly.LoadFrom(p);}catch{}
   Assembly camp=Assembly.LoadFrom(Path.Combine(bin,"TaleWorlds.CampaignSystem.dll"));
   Assembly core=Assembly.LoadFrom(Path.Combine(bin,"TaleWorlds.Core.dll"));
   object model=Activator.CreateInstance(camp.GetType("TaleWorlds.CampaignSystem.GameComponents.DefaultTradeItemPriceFactorModel",true));
   Type catType=core.GetType("TaleWorlds.Core.ItemCategory",true);
   BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
   MethodInfo method=model.GetType().GetMethod("GetBasePriceFactor",flags);
   int count=0;
   using(var w=new StreamWriter(new FileStream(args[1],FileMode.CreateNew,FileAccess.Write,FileShare.None))){
     w.WriteLine("animal,trade_good,stock,supply,demand,selling,transfer,native");
     foreach(bool animal in new[]{false,true})foreach(bool trade in new[]{false,true}){
       object cat=Activator.CreateInstance(catType);
       catType.GetProperty("IsAnimal",flags).GetSetMethod(true).Invoke(cat,new object[]{animal});
       catType.GetProperty("IsTradeGood",flags).GetSetMethod(true).Invoke(cat,new object[]{trade});
       foreach(int stock in new[]{0,10,100,1000,10000})
       foreach(int supply in new[]{0,1,10,100})
       foreach(int demand in new[]{0,1,10,100,1000})
       foreach(bool selling in new[]{false,true}){
         float v=(float)method.Invoke(model,new object[]{cat,(float)stock,(float)supply,(float)demand,selling,15});
         if(float.IsNaN(v)||float.IsInfinity(v))throw new Exception("nonfinite native result");
         w.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},15,{6:R}",animal?1:0,trade?1:0,stock,supply,demand,selling?1:0,v));count++;
       }
     }
   }
   Console.WriteLine("NATIVE_ROWS "+count);return count==800?0:1;
 }
}
