// Research only: invoke a narrow allowlist of installed managed models in a separate process.
// No campaign initialization, no game saves, no proprietary assembly redistribution.
using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
class NativeProbe {
 static int passed=0, failed=0;
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static void Check(string label, Func<bool> f) {
   try { if(!f()) throw new Exception("comparison failed"); passed++; Console.WriteLine("PASS "+label); }
   catch(Exception e){failed++;while(e.InnerException!=null)e=e.InnerException;Console.WriteLine("FAIL "+label+" "+e.GetType().Name+" "+e.Message);}
 }
 static object Get(object obj,string name){return obj.GetType().GetProperty(name,flags).GetValue(obj,null);}
 static object Create(Assembly a,string type){return Activator.CreateInstance(a.GetType(type,true));}
 static float Expected(float stock,float supply,float demand,bool selling,int transfer,bool animal,bool trade){
   if(selling) stock+=transfer;
   float r=(float)Math.Pow(demand/(.1f*supply+.04f*stock+2f),animal?.3f:.6f);
   return Math.Max(trade?.1f:.8f,Math.Min(trade?10f:1.3f,r));
 }
 static int Main(string[] args){
   if(args.Length!=1)return 2;
   string bin=Path.GetFullPath(args[0]);
   foreach(string p in Directory.GetFiles(bin,"TaleWorlds*.dll")){try{Assembly.LoadFrom(p);}catch{}}
   Assembly camp=Assembly.LoadFrom(Path.Combine(bin,"TaleWorlds.CampaignSystem.dll"));
   Assembly core=Assembly.LoadFrom(Path.Combine(bin,"TaleWorlds.Core.dll"));
   Assembly lib=Assembly.LoadFrom(Path.Combine(bin,"TaleWorlds.Library.dll"));
   string prefix="TaleWorlds.CampaignSystem.GameComponents.";
   string[] ageNames={"BecomeInfantAge","BecomeChildAge","BecomeTeenagerAge","HeroComesOfAge","MiddleAdultHoodAge","BecomeOldAge","MaxAge"};
   int[] ageValues={3,6,14,18,35,55,128};
   for(int i=0;i<ageNames.Length;i++){int j=i;Check("age "+ageNames[j],()=>Convert.ToInt32(Get(Create(camp,prefix+"DefaultAgeModel"),ageNames[j]))==ageValues[j]);}
   foreach(string n in new[]{"MinimumMarriageAgeMale","MinimumMarriageAgeFemale"})Check("marriage "+n,()=>Convert.ToInt32(Get(Create(camp,prefix+"DefaultMarriageModel"),n))==18);
   string[] pregNames={"MaternalMortalityProbabilityInLabor","StillbirthProbability","DeliveringFemaleOffspringProbability","DeliveringTwinsProbability"};
   float[] pregValues={.015f,.01f,.51f,.03f};
   for(int i=0;i<pregNames.Length;i++){int j=i;Check("pregnancy "+pregNames[j],()=>Math.Abs(Convert.ToSingle(Get(Create(camp,prefix+"DefaultPregnancyModel"),pregNames[j]))-pregValues[j])<.000001f);}
   MethodInfo round=lib.GetType("TaleWorlds.Library.MathF",true).GetMethod("Round",new[]{typeof(float)});
   foreach(float x in new[]{-.5f,.5f,1.5f,2.5f,-1.5f})Check("round "+x,()=>Convert.ToInt32(round.Invoke(null,new object[]{x}))==(int)Math.Round(x));
   Check("price grid 4 categories x 5 stocks x 4 supplies x 5 demands x 2 directions",()=>{
     object model=Create(camp,prefix+"DefaultTradeItemPriceFactorModel");
     Type catType=core.GetType("TaleWorlds.Core.ItemCategory",true);
     MethodInfo method=model.GetType().GetMethod("GetBasePriceFactor",flags);
     int cases=0;double maxError=0;
     foreach(bool animal in new[]{false,true})foreach(bool trade in new[]{false,true}){
       object cat=Activator.CreateInstance(catType);
       // Only two inspected backing properties of a synthetic category are set;
       // no object registration or fabricated live campaign state.
       catType.GetProperty("IsAnimal",flags).GetSetMethod(true).Invoke(cat,new object[]{animal});
       catType.GetProperty("IsTradeGood",flags).GetSetMethod(true).Invoke(cat,new object[]{trade});
       foreach(float stock in new[]{0f,10f,100f,1000f,10000f})
       foreach(float supply in new[]{0f,1f,10f,100f})
       foreach(float demand in new[]{0f,1f,10f,100f,1000f})
       foreach(bool selling in new[]{false,true}){
         float actual=(float)method.Invoke(model,new object[]{cat,stock,supply,demand,selling,15});
         float expected=Expected(stock,supply,demand,selling,15,animal,trade);
         double err=Math.Abs(actual-expected);
         if(float.IsNaN(actual)||err>0.00002)throw new Exception("price mismatch");
         maxError=Math.Max(maxError,err);cases++;
       }
     }
     Console.WriteLine("PRICE_CASES "+cases+" MAX_ABS_ERROR "+maxError.ToString("R",CultureInfo.InvariantCulture));return cases==800;
   });
   Console.WriteLine("CHECKS "+passed+" passed "+failed+" failed");
   return failed==0?0:1;
 }
}
