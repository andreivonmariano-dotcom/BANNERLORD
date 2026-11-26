using System;
using CUResearchLab;
public static class TransferPolicySmoke
{
    static int passed;
    static void Check(bool pass, string name) { if (!pass) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    public static int Main()
    {
        Check(TransferPolicy.CanTransfer(100,0,true,false), "exact funds");
        Check(!TransferPolicy.CanTransfer(99,0,true,false), "insufficient funds");
        Check(!TransferPolicy.CanTransfer(100,0,false,false), "invalid authority or location");
        Check(!TransferPolicy.CanTransfer(100,0,true,true), "repeat attempt");
        Check(!TransferPolicy.CanTransfer(100,-1,true,false), "invalid treasury");
        Check(!TransferPolicy.CanTransfer(100,int.MaxValue-99,true,false), "overflow");
        Check(TransferPolicy.CanTransfer(100,int.MaxValue-100,true,false), "maximum safe treasury");
        Check(TransferPolicy.Confirmed(100,0,0,100), "exact debit and credit");
        Check(!TransferPolicy.Confirmed(100,0,0,99), "partial credit rejected");
        Check(!TransferPolicy.Confirmed(100,0,1,100), "partial debit rejected");
        Check(!TransferPolicy.Confirmed(100,0,100,0), "no effect rejected");
        Console.WriteLine("PASS: " + passed + " independent transfer policy checks (not native execution)");
        return 0;
    }
}
