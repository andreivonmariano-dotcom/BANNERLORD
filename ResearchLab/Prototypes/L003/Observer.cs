using System;
using System.IO;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
[assembly:System.Reflection.AssemblyVersion("0.0.3.1")]
namespace CUNativeObservation {
 public sealed class SubModule : MBSubModuleBase {
  protected override void OnGameStart(Game game, IGameStarter starter) {
   base.OnGameStart(game,starter);
   var campaign=starter as CampaignGameStarter;
   if(campaign!=null)campaign.AddBehavior(new Observer());
  }
 }
 internal sealed class Observer : CampaignBehaviorBase {
  const int Limit=1800;
  int count; bool stopped;
  readonly string session=Guid.NewGuid().ToString("N");
  static string S(string value){return Uri.EscapeDataString(value??"none");}
  static string F(float value){return value.ToString("R",CultureInfo.InvariantCulture);}
  static string Id(Hero h){return h==null?"none":S(h.StringId);}
  bool Ready {get{return !stopped&&count<Limit&&Campaign.Current!=null;}}
  public override void SyncData(IDataStore store){}
  public override void RegisterEvents(){
   CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this,OnSession);
   CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this,OnLoaded);
   CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this,OnHourly);
   CampaignEvents.OnPartyConsumedFoodEvent.AddNonSerializedListener(this,OnFood);
   CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this,OnArrival);
   CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this,OnDeparture);
   CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this,OnRecruit);
   CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this,OnGovernor);
   CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this,OnTown);
  }
  static long Ticks(){
   var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
   var property=typeof(Campaign).GetProperty("MapTimeTracker",f);
   if(property==null)throw new InvalidOperationException("tracker unavailable");
   var tracker=property.GetValue(Campaign.Current,null);
   if(tracker==null)throw new InvalidOperationException("tracker null");
   var tick=tracker.GetType().GetProperty("NumTicks",f);
   if(tick==null||tick.PropertyType!=typeof(long))throw new InvalidOperationException("tick unavailable");
   return (long)tick.GetValue(tracker,null);
  }
  void Write(string data){
   if(!Ready)return;
   try {
    var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BannerlordResearchLab","D002-observations");
    Directory.CreateDirectory(dir);
    var file=Path.Combine(dir,"session-"+session+".log");
    var row="seq="+(count+1)+" tick="+Ticks()+" "+data+Environment.NewLine;
    File.AppendAllText(file,row); count++;
   }catch{stopped=true;}
  }
  void Guard(Action a){if(!Ready)return;try{a();}catch(Exception e){Write("kind=error type="+S(e.GetType().Name));}}
  void Models(){Guard(()=>{var m=Campaign.Current.Models; Write("kind=models wage="+S(m.PartyWageModel.GetType().FullName)+" food="+S(m.MobilePartyFoodConsumptionModel.GetType().FullName)+" healing="+S(m.PartyHealingModel.GetType().FullName)+" finance="+S(m.ClanFinanceModel.GetType().FullName)+" distance="+S(m.MapDistanceModel.GetType().FullName)+" tax="+S(m.SettlementTaxModel.GetType().FullName));});}
  string Roles(MobileParty p){
   string row="";
   foreach(PartyRole role in new[]{PartyRole.Surgeon,PartyRole.Quartermaster,PartyRole.Scout,PartyRole.Engineer,PartyRole.FirstMate,PartyRole.Navigator}){
    var raw=p.GetRoleHolder(role);var effective=p.GetEffectiveRoleHolder(role);
    row+=" raw_"+role+"="+Id(raw)+" effective_"+role+"="+Id(effective);
   }
   var surgeon=p.GetEffectiveRoleHolder(PartyRole.Surgeon);
   return row+" medicine="+(surgeon==null?-1:surgeon.GetSkillValue(DefaultSkills.Medicine));
  }
  void Snapshot(string reason){Guard(()=>{var p=MobileParty.MainParty;if(p==null||!p.IsActive)return;
   var h=Hero.MainHero;
   Write("kind=party reason="+reason+" id="+S(p.StringId)+" healthy="+p.MemberRoster.TotalHealthyCount+" wounded="+p.MemberRoster.TotalWounded+" food="+F(p.Food)+" trade_gold="+p.PartyTradeGold+" hero="+Id(h)+" hero_gold="+(h==null?-1:h.Gold)+" hero_hp="+(h==null?-1:h.HitPoints)+" settlement="+S(p.CurrentSettlement==null?null:p.CurrentSettlement.StringId)+" sea="+p.IsCurrentlyAtSea+Roles(p));
  });}
  void TownSnapshot(Town t,string reason){Guard(()=>{if(t==null||t.Settlement==null)return;
   Write("kind=town reason="+reason+" id="+S(t.Settlement.StringId)+" governor="+Id(t.Governor)+" owner="+S(t.OwnerClan==null?null:t.OwnerClan.StringId)+" market_gold="+t.Gold+" construction_reserve="+t.BoostBuildingProcess+" security="+F(t.Security)+" prosperity="+F(t.Prosperity)+" loyalty="+F(t.Loyalty)+" food="+F(t.FoodStocks));
  });}
  void OnSession(CampaignGameStarter s){Models();Snapshot("session");}
  void OnLoaded(){Models();Snapshot("loaded");}
  void OnHourly(){Snapshot("hourly_event");}
  void OnFood(MobileParty p){if(p==MobileParty.MainParty)Snapshot("food_event");}
  void OnArrival(MobileParty p,Settlement s,Hero h){if(p!=MobileParty.MainParty)return;Guard(()=>{Write("kind=arrival settlement="+S(s==null?null:s.StringId)+" hero="+Id(h));Snapshot("arrival_event");if(s!=null)TownSnapshot(s.Town,"arrival_event");});}
  void OnDeparture(MobileParty p,Settlement s){if(p!=MobileParty.MainParty)return;Guard(()=>{Write("kind=departure settlement="+S(s==null?null:s.StringId));Snapshot("departure_event");if(s!=null)TownSnapshot(s.Town,"departure_event");});}
  void OnRecruit(Hero buyer,Settlement s,Hero seller,CharacterObject troop,int n){if(buyer!=Hero.MainHero && seller!=Hero.MainHero)return;Guard(()=>{Write("kind=recruit_event hero1="+Id(buyer)+" hero2="+Id(seller)+" settlement="+S(s==null?null:s.StringId)+" troop="+S(troop==null?null:troop.StringId)+" count="+n);Snapshot("recruit_event");});}
  bool Relevant(Town t){return t!=null&&((Clan.PlayerClan!=null&&t.OwnerClan==Clan.PlayerClan)||(MobileParty.MainParty!=null&&MobileParty.MainParty.CurrentSettlement==t.Settlement));}
  void OnGovernor(Town t,Hero oldG,Hero newG){Guard(()=>{if(!Relevant(t))return;Write("kind=governor_event old="+Id(oldG)+" new="+Id(newG));TownSnapshot(t,"governor_event");});}
  void OnTown(Town t){Guard(()=>{if(Relevant(t))TownSnapshot(t,"daily_town_event");});}
 }
}