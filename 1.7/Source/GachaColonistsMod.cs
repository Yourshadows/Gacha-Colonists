using Verse;

namespace GachaColonists
{
    public sealed class GachaColonistsMod : Mod
    {
        public GachaColonistsMod(ModContentPack content) : base(content)
        {
            GetSettings<GachaColonistsSettings>();
            Log.Message("[Gacha Colonists] Loaded development build.");
        }

        public override string SettingsCategory()
        {
            return "Gacha Colonists";
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            GetSettings<GachaColonistsSettings>().DoWindowContents(inRect);
        }
    }
}
