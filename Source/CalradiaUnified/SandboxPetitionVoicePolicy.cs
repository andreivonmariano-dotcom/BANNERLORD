namespace CalradiaUnified
{
    /// <summary>
    /// Dialogue tone is grounded in actual native Hero traits,
    /// current relation and the already observed character goal.
    /// No invented family ties, memories or accomplishments.
    /// </summary>
    public static class SandboxPetitionVoicePolicy
    {
        public static string Style(string goal,int nativeHonor,
            int nativeGenerosity,int nativeCalculating,int relation)
        {
            if(relation<0 ||
                (nativeCalculating>=2 && relation<20))
                return "Guarded";
            if((goal=="ProtectSharedHousehold" ||
                goal=="SupportHousehold" ||
                goal=="MaintainHousehold") && nativeHonor>=1)
                return "Duty";
            if((goal=="EstablishLivelihood" ||
                goal=="BuildTradingContacts") &&
                nativeCalculating>=1)
                return "Ambitious";
            if(nativeGenerosity>=1 && relation>=10)
                return "Warm";
            return "Direct";
        }
    }
}
