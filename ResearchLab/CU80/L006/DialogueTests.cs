using System;
using System.IO;
using System.Reflection;
using CU80.Lab;
public static class DialogueTests
{
    static int count;
    static void C(string name,bool ok){if(!ok)throw new Exception("FAIL "+name);count++;Console.WriteLine("PASS "+name);}
    static Snapshot S(string actor="a",string session="s",bool assigned=false,int wounded=17)
    {return new Snapshot(session,"p",actor,"c","party","p:1;a:1;t:20","model",100,960,wounded,80,assigned?80:20,assigned?1:0,1,
        Authority.All,new[]{"","",assigned?actor:"","","",""},new[]{"p","p",assigned?actor:"p","p","p","p"});}
    sealed class Fake:ISurgeonPort
    {
        public Snapshot State=S(),After=S(assigned:true);
        public int Calls,Reads;public bool Throws;
        public Action During;
        public Snapshot Read(){Reads++;return State;}
        public void Assign(string p,string a,string party){Calls++;if(During!=null)During();State=After;if(Throws)throw new InvalidOperationException();}
    }
    public static void Main(string[] args)
    {
        // This resolver supplies installed native assembly metadata for type loading only.
        // No native starter, singleton, text manager or game is invoked by these tests.
        AppDomain.CurrentDomain.AssemblyResolve+=(sender,e)=>{
            var f=Path.Combine(args[0],new AssemblyName(e.Name).Name+".dll");
            return File.Exists(f)?Assembly.LoadFrom(f):null;
        };
        Run();
    }
    static void Run()
    {
        int memory=0;var f=new Fake();var l=new DurableLedger();
        var d=new SurgeonDialogue(f,"s",l,s=>memory++);
        string k=DurableLedger.SurgeonKey(f.State);
        C("availability reads only",d.Available()&&d.Available()&&l.Count==0&&f.Calls==0);
        d.Open();C("explicit open ready no mutation",d.Ready()&&l.Count==0&&f.Calls==0);
        f.During=()=>C("intent exists before native command",l.State(k)==AttemptState.Started);
        d.Decide(Choice.Assign);
        C("confirmed action and one memory",f.Calls==1&&memory==1&&l.State(k)==AttemptState.Confirmed);
        d.Decide(Choice.Assign);C("repeated choice no duplicate",f.Calls==1&&memory==1);
        f=new Fake();l=new DurableLedger();d=new SurgeonDialogue(f,"s",l,s=>memory++);
        d.Open();f.State=S(actor:"other");d.Decide(Choice.Assign);
        C("changed speaker no command or ledger",f.Calls==0&&l.Count==0);
        f=new Fake();l=new DurableLedger();d=new SurgeonDialogue(f,"s",l,s=>memory++);
        d.Open();d.Decide(Choice.Defer);
        C("defer durable no action",l.State(k)==AttemptState.Deferred&&f.Calls==0);
        var reload=DurableLedger.Load(l.Pack(),false);
        d=new SurgeonDialogue(f,"s",reload,s=>memory++);
        C("reload same concern suppressed",!d.Available());
        f.State=S(wounded:18);C("new concern available after reload",d.Available());
        f=new Fake{Throws=true};l=new DurableLedger();d=new SurgeonDialogue(f,"s",l,s=>memory++);
        d.Open();d.Decide(Choice.Assign);C("exception uncertain no memory",l.State(k)==AttemptState.Uncertain&&memory==1&&f.Calls==1);
        reload=DurableLedger.Load(l.Pack(),false);d=new SurgeonDialogue(f,"s",reload,s=>memory++);
        C("uncertainty persists after reload",reload.HasUncertain("p")&&!d.Available());
        d.ResetConversation();C("native holder observed not confirmed",reload.State(k)==AttemptState.ObservedAssigned&&memory==1);
        f=new Fake();l=new DurableLedger();d=new SurgeonDialogue(f,"s",l,s=>memory++);
        d.Open();d.ResetConversation();d.Decide(Choice.Assign);
        C("closed conversation stale token blocked",f.Calls==0&&l.Count==0);
        f=new Fake();l=new DurableLedger();d=new SurgeonDialogue(f,"s",l,s=>{throw new InvalidOperationException();});
        d.Open();d.Decide(Choice.Assign);
        C("memory failure no duplicate action",l.State(k)==AttemptState.Confirmed&&f.Calls==1&&!d.Available());
        Console.WriteLine("TOTAL_PASS="+count);
    }
}