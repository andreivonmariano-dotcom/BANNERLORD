# Calradia Unified v0.70t: Knight sworn-service native interaction

## Evidence that motivated the change

Private user report: `CU-Session-20261121T115957_a1e76bb9-CLOSED.zip`, game DLL hash `90acac5b006fe1202257bfe2de823f3492b86e362215c80473e7991ec0d54817`. One process recorded three new games, not three independent reload sessions. Knight, Lord and King actual starting contracts passed. Knight created a Squire, selected Ingalther (`lord_4_16`) as personal patron, recorded relation 1→24 and an active oath, but the player saw the raw ID and `Native relation: unavailable`. The old oath relied on `MBObjectManager.GetObject<Hero>`; the native living-Hero collection can resolve this lord. The old Knight had a 30-day wait and clickable town/castle text, not an actual patron conversation.

## v0.70t implementation

- Resolve actual Hero by saved `StringId` against `Hero.AllAliveHeroes`, then the object manager. Display the real patron and clan and current native settlement/party whereabouts when known, otherwise say the location is unknown. The native Encyclopedia remains the intended tracker.
- Add `CampaignGameStarter.AddPlayerLine/AddDialogLine` with `hero_main_options` for the specific `Hero.OneToOneConversationHero` who matches the saved oath's patron ID, clan and kingdom. Available in ordinary native lord conversation, including campaign-map party encounters.
- A first in-person audience is allowed from the day of pledge. It calls the existing saved `RecordMuster` and native `ChangeRelationAction` exactly once. A later review must observe the normal 30-day term and 7-day opening. A remote or unrelated Hero cannot trigger it.
- Conversation supports inspecting duty expectations and requesting honorable release. The living successor can accept an explicit renewed oath after confirmed native succession. Settlement oath panel becomes an informational route to the real lord, not a remote award button. Native standing penalties for missed reviews and broken oaths persist.
- There is still **no native kingdom membership** in Sworn House. This is the distinct personal service identity of an independent landed-*free*, landless household. Kingdom vassalage is a political contract requiring a separate proven ruler-approved native action. No fictional wages, promotions, fiefs, kingdom votes or battlefield orders are granted.
- Real-game play acceptance now requires event `cu_knight_patron_conversation_muster` with `native_conversation=true` and verified native favor, plus `knight_patron_conversation.png`. This is not automatically satisfied by the separate `cu_knight_oath_start` receipt. Live first/second report, release, capture and save/reload must still be checked.

## Career depth still required

A fully featured Knight career needs native event-backed assignments, such as joining and protecting a patron's field army, escorting a real party, capturing an actual enemy lord in service, and reporting outcomes to the patron. Each duty must be tied to real game parties, faction relations, battles, rewards and a quest record, not a fabricated settlement menu or a free status flag. A separate formal kingdom service path requires native political acceptance and independent clan ownership tests.

The source build cannot certify that any NPC conversation rendered. Keep PR #93 draft, installer pinned, and the native gameplay gate blocked until the user's exact-game run verifies the new controls and saved state.
