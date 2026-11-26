# API-001 | Local managed assembly metadata encyclopedia
26 November 2026 PHT. Live canonical handbook v2.0, AGENTS.md, PROJECT_INSTRUCTIONS.md, production handoff and current PR #100 a116a97b01e33d04b7dc3b392b5bcec36f32149a, CI 45639026509 and 45639011312 successful, lab 515d27a074e18e815bd2b8ac62dbac1c5bf9935b, INV-001, SIM-001, ENG-002, DUT-002 consulted. Remote process 39948 verified unchanged CampaignSystem d7e5c67a04ef83964c882fda2a1f20f0e2afaf2bfe31345ed31d1061c495d462, SaveSystem 7c5a1e199007b0125c5091b0e85b1035e7aeafaaaa588e0200fb0d2777a9ae7a and CU e6bcbde295ecf0527ec97b0c4805d11916a28bd92ee099f18d838da68e6d0bba. Existing Python 3.14.5/PowerShell 5.1; no new software installed. All outputs outside game and native saves; no proprietary database, XML, binary, or raw private save uploaded.
Source c4ffb3eaf13372a879ca858e115aee7941965b58 Index-Assemblies.ps1; fresh process 36808 preloaded official dependencies, normal reflection metadata only (no gameplay methods invoked, no third-party assemblies indexed). Selected 16 official assembly files; counts are declared metadata, include constructors and special names. Process exit 0, reported 34.21s, all selected indexed with zero reported missing types or per-type errors; NavigationSystem returned zero types (reason not established, no claim of complete API). Per-assembly dependencies, version and SHA, type full name/base/visibility, declared methods/constructors with signature/access/static, properties/events/fields with signatures; does not prove parameter semantics, control flow, native interop, effective model/patch owner, call edges or actual gameplay. Local output %LOCALAPPDATA%/BannerlordResearchLab/Simulator/AssemblyIndex/c4ffb3eaf13372a879ca858e115aee7941965b58/members.jsonl SHA256 a04013b926820e5bfedfed09d178b301641f2607f1473f309c3fe23811c58ea2; summary.csv alongside. Rows by assembly (types/methods/properties/events/fields):
- CampaignSystem 2388/28187/5551/24/10171
- CampaignSystem.ViewModelCollection 764/10393/3146/3/5207
- SaveSystem 135/1073/160/0/375
- Core 322/3190/1044/11/2126
- Library 260/2061/274/30/870
- ObjectSystem 28/241/27/0/47
- MountAndBlade 1749/17184/2871/177/9194
- Engine 233/4255/233/6/1380
- GauntletUI 157/2360/676/17/1202
- ModuleManager 18/129/28/0/75
- NavigationSystem 0/0/0/0/0
- SandBox 608/5375/590/19/2632
- SandBox.ViewModelCollection 193/1967/532/4/1134
- NavalDLC 718/7870/1137/42/4834
- NavalDLC.ViewModelCollection 128/1463/338/9/633
- StoryMode 159/1795/257/0/729.
Source 4d41219dd2e9d4f4804156ff96282b31348f9e12 assembly_data.py, process 22896 imported JSONL into local assemblies.sqlite with integrity_check ok: 16 assembly records, 7860 types, 87543 methods, 16864 properties, 342 CLR events, 40609 fields, no error records. SHA256 94462c1fc7e1526aa8cbdaa60e445a142a9a45f0ce81260d08decd3b94771aca. Separate queries 280 CampaignEvents property rows and 88 QuestBase method/constructor rows. Example SQL: SELECT signature FROM members WHERE kind='method' AND type=? ORDER BY name; inspect record JSON for access and assembly provenance. Remaining installed unique assemblies and provider contracts not covered. Next index selected additional official/UI assemblies, separate provider metadata and licensing, trace selected call edges and exact lifecycle as needed; no broad game decompilation publication.
