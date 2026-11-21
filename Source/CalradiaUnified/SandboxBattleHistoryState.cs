using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Read-only receipts for real finished campaign map encounters.
    /// Does not award renown, ranks, wages, relations, kills or casualties.
    /// The battle event provides party involvement, not proof the player
    /// personally struck an enemy.
    /// </summary>
    public sealed class SandboxBattleHistoryState
    {
        public const string Version = "CU_BATTLE_HISTORY_1";
        public const int MaxEntries = 32;
        private readonly List<Entry> entries = new List<Entry>();

        public sealed class Entry
        {
            public string Key = "", Kind = "", Outcome = "";
            public string Source = "", SettlementId = "", LordId = "";
            public int Day;
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
        private static bool ValidKey(string key)
        {
            return key != null && key.Length == 16 &&
                key.All(c => (c >= '0' && c <= '9') ||
                             (c >= 'a' && c <= 'f'));
        }
        private static bool ValidKind(string value)
        {
            return value == "Field" || value == "Siege" ||
                value == "Raid" || value == "Hideout" ||
                value == "Other";
        }
        private static bool ValidOutcome(string value)
        {
            return value == "Victory" || value == "Defeat" ||
                value == "Unresolved";
        }
        private static bool ValidSource(string value)
        {
            return value == "PlayerParty" || value == "ServeAsSoldier";
        }

        public bool Record(string key, int day, string kind,
            string outcome, string source, string settlementId,
            string lordId)
        {
            if (!ValidKey(key) || day < 0 || day > 2000000 ||
                !ValidKind(kind) || !ValidOutcome(outcome) ||
                !ValidSource(source) ||
                !ValidId(settlementId, true) ||
                !ValidId(lordId, true) ||
                entries.Any(e => e.Key == key))
                return false;

            // The newest receipts survive long campaigns, with bounded saves.
            if (entries.Count == MaxEntries) entries.RemoveAt(0);
            entries.Add(new Entry {
                Key = key, Day = day, Kind = kind, Outcome = outcome,
                Source = source, SettlementId = settlementId,
                LordId = lordId
            });
            return true;
        }

        /// <summary>A stable key over canonical engine-observed event data.</summary>
        public static string EventKey(string canonical)
        {
            if (String.IsNullOrEmpty(canonical) || canonical.Length > 2048)
                return "";
            // FNV-1a, defined explicitly so the hash is stable across loads.
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

        public string DescribeRecent(int count)
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
                     " under " + e.LordId : "")));
        }

        public string Serialize()
        {
            var rows = new List<string> { Version };
            foreach (var e in entries)
                rows.Add(String.Join("|", new[] {
                    e.Key, e.Day.ToString(CultureInfo.InvariantCulture),
                    e.Kind, e.Outcome, e.Source, e.SettlementId, e.LordId
                }));
            return String.Join("\n", rows);
        }

        public static SandboxBattleHistoryState Parse(string packed)
        {
            var empty = new SandboxBattleHistoryState();
            if (String.IsNullOrEmpty(packed) || packed.Length > 12000)
                return empty;
            var rows = packed.Replace("\r", "").Split('\n');
            if (rows.Length < 1 || rows.Length > MaxEntries + 1 ||
                rows[0] != Version) return empty;
            var loaded = new SandboxBattleHistoryState();
            for (int i = 1; i < rows.Length; i++)
            {
                var cells = rows[i].Split('|');
                int day;
                if (cells.Length != 7 ||
                    !Int32.TryParse(cells[1], NumberStyles.None,
                        CultureInfo.InvariantCulture, out day) ||
                    !loaded.Record(cells[0], day, cells[2], cells[3],
                        cells[4], cells[5], cells[6]))
                    return empty;
            }
            return loaded;
        }
    }
}
