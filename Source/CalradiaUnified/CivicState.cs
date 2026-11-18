using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// Compact, versioned primitive-only save state. No engine handles or object references are serialized.
    /// Settlement IDs are validated so corrupted payloads fail closed.
    /// </summary>
    public sealed class CivicState
    {
        public sealed class Depot
        {
            public string SettlementId;
            public int Grain;
            public int Investment;
        }
        public sealed class Shipment
        {
            public string From;
            public string To;
            public int Grain;
            public int DaysRemaining;
            public string PartyId = ""; // empty for older abstract shipments
            public bool Physical;
        }
        public sealed class CouncilRecord
        {
            public string Id;
            public int LevyToday;
            public int MissionCooldown;
            public int LocalToday;
            public int GarrisonToday;
        }
        public readonly List<Depot> Depots = new List<Depot>();
        public readonly List<Shipment> Shipments = new List<Shipment>();
        public readonly List<CouncilRecord> Council = new List<CouncilRecord>();
        public CouncilRecord CouncilFor(string id)
        {
            if (!Valid(id)) return null;
            CouncilRecord result = Council.FirstOrDefault(x => x.Id == id);
            if (result == null && Council.Count < 512)
            {
                result = new CouncilRecord { Id = id };
                Council.Add(result);
            }
            return result;
        }

        public Depot Get(string id)
        {
            if (!Valid(id)) return null;
            Depot depot = Depots.FirstOrDefault(d => d.SettlementId == id);
            if (depot == null && Depots.Count < 512)
            {
                depot = new Depot { SettlementId = id };
                Depots.Add(depot);
            }
            return depot;
        }
        public int Stock(string id)
        {
            var d = Depots.FirstOrDefault(x => x.SettlementId == id);
            return d == null ? 0 : d.Grain;
        }
        public bool HasShipment(string from)
        {
            return Shipments.Any(s => s.From == from);
        }
        public string Serialize()
        {
            var parts = new List<string> { "CU5" };
            foreach (var d in Depots.OrderBy(x => x.SettlementId))
                parts.Add("D," + d.SettlementId + "," + d.Grain.ToString(CultureInfo.InvariantCulture)
                    + "," + d.Investment.ToString(CultureInfo.InvariantCulture));
            foreach (var s in Shipments.OrderBy(x => x.From).ThenBy(x => x.To))
                parts.Add("S," + s.From + "," + s.To + "," + s.Grain.ToString(CultureInfo.InvariantCulture)
                    + "," + s.DaysRemaining.ToString(CultureInfo.InvariantCulture)
                    + "," + (s.Physical ? "P" : "A") + "," + (s.PartyId ?? ""));
            foreach (var c in Council.OrderBy(x => x.Id))
                parts.Add("K," + c.Id + "," + c.LevyToday.ToString(CultureInfo.InvariantCulture)
                    + "," + c.MissionCooldown.ToString(CultureInfo.InvariantCulture)
                    + "," + c.LocalToday.ToString(CultureInfo.InvariantCulture)
                    + "," + c.GarrisonToday.ToString(CultureInfo.InvariantCulture));
            return String.Join(";", parts);
        }
        public static CivicState Parse(string data)
        {
            var result = new CivicState();
            if (String.IsNullOrEmpty(data) || data.Length > 150000 ||
                !(data == "CU3" || data == "CU4" || data == "CU5" ||
                data.StartsWith("CU3;", StringComparison.Ordinal) ||
                data.StartsWith("CU4;", StringComparison.Ordinal) ||
                data.StartsWith("CU5;", StringComparison.Ordinal))) return result;
            var records = data.Split(';');
            foreach (var record in records.Skip(1))
            {
                var fields = record.Split(',');
                int a, b;
                if (fields.Length == 4 && fields[0] == "D" && Valid(fields[1])
                    && Int32.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out a)
                    && Int32.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out b)
                    && result.Depots.Count < 512 && result.Depots.All(x => x.SettlementId != fields[1]))
                {
                    result.Depots.Add(new Depot { SettlementId = fields[1], Grain = Limit(a, 0, 100000),
                        Investment = Limit(b, 0, 10) });
                }
                else if ((fields.Length == 5 || fields.Length == 7) && fields[0] == "S"
                    && Valid(fields[1]) && Valid(fields[2])
                    && Int32.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out a)
                    && Int32.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out b)
                    && result.Shipments.Count < 512)
                {
                    bool physical = fields.Length == 7 && fields[5] == "P";
                    string partyId = physical && Valid(fields[6]) ? fields[6] : "";
                    // Physical records without a party identifier are not actionable.
                    if (!physical || partyId.Length > 0)
                        result.Shipments.Add(new Shipment { From = fields[1], To = fields[2],
                            Grain = Limit(a, 0, 1000), DaysRemaining = Limit(b, 0, 365),
                            Physical = physical, PartyId = partyId });
                }
                else if ((fields.Length == 4 || fields.Length == 6) && fields[0] == "K" && Valid(fields[1])
                    && Int32.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out a)
                    && Int32.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out b)
                    && result.Council.Count < 512 && result.Council.All(x => x.Id != fields[1]))
                {
                    int local = 0, garrison = 0;
                    if (fields.Length == 6 &&
                        (!Int32.TryParse(fields[4], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out local) ||
                         !Int32.TryParse(fields[5], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out garrison)))
                        continue;
                    result.Council.Add(new CouncilRecord { Id = fields[1],
                        LevyToday = Limit(a, 0, 100), MissionCooldown = Limit(b, 0, 365),
                        LocalToday = Limit(local, 0, 100),
                        GarrisonToday = Limit(garrison, 0, 100) });
                }
            }
            return result;
        }
        public static bool Valid(string id)
        {
            if (String.IsNullOrEmpty(id) || id.Length > 100) return false;
            return id.All(c => Char.IsLetterOrDigit(c) || c == '_' || c == '-');
        }
        public static int Limit(int x, int low, int high) { return Math.Max(low, Math.Min(high, x)); }
    }
}
