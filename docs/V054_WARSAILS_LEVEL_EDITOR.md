# v0.54 | War Sails skill coverage and editable player/spouse levels

**Baseline:** v0.53 single scrolling stats screen (player/spouse toggle), age-limited child development, and v0.51 verified native age controls.

## Real War Sails content
TaleWorlds' War Sails expansion adds exactly three skills: **Mariner**, **Boatswain**, and **Shipmaster**. It introduces no new attribute. Native skill objects may exist outside the original `DefaultSkills` properties; Calradia Unified resolves them from the live `MBObjectManager.GetObjectTypeList<SkillObject>()`. Only registered skills receive sliders. The base game without the expansion keeps its original skills and never loads a made-up naval skill.

## Creator
- The single scrollable all-stats page displays Hero level, six native attributes, 18 original skills (with their focus), five personality traits, and up to three additional War Sails skills and focus values.
- Use the Player/Spouse buttons above the slider list. The **player** starts from true native values after the story. The **spouse** is generated later, so its baseline is provisional. Unedited spouse rows mean `AUTO` and preserve native generation. Manually adjusted rows apply exact values after creation.
- Player stats respect Native, Balanced (30 points) or Unrestricted. Player level +1 in Balanced costs 4 points. Spouse overrides are explicit and independent of the player budget. A spouse's selected level and skills apply before children inherit training.
- Hero level is supported within **1–50**. The native development XP model returns a sentinel at high levels, so the editor deliberately caps the range. Editing level stores `Hero.Level` and sets `HeroDeveloper.SetInitialLevel` to match XP; this does not mean granting unrelated attribute/focus points.
- War Sails registration status appears on all three age/stat prefab variants. Base-game installations show zero registered naval skills rather than an invalid slider.

## Child skills and family tradition
- Newly generated children receive age-scaled training based on the actual final skills of both parents, independent individual variation, and their selected upbringing.
- **Seafaring** is an optional new family scenario/upbringing and prioritizes all three naval skills, with land skills for an ordinary Bannerlord campaign. Martial can favor Mariner; Mercantile can favor Boatswain and Shipmaster.
- Age 4 has a cap of 0 trained skills, age 19 has a cap of 75, and higher ages permit more development. Milestones at 6/10/14/18 improve stats but never erase skills already earned.
- The v0.53 native youth face correction (clean-shaven ages 18–21) is preserved.

## Live acceptance gate
1. Verify the actual installed DLL hash and that the correct base-game + War Sails DLC modules are enabled.
2. During new Sandbox character creation, open **All Stats**. Check the Player and Spouse tabs, level slider 1–50, 21 skill names including 3 naval entries and their focus, and all six attributes and five personality traits.
3. Test Player 25, Spouse 22, Player OneHanded 200 and Mariner 150, Spouse Shipmaster 100. Confirm saved values and native Clan/Encyclopedia Hero level and skills.
4. Select Seafaring Household, spawn children aged 4, 12, 19 and 25. Confirm the four-year-old has no trained skills and older offspring have age-appropriate nav/land skills influenced by both parents. Save/reload and check that no milestones repeat.
5. Run a base-game-only character start with War Sails disabled and verify naval sliders are absent without throwing errors.

**Verification boundary:** Pinned 1.5.7 reference compilation, C# policy checks and static XML tests are development preflight. No game-level pass is claimed without the above real-session evidence.
