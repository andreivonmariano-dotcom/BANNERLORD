using System;
using System.Collections;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// No settlement departure hooks. Observes the player's food and army.
    /// An opt-in, save-safe forage effect adds a limited amount of vanilla grain
    /// to the player's roster after a cooldown. Never orders NPC parties.
    /// </summary>
    public sealed class SupplyBehavior : CampaignBehaviorBase
    {
        private readonly Settings _settings;
        private int _daysSinceForage;

        public SupplyBehavior(Settings settings)
        {
            _settings = settings;
            _daysSinceForage = settings.ForageEveryDays;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyParty);
        }

        public override void SyncData(IDataStore store)
        {
            store.SyncData("CU_v020_days_since_forage", ref _daysSinceForage);
            if (_daysSinceForage < 0) _daysSinceForage = 0;
            if (_daysSinceForage > 100000) _daysSinceForage = _settings.ForageEveryDays;
        }

        private void OnDailyParty(MobileParty party)
        {
            if (party == null || !Object.ReferenceEquals(party, MobileParty.MainParty)) return;
            try
            {
                if (_daysSinceForage < 100000) _daysSinceForage++;
                int days = party.GetNumDaysForFoodToLast();
                if (_settings.EnableSupplyWarnings && SupplyPolicy.IsLowFood(days, _settings.SupplyWarningDays))
                {
                    Show("[Calradia Unified] Low supplies: about " + days + " day(s) remain.");
                }
                if (_settings.EnableArmyWarnings) ReportArmyFood(party);
                bool inSettlement = Read(party, "CurrentSettlement") != null;
                bool inArmy = Read(party, "Army") != null;
                bool inEvent = Read(party, "MapEvent") != null;
                bool besieged = Read(party, "BesiegedSettlement") != null ||
                    Read(party, "SiegeEvent") != null;
                if (!SupplyPolicy.ShouldForage(_settings.EnablePlayerForaging, days,
                    _settings.ForageWhenDaysAtMost, inSettlement, inArmy, inEvent, besieged,
                    _daysSinceForage, _settings.ForageEveryDays)) return;

                // A verified vanilla item and roster entry point, guarded for nulls.
                var grain = DefaultItems.Grain;
                if (grain == null || party.ItemRoster == null) return;
                party.ItemRoster.AddToCounts(grain, _settings.GrainPerForage);
                _daysSinceForage = 0;
                Show("[Calradia Unified] Foraging gathered " + _settings.GrainPerForage + " grain.");
                Log.Write("Player-only foraging added grain=" + _settings.GrainPerForage);
            }
            catch (Exception ex)
            {
                DiagnosticJournal.Exception("player_supply_daily_tick", ex);
                Log.Write("Daily player supply event failed: " + ex);
            }
        }

        private void ReportArmyFood(MobileParty mainParty)
        {
            try
            {
                object army = Read(mainParty, "Army");
                var parties = Read(army, "Parties") as IEnumerable;
                if (parties == null) return;
                int inspected = 0, critical = 0;
                foreach (var item in parties)
                {
                    var member = item as MobileParty;
                    if (member == null) continue;
                    int foodDays = member.GetNumDaysForFoodToLast();
                    if (foodDays < 0) continue;
                    inspected++;
                    if (SupplyPolicy.IsLowFood(foodDays, _settings.SupplyWarningDays)) critical++;
                }
                if (critical > 0)
                    Show("[Calradia Unified] Army provisions: " + critical + " of " + inspected
                        + " inspected parties are low on food.");
            }
            catch (Exception ex) { Log.Write("Army advisor check skipped: " + ex.GetType().Name); }
        }

        private static object Read(object instance, string name)
        {
            if (instance == null) return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var property = instance.GetType().GetProperty(name, flags);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(instance, null);
            var field = instance.GetType().GetField(name, flags);
            return field == null ? null : field.GetValue(instance);
        }

        private static void Show(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message));
        }
    }
}
