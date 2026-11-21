using System;
using System.Globalization;

namespace CalradiaUnified
{
    /// <summary>
    /// At most two in-person acknowledgements of verified joint victories.
    /// No automatic battle pay, standing title, renown or kingdom action.
    /// </summary>
    public sealed class SandboxKnightBattleRecognitionState
    {
        public const string Version = "CU_KNIGHT_BATTLE_RECOGNITION_1";
        public string PatronId = "";
        public int Stage, LastDay = -1;

        private static bool ValidId(string id)
        {
            if (String.IsNullOrEmpty(id) || id.Length > 80)
                return false;
            foreach (char c in id)
                if (!Char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    return false;
            return true;
        }
        public int RequiredVictories
        {
            get { return Stage == 0 ? 2 : Stage == 1 ? 5 : Int32.MaxValue; }
        }
        public bool CanAcknowledge(string patronId, int victories,
            bool swornAndSpeaking, int day)
        {
            return ValidId(patronId) && day >= 0 && day <= 2000000 &&
                swornAndSpeaking && Stage < 2 &&
                (PatronId.Length == 0 || PatronId == patronId) &&
                victories >= RequiredVictories &&
                (LastDay < 0 || day > LastDay);
        }
        public bool MarkAcknowledged(string patronId, int victories,
            bool swornAndSpeaking, int day)
        {
            if (!CanAcknowledge(patronId, victories,
                swornAndSpeaking, day)) return false;
            if (PatronId.Length == 0) PatronId = patronId;
            Stage++;
            LastDay = day;
            return true;
        }
        public string Serialize()
        {
            return Version + "\n" + PatronId + "|" +
                Stage.ToString(CultureInfo.InvariantCulture) + "|" +
                LastDay.ToString(CultureInfo.InvariantCulture);
        }
        public static SandboxKnightBattleRecognitionState Parse(
            string packed)
        {
            var blank = new SandboxKnightBattleRecognitionState();
            if (String.IsNullOrEmpty(packed) || packed.Length > 150)
                return blank;
            var rows = packed.Replace("\r", "").Split('\n');
            if (rows.Length != 2 || rows[0] != Version)
                return blank;
            var cells = rows[1].Split('|');
            int stage, day;
            if (cells.Length != 3 ||
                !Int32.TryParse(cells[1], NumberStyles.None,
                    CultureInfo.InvariantCulture, out stage) ||
                !Int32.TryParse(cells[2], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out day) ||
                stage < 0 || stage > 2 || day < -1 ||
                (stage > 0 && (!ValidId(cells[0]) || day < 0)) ||
                (stage == 0 && (cells[0].Length > 0 || day != -1)))
                return blank;
            return new SandboxKnightBattleRecognitionState {
                PatronId = cells[0], Stage = stage, LastDay = day
            };
        }
    }
}
