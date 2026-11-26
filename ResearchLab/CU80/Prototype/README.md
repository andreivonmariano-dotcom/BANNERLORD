# L004 | Situation-driven surgeon encounter (isolated C# executable)
Original research prototype, no TaleWorlds or CU reference, no module manifest, no game action or save modification. Compile on Windows PowerShell 5.1 with installed Framework64 csc (no software installation):
```powershell
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$out=Join-Path (Get-Location) 'CU80.Tests.exe'
& $csc @('/nologo','/target:exe','/warnaserror+','/optimize+',('/out:'+$out),'Encounter.cs','Tests.cs')
if($LASTEXITCODE -ne 0){throw 'compile failed'}
& $out
if($LASTEXITCODE -ne 0){throw 'tests failed'}
```
Native bridge is an interface only. Snapshot authority flags must be derived from actual existing CU/native guards on the campaign thread; fake adapter supplied in Tests.cs. Snapshot six-slot order Quartermaster, Scout, Surgeon, Engineer, FirstMate, Navigator is local array indexing, not native enum values. RosterKey must describe identity/count, not wound status; Provider must identify active provider context. Unknown must fail closed, never encoded as an optimistic empty vacancy. Offer uses exact snapshot including tick, so a benign time change may invalidate; evaluate UX in isolated test. Revision 2 keeps up to 64 exact in-session identity contexts without eviction; repeated rendering returns the same pending token, speaker switch invalidates previous token, and unchanged context does not resurface. Persistence across reload remains deferred. Lock is per controller, no cross-reload persistence or recovery workflow; native adapter and lifecycle integration must prevent recreating controller to retry uncertain command. No durable transaction ledger or user-facing native dialog implemented. Actual role assignment and recovery, UI, player acceptance, provider and migration tests NOT_RUN. Use existing CU native handler, not second setter.
Verified 54 executable checks process37872, source SHA256 e4a29bc8381cc7b8ae3e355a1bec405f6f6300c614e0f497e4b96899abc62587; tests b79bbfd6be3f76ae1f9e4d43432d0f7ebe3a752113a1ddc9b573da72439100b1; exe54cbc1e832dd3b4960ee5da52702ef536bc7886af3c91198a444af9b8a4db278. Initial process45976 failed CS2005/CS2008 due PowerShell /out: token separation, corrected with array. No exe published. F061 still blocks native advancement.

Revision 2 process10864: 61/61 checks, including seven additional UI/replay/capacity cases. Source SHA256 31c8264fac44329a4abb2e40bbeaa1c082889b92419ca95c81739938fcb49b7d; tests 123c72d6f447bbdef5a14baac986eb9056f314bd13eae87e8f6b19db66c08103; EXE 382554be95698e5c364d3c89b64fa6561e84191c096ecf7e010706122fb7fdc8. Original 54-check source preserved at ad9d434b6249a886ab2422c252c6cb37f7956c24. No native invocation.
