using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
class TraceContracts {
 static Dictionary<ushort,OpCode> ops=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(x=>x.FieldType==typeof(OpCode)).Select(x=>(OpCode)x.GetValue(null)).ToDictionary(x=>unchecked((ushort)x.Value));
 static int Main(string[] args) {
  foreach(string p in Directory.GetFiles(args[0],"TaleWorlds*.dll"))try{Assembly.LoadFrom(p);}catch{}
  var a=Assembly.LoadFrom(Path.Combine(args[0],"TaleWorlds.CampaignSystem.dll"));
  var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
  var targets=new Dictionary<string,string[]> {
   {"RecruitmentCampaignBehavior",new[]{"ApplyInternal","RecruitVolunteersFromNotable","CheckRecruiting","OnBeforeSettlementEntered"}},
   {"DefaultVolunteerModel",new[]{"GetBasicVolunteer","MaximumIndexCanPartyRecruitFromHeroInternal","GetDailyVolunteerProductionProbability"}},
   {"DefaultPartyWageModel",new[]{"GetTroopRecruitmentCost"}},
   {"DefaultMobilePartyFoodConsumptionModel",new[]{"CalculateDailyBaseFoodConsumptionf","CalculateDailyFoodConsumptionf","DoesPartyConsumeFood"}},
   {"FoodConsumptionBehavior",new[]{"PartyConsumeFood","MakeFoodConsumption","SlaughterLivestock"}},
   {"DefaultPartyHealingModel",new[]{"GetDailyHealingForRegulars","GetDailyHealingHpForHeroes"}},
   {"DefaultMapDistanceModel",new[]{"GetDistance","PathExistBetweenPoints"}},
   {"DefaultClanFinanceModel",new[]{"CalculateClanGoldChange","CalculateClanIncome","CalculateClanExpenses"}}
  };
  int bodies=0,errors=0;
  using(var w=new StreamWriter(new FileStream(args[1],FileMode.CreateNew))) {
   foreach(var kv in targets) {
    var t=a.GetTypes().Single(x=>x.Name==kv.Key);
    foreach(string name in kv.Value) {
     var methods=t.GetMethods(flags).Where(m=>m.Name==name).ToArray();if(methods.Length==0)throw new Exception(name);
     foreach(var m in methods) {
      w.WriteLine("METHOD "+t.Name+"."+m.Name+"("+string.Join(", ",m.GetParameters().Select(p=>p.ParameterType.Name+" "+p.Name))+")");
      var b=m.GetMethodBody().GetILAsByteArray();int i=0;bodies++;
      while(i<b.Length) {
       int pos=i;ushort k=b[i++];if(k==0xfe)k=(ushort)(0xfe00|b[i++]);var op=ops[k];int n;
       switch(op.OperandType) {
        case OperandType.InlineNone:n=0;break;
        case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:n=1;break;
        case OperandType.InlineVar:n=2;break;
        case OperandType.InlineI8:case OperandType.InlineR:n=8;break;
        case OperandType.InlineSwitch:n=4+4*BitConverter.ToInt32(b,i);break;
        default:n=4;break;
       }
       if(op.OperandType==OperandType.InlineMethod||op.OperandType==OperandType.InlineField) {
        try {var member=m.Module.ResolveMember(BitConverter.ToInt32(b,i),t.GetGenericArguments(),m.GetGenericArguments());w.WriteLine("  "+pos+" "+op.Name+" "+member.DeclaringType.Name+"."+member.Name);}
        catch(Exception e){errors++;w.WriteLine("  UNRESOLVED "+e.GetType().Name);}
       }
       i+=n;
      }
      if(i!=b.Length)throw new Exception("IL alignment");
     }
    }
   }
  }
  Console.WriteLine("BODIES "+bodies+" UNRESOLVED "+errors);return errors==0?0:1;
 }
}