using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace Better_Work_Tab.ModSupport
{
    /// <summary>
    /// Vanilla Skills Expanded compatibility module.
    /// Caches VSE's passion tiers in a canonical rank order so the passion condition
    /// can use "at least X" threshold semantics correctly.
    /// Reflection is used so the assembly stays optional.
    /// </summary>
    public sealed class VSESupport : ModSupportModuleBase
    {
        public override string PackageId => "vanillaexpanded.skills";
        public override string DisplayName => "Vanilla Skills Expanded";

        // Passion enum ints: None=0, Minor=1, Major=2, Apathy=3, Natural=4, Critical=5
        // Desired rank order (lowest to highest): Apathy, None, Minor, Major, Natural, Critical
        private static readonly int[] RankByPassionIndex = { 1, 2, 3, 0, 4, 5 };
        private static readonly int[] EnumIndexByRank    = { 3, 0, 1, 2, 4, 5 };

        private static string[] _passionLabels;

        public static bool IsActive => _passionLabels != null;

        /// <summary>Display labels in rank order (index 0 = weakest).</summary>
        public static string[] PassionLabels => _passionLabels;

        /// <summary>
        /// Returns the rank of a Passion value in our desired ordering.
        /// Falls back to the raw enum int if called before VSE is detected.
        /// </summary>
        public static int GetRank(Passion passion)
        {
            int idx = (int)passion;
            if (idx < 0 || idx >= RankByPassionIndex.Length) return idx;
            return RankByPassionIndex[idx];
        }

        public override void OnModsDetected()
        {
            var managerType = AccessTools.TypeByName("VSE.Passions.PassionManager");
            var passionsField = managerType != null ? AccessTools.Field(managerType, "Passions") : null;
            var passions = passionsField?.GetValue(null) as System.Array;

            if (passions == null || passions.Length < EnumIndexByRank.Length)
            {
                Debug("[VSE] Passions array missing or shorter than expected; passion compat disabled.");
                return;
            }

            var passionDefType = AccessTools.TypeByName("VSE.Passions.PassionDef");
            var indicatorField = AccessTools.Field(passionDefType, "indicatorString");
            var labelField     = AccessTools.Field(typeof(Def), "label");

            if (indicatorField == null || labelField == null)
            {
                Debug("[VSE] Required PassionDef fields not found; passion compat disabled.");
                return;
            }

            var labels = new string[EnumIndexByRank.Length];
            for (int rank = 0; rank < EnumIndexByRank.Length; rank++)
            {
                var def       = passions.GetValue(EnumIndexByRank[rank]);
                var indicator = indicatorField.GetValue(def) as string;
                var label     = (labelField.GetValue(def) as string ?? rank.ToString()).CapitalizeFirst();
                labels[rank]  = string.IsNullOrEmpty(indicator) ? label : indicator;
            }

            _passionLabels = labels;
            Debug($"[VSE] Cached {_passionLabels.Length} passion tiers in rank order.");
        }
    }
}
