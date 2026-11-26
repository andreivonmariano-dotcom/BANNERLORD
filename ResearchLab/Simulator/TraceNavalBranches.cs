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
  var a=Assembly.LoadFrom(args[2]);
  var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
  var targets=new Dictionary<string,string[]> {
   {"NavalDLCPartyWageModel",new[]{"GetCharacterWage","GetTotalWage","GetTroopRecruitmentCost","get_MaxWagePaymentLimit"}},
   {"NavalDLCMobilePartyFoodConsumptionModel",new[]{"CalculateDailyBaseFoodConsumptionf","CalculateDailyFoodConsumptionf","DoesPartyConsumeFood","get_NumberOfMenOnMapToEatOneFood"}},
   {"NavalDLCPartyHealingModel",new[]{"GetBattleEndHealingAmount","GetDailyHealingForRegulars","GetDailyHealingHpForHeroes","GetSiegeBombardmentHitSurgeryChance","GetSkillXpFromHealingTroop","GetSurgeryChance","GetSurvivalChance"}},
   {"NavalDLCClanFinanceModel",new[]{"AddPartyExpense","ApplyMoraleEffect","CalculateClanExpenses","CalculateClanGoldChange","CalculateClanIncome","CalculateNotableDailyGoldChange","CalculateOwnerIncomeFromCaravan","CalculateOwnerIncomeFromWorkshop","CalculatePartyWage","CalculateTownIncomeFromProjects","CalculateTownIncomeFromTariffs","CalculateVillageIncome","RevenueSmoothenFraction","get_PartyGoldLowerThreshold"}},
   {"NavalDLCMapDistanceModel",new[]{"GetClosestEntranceToFace","GetDistance","GetLandRatioOfPathBetweenSettlements","GetMaximumDistanceBetweenTwoConnectedSettlements","GetNeighborsOfFortification","GetPortToGateDistanceForSettlement","GetTransitionCostAdjustment","PathExistBetweenPoints","RegisterDistanceCache","get_MaximumSpawnDistanceForCompanionsAfterDisband","get_RegionSwitchCostFromLandToSea","get_RegionSwitchCostFromSeaToLand"}}
  };
  int bodies=0,errors=0;
  using(var w=new StreamWriter(new FileStream(args[1],FileMode.CreateNew))) {
   foreach(var kv in targets) {
    var t=a.GetType("NavalDLC.GameComponents."+kv.Key,true);
    foreach(string name in kv.Value) {
     var methods=t.GetMethods(flags).Where(m=>m.Name==name).ToArray();if(methods.Length==0)throw new Exception(name);
     foreach(var m in methods) {
      w.WriteLine("METHOD "+t.Name+"."+m.Name+"("+string.Join(", ",m.GetParameters().Select(p=>p.ParameterType.Name+" "+p.Name))+")");
      w.WriteLine("BASE "+t.BaseType.FullName+" | SLOT "+m.GetBaseDefinition().DeclaringType.FullName);var b=m.GetMethodBody().GetILAsByteArray();int i=0;bodies++;
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
        try {var member=m.Module.ResolveMember(BitConverter.ToInt32(b,i),t.GetGenericArguments(),m.GetGenericArguments());w.WriteLine("  "+pos+" "+op.Name+" "+member.DeclaringType.FullName+"."+member.Name);}
        catch(Exception e){errors++;w.WriteLine("  UNRESOLVED "+e.GetType().Name);}
       }
       if(op.OperandType==OperandType.ShortInlineBrTarget||op.OperandType==OperandType.InlineBrTarget)w.WriteLine("  BRANCH "+pos+" "+op.Name+" -> "+(i+n+(op.OperandType==OperandType.ShortInlineBrTarget?(sbyte)b[i]:BitConverter.ToInt32(b,i)))); if(op.OperandType==OperandType.ShortInlineI||op.OperandType==OperandType.InlineI||op.OperandType==OperandType.ShortInlineR||op.OperandType==OperandType.InlineR)w.WriteLine("  CONST "+pos+" "+op.Name+" "+(op.OperandType==OperandType.ShortInlineI?((sbyte)b[i]).ToString():op.OperandType==OperandType.InlineI?BitConverter.ToInt32(b,i).ToString():op.OperandType==OperandType.ShortInlineR?BitConverter.ToSingle(b,i).ToString("R"):BitConverter.ToDouble(b,i).ToString("R"))); if(op.OperandType==OperandType.InlineNone && (op.Name.StartsWith("ldc.")||op.Name=="ret"||op.Name=="add"||op.Name=="sub"||op.Name=="mul"||op.Name=="div"))w.WriteLine("  OP "+pos+" "+op.Name); i+=n;
      }
      if(i!=b.Length)throw new Exception("IL alignment");
     }
    }
   }
  }
  Console.WriteLine("BODIES "+bodies+" UNRESOLVED "+errors);return errors==0?0:1;
 }
}