# Source of truth and instruction maintenance

**Project:** BANNERLORD / Calradia Unified  
**Governance baseline:** 18 November 2026  
**Canonical development repository:** https://github.com/andreivonmariano-dotcom/BANNERLORD

## Controlling sources

The user-provided **BANNERLORD | Comprehensive Project Source Instructions | v1.0** Google Doc is the long-form project handbook and is attached to this ChatGPT project as a source. Its maintained repository mirror is [PROJECT_INSTRUCTIONS.md](../PROJECT_INSTRUCTIONS.md). The compact standing instructions are [CHATGPT_PROJECT_INSTRUCTIONS.txt](../CHATGPT_PROJECT_INSTRUCTIONS.txt). [AGENTS.md](../AGENTS.md) directs coding tools to them.

Treat these as the project's persistent operating standard. GitHub source, CI logs and exact build revisions establish current *technical state*. The historical `modlist` Google Drive folder establishes archive provenance, not proof of compatibility. The [78-mod tracker](../Data/Migration_78.csv) and [acceptance checklist](../Data/CalradiaUnified_78_Mod_Acceptance_Checklist.csv) record feature coverage. Do not publish the private Drive folder contents or proprietary third-party binaries.

## Conflict handling

1. Current user instructions and higher-priority platform rules control the present task.
2. The long-form project handbook controls project process and editorial conventions. Its repository mirror should be kept aligned.
3. The current repository files and completed CI runs establish what has been written or compiled.
4. Official TaleWorlds documentation and version-pinned references establish engine facts.
5. The modlist archive documents historical candidates and versions. Test each independent replacement on the pinned game version.

An older summary never overrides a current source file, and green CI does not count as game testing. For a changed policy, revise the Google Doc and the two mirrored files together, update the version record, and verify the resulting pages. Do not make a contradictory rule only in a README.

## Scope and release rule

This repository concerns Mount & Blade II: Bannerlord modding. The target product is one paste-ready `Modules/CalradiaUnified` folder. The historical 78 mods are *inspirations and acceptance inputs*, not 78 completed replacements. Use original implementation and assets only with appropriate permissions. The latest documented v0.8.0 compiled candidate has **Gate B** evidence; Gate C game startup and later tests are unverified.

## Progress records

Track scope in [ROADMAP.md](ROADMAP.md), original IDs in [78_MOD_COVERAGE.md](78_MOD_COVERAGE.md), reproducible tests in [TEST_MATRIX.md](TEST_MATRIX.md), and release gates in [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md). Issue titles beginning `[M0]` through `[M9]` are milestone tracking issues. They are *not* evidence that GitHub native milestone objects exist. Mark a checkbox complete only after recording the exact evidence URL, commit and game version.
