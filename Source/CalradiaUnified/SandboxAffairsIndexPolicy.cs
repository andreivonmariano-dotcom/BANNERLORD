using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    // Presentation only. The existing campaign states remain the authority.
    public sealed class SandboxAffair
    {
        public string Id="",Category="",Title="",Status="",Detail="";
        public string HeroId="",SettlementId="";
        public int Day=-1;
        public bool Pending,Completed,Unread;
    }
    public static class SandboxAffairsIndexPolicy
    {
        public const int PageSize=5;
        public static SandboxAffair[] Select(IEnumerable<SandboxAffair> items,string tab)
        {
            return (items??Enumerable.Empty<SandboxAffair>())
                .Where(x=>x!=null && !String.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x=>x.Id,StringComparer.Ordinal).Select(g=>g.First())
                .Where(x=>tab=="Pending"?x.Pending:
                    tab=="History"?!x.Pending && x.Category=="Letters":
                    x.Category==tab)
                .OrderByDescending(x=>x.Pending)
                .ThenByDescending(x=>x.Day)
                .ThenBy(x=>x.Id,StringComparer.Ordinal).ToArray();
        }
        public static SandboxAffair[] Inbox(IEnumerable<SandboxAffair> items)
        {
            return (items??Enumerable.Empty<SandboxAffair>())
                .Where(x=>x!=null && !String.IsNullOrWhiteSpace(x.Id) && (x.Pending || x.Unread))
                .GroupBy(x=>x.Id,StringComparer.Ordinal).Select(g=>g.First())
                .OrderByDescending(x=>x.Pending).ThenByDescending(x=>x.Unread)
                .ThenByDescending(x=>x.Day).ThenBy(x=>x.Id,StringComparer.Ordinal).ToArray();
        }
        public static SandboxAffair[] Archive(IEnumerable<SandboxAffair> items)
        {
            return Select(items,"History").Where(x=>!x.Unread).ToArray();
        }
        public static int PageCount(int count)
        {
            return Math.Max(1,(Math.Max(0,count)+PageSize-1)/PageSize);
        }
    }
}
