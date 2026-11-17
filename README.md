# Calradia Unified v0.9.0: depot and convoy accounting, M0 evidence, feature traceability

**Target:** Mount & Blade II: Bannerlord PC v1.5.7. **Distribution target:** one `Modules/CalradiaUnified` folder, with no installer and no old third-party mod DLLs.

## Actual project status

- [v0.8.0 successful hosted build](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44134919335): the first version-pinned C# build compiled a real DLL and ran its configured tests. No in-game startup or save/load acceptance test was recorded.
- **v0.9.0 change set:** source-implemented bounded depot transfers, improved real convoy unloading, independent policy assertions, M0 log evidence collection, and an auditable 93-record feature registry. Treat v0.9.0 as **source pending CI** until its own build succeeds.
- Feature switches that mutate gameplay remain disabled by default. A successful new CI run confirms compilation and policy-test execution only.

## Development workstreams

1. **[M0: startup and save validation](docs/M0_TEST_PROCEDURE.md).** A read-only PowerShell collector can record module manifests, DLL hashes and logs from a Windows game installation. Only actual game observations can pass the runtime gates.
2. **[M1: depot and real convoy hardening](docs/M1_IMPLEMENTATION_NOTES.md).** Real `MobileParty` convoys carry actual grain. Deposits and deliveries are bounded by depot capacity. Excess cargo stays in the party for a later unloading tick.
3. **[Feature-level tracking](docs/FEATURE_STATUS_WORKFLOW.md).** `Data/Feature_Registry.csv` holds 15 current code features and 78 historical references with individual statuses. GitHub Actions validates its traceability and the source build.

## Build

See [`.github/workflows/bannerlord-build.yml`](.github/workflows/bannerlord-build.yml). The Windows workflow runs source checks, registry validation and .NET policy tests, restores version-pinned references, compiles a real DLL and packages only runtime files. The GitHub artifact is named `COMPILED_UNTESTED` until game startup, save/load and feature tests are actually observed.

## Install candidate for supervised game testing

Extract the compiled ZIP into the game installation root, preserving:

```
Modules/CalradiaUnified/SubModule.xml
Modules/CalradiaUnified/ModuleData/calradia-config.ini
Modules/CalradiaUnified/bin/Win64_Shipping_Client/CalradiaUnified.dll
```

Keep official game modules and Calradia Unified enabled. Back up prior saves and remove old Calradia Unified versions before testing. No installer is required. Follow the [M0 procedure](docs/M0_TEST_PROCEDURE.md) and record results in [issue #1](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/1).

## Project records

- [Full source instructions](PROJECT_INSTRUCTIONS.md) and [source policy](docs/SOURCE_OF_TRUTH.md)
- [Roadmap](docs/ROADMAP.md) and [M1 tracking](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/2)
- [78 original mod IDs](docs/78_MOD_COVERAGE.md) and [migration checklist](Data/CalradiaUnified_78_Mod_Acceptance_Checklist.csv)
- [Test matrix](docs/TEST_MATRIX.md) and [release gate checklist](docs/RELEASE_CHECKLIST.md)

**Scope:** The original 78 mods have not been fully reconstructed. `COMPILED` in the feature registry refers to source compiled in the historical v0.8.0 run, not to completed replacement parity or a passed game test.
