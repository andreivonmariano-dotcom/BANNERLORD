# Calradia Unified release-readiness checklist

**One-folder product:** `Modules/CalradiaUnified` directly extracted to the Bannerlord installation. No installer and no bundled copies of obsolete framework DLLs.

## Identity and provenance
- [ ] Identify exact Bannerlord game build, module manifest version, applicable War Sails version and official dependency versions.
- [ ] Pin source commit SHA and successful CI run URL.
- [ ] Record compiled DLL name, size and SHA-256. Verify the runtime ZIP integrity and hash.
- [ ] State features present, default-off experimental flags, known conflicts and known issues.
- [ ] Attach a completed 78-mod feature coverage update. An inspiration is not proof of full replacement.

## Engineering gates
- [ ] Gate A: Source, manifest and XML validation results recorded.
- [ ] Gate B: C# and independent policy tests executed and passed on the exact source revision.
- [ ] Gate C: Tested game launcher, new campaign and startup logs show no reproducible mod startup fault.
- [ ] Gate D: Every enabled feature triggered and matched observed expected game state.
- [ ] Gate E: Save, quit, reload, battle/siege/settlement transitions and documented regression scenarios passed.
- [ ] Gate F: Full campaign smoke suite plus at least 60 minutes of play completed, with crash reports inspected.
- [ ] Gate G: Release notes and exact tested ZIP published, without mismatched source/compiled artifacts.

## User experience and distribution
- [ ] Runtime ZIP contains only `Modules/CalradiaUnified/SubModule.xml`, `ModuleData`, a real nonzero DLL under `bin/Win64_Shipping_Client`, and genuinely required runtime assets.
- [ ] Confirm correct module ID and load order against the official dependencies.
- [ ] Test clean install and removal after backup of prior module folder and saves.
- [ ] No development files, build scripts, test projects, obsolete DLLs, unlicensed assets or placeholder binaries in release ZIP.
- [ ] Record save compatibility and new-campaign requirement, where relevant.
- [ ] Announce release only after Gate G. Use `COMPILED_UNTESTED` for a Gate B-only binary.

## Sign-off evidence (complete before publication)

- Source commit:
- CI run:
- Compiled DLL SHA-256:
- ZIP SHA-256:
- Game build and official modules:
- Expansion build (or not applicable):
- Gameplay test log and tester/date:
- Known limitations:
- Migration tracker revision:
- Release tag and artifact URL:
