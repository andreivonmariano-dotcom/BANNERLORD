using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace CalradiaUnified.CU80
{
    public enum AttemptState { Started=1, Confirmed=2, Uncertain=3, Declined=4, Deferred=5, NoAction=6, ObservedAssigned=7 }
    public sealed class DurableLedger
    {
        public const int Capacity=128;
        public const string SaveKey="CU80_contextual_attempts_1";
        private const string Header="CU80_LEDGER_1";
        private readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>(StringComparer.Ordinal);
        private string invalidPacked;
        private sealed class Entry { public string Key,Player,Actor,Subject,Kind; public AttemptState State; }
        public bool Valid {get{return invalidPacked==null;}}
        public int Count {get{return entries.Count;}}
        private static bool Id(string s){return !String.IsNullOrWhiteSpace(s)&&s.Length<=240;}
        private static bool Key(string s){return s!=null&&s.Length==64&&s.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');}
        private static bool Kind(string s){return s=="Surgeon"||s=="HouseholdAid"||s=="LivelihoodAid";}
        private static string E(string s){return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));}
        private static string D(string s){var v=new UTF8Encoding(false,true).GetString(Convert.FromBase64String(s));if(E(v)!=s)throw new FormatException();return v;}
        private static string Hash(params string[] fields)
        {
            using(var sha=SHA256.Create())return String.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(String.Join("|",fields.Select(E)))).Select(b=>b.ToString("x2",CultureInfo.InvariantCulture)));
        }
        public static string SurgeonKey(Snapshot s)
        {
            if(s==null||!s.Complete)return null;
            return Hash("Surgeon",s.Player,s.Actor,s.Clan,s.Party,s.RosterKey,s.Provider,
                s.Wounded.ToString(CultureInfo.InvariantCulture),s.ActorMedicine.ToString(CultureInfo.InvariantCulture),
                s.EffectiveMedicine.ToString(CultureInfo.InvariantCulture));
        }
        // Existing agency record must match the pending request's day and quota; never mint a new request.
        public static string PetitionKey(string player,string actor,string kind,int openedDay,int requestsMade,int lastDay)
        {
            if(!Id(player)||!Id(actor)||player==actor||(kind!="HouseholdAid"&&kind!="LivelihoodAid")||
                openedDay<0||lastDay!=openedDay||requestsMade<1)return null;
            return Hash("Petition",player,actor,kind,openedDay.ToString(CultureInfo.InvariantCulture),requestsMade.ToString(CultureInfo.InvariantCulture));
        }
        public bool HasUncertain(string player)
        {
            return !Valid||entries.Values.Any(x=>x.Player==player&&(x.State==AttemptState.Started||x.State==AttemptState.Uncertain));
        }
        public bool CanBegin(string key,string player)
        {
            return Valid&&Key(key)&&Id(player)&&entries.Count<Capacity&&!entries.ContainsKey(key)&&!HasUncertain(player);
        }
        public bool Begin(string key,string player,string actor,string subject,string kind)
        {
            if(!CanBegin(key,player)||!Id(actor)||player==actor||!Id(subject)||!Kind(kind))return false;
            entries.Add(key,new Entry{Key=key,Player=player,Actor=actor,Subject=subject,Kind=kind,State=AttemptState.Started});
            return true;
        }
        public bool Finish(string key,AttemptState state)
        {
            Entry e;
            if(!Valid||key==null||!entries.TryGetValue(key,out e)||e.State!=AttemptState.Started||
               (state!=AttemptState.Confirmed&&state!=AttemptState.Uncertain&&state!=AttemptState.Declined&&state!=AttemptState.Deferred&&state!=AttemptState.NoAction))return false;
            e.State=state;return true;
        }
        public AttemptState? State(string key){Entry e;return key!=null&&entries.TryGetValue(key,out e)?(AttemptState?)e.State:null;}
        // Reconciliation is observation, not reconstruction of the original receipt.
        // No retry or Chronicle claim; absence of assignment is not proof of no side effect.
        public bool ObserveExistingSurgeon(string key,Snapshot current)
        {
            Entry e;
            if(!Valid||key==null||!entries.TryGetValue(key,out e)||e.Kind!="Surgeon"||
                (e.State!=AttemptState.Started&&e.State!=AttemptState.Uncertain)||current==null||!current.Complete||
                e.Player!=current.Player||e.Actor!=current.Actor||e.Subject!=current.Party||
                current.Raw(Snapshot.Surgeon)!=e.Actor||current.Effective(Snapshot.Surgeon)!=e.Actor)return false;
            e.State=AttemptState.ObservedAssigned;return true;
        }
        public bool ReconcileObservedSurgeon(Snapshot current)
        {
            if(!Valid||current==null||!current.Complete)return false;
            var matches=entries.Values.Where(e=>e.Kind=="Surgeon"&&e.Player==current.Player&&e.Actor==current.Actor&&
                e.Subject==current.Party&&(e.State==AttemptState.Started||e.State==AttemptState.Uncertain)).ToArray();
            return matches.Length==1&&ObserveExistingSurgeon(matches[0].Key,current);
        }
        public string Pack()
        {
            if(!Valid)return invalidPacked;
            return String.Join("\n",new[]{Header}.Concat(entries.Values.OrderBy(x=>x.Key,StringComparer.Ordinal)
                .Select(x=>String.Join("|",x.Key,E(x.Player),E(x.Actor),E(x.Subject),x.Kind,((int)x.State).ToString(CultureInfo.InvariantCulture)))));
        }
        public static DurableLedger Load(string packed,bool legacyMissing)
        {
            if(legacyMissing&&String.IsNullOrEmpty(packed))return new DurableLedger();
            var l=new DurableLedger();
            try
            {
                if(String.IsNullOrEmpty(packed)||packed.Length>262144)throw new FormatException();
                var rows=packed.Split('\n');
                if(rows[0]!=Header||rows.Length>Capacity+1)throw new FormatException();
                foreach(var row in rows.Skip(1))
                {
                    var f=row.Split('|');int n;
                    if(f.Length!=6||!Key(f[0])||!Kind(f[4])||!Int32.TryParse(f[5],NumberStyles.None,CultureInfo.InvariantCulture,out n)||
                        n<1||n>7||l.entries.ContainsKey(f[0]))throw new FormatException();
                    string p=D(f[1]),a=D(f[2]),s=D(f[3]);
                    if(!Id(p)||!Id(a)||p==a||!Id(s)||(n==7&&f[4]!="Surgeon"))throw new FormatException();
                    l.entries.Add(f[0],new Entry{Key=f[0],Player=p,Actor=a,Subject=s,Kind=f[4],State=(AttemptState)n});
                }
                if(l.Pack()!=packed)throw new FormatException();
                return l;
            }
            catch {return new DurableLedger{invalidPacked=packed??""};}
        }
    }
}