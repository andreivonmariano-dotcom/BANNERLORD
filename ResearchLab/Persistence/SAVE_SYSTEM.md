# SAV-001 | Save system
METADATA partial. CampaignBehaviorBase.RegisterEvents/SyncData(IDataStore), IDataStore.SyncData<T>(string, ref T) and session/load events verified in [native reference](../NATIVE_ENGINE_1_5_7.md). SaveSystem fingerprint recorded; custom containers, SaveableTypeDefiner IDs, migration and callback timing NOT_RUN. L001 has no save keys; its repeat guard is behavior-lifetime only. Next: isolated bounded schema, stable keys, migration and missing-field cases; disposable save/quit/relaunch/reload. No production schema change or save compatibility claim.

Follow-up [SAV-002](NATIVE_SAVE_REGISTRATION.md): installed registration/behavior storage and native quest save members inspected; actual custom registration and save/reload still NOT_RUN.
