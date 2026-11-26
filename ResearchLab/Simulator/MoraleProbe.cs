// Isolated native morale arithmetic probe, no campaign initialization.
using System;using System.IO;using System.Reflection;using System.Globalization;
class MoraleProbe {
 static int Main(string[] args){
  foreach(string p in Directory.GetFiles(args[0],"TaleWorlds*.dll"))try{Assembly.LoadFrom(p);}catch{}
  Assembly a=Assembly.LoadFrom(Path.Combine(args[0],"TaleWorlds.CampaignSystem.dll"));
  Type t=a.GetType("TaleWorlds.CampaignSystem.GameComponents.DefaultCombatSimulationModel",true);
  Type e=a.GetType("TaleWorlds.CampaignSystem.ExplainedNumber",true);
  object m=Activator.CreateInstance(t);
  MethodInfo method=t.GetMethod("CalculateSimulationMoraleEffects",BindingFlags.Instance|BindingFlags.NonPublic);
  int n=0;double max=0;
  foreach(float d in new[]{0f,20f,40f,100f})foreach(float s in new[]{0f,25f,50f,75f,100f})foreach(float v in new[]{0f,25f,50f,75f,100f}){
   object en=Activator.CreateInstance(e,new object[]{d,false,null});
   object[] p={s,v,en};method.Invoke(m,p);
   float actual=(float)e.GetProperty("ResultNumber").GetValue(p[2],null);
   float expected=d*(1f+(Math.Min(s-50f,0f)-Math.Max(v-50f,0f))*.005f);
   double err=Math.Abs(actual-expected);max=Math.Max(max,err);if(err>0.00002)return 1;n++;
  }
  Console.WriteLine("NATIVE_MORALE_CASES "+n+" MAX_ABS_ERROR "+max.ToString("R",CultureInfo.InvariantCulture));
  return n==100?0:2;
 }
}
