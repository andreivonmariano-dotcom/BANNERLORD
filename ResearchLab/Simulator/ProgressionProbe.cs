// Local-only isolated managed model research. Do not initialize a campaign.
using System;
using System.IO;
using System.Reflection;
using System.Globalization;
class ProgressionProbe {
 static int passed,failed;
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static Assembly camp;
 static object Model(string name){return Activator.CreateInstance(camp.GetType("TaleWorlds.CampaignSystem.GameComponents."+name,true));}
 static object Invoke(object o,string name,params object[] args){
   foreach(MethodInfo m in o.GetType().GetMethods(flags))
      if(m.Name==name&&m.GetParameters().Length==args.Length)return m.Invoke(o,args);
   throw new MissingMethodException(name);
 }
 static void Check(string n,Func<bool> f){try{if(!f())throw new Exception("mismatch");passed++;Console.WriteLine("PASS "+n);}catch(Exception e){while(e.InnerException!=null)e=e.InnerException;failed++;Console.WriteLine("FAIL "+n+" "+e.GetType().Name+" "+e.Message);}}
 static void Property(string model,string name,double expected){Check(model+"."+name,()=>{double actual=Convert.ToDouble(Invoke(Model(model),"get_"+name));Console.WriteLine("VALUE "+name+" "+actual.ToString("R",CultureInfo.InvariantCulture));return Math.Abs(actual-expected)<.000001;});}
 static int Main(string[] args){
   if(args.Length!=1)return 2;
   foreach(string p in Directory.GetFiles(args[0],"TaleWorlds*.dll"))try{Assembly.LoadFrom(p);}catch{}
   camp=Assembly.LoadFrom(Path.Combine(args[0],"TaleWorlds.CampaignSystem.dll"));
   string d="DefaultDiplomacyModel",c="DefaultClanTierModel",x="DefaultCharacterDevelopmentModel";
   Property(d,"MinimumRelationWithConversationCharacterToJoinKingdom",-10);Property(d,"GiftingTownRelationshipBonus",20);Property(d,"GiftingCastleRelationshipBonus",10);Property(d,"MaxRelationLimit",100);Property(d,"MinRelationLimit",-100);Property(d,"MaxNeutralRelationLimit",50);Property(d,"MinNeutralRelationLimit",-50);Property(d,"WarDeclarationScorePenaltyAgainstTradePartners",.7);
   Check("army disband relation costs",()=>Convert.ToInt32(Invoke(Model(d),"GetRelationCostOfDisbandingArmy",true))==-4&&Convert.ToInt32(Invoke(Model(d),"GetRelationCostOfDisbandingArmy",false))==-1);
   Check("expulsion/support relation",()=>Convert.ToInt32(Invoke(Model(d),"GetRelationCostOfExpellingClanFromKingdom"))==-20&&Convert.ToInt32(Invoke(Model(d),"GetRelationValueOfSupportingClan"))==1);
   Property(c,"MinClanTier",0);Property(c,"MaxClanTier",6);Property(c,"MercenaryEligibleTier",1);Property(c,"VassalEligibleTier",2);Property(c,"BannerEligibleTier",0);Property(c,"RebelClanStartingTier",3);Property(c,"CompanionToLordClanStartingTier",2);
   Check("renown thresholds ordered",()=>{object m=Model(c);int last=-1;for(int i=0;i<=6;i++){int value=Convert.ToInt32(Invoke(m,"GetRequiredRenownForTier",i));Console.WriteLine("RENOWN "+i+" "+value);if(value<=last)return false;last=value;}return true;});
   Property(x,"MaxFocusPerSkill",5);Property(x,"MaxAttribute",10);Property(x,"AttributePointsAtStart",15);Property(x,"LevelsPerAttributePoint",4);Property(x,"FocusPointsPerLevel",1);Property(x,"FocusPointsAtStart",5);Property(x,"MaxSkillRequiredForEpicPerkBonus",250);Property(x,"MinSkillRequiredForEpicPerkBonus",200);
   Check("trait threshold range",()=>{object m=Model(x);for(int i=-3;i<=3;i++){int v=Convert.ToInt32(Invoke(m,"GetTraitXpRequiredForTraitLevel",null,i));int expected=i< -1?-4000:i==-1?-1000:i==0?0:i==1?1000:4000;Console.WriteLine("TRAIT "+i+" "+v);if(v!=expected)return false;}return true;});
   Check("skill XP samples",()=>{object m=Model(x);int last=-1;foreach(int i in new[]{0,1,10,25,50,100,150,200,250,300}){int v=Convert.ToInt32(Invoke(m,"GetXpRequiredForSkillLevel",i));Console.WriteLine("SKILL_XP "+i+" "+v);if(v<last)return false;last=v;}return true;});
   Property("DefaultPartyWageModel","MaxWagePaymentLimit",10000);Property("DefaultIssueModel","IssueOwnerCoolDownInDays",30);Property("DefaultPartyMoraleModel","HighMoraleValue",60);
   Check("default combat pursuit round count",()=>Convert.ToInt32(Invoke(Model("DefaultCombatSimulationModel"),"GetPursuitRoundCount",new object[]{null}))==4);
   Console.WriteLine("CHECKS "+passed+" passed "+failed+" failed");return failed==0?0:1;
 }
}
