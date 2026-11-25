using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Bounded receipts for real native campaign battles. Native MapEventParty
    /// death/wound rosters supply counts. Zero is a real observed zero;
    /// -1 means the older save has no casualty observation.
    /// No personal kill, injury cause, salary, renown or promotion inferred.
    /// </summary>
    public sealed class SandboxBattleHistoryState
    {
        public const string Version = "CU_BATTLE_HISTORY_3";
        private const string PreviousVersion = "CU_BATTLE_HISTORY_2";
        private const string LegacyVersion = "CU_BATTLE_HISTORY_1";
        public const int MaxEntries = 32;
        private const int MaxNamedHeroes = 4;
        private readonly List<Entry> entries = new List<Entry>();
        public sealed class Entry
        {
            public string Key = "", Kind = "", Outcome = "";
            public string Source = "", SettlementId = "", LordId = "";
            public string PatronWitnessId = "";
            // Empty for older saves and for battles outside native mercenary service.
            public string MercenaryHeroId = "", MercenaryKingdomId = "";
            // IDs are actual engine Hero.StringId, never names invented by CU.
            public string WoundedHeroIds = "", FallenHeroIds = "";
            public int Day, PartyDead = -1, PartyWounded = -1;
        }
        public IList<Entry> Entries { get { return entries.AsReadOnly(); } }

        private static bool ValidId(string value, bool optional)
        {
            if (value == null || value.Length > 80 ||
                (!optional && value.Length == 0)) return false;
            foreach (char c in value)
                if (!Char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    return false;
            return true;
        }
        private static bool ValidIds(string values)
        {
            if (values == null || values.Length > 324) return false;
            if (values.Length == 0) return true;
            var parts = values.Split(',');
            return parts.Length <= MaxNamedHeroes &&
                parts.All(p => ValidId(p, false)) &&
                parts.Distinct(StringComparer.Ordinal).Count() == parts.Length;
        }
        private static bool ValidKey(string key)
        {
            return key != null && key.Length == 16 &&
                key.All(c => (c >= '0' && c <= '9') ||
                             (c >= 'a' && c <= 'f'));
        }
        private static bool ValidKind(string kind)
        {
            return kind == "Field" || kind == "Siege" ||
                kind == "Raid" || kind == "Hideout" || kind == "Other";
        }
        private static bool ValidOutcome(string result)
        {
            return result == "Victory" || result == "Defeat" ||
                result == "Unresolved";
        }
        private static bool ValidSource(string source)
        {
            return source == "PlayerParty" || source == "ServeAsSoldier";
        }

        // Old callers and v0.72 tests retain their original behavior.
        public bool Record(string key, int day, string kind,
            string outcome, string source, string settlementId,
            string lordId)
        {
            return RecordObserved(key, day, kind, outcome, source,
                settlementId, lordId, "", -1, -1, "", "");
        }

        public bool RecordObserved(string key, int day, string kind,
            string outcome, string source, string settlementId,
            string lordId, string patronWitnessId, int partyDead,
            int partyWounded, string woundedHeroIds,
            string fallenHeroIds, string mercenaryHeroId = "",
            string mercenaryKingdomId = "")
        {
            if (!ValidKey(key) || day < 0 || day > 2000000 ||
                !ValidKind(kind) || !ValidOutcome(outcome) ||
                !ValidSource(source) || !ValidId(settlementId, true) ||
                !ValidId(lordId, true) ||
                !ValidId(patronWitnessId, true) ||
                !ValidId(mercenaryHeroId, true) || !ValidId(mercenaryKingdomId, true) ||
                ((mercenaryHeroId.Length==0)!=(mercenaryKingdomId.Length==0)) ||
                (source!="PlayerParty" && mercenaryKingdomId.Length>0) ||
                !ValidIds(woundedHeroIds) || !ValidIds(fallenHeroIds) ||
                partyDead < -1 || partyDead > 100000 ||
                partyWounded < -1 || partyWounded > 100000 ||
                ((partyDead == -1) != (partyWounded == -1)) ||
                // A named Hero cannot outnumber a verified roster count.
                (partyDead >= 0 &&
                    (woundedHeroIds.Length > 0 &&
                     woundedHeroIds.Split(',').Length > partyWounded ||
                     fallenHeroIds.Length > 0 &&
                     fallenHeroIds.Split(',').Length > partyDead)) ||
                // Names require an observed native casualty roster.
                (partyDead < 0 && (woundedHeroIds.Length != 0 ||
                    fallenHeroIds.Length != 0)) ||
                // "Patron witness" is reserved for independent player party.
                (source != "PlayerParty" && patronWitnessId.Length > 0) ||
                (woundedHeroIds.Length>0 && fallenHeroIds.Length>0 &&
                    woundedHeroIds.Split(',').Intersect(fallenHeroIds.Split(','),StringComparer.Ordinal).Any()) ||
                entries.Any(e => e.Key == key))
                return false;

            if (entries.Count == MaxEntries) entries.RemoveAt(0);
            entries.Add(new Entry {
                Key = key, Day = day, Kind = kind, Outcome = outcome,
                Source = source, SettlementId = settlementId,
                LordId = lordId, PatronWitnessId = patronWitnessId,
                PartyDead = partyDead, PartyWounded = partyWounded,
                WoundedHeroIds = woundedHeroIds,
                FallenHeroIds = fallenHeroIds,
                MercenaryHeroId = mercenaryHeroId, MercenaryKingdomId = mercenaryKingdomId
            });
            return true;
        }

        public static string EventKey(string canonical)
        {
            if (String.IsNullOrEmpty(canonical) || canonical.Length > 2048)
                return "";
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (char c in canonical)
                {
                    hash ^= (byte)c;
                    hash *= 1099511628211UL;
                    hash ^= (byte)(c >> 8);
                    hash *= 1099511628211UL;
                }
                return hash.ToString("x16", CultureInfo.InvariantCulture);
            }
        }

        public int WitnessedVictoriesWith(string actualPatronId)
        {
            if (!ValidId(actualPatronId, false)) return 0;
            return entries.Count(e => e.Source == "PlayerParty" &&
                e.Outcome == "Victory" &&
                e.PatronWitnessId == actualPatronId);
        }

        // Old battle receipts do not prove employment at the time of the battle.
        public int LatestMercenaryVictory(string heroId,string kingdomId,int afterDay)
        {
            if(!ValidId(heroId,false) || !ValidId(kingdomId,false))return -1;
            return entries.Where(e=>e.Source=="PlayerParty" && e.Outcome=="Victory" &&
                e.Day>afterDay && e.MercenaryHeroId==heroId &&
                e.MercenaryKingdomId==kingdomId).Select(e=>e.Day)
                .DefaultIfEmpty(-1).Max();
        }

        private static string VisibleHeroes(string ids,
            Func<string, string> nameLookup)
        {
            if (nameLookup == null || String.IsNullOrEmpty(ids))
                return ids;
            return String.Join(", ", ids.Split(',')
                .Select(id => {
                    string name = nameLookup(id);
                    return String.IsNullOrWhiteSpace(name) ? id : name;
                }));
        }

        public string DescribeRecent(int count,
            Func<string, string> nativeNameLookup = null)
        {
            if (entries.Count == 0)
                return "No completed battles have been witnessed yet.";
            return String.Join("\n", entries.AsEnumerable().Reverse()
                .Take(Math.Max(0, Math.Min(count, 8)))
                .Select(e => "Day " + e.Day + ": " + e.Kind + " " +
                    e.Outcome + " (" +
                    (e.Source == "ServeAsSoldier" ?
                     "enlisted host army" : "player party") + ")" +
                    (e.SettlementId.Length > 0 ?
                     " at " + e.SettlementId : "") +
                    (e.LordId.Length > 0 ?
                     " under " + e.LordId : "") +
                    (e.PartyDead >= 0 ? " | party fallen " +
                     e.PartyDead + ", wounded " + e.PartyWounded : "") +
                    (e.WoundedHeroIds.Length > 0 ?
                     " | wounded own-clan Heroes: " +
                     VisibleHeroes(e.WoundedHeroIds, nativeNameLookup) : "") +
                    (e.FallenHeroIds.Length > 0 ?
                     " | fallen own-clan Heroes: " +
                     VisibleHeroes(e.FallenHeroIds, nativeNameLookup) : "") +
                    (e.PatronWitnessId.Length > 0 ?
                     " | same-side patron " + e.PatronWitnessId : "")));
        }

        public string Serialize()
        {
            var rows = new List<string> { Version };
            foreach (var e in entries)
                rows.Add(String.Join("|", new[] {
                    e.Key, e.Day.ToString(CultureInfo.InvariantCulture),
                    e.Kind, e.Outcome, e.Source, e.SettlementId,
                    e.LordId, e.PatronWitnessId,
                    e.PartyDead.ToString(CultureInfo.InvariantCulture),
                    e.PartyWounded.ToString(CultureInfo.InvariantCulture),
                    e.WoundedHeroIds, e.FallenHeroIds,
                    e.MercenaryHeroId, e.MercenaryKingdomId
                }));
            return String.Join("\n", rows);
        }

        public static SandboxBattleHistoryState Parse(string packed)
        {
            var empty = new SandboxBattleHistoryState();
            if (String.IsNullOrEmpty(packed) || packed.Length > 49152)
                return empty;
            var rows = packed.Replace("\r", "").Split('\n');
            if (rows.Length < 1 || rows.Length > MaxEntries + 1 ||
                (rows[0] != Version && rows[0] != PreviousVersion &&
                    rows[0] != LegacyVersion))
                return empty;
            bool old = rows[0] == LegacyVersion;
            bool current = rows[0] == Version;
            var loaded = new SandboxBattleHistoryState();
            for (int i = 1; i < rows.Length; i++)
            {
                var cells = rows[i].Split('|');
                int day, died = -1, wounded = -1;
                if (cells.Length != (old ? 7 : current ? 14 : 12) ||
                    !Int32.TryParse(cells[1], NumberStyles.None,
                        CultureInfo.InvariantCulture, out day))
                    return empty;
                if (!old && (!Int32.TryParse(cells[8],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out died) ||
                    !Int32.TryParse(cells[9], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out wounded)))
                    return empty;
                if (!loaded.RecordObserved(cells[0], day, cells[2],
                    cells[3], cells[4], cells[5], cells[6],
                    old ? "" : cells[7], died, wounded,
                    old ? "" : cells[10], old ? "" : cells[11],
                    current ? cells[12] : "", current ? cells[13] : ""))
                    return empty;
            }
            return loaded;
        }
    }
}
