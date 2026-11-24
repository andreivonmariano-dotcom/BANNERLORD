# Living Society: depth and diversity acceptance record

Source/test checkpoint: 765fd0bc7397739135b9572cdf6c3e7cd624fbcb. Target: Bannerlord 1.5.7.125802, War Sails 1.3.7. Draft PR #100.

## Implemented in source
- Council: 36 authored arguments (4 topics × 3 priorities × 3 variants), deterministic speaker/topic/day/round rotation, topic-sensitive preferences, practical questions and reconsideration round. Four menu stages with exit paths.
- Occasions: family gathering, fellowship, marriage celebration, remembrance, court assembly. Distinct choices, replies and recollections. Only actual participants receive memories; one recorded group declaration per player/participant/day. Prior memory rows remain readable.
- Local scheduling: one pending gathering; 2–4 invitations, minimum two attending, due after two days with a three-day attendance window and seven-day post-closure scheduling cooldown. Modeled acceptance/refusal, actual presence checks, completion/cancellation/expiry. Saved as CU_gathering_plan_1. The latest plan is retained until replaced. No forced travel or new native resource effects.
- Peer observation: rotated bounded candidate window (12), at most one sample/day, 48 pairs, seven-day interval per pair. Native relationship trend and repeated modeled priority agreement. Legacy seven-field rows migrate to eleven-field rows without manufacturing a historical baseline.

## Verification
32 static source-contract checks passed at 68605084490bc265d691e9647e7a826c33a4c451. Twelve additional occasion-memory source checks passed at 765fd0bc7397739135b9572cdf6c3e7cd624fbcb. Eleven new policy test cases are authored, not executed. GitHub connector returned no workflow runs or combined statuses for the final source checkpoint. This does not prove that no CI run exists. No new compiler result, installable ZIP, in-game result, or mod-stack certification is available.

## Required acceptance
- Compile and execute PolicySmoke and source-contract script.
- Test repeated discussions on different days, same and different speakers, changes in siege/family/finance conditions, disagreement, second round, and exit from all four menus.
- Test invitation refusal, due window, missed gathering, cancellation, absent/dead participants, owner/venue changes, reloading, and eligibility changes. The current capped candidate pool can exclude an invited character when the local roster changes; this needs an explicit acceptance case.
- Verify actual participant-only occasion recollections and old save migrations; test long-running peer observations and bounded storage.
- Verify native 1.5.7 multi-agent API probe, participant indexing, scene entry/exit, gestures and camera behavior before activating real 3D scenes.
- Verify the full 41-module stack and long-campaign save behavior before release.

## Limits
Physical multi-agent scenes remain diagnostic only. Gatherings are text interactions, not native weddings, funerals or physical feasts. Invitations do not order NPC travel. Peer observations do not create autonomous romances, friendships, feuds or factions. No native relationship or diplomatic mutation is added here. Keep draft open and release gated. The earlier v0.74.0 hotfix artifact is not a build of this work.
