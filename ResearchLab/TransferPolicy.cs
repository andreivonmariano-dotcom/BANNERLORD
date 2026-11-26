namespace CUResearchLab
{
    public static class TransferPolicy
    {
        public const int Amount = 100;
        public static bool CanTransfer(int purse, int treasury, bool eligible, bool attempted)
        {
            return eligible && !attempted && purse >= Amount && treasury >= 0 &&
                treasury <= int.MaxValue - Amount;
        }
        public static bool Confirmed(int purseBefore, int treasuryBefore, int purseAfter, int treasuryAfter)
        {
            return (long)purseBefore - purseAfter == Amount &&
                (long)treasuryAfter - treasuryBefore == Amount;
        }
    }
}
