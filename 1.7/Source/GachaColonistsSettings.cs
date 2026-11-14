using UnityEngine;
using Verse;

namespace GachaColonists
{
    public sealed class GachaColonistsSettings : ModSettings
    {
        public int summonCost = 1000;
        public int tenSummonCost = 10000;
        private int settingsVersion;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref summonCost, "summonCost", 1000);
            Scribe_Values.Look(ref tenSummonCost, "tenSummonCost", 10000);
            Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0);
            if (Scribe.mode == LoadSaveMode.LoadingVars && settingsVersion < 1)
            {
                summonCost = 1000;
                tenSummonCost = 10000;
                settingsVersion = 1;
            }
            base.ExposeData();
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("GC_SingleSummonCost".Translate() + " " + summonCost + " " + "GC_Silver".Translate());
            summonCost = (int)listing.Slider(summonCost, 100f, 5000f);

            listing.Label("GC_TenSummonsCost".Translate() + " " + tenSummonCost + " " + "GC_Silver".Translate());
            tenSummonCost = (int)listing.Slider(tenSummonCost, 1000f, 50000f);

            listing.End();
        }
    }
}

