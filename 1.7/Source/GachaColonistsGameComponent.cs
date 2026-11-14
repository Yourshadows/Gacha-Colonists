using Verse;

namespace GachaColonists
{
    public sealed class GachaColonistsGameComponent : GameComponent
    {
        public GachaColonistsGameComponent(Game game)
        {
        }
        private int pullsUntilGuaranteedRarity;

        public int PullsUntilGuaranteedRarity
        {
            get { return pullsUntilGuaranteedRarity; }
        }

        public string RollNextRarity()
        {
            pullsUntilGuaranteedRarity++;
            if (pullsUntilGuaranteedRarity >= 5)
            {
                pullsUntilGuaranteedRarity = 0;
                return Rand.Value < 0.25f ? "UR" : "SSR";
            }

            float roll = Rand.Value * 100f;
            if (roll < 5f)
            {
                return "UR";
            }

            if (roll < 20f)
            {
                return "SSR";
            }

            if (roll < 50f)
            {
                return "SR";
            }

            return "R";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pullsUntilGuaranteedRarity, "pullsUntilGuaranteedRarity", 0);
            if (pullsUntilGuaranteedRarity < 0 || pullsUntilGuaranteedRarity >= 5)
            {
                pullsUntilGuaranteedRarity = 0;
            }
        }
    }
}

