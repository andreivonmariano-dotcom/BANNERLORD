#!/usr/bin/env python3
"""v0.29: distinct opt-in native 3D review and actual post-creation Hero portraits.
Source-only contract, not a substitute for an in-game stage or Gauntlet render test.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parent.parent
source=root/"Source/CalradiaUnified"
native=(source/"CUNativeOriginReviewStage.cs").read_text()
gallery=(source/"CUFamilyPortraitGallery.cs").read_text()
behavior=(source/"SandboxOriginBehavior.cs").read_text()
profile=(source/"SandboxOriginProfile.cs").read_text()
creator=(source/"SandboxCreatorOverlay.cs").read_text()
source_files=list(source.glob("*.cs"))
views="".join(p.read_text() for p in source_files)

# A completely distinct optional stage, NEVER an override of any native view.
assert "[CharacterCreationStageView(typeof(CUNativeOriginReviewStage))]" in native
assert native.count("[CharacterCreationStageView(")==1
assert "CUNativeOriginReviewStage : CharacterCreationStageBase" in native
assert "CharacterCreationReviewStageView" in native
assert "base(manager,affirmativeAction,affirmativeText" in native
assert 'GauntletLayer("CUOriginNativeReview", 2, false)' in native
assert "summaryLayer.IsFocusLayer = false;" in native
assert 'summaryLayer.LoadMovie(' in native
assert "base.GetLayers()" in native
assert "base.OnFinalize()" in native
assert "new BodyGeneratorView" not in views
assert "CharacterCreationStageView(typeof(CharacterCreationFaceGeneratorStage))" not in views
assert "CharacterCreationStageView(typeof(CharacterCreationCultureStage))" not in views
assert "CharacterCreationStageView(typeof(CharacterCreationReviewStage))" not in views

# Activation is opt-in only, with native content stages present.
assert "public bool NativeIntegrationReviewEnabled;" in profile
assert "NativeIntegrationReviewEnabled=false;" in profile
assert 'case "NativeIntegrationReviewEnabled":' in profile
assert '"NativeIntegrationReviewEnabled=" +' in profile
assert 'if (key == "NativeIntegrationReviewEnabled")' in creator
assert 'return advancedFieldsVisible;' in creator
assert "originProfile.NativeIntegrationReviewEnabled &&" in behavior
assert "manager.GetStage<CharacterCreationCultureStage>() != null" in behavior
assert "manager.GetStage<CharacterCreationFaceGeneratorStage>() != null" in behavior
assert "manager.GetStage<CharacterCreationReviewStage>() != null" in behavior
assert "manager.GetStage<CharacterCreationOptionsStage>() != null" in behavior
assert "manager.AddStage(new CUNativeOriginReviewStage());" in behavior
assert "manager.RemoveStage" not in behavior
assert '"cu_native_review_attached"' in behavior
assert '"cu_native_review_skipped"' in behavior
assert '"cu_native_review_view_loaded"' in native
assert '"|native_cu_review="+profile.NativeIntegrationReviewEnabled' in creator
assert "candidate.NativeStageEnabled=false;" in creator

# Portraits are created from real registered Heroes only after creation,
# are read-only, and never manufacture a preview baby or add it to a party.
assert "CharacterCode.CreateFrom(hero.CharacterObject)" in gallery
assert "new CharacterImageIdentifierVM(" in gallery
assert "player.Spouse.Spouse == player" in gallery
assert "h.Mother == player || h.Father == player" in gallery
assert "Hero.MainHero" in gallery
assert "hero.StayingInSettlement" in gallery
assert "hero.LastKnownClosestSettlement" in gallery
assert "HeroCreator.CreateChild" not in gallery
assert "HeroCreator.CreateSpecialHero" not in gallery
assert "ChangeState" not in gallery
assert "TeleportHero" not in gallery
assert "AddHeroToParty" not in gallery
assert "MarriageAction" not in gallery
assert '"cu_family_portraits_open"' in gallery
assert '"cu_family_portraits_" + from' in behavior
assert 'new CUFamilyPortraitScreen()' in behavior

# Every visible C# field must exist in XML and parent types.
nativeXml=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUNativeOriginReview.xml").getroot()
galleryXml=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUFamilyPortraits.xml").getroot()
creatorXml=ET.parse(root/"Modules/CalradiaUnified/GUI/Prefabs/CUSandboxCreator.xml").getroot()
for x in (nativeXml,galleryXml,creatorXml):
    assert x.tag=="Prefab"
nativeBindings=[n.get("Text","")[1:] for n in nativeXml.iter("TextWidget") if n.get("Text","").startswith("@")]
for name in nativeBindings:
    assert re.search(r"public string\s+"+name+r"\b",native),name
assert len(nativeBindings)>=7
assert not list(nativeXml.iter("ButtonWidget")), "read-only native review overlay must not steal controls"
assert any(n.get("Command.Click")=="ExecuteClose" for n in galleryXml.iter("ButtonWidget"))
assert any(n.tag=="ImageIdentifierWidget" and n.get("DataSource")=="{Portrait}"
           and n.get("ImageId")=="@Id" for n in galleryXml.iter())
assert any(n.tag=="ListPanel" and n.get("DataSource")=="{Members}"
           for n in galleryXml.iter())
assert any(n.get("Text")=="@SpousePreviewSummary" for n in creatorXml.iter("TextWidget"))
assert any(n.get("Text")=="@NativeNextSummary" for n in creatorXml.iter("TextWidget"))
print("PASS: optional unique native 3D review, preserved native stages, real-Hero portrait list and nonmutating gallery (STATIC)")
