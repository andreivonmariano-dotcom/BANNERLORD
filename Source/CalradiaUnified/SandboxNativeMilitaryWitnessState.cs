using System;
using System.Globalization;

namespace CalradiaUnified
{
    /// <summary>
    /// Receipts for actual Kingdom membership and native Army command.
    /// This state never issues a clan join, army formation or appointment.
    /// </summary>
    public sealed class SandboxNativeMilitaryWitnessState
    {
        public const string Version="CU_NATIVE_MILITARY_WITNESS_1";
        public string KingdomId="";
        public int VassalDay=-1,ArmyCommandDay=-1;
        public bool Observe(int day,string kingdomId,
            bool nativeVassal,bool nativeArmyCommander)
        {
            if(day<0 || String.IsNullOrEmpty(kingdomId) ||
                kingdomId.Length>80 || !nativeVassal)
                return false;
            foreach(var c in kingdomId)
                if(!Char.IsLetterOrDigit(c) && c!='_' && c!='-')
                    return false;
            if(KingdomId.Length>0 && KingdomId!=kingdomId)
                return false;
            bool changed=false;
            if(KingdomId.Length==0)KingdomId=kingdomId;
            if(VassalDay<0)
            {
                VassalDay=day;
                changed=true;
            }
            if(nativeArmyCommander && ArmyCommandDay<0)
            {
                ArmyCommandDay=day;
                changed=true;
            }
            return changed;
        }
        public string Serialize()
        {
            return Version+"\n"+String.Join("|",new[]{
                KingdomId,
                VassalDay.ToString(CultureInfo.InvariantCulture),
                ArmyCommandDay.ToString(CultureInfo.InvariantCulture)});
        }
        public static SandboxNativeMilitaryWitnessState Parse(string input)
        {
            var empty=new SandboxNativeMilitaryWitnessState();
            if(String.IsNullOrEmpty(input) || input.Length>180)
                return empty;
            var rows=input.Replace("\r","").Split('\n');
            if(rows.Length!=2 || rows[0]!=Version)return empty;
            var f=rows[1].Split('|');
            int vassal,army;
            if(f.Length!=3 ||
                !Int32.TryParse(f[1],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out vassal) ||
                !Int32.TryParse(f[2],NumberStyles.Integer,
                    CultureInfo.InvariantCulture,out army) ||
                vassal< -1 || army< -1 ||
                (army>=0 && (vassal<0 || army<vassal)) ||
                (vassal>=0 && f[0].Length==0) ||
                f[0].Length>80)return empty;
            foreach(char c in f[0])
                if(!Char.IsLetterOrDigit(c) && c!='_' && c!='-')
                    return empty;
            return new SandboxNativeMilitaryWitnessState{
                KingdomId=f[0],VassalDay=vassal,
                ArmyCommandDay=army};
        }
    }
}
