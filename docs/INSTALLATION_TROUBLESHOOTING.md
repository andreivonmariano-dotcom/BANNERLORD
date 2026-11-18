# Calradia Unified v0.16.0: mod missing from Bannerlord launcher

**Status:** Installation instructions. Bannerlord launch remains untested.

## The GitHub download has two ZIP layers

The compiled build contains an inner `CalradiaUnified_v0.16.0_COMPILED_UNTESTED.zip` archive with `Modules/CalradiaUnified/...`. GitHub Actions wraps that file in a second outer ZIP when downloading build artifacts. Extract both layers. Installing only the outer ZIP places a ZIP file in the game folder, not a mod.

## Manual installation

1. Close the Bannerlord game and launcher.
2. Open your actual installed Bannerlord game folder. From Steam, use **Properties > Installed Files > Browse**. The directory must contain `Modules/Native/SubModule.xml`.
3. Extract the downloaded **outer** GitHub artifact ZIP into a temporary folder.
4. Extract its **inner** `CalradiaUnified_v0.16.0_COMPILED_UNTESTED.zip` into the actual game root, the folder containing `Modules`.
5. Check `Modules/CalradiaUnified/SubModule.xml` and `Modules/CalradiaUnified/bin/Win64_Shipping_Client/CalradiaUnified.dll` exist directly at those paths. No extra version folder or `Modules` folder belongs between `Modules/CalradiaUnified` and the XML.
6. Restart the launcher. Select **Singleplayer > Mods**. The mod name is **Calradia Unified - Sandbox Origin Designer Candidate**.

Do not put the mod under your Documents folder. Move any earlier `CalradiaUnified` version outside `Modules` before installing the current candidate.

## Optional installer for the GitHub download

The repo provides `scripts/Install-CU-From-Artifact.ps1`. It accepts either the outer downloaded artifact ZIP or the inner installation ZIP and checks the exact module path and DLL hash before reporting success. It refuses to overwrite an existing version unless you explicitly pass `-ReplaceExisting`, which moves the prior module outside `Modules` into a backup.

Example command from the repository root, replacing both paths with your own:

```powershell
./scripts/Install-CU-From-Artifact.ps1 -GameRoot 'D:\Games\Mount & Blade II Bannerlord' -ArtifactZip "$HOME\Downloads\CalradiaUnified-v0.16.0-compiled-untested.zip"
```

## Still invisible after correct installation?

Confirm that `SubModule.xml` is not `SubModule.xml.txt` and sits directly inside `CalradiaUnified`. Confirm the game root contains `Modules/Native` and belongs to the installed launcher, not an older installation. Completely exit the launcher and reopen it. Send the absolute `SubModule.xml` file path and a screenshot of the Singleplayer Mods list to distinguish path and parsing failures.

Correct file placement proves only installation. The new Sandbox UI and runtime changes still require v1.5.7 game testing.
