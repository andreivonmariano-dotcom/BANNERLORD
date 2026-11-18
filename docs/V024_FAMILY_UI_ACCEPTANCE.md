# Calradia Unified v0.24 | Vassal family correction and readable creator

**Target:** Bannerlord PC v1.5.7.125802. **Gate:** Source and v1.5.7 reference build only until a genuine campaign and save/reload test succeeds. Known v0.23 failure: [issue #50](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/50).

## What the user's actual game proved

- The custom creator does reach the native culture/appearance/background/age/difficulty screens and starts a real campaign. The v0.22 culture crash was not repeated in the reported v0.23 session.
- Real political start succeeds: `role=Lord|native_type=vassal|kingdom=vlandia|fiefs=3`.
- Real family **failed**: `cu_family_creation_status requested=True|result=spouse_template_missing`. `sandbox_start_contract` correctly reported `requested_spouse_missing,requested_children_missing`.
- The two wanderer companions are **native Advanced Start recruits**, not spouse/children. The v1.5.7 native Vassal implementation explicitly calls `GiveStartingCompanions(2, random)`. Do not repurpose those heroes, delete them by surprise, or count them as children.
- Screenshot: CU buttons and text float over a bright cinematic. The previous Frame9 brush panels do not give visual separation. Summary says family off during editing despite a requested spouse and two children. The user finds navigation confusing.

## Family correction

`SandboxFamilyCreator.FindTemplate` no longer requires one rare exact-gender/culture/occupation `IsTemplate` record.

It now attempts, in order:
1. Explicit native character template ID, only if provided.
2. Matching native Lord/Wanderer template of requested sex and culture.
3. Any native template of that sex and culture.
4. A **cloned source CharacterObject from a living native Lord** of the same sex and culture, using `HeroCreator.CreateSpecialHero`. The donor's real Hero remains untouched.
5. A native template of the requested sex from another culture.
6. A cloned living Lord CharacterObject of the requested sex from another culture. The newly created spouse's culture is then set to the player's preferred culture. The donor's culture does not change.

Each attempt logs `cu_family_template_scan` with the requested sex, culture, candidate counts, method, and actual source ID. A user-specified explicit template ID is not replaced silently.

A created spouse must then pass `Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage`. The mod logs `cu_family_marriage_gate` with actual player/spouse age, `CanMarry()` flags and clan IDs. It uses native `MarriageAction.Apply` and verifies reciprocal spouse refs/clan.

**Children:** after the real spouse exists, children are generated from the actual parents' `HeroCreationModel.GetCharacterTemplateForOffspring(mother,father,isFemale)`, through `HeroCreator.CreateChild` with the requested age and name. The mod writes/verifies reciprocal `Mother`, `Father`, and parent `Children` references. It does not create two separate Wanderer companions.

**Limits:** this remains a real hero-creation attempt, not a guaranteed success. A marriage rejection or native offspring-model failure can leave a partially created hero and will be reported. Do not retry the same save or modify an important existing game until one disposable campaign and save/reload pass.

## Readable screen correction

The frame now uses native `Widget Sprite="BlankWhiteSquare_9" Color="#..."` solid panels instead of relying on a visually ineffective `ImageWidget Brush="Frame9"`.
- Darken the entire busy cinematic under the GUI.
- Give the editor a bronze edge, dark steel header, opaque left navigation, wide editing form and separate right-side **CAMPAIGN PLAN**.
- Show role, kingdom, spouse request, child count, fief and world plan as independent live summary bindings. A spouse request is shown as *planned*, not incorrectly marked `family: off` during editing.
- Make choice label/value/help readable against dark backgrounds. Use Back, Continue, and BEGIN CAMPAIGN in a fixed footer. More options toggles advanced fields.
- Quick Appearance clearly explains that Bannerlord's native 3D culture and face editor still follows. The Review step shows the confirmed plan and does not edit role a second time.
- This remains a native-Gauntlet XML prototype and **requires an actual screenshot and mouse/controller test**. Fixed 1280 x 700 geometry has not been proved at small resolutions. Text-wrap and focus issues can still exist.

## In-game acceptance in the exact user's installation

1. Install the v0.24 compiled-untested ZIP through `scripts/Install-CU-From-Artifact.ps1`. Use `D:\Steam\steamapps\common\Mount & Blade II Bannerlord` as the game root. Verify installed `SubModule.xml` version and compiled DLL SHA-256 against the successful CI run.
2. Start new disposable Sandbox with **Lord**, **Vlandia**, **Begin married: Yes**, **spouse gender: Auto**, **spouse name of your choice**, **spouse age: 30**, **2 children** with valid ages (7 and 8 fit 30-year-old parents). Keep fief mode Native and world scenario Default. Do not use existing saves.
3. Take a screenshot of the CU panel first. Verify dark backgrounds, selectable values, visible help, right-side role/household summary, and functional Back/Continue/More options.
4. Finish Bannerlord native culture, appearance, background, age and difficulty. Open Clan/Fiefs. Verify Lord/Vlandia still works.
5. Check Clan/Families and Encyclopedia for a real spouse with the custom name and two children linked to both parents. Two vassal wanderer companions should remain companions and are NOT family.
6. Check `%LOCALAPPDATA%\CalradiaUnified\Diagnostics\events.csv` for `cu_family_template_scan`, `cu_family_marriage_gate`, `cu_family_spouse_created`, `cu_family_child_source`, `cu_family_child_created`, `cu_family_verification`, `cu_family_creation_status`, `cu_native_companion_snapshot` and `sandbox_start_contract`. Check `exceptions.csv` separately if present.
7. Save, exit the entire game, reload. Verify the spouse, both children, and vassal rank persist without duplication.
8. Failure reporting: template selection failure, eligibility failure, or child model missing must remain visible as a failed acceptance. Do not claim pass from reference compilation or a screen request.

## Architectural boundary

The exact finished request is one CU creator replacing every native creation page. v0.24 **does not** do this. It corrects the proven family and readability failures without reintroducing the competing native face-stage registration that crashed v0.22. The true unified 3D screen remains [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44), with separate game-state and preview engineering.
