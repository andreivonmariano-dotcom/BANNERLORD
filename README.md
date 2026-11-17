# Calradia Unified v0.8.0: hosted-build and guarded strategy commands

**Current verified state:** [GitHub Actions run 44134919335](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44134919335) compiled a real v0.8.0 `CalradiaUnified.dll` candidate. Game startup, campaign function and save/load remain **NOT TESTED**. The full reconstruction of the 78 historical mods is unfinished.

**Project governance:** [Source of truth](docs/SOURCE_OF_TRUTH.md) | [Full handbook](PROJECT_INSTRUCTIONS.md) | [Roadmap and milestones](docs/ROADMAP.md) | [78 original IDs](docs/78_MOD_COVERAGE.md) | [Test matrix](docs/TEST_MATRIX.md) | [Release checklist](docs/RELEASE_CHECKLIST.md)

## Q1: GitHub Actions build

The project uses `.github/workflows/bannerlord-build.yml` in this repository. Open **Actions**, choose
**Calradia Unified 1.5.7 - compile and package candidate**, then **Run workflow**.
It restores `Bannerlord.ReferenceAssemblies` **1.5.7.125802**, runs policy checks,
compiles a .NET Framework 4.7.2 DLL on a Windows runner, and packages the resulting
`Modules/CalradiaUnified` folder only if compilation succeeds. If compilation fails,
see the workflow logs and `BuildResults/compile.log` artifact where present.

The first successful hosted build is [run 44134919335](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44134919335).
Compilation passed. Game startup, save/load and gameplay remain untested.

## Q2: actual NPC party movement linked to strategy

`KingdomAutonomyBehavior` invokes `KingdomStrategyPolicy.Choose` and maps three
non-combat decisions through `KingdomCommandPolicy.Choose`:

- Resupply: order a non-player lord of the *player's clan* toward a friendly,
  unbesieged town/castle with recorded food stocks.
- Recruit / Recover: order an eligible party toward a friendly town/castle.
  The order itself does not add troops or food; vanilla systems decide what happens
  after the party arrives.
- Patrol: optionally issue `SetMovePatrolAroundSettlement` at an own-clan town/castle
  during **confirmed peace**, with a minimum troop count, no active movement,
  and a confirmed safe target.

All three commands are opt-in via `ModuleData/calradia-config.ini`; **all default off**.
Actual movement invokes a restricted set of public Bannerlord methods using a
signature-checked reflection adapter. A missing or changed v1.5.7 method means **no
order**, not an unsafe fallback. AI or strategic decisions labeled Defend or
SiegeSupport never issue combat/siege orders in this release. Parties in battles,
armies, sieges, settlements, at sea, or blocked by cooldown never receive orders.
The runtime only changes NPC travel orders, not save files or party inventories.

The source compiled against the pinned v1.5.7 reference package. Live game startup,
30-day campaign, settlement departure, and save/load tests have not yet been run.

## Ready-to-paste artifact criteria

A successful GitHub Actions build produces `CalradiaUnified_v0.8.0_COMPILED_UNTESTED.zip`
containing `Modules/CalradiaUnified` and the built DLL. It is a *candidate* for testing,
not the final 78-mod overhaul. No Windows installer is part of that artifact.

## Current local verification

`python verify_source.py` checks source structure, project XML, required flags, code
integration and the 78-mod migration ledger. `Tests/PolicySmoke` exercises independent C#
policies. The first successful hosted CI run executed these stages and built the DLL.
No evidenced in-game test exists.


## First successful hosted DLL build

On November 17, 2026 (UTC), GitHub Actions run [44134919335](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44134919335) completed successfully after repairing a missing compile reference in the policy-test project.

- Build artifact: `CalradiaUnified-v0.8.0-compiled-untested`.
- Inner ZIP: `CalradiaUnified_v0.8.0_COMPILED_UNTESTED.zip`.
- Contents: `Modules/CalradiaUnified/` with `SubModule.xml`, `ModuleData/calradia-config.ini`, and `bin/Win64_Shipping_Client/CalradiaUnified.dll`.
- To install a test candidate, extract the inner ZIP into your Bannerlord game root, preserving the `Modules/CalradiaUnified` path. Move any older `CalradiaUnified` folder out of `Modules` first.
- This is **compiled but not game-tested**. Use a new campaign or a backed-up save. Experimental NPC orders default to disabled. The full 78-mod recreation is not complete.

The downloadable compiled artifact is available on the workflow run's **Artifacts** section.
