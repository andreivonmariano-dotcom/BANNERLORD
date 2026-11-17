using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    public sealed partial class CivicBehavior
    {
        private void RegisterPhysicalConvoyMenu(CampaignGameStarter starter, string menu)
        {
            starter.AddGameMenuOption(menu, "cu_guarded_convoy", "CU: Dispatch guarded grain convoy",
                x => CanVisit() && PhysicalConvoyEligible(), x => Safe(DispatchPhysicalConvoy), false);
        }
        private int CountActiveConvoys()
        {
            return state.Shipments.Count(x => x.Physical);
        }
        private bool PhysicalConvoyEligible()
        {
            if (!settings.EnablePhysicalConvoys || !Owned()) return false;
            var target = Destination();
            var source = CurrentDepot();
            return target != null && source != null && ConvoyPolicy.CanDispatch(
                settings.EnablePhysicalConvoys, Owned(), target.OwnerClan == Clan.PlayerClan,
                source.Grain, settings.ConvoyCargoGrain, CountActiveConvoys(), settings.MaxActiveConvoys,
                state.HasShipment(source.SettlementId));
        }
        private void DispatchPhysicalConvoy()
        {
            if (!PhysicalConvoyEligible() || DefaultItems.Grain == null) return;
            var sourceSettlement = Settlement.CurrentSettlement;
            var destination = Destination();
            var sourceDepot = CurrentDepot();
            var troop = LocalRecruit();
            if (troop == null || destination == null || sourceSettlement == null || sourceDepot == null) return;
            MobileParty party = null;
            try
            {
                // Check movement API first. Do not create an orphan party on unsupported versions.
                MethodInfo move = ConvoyMovementMethod();
                if (move == null) { Notice("Convoy route API unavailable on this game build."); return; }
                var soldiers = TroopRoster.CreateDummyTroopRoster();
                soldiers.AddToCounts(troop, settings.ConvoyGuards);
                var prisoners = TroopRoster.CreateDummyTroopRoster();
                party = CustomPartyComponent.CreateCustomPartyWithTroopRoster(
                    sourceSettlement.GatePosition, 1f, sourceSettlement,
                    new TextObject("Calradia Unified Supply Convoy"), Clan.PlayerClan,
                    soldiers, prisoners, Hero.MainHero, "", "", 0f, true);
                if (party == null || party.MemberRoster == null || party.MemberRoster.TotalManCount <= 0)
                    throw new InvalidOperationException("Custom convoy party was not initialized with guards.");
                party.ItemRoster.AddToCounts(DefaultItems.Grain, settings.ConvoyCargoGrain);
                if (!InvokeMove(party, destination, move))
                    throw new InvalidOperationException("No valid engine route order could be issued.");
                var shipment = new CivicState.Shipment {
                    From = sourceSettlement.StringId, To = destination.StringId,
                    Grain = settings.ConvoyCargoGrain, Physical = true,
                    PartyId = party.StringId, DaysRemaining = settings.ConvoyTimeoutDays
                };
                if (!CivicState.Valid(shipment.PartyId))
                    throw new InvalidOperationException("Engine supplied invalid party identifier.");
                sourceDepot.Grain -= shipment.Grain;
                state.Shipments.Add(shipment);
                Notice("Guarded convoy sent to " + destination.Name + ". Its party can be attacked on the campaign map.");
            }
            catch (Exception e)
            {
                Log.Write("Convoy creation failed (no depot stock deducted): " + e);
                if (party != null) TryDestroyConvoy(party);
            }
        }
        private static MethodInfo ConvoyMovementMethod()
        {
            return typeof(MobileParty).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "SetMoveGoToSettlement")
                .Where(m => {
                    var p = m.GetParameters();
                    return p.Length >= 1 && p[0].ParameterType == typeof(Settlement)
                        && p.Skip(1).All(x => x.IsOptional || x.ParameterType == typeof(bool)
                           || x.ParameterType.IsEnum);
                }).OrderBy(m => m.GetParameters().Length).FirstOrDefault();
        }
        private static bool InvokeMove(MobileParty party, Settlement destination, MethodInfo method)
        {
            if (party == null || destination == null || method == null) return false;
            var p = method.GetParameters();
            var args = new object[p.Length];
            args[0] = destination;
            for (int i = 1; i < p.Length; i++)
            {
                if (p[i].ParameterType.IsEnum) args[i] = Enum.ToObject(p[i].ParameterType, 0);
                else if (p[i].ParameterType == typeof(bool)) args[i] = false;
                else if (p[i].IsOptional) args[i] = p[i].DefaultValue;
                else return false;
            }
            method.Invoke(party, args); // engine pathfinding, never teleport
            return true;
        }
        private static void TryDestroyConvoy(MobileParty party)
        {
            try { if (party != null) DestroyPartyAction.Apply(null, party); }
            catch (Exception ex) { Log.Write("Convoy cleanup failed: " + ex); }
        }
        private void TickPhysicalConvoys()
        {
            foreach (var shipment in state.Shipments.Where(s => s.Physical).ToArray())
            {
                shipment.DaysRemaining--;
                var party = MobileParty.All.FirstOrDefault(x => x != null && x.StringId == shipment.PartyId);
                if (ConvoyPolicy.Lost(party != null, party != null && party.IsActive,
                    party != null && party.MemberRoster != null && party.MemberRoster.TotalManCount > 0))
                {
                    Notice("A supply convoy was destroyed or disappeared. Its cargo was lost.");
                    state.Shipments.Remove(shipment);
                    continue;
                }
                var target = Settlement.All.FirstOrDefault(s => s != null && s.StringId == shipment.To);
                if (target != null && party.CurrentSettlement == target)
                {
                    var targetDepot = state.Get(shipment.To);
                    if (target.OwnerClan == Clan.PlayerClan && targetDepot != null && DefaultItems.Grain != null)
                    {
                        int actual = party.ItemRoster.GetItemNumber(DefaultItems.Grain);
                        int cargo = ConvoyPolicy.DeliverableGrain(shipment.Grain, actual,
                            settings.DepotCapacity - targetDepot.Grain);
                        targetDepot.Grain += cargo;
                        Notice("Supply convoy arrived at " + target.Name + ": " + cargo + " grain delivered.");
                    }
                    else Notice("Convoy reached a lost settlement. Its cargo was not deposited.");
                    TryDestroyConvoy(party);
                    state.Shipments.Remove(shipment);
                    continue;
                }
                if (ConvoyPolicy.TimedOut(shipment.DaysRemaining))
                {
                    // Do not teleport or credit cargo after a stalled journey.
                    TryDestroyConvoy(party);
                    state.Shipments.Remove(shipment);
                    Notice("A supply convoy timed out without delivery. No grain was credited.");
                }
            }
        }
    }
}
