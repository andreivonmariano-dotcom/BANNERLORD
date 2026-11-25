using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace CalradiaUnified
{
    // Retired party-order adapter. Preserve historical cooldown state without executing orders.
    public sealed class KingdomAutonomyBehavior : CampaignBehaviorBase
    {
        private int campaignDay;
        private readonly Dictionary<string, int> nextOrderDay = new Dictionary<string, int>(StringComparer.Ordinal);
        public KingdomAutonomyBehavior(Settings settings) { }

        public override void RegisterEvents()
        {
            // Compatibility serialization only. Do not compete with native or external party AI.
        }

        public override void SyncData(IDataStore store)
        {
            // A primitive string prevents custom SaveableTypeDefiner dependencies.
            string encoded = Serialize();
            store.SyncData("CU_v060_kingdom_orders", ref encoded);
            store.SyncData("CU_v060_kingdom_day", ref campaignDay);
            Restore(encoded);
            if (campaignDay < 0) campaignDay = 0;
        }

        private static bool SafeId(string id)
        {
            return !String.IsNullOrEmpty(id) && id.Length <= 100 &&
                   id.All(c => Char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }
        private string Serialize()
        {
            var b = new StringBuilder("CU6");
            foreach (var record in nextOrderDay.OrderBy(x => x.Key, StringComparer.Ordinal).Take(1024))
                if (SafeId(record.Key)) b.Append(';').Append(record.Key).Append(',')
                    .Append(Math.Max(0, record.Value).ToString(CultureInfo.InvariantCulture));
            return b.ToString();
        }
        private void Restore(string packed)
        {
            nextOrderDay.Clear();
            if (String.IsNullOrEmpty(packed) || packed.Length > 64000 || !packed.StartsWith("CU6;")) return;
            foreach (string item in packed.Split(';').Skip(1).Take(1024))
            {
                string[] parts = item.Split(',');
                int day;
                if (parts.Length == 2 && SafeId(parts[0]) &&
                    Int32.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out day) && day >= 0)
                    nextOrderDay[parts[0]] = day;
            }
        }
    }
}
