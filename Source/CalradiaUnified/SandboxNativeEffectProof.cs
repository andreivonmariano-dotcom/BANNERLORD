using System;

namespace CalradiaUnified
{
    /// <summary>
    /// Native game effects are facts only after both sides of a transfer,
    /// or the real relationship score, have been observed after the action.
    /// All values refer to the actual Heroes, not a button or intention.
    /// </summary>
    public static class SandboxNativeEffectProof
    {
        public static string GiftResult(int payerBefore,int payerAfter,
            int recipientBefore,int recipientAfter,int intended)
        {
            if(intended<=0 || payerBefore<0 || payerAfter<0 ||
                recipientBefore<0 || recipientAfter<0)
                return "invalid";
            long debit=(long)payerBefore-payerAfter;
            long credit=(long)recipientAfter-recipientBefore;
            if(debit==intended && credit==intended)return "confirmed";
            if(debit==0 && credit==0)return "no_native_effect";
            return "unbalanced_native_effect";
        }
        public static bool AnyPayerDebit(int before,int after)
        {
            return before>after;
        }
        public static int ObservedDelta(int before,int after)
        {
            if(before< -100 || before>100 ||
                after< -100 || after>100)return 0;
            return after-before;
        }
        public static bool RelationConfirmed(int before,int after,
            int requested)
        {
            if(requested==0)return before==after;
            if(before< -100 || before>100 ||
                after< -100 || after>100)return false;
            int bounded=Math.Max(-100,Math.Min(100,before+requested));
            return after==bounded && after!=before;
        }
    }
}
