# v0.61a | Automatic Sandbox household, cultural names and officer duties

**State:** Experimental implementation on draft PR #88. This change follows successful v0.60b Windows build 44765201958. No game-side v0.61a verification has been performed.

## Default creator flow

World Setup retains scenario, role, starting kingdom and noble origin. Actual older noble identity is auto-matched only after native culture, age, clan and kingdom are known. The optional exact Hero ID is still available through Advanced World choices. An unsuitable king/lord relative is refused rather than forged.

Native culture, FaceGen, narrative, Age slider, button-only All Stats, Banner, Clan and Review retain the established gameplay flow.

House & Fortune asks only **begin married**, **child count if married**, and **sibling count** in default mode. Press **Advanced settings** to edit individual names, ages, genders, upbringing, chosen spouse's background, native companions vs cultural officers, gold/influence/troops, starting holdings and household residence. No family is created without the player's count/marriage choices. Temporary House input survives a native Back/reentry during the same game session.

## Automatic suggestions

When House opens for the first time, it uses the actual selected native Hero culture, sex and age. Name pools cover Vlandia, Sturgia, Battania, Empire, Aserai and Khuzait, with sex-matched suggestions and duplicate avoidance. Spouse, children, siblings and generated parents use these culture-specific names, and advanced text editing can overwrite all names.

The founder's age determines the suggested spouse and child ages within game limits, and starting role determines the proposed family upbringing: Martial for lords and soldiers, Mercantile for traders, Seafaring for admirals, Natural otherwise. Lord/King spouses default to native randomly matched noble birth; the user must explicitly opt into marriage.

## Custom companions

Supported native start roles now **default to RoleBased**, so native starting companions are replaced by a role- and culture-matched roster. Wanderer/Beggar retain native companions. Advanced House can select Native to keep the original roster. RoleBased retains the v0.60b native-dismissal preflight.

Officers receive culture-specific names. Their native donor templates are chosen by office skill specialization and culture. Their equipment uses installed-game battle gear from that native source. Skills and personality traits are assigned by office. Navigator is assigned native Scout and Household Steward, Chamberlain or Factor is assigned native Quartermaster. Military titles without a Bannerlord native party office do not invent a commission.

## Remaining acceptance gates

The Windows source checks and pinned 1.5.7 compilation do not simulate the Gauntlet renderer, game-native marriage, actual equipment slots, native party assignments, age edits, cultural naming or save/load. Test a fresh Lord and an Admiral campaign, then inspect Desktop report ZIPs, in-game clan family and party rosters. Observe native defaults, Advanced override, Back draft persistence and clean refusal on impossible noble ties. Keep PR #88 draft.
