using System;
using System.Collections.Generic;

namespace CalradiaUnified
{
    /// <summary>
    /// Pure parity gate for a CU editor built on the real character creator.
    /// This is a structural requirement, not proof of visual stage behavior.
    /// </summary>
    public static class NativeCreatorParityPolicy
    {
        public static readonly string[] RequiredNativeStages =
        {
            "CharacterCreationCultureStage",
            "CharacterCreationFaceGeneratorStage",
            "CharacterCreationNarrativeStage",
            "CharacterCreationBannerEditorStage",
            "CharacterCreationClanNamingStage",
            "CharacterCreationReviewStage",
            "CharacterCreationOptionsStage"
        };

        public static string[] MissingStages(IEnumerable<string> current)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (current != null)
                foreach (var stage in current)
                    if (!String.IsNullOrEmpty(stage)) seen.Add(stage);
            var missing = new List<string>();
            foreach (var required in RequiredNativeStages)
                if (!seen.Contains(required)) missing.Add(required);
            return missing.ToArray();
        }

        public static bool CanAddFaceWorkshop(bool enabled, bool inSandbox,
            bool reviewRequested, bool uniqueViewDiscovered,
            IEnumerable<string> presentNativeStages)
        {
            return enabled && inSandbox && !reviewRequested &&
                uniqueViewDiscovered &&
                MissingStages(presentNativeStages).Length == 0;
        }
    }
}
