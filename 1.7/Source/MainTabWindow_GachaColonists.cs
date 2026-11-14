using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace GachaColonists
{
    public sealed class MainTabWindow_GachaColonists : MainTabWindow
    {
        private const float ButtonHeight = 36f;
        private static readonly Dictionary<ThingDef, float> ApparelProtectionCache = new Dictionary<ThingDef, float>();
        private static List<SummonResult> lastSummonResults = new List<SummonResult>();
        private static Game lastResultsGame;
        private static readonly GachaArchetype[] Archetypes = new GachaArchetype[]
        {
            new GachaArchetype("Боец", new string[] { "Shooting", "Melee" }, WeaponStyle.Ranged),
            new GachaArchetype("Медик", new string[] { "Medical", "Intellectual", "Social" }, WeaponStyle.None),
            new GachaArchetype("Инженер", new string[] { "Construction", "Crafting", "Intellectual" }, WeaponStyle.Any),
            new GachaArchetype("Следопыт", new string[] { "Shooting", "Plants", "Animals" }, WeaponStyle.Ranged),
            new GachaArchetype("Лидер", new string[] { "Social", "Intellectual", "Cooking" }, WeaponStyle.Any),
            new GachaArchetype("Псионик", new string[] { "Intellectual", "Medical", "Social" }, WeaponStyle.None)
        };

        private enum WeaponStyle
        {
            Any,
            Melee,
            Ranged,
            None
        }

        private sealed class GachaArchetype
        {
            public string Name;
            public string[] PreferredSkills;
            public WeaponStyle PreferredWeaponStyle;

            public GachaArchetype(string name, string[] preferredSkills, WeaponStyle preferredWeaponStyle)
            {
                Name = name;
                PreferredSkills = preferredSkills;
                PreferredWeaponStyle = preferredWeaponStyle;
            }
        }

        private sealed class SummonResult
        {
            public Pawn Pawn;
            public Pawn Companion;
            public string Rarity;
            public GachaArchetype Archetype;

            public SummonResult(Pawn pawn, Pawn companion, string rarity, GachaArchetype archetype)
            {
                Pawn = pawn;
                Companion = companion;
                Rarity = rarity;
                Archetype = archetype;
            }
        }

        private sealed class RaceGenerationCandidate
        {
            public List<PawnKindDef> Kinds;
            public XenotypeDef Xenotype;

            public RaceGenerationCandidate(List<PawnKindDef> kinds, XenotypeDef xenotype)
            {
                Kinds = kinds;
                Xenotype = xenotype;
            }
        }

        private sealed class RarityTraitCandidate
        {
            public TraitDef Def;
            public int Degree;

            public RarityTraitCandidate(TraitDef def, int degree)
            {
                Def = def;
                Degree = degree;
            }
        }

        public override Vector2 RequestedTabSize
        {
            get { return new Vector2(1020f, 520f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            GachaColonistsSettings settings = LoadedModManager.GetMod<GachaColonistsMod>().GetSettings<GachaColonistsSettings>();
            if (lastResultsGame != Current.Game)
            {
                lastSummonResults = new List<SummonResult>();
                lastResultsGame = Current.Game;
            }

            float gap = 10f;
            float leftWidth = 242f;
            float centerWidth = 270f;
            float panelY = 43f;
            float panelHeight = inRect.height - panelY;
            float rightWidth = inRect.width - leftWidth - centerWidth - gap * 2f;
            Rect leftRect = new Rect(0f, panelY, leftWidth, panelHeight);
            Rect centerRect = new Rect(leftWidth + gap, panelY, centerWidth, panelHeight);
            Rect rightRect = new Rect(leftWidth + centerWidth + gap * 2f, panelY, rightWidth, panelHeight);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, 390f, 36f), "GC_Title".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(inRect.width - 280f, 0f, 280f, 32f), "GC_SilverOnMap".Translate() + " " + AvailableSilver().ToString("N0"));
            Text.Anchor = TextAnchor.UpperLeft;

            DrawRatesPanel(leftRect);
            DrawSummonPanel(centerRect, settings);
            DrawResultsPanel(rightRect);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static void DrawPanel(Rect rect, string title)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.055f, 0.065f, 0.075f, 0.96f));
            Widgets.DrawBox(rect, 1);
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.88f, 0.84f, 0.73f);
            Widgets.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 26f), title);
            GUI.color = Color.white;
        }

        private static void DrawRatesPanel(Rect rect)
        {
            DrawPanel(rect, "GC_RatesAndPity".Translate());
            float x = rect.x + 12f;
            float width = rect.width - 24f;
            float y = rect.y + 43f;

            Widgets.DefLabelWithIcon(new Rect(x, y, width, 28f), ThingDefOf.Silver, 2f, 6f);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(x + 85f, y, width - 88f, 28f), AvailableSilver().ToString("N0"));
            Text.Anchor = TextAnchor.UpperLeft;
            y += 38f;

            DrawRarityRow(new Rect(x, y, width, 34f), RarityLabel("UR"), "5%", RarityColor("UR"));
            y += 35f;
            DrawRarityRow(new Rect(x, y, width, 34f), RarityLabel("SSR"), "15%", RarityColor("SSR"));
            y += 35f;
            DrawRarityRow(new Rect(x, y, width, 34f), RarityLabel("SR"), "30%", RarityColor("SR"));
            y += 35f;
            DrawRarityRow(new Rect(x, y, width, 34f), RarityLabel("R"), "50%", RarityColor("R"));
            y += 47f;

            Widgets.Label(new Rect(x, y, width, 24f), "GC_GuaranteeHigh".Translate());
            y += 27f;
            GachaColonistsGameComponent pity = GetPityComponent();
            float progress = Mathf.Clamp01(pity.PullsUntilGuaranteedRarity / 5f);
            Widgets.FillableBar(new Rect(x, y, width, 20f), progress);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(x, y, width, 20f), pity.PullsUntilGuaranteedRarity + "/5");
            Text.Anchor = TextAnchor.UpperLeft;
            y += 27f;
            Widgets.Label(new Rect(x, y, width, 55f), "GC_FifthGuarantee".Translate());
        }

        private static void DrawRarityRow(Rect rect, string rarity, string chance, Color color)
        {
            Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.035f));
            Widgets.DrawBoxSolid(new Rect(rect.x + 5f, rect.y + 6f, 22f, 22f), color);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x + 36f, rect.y + 3f, rect.width - 80f, 27f), rarity);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rect.x + rect.width - 65f, rect.y + 3f, 58f, 27f), chance);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawSummonPanel(Rect rect, GachaColonistsSettings settings)
        {
            DrawPanel(rect, "GC_Summon".Translate());
            float x = rect.x + 12f;
            float width = rect.width - 24f;
            float capsuleY = rect.y + 46f;
            Rect capsuleRect = new Rect(x, capsuleY, width, rect.height - 157f);
            Widgets.DrawBoxSolidWithOutline(capsuleRect, new Color(0.09f, 0.105f, 0.12f), new Color(0.48f, 0.38f, 0.21f), 1);

            Rect iconRect = new Rect(capsuleRect.center.x - 54f, capsuleRect.y + 42f, 108f, 108f);
            Widgets.ThingIcon(iconRect, ThingDefOf.Human, null, null, 1f, new Color(0.93f, 0.83f, 0.61f));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(capsuleRect.x + 10f, iconRect.yMax + 7f, capsuleRect.width - 20f, 30f), "GC_RandomColonist".Translate());
            Widgets.Label(new Rect(capsuleRect.x + 10f, iconRect.yMax + 35f, capsuleRect.width - 20f, 42f), "GC_RaceArchetypeRarity".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            float buttonsY = rect.yMax - 92f;
            float buttonGap = 8f;
            float buttonWidth = (width - buttonGap) * 0.5f;
            bool canSummon = Find.CurrentMap != null;
            bool enoughForSingle = canSummon && AvailableSilver() >= settings.summonCost;
            bool enoughForTen = canSummon && AvailableSilver() >= settings.tenSummonCost;
            if (Widgets.ButtonText(new Rect(x, buttonsY, buttonWidth, ButtonHeight), "GC_SummonOne".Translate(), true, true, enoughForSingle))
            {
                TrySummon(1, settings.summonCost);
            }

            if (Widgets.ButtonText(new Rect(x + buttonWidth + buttonGap, buttonsY, buttonWidth, ButtonHeight), "GC_SummonTen".Translate(), true, true, enoughForTen))
            {
                TrySummon(10, settings.tenSummonCost);
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(x, buttonsY + ButtonHeight + 2f, buttonWidth, 18f), settings.summonCost.ToString("N0") + " " + "GC_Silver".Translate());
            Widgets.Label(new Rect(x + buttonWidth + buttonGap, buttonsY + ButtonHeight + 2f, buttonWidth, 18f), settings.tenSummonCost.ToString("N0") + " " + "GC_Silver".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawResultsPanel(Rect rect)
        {
            int resultCount = lastSummonResults == null ? 0 : lastSummonResults.Count;
            DrawPanel(rect, resultCount > 0 ? "GC_LastSummon".Translate() + " x" + resultCount : "GC_Results".Translate());
            float x = rect.x + 10f;
            float y = rect.y + 42f;
            float gap = 6f;
            int columns = 2;
            float cardWidth = (rect.width - 20f - gap) / columns;
            float cardHeight = 74f;

            if (resultCount == 0)
            {
                GUI.color = new Color(0.72f, 0.72f, 0.72f);
                Widgets.Label(new Rect(x + 8f, y + 8f, rect.width - 36f, 80f), "GC_ResultsPlaceholder".Translate());
                GUI.color = Color.white;
                return;
            }

            int visibleCount = Math.Min(resultCount, 10);
            for (int i = 0; i < visibleCount; i++)
            {
                int column = i % columns;
                int row = i / columns;
                Rect card = new Rect(x + column * (cardWidth + gap), y + row * (cardHeight + gap), cardWidth, cardHeight);
                DrawResultCard(card, lastSummonResults[i]);
            }
        }

        private static void DrawResultCard(Rect rect, SummonResult result)
        {
            Color rarityColor = RarityColor(result.Rarity);
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.11f, 0.12f, 0.13f, 0.98f), rarityColor, 1);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), rarityColor);
            Widgets.ThingIcon(new Rect(rect.x + 8f, rect.y + 12f, 46f, 46f), result.Pawn, 1f, Rot4.South, false, 1f, false);

            float textX = rect.x + 60f;
            float textWidth = rect.width - 67f;
            Text.Font = GameFont.Small;
            GUI.color = rarityColor;
            Widgets.Label(new Rect(textX, rect.y + 5f, textWidth, 21f), RarityLabel(result.Rarity) + " · " + result.Pawn.LabelShort);
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(textX, rect.y + 27f, textWidth, 18f), ArchetypeLabel(result.Archetype) + " · " + result.Pawn.def.label);

            string reward = "";
            if (result.Companion != null)
            {
                reward = "GC_Mech".Translate() + " " + result.Companion.LabelShort;
            }
            else if (result.Archetype.Name == "Псионик")
            {
                reward = "GC_Psycasts".Translate() + " " + PsycastLabelsFor(result.Pawn).Count;
            }

            if (!string.IsNullOrEmpty(reward))
            {
                Widgets.Label(new Rect(textX, rect.y + 46f, textWidth, 17f), reward);
            }

            Text.Font = GameFont.Small;
        }

        private static string RarityLabel(string rarity)
        {
            return ("GC_Rarity_" + rarity).Translate();
        }

        private static string ArchetypeLabel(GachaArchetype archetype)
        {
            switch (archetype.Name)
            {
                case "Боец": return "GC_Archetype_Fighter".Translate();
                case "Медик": return "GC_Archetype_Medic".Translate();
                case "Инженер": return "GC_Archetype_Engineer".Translate();
                case "Следопыт": return "GC_Archetype_Ranger".Translate();
                case "Лидер": return "GC_Archetype_Leader".Translate();
                case "Псионик": return "GC_Archetype_Psycaster".Translate();
                default: return archetype.Name;
            }
        }
        private static Color RarityColor(string rarity)
        {
            switch (rarity)
            {
                case "UR": return new Color(1f, 0.76f, 0.24f);
                case "SSR": return new Color(0.72f, 0.43f, 0.98f);
                case "SR": return new Color(0.35f, 0.68f, 1f);
                default: return new Color(0.68f, 0.7f, 0.72f);
            }
        }

        private static int AvailableSilver()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return 0;
            }

            List<Thing> silverStacks = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            int amount = 0;
            for (int i = 0; i < silverStacks.Count; i++)
            {
                amount += silverStacks[i].stackCount;
            }

            return amount;
        }

        private static void TrySummon(int count, int cost)
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("GC_NeedColonyMap".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<Thing> silverStacks = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            int availableSilver = 0;
            for (int i = 0; i < silverStacks.Count; i++)
            {
                availableSilver += silverStacks[i].stackCount;
            }

            if (availableSilver < cost)
            {
                Messages.Message("GC_NotEnoughSilver".Translate() + " " + cost + ".", MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<SummonResult> pawns = new List<SummonResult>();
            HashSet<ThingDef> usedApparelMaterials = new HashSet<ThingDef>();
            HashSet<ThingDef> usedApparelDefs = new HashSet<ThingDef>();
            HashSet<ThingDef> usedWeapons = new HashSet<ThingDef>();
            HashSet<TraitDef> usedTraits = new HashSet<TraitDef>();
            HashSet<AbilityDef> usedPsycasts = new HashSet<AbilityDef>();
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = GenerateRandomColonist();
                if (pawn == null)
                {
                    for (int j = 0; j < pawns.Count; j++)
                    {
                        pawns[j].Pawn.Destroy(DestroyMode.Vanish);
                        if (pawns[j].Companion != null)
                        {
                            pawns[j].Companion.Destroy(DestroyMode.Vanish);
                        }
                    }

                    Messages.Message("GC_SilverNotSpent".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }

                string rarity = GetPityComponent().RollNextRarity();
                GachaArchetype archetype = SelectLeastUsedArchetype(pawns, pawn);
                Pawn companion = ApplyRarityBenefits(pawn, rarity, archetype, usedApparelMaterials, usedApparelDefs, usedWeapons, usedTraits, usedPsycasts);
                pawns.Add(new SummonResult(pawn, companion, rarity, archetype));
            }

            ConsumeSilver(silverStacks, cost);
            lastSummonResults = pawns;
            lastResultsGame = Current.Game;

            for (int i = 0; i < pawns.Count; i++)
            {
                ActiveTransporterInfo pod = new ActiveTransporterInfo();
                pod.innerContainer.TryAdd(pawns[i].Pawn);
                if (pawns[i].Companion != null)
                {
                    pod.innerContainer.TryAdd(pawns[i].Companion);
                }
                pod.openDelay = 120;
                pod.leaveSlag = false;
                DropPodUtility.MakeDropPodAt(DropCellFinder.RandomDropSpot(map), map, pod, Faction.OfPlayer);
            }

            List<string> results = new List<string>();
            for (int i = 0; i < pawns.Count; i++)
            {
                string result = RarityLabel(pawns[i].Rarity) + " — " + pawns[i].Pawn.LabelShort + " («" + ArchetypeLabel(pawns[i].Archetype) + "»)";
                if (pawns[i].Companion != null)
                {
                    result += " + " + pawns[i].Companion.LabelShort;
                }
                else if (pawns[i].Archetype.Name == "Псионик")
                {
                    List<string> abilityNames = PsycastLabelsFor(pawns[i].Pawn);
                    if (abilityNames.Count > 0)
                    {
                        result += ": " + string.Join(", ", abilityNames);
                    }
                }

                results.Add(result);
            }

            Find.LetterStack.ReceiveLetter(
                "GC_SummonResults".Translate() + " x" + count,
                "GC_PawnsArrive".Translate() + "\n" + string.Join("\n", results),
                LetterDefOf.PositiveEvent,
                new LookTargets(pawns[0].Pawn));
        }

        private static GachaColonistsGameComponent GetPityComponent()
        {
            GachaColonistsGameComponent component = Current.Game.GetComponent<GachaColonistsGameComponent>();
            if (component == null)
            {
                component = new GachaColonistsGameComponent(Current.Game);
                Current.Game.components.Add(component);
            }

            return component;
        }

        private static Pawn GenerateRandomColonist()
        {
            Dictionary<ThingDef, List<PawnKindDef>> kindsByRace = new Dictionary<ThingDef, List<PawnKindDef>>();
            List<PawnKindDef> allKinds = DefDatabase<PawnKindDef>.AllDefsListForReading;
            for (int i = 0; i < allKinds.Count; i++)
            {
                PawnKindDef kind = allKinds[i];
                if (kind.race == null || kind.race.race == null || !kind.race.race.Humanlike || kind.race.defName == "CreepJoiner" || kind.factionLeader || kind.isBoss || kind.hostileToAll || kind.mutant != null)
                {
                    continue;
                }

                List<PawnKindDef> raceKinds;
                if (!kindsByRace.TryGetValue(kind.race, out raceKinds))
                {
                    raceKinds = new List<PawnKindDef>();
                    kindsByRace.Add(kind.race, raceKinds);
                }

                raceKinds.Add(kind);
            }

            List<RaceGenerationCandidate> candidates = new List<RaceGenerationCandidate>();
            RaceGenerationCandidate baselinerCandidate = null;
            foreach (KeyValuePair<ThingDef, List<PawnKindDef>> raceEntry in kindsByRace)
            {
                List<PawnKindDef> suitableKinds = new List<PawnKindDef>();
                for (int i = 0; i < raceEntry.Value.Count; i++)
                {
                    PawnKindDef kind = raceEntry.Value[i];
                    if (kind.defaultFactionDef == null || kind.defaultFactionDef == Faction.OfPlayer.def)
                    {
                        suitableKinds.Add(kind);
                    }
                }

                if (suitableKinds.Count == 0)
                {
                    suitableKinds = raceEntry.Value;
                }

                if (raceEntry.Key == ThingDefOf.Human)
                {
                    suitableKinds = new List<PawnKindDef> { PawnKindDefOf.Colonist };
                    if (ModsConfig.BiotechActive)
                    {
                        XenotypeDef baseliner = DefDatabase<XenotypeDef>.GetNamedSilentFail("Baseliner");
                        if (baseliner != null)
                        {
                            baselinerCandidate = new RaceGenerationCandidate(suitableKinds, baseliner);
                        }

                        List<XenotypeDef> xenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;
                        for (int i = 0; i < xenotypes.Count; i++)
                        {
                            if (xenotypes[i].defName != "Baseliner")
                            {
                                candidates.Add(new RaceGenerationCandidate(suitableKinds, xenotypes[i]));
                            }
                        }
                    }
                    else
                    {
                        candidates.Add(new RaceGenerationCandidate(suitableKinds, null));
                    }
                }
                else
                {
                    candidates.Add(new RaceGenerationCandidate(suitableKinds, null));
                }
            }

            if (candidates.Count == 0)
            {
                if (baselinerCandidate == null)
                {
                    return PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer));
                }
            }

            if (baselinerCandidate != null && Rand.Value < 0.35f)
            {
                Pawn baselinerPawn = TryGeneratePawnFromCandidate(baselinerCandidate);
                if (baselinerPawn != null)
                {
                    return baselinerPawn;
                }
            }

            while (candidates.Count > 0)
            {
                int candidateIndex = Rand.Range(0, candidates.Count);
                RaceGenerationCandidate selected = candidates[candidateIndex];
                candidates.RemoveAt(candidateIndex);
                Pawn pawn = TryGeneratePawnFromCandidate(selected);
                if (pawn != null)
                {
                    return pawn;
                }
            }

            if (baselinerCandidate != null)
            {
                Pawn baselinerPawn = TryGeneratePawnFromCandidate(baselinerCandidate);
                if (baselinerPawn != null)
                {
                    return baselinerPawn;
                }
            }

            return PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer));
        }

        private static Pawn TryGeneratePawnFromCandidate(RaceGenerationCandidate candidate)
        {
            PawnKindDef selectedKind = candidate.Kinds[Rand.Range(0, candidate.Kinds.Count)];
            try
            {
                PawnGenerationRequest request = new PawnGenerationRequest(selectedKind, Faction.OfPlayer);
                request.ForceGenerateNewPawn = true;
                if (candidate.Xenotype != null)
                {
                    request.ForcedXenotype = candidate.Xenotype;
                }

                return PawnGenerator.GeneratePawn(request);
            }
            catch (Exception ex)
            {
                Log.Warning("[Gacha Colonists] Skipped incompatible race " + selectedKind.race.defName + ": " + ex.Message);
                return null;
            }
        }

        private static GachaArchetype SelectLeastUsedArchetype(List<SummonResult> previousResults, Pawn pawn)
        {
            int minimumCount = int.MaxValue;
            List<GachaArchetype> leastUsed = new List<GachaArchetype>();
            for (int i = 0; i < Archetypes.Length; i++)
            {
                if ((Archetypes[i].Name == "Псионик" && !ModsConfig.RoyaltyActive) || !CanPawnUseArchetype(pawn, Archetypes[i]))
                {
                    continue;
                }

                int count = 0;
                for (int j = 0; j < previousResults.Count; j++)
                {
                    if (previousResults[j].Archetype == Archetypes[i])
                    {
                        count++;
                    }
                }

                if (count < minimumCount)
                {
                    minimumCount = count;
                    leastUsed.Clear();
                    leastUsed.Add(Archetypes[i]);
                }
                else if (count == minimumCount)
                {
                    leastUsed.Add(Archetypes[i]);
                }
            }

            return leastUsed.Count > 0
                ? leastUsed[Rand.Range(0, leastUsed.Count)]
                : Archetypes[Rand.Range(0, Archetypes.Length)];
        }

        private static bool CanPawnUseArchetype(Pawn pawn, GachaArchetype archetype)
        {
            if (archetype.Name == "Псионик" && pawn.GetStatValue(StatDefOf.PsychicSensitivity) <= 0f)
            {
                return false;
            }

            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                if (pawn.skills.skills[i].def.defName == archetype.PreferredSkills[0])
                {
                    return !pawn.skills.skills[i].TotallyDisabled;
                }
            }

            return false;
        }

        private static Pawn ApplyRarityBenefits(Pawn pawn, string rarity, GachaArchetype archetype, HashSet<ThingDef> usedApparelMaterials, HashSet<ThingDef> usedApparelDefs, HashSet<ThingDef> usedWeapons, HashSet<TraitDef> usedTraits, HashSet<AbilityDef> usedPsycasts)
        {
            ApplyRarityTraits(pawn, rarity, archetype, usedTraits);
            if (rarity == "R")
            {
                SetCommonPawnStatsAndRemoveGear(pawn, archetype);
                EnsureArchetypeSkills(pawn, rarity, archetype);
                if (archetype.Name == "Псионик")
                {
                    GivePsycasts(pawn, rarity, usedPsycasts);
                }
                return archetype.Name == "Инженер" ? AddEngineerMechAndLink(pawn) : null;
            }

            int skillCount;
            int skillBonus;
            int apparelCount;
            float gearQuality;
            QualityCategory itemQuality;

            switch (rarity)
            {
                case "UR":
                    skillCount = 4;
                    skillBonus = 3;
                    apparelCount = 3;
                    gearQuality = 0f;
                    itemQuality = QualityCategory.Excellent;
                    break;
                case "SSR":
                    skillCount = 3;
                    skillBonus = 2;
                    apparelCount = 2;
                    gearQuality = 0.3f;
                    itemQuality = QualityCategory.Good;
                    break;
                case "SR":
                    skillCount = 2;
                    skillBonus = 1;
                    apparelCount = 2;
                    gearQuality = 0.55f;
                    itemQuality = QualityCategory.Normal;
                    break;
                default:
                    return null;
            }

            if (archetype.Name == "Псионик")
            {
                skillCount = Math.Max(1, skillCount - 1);
                skillBonus = Math.Max(0, skillBonus - 1);
            }

            ImproveSkills(pawn, skillCount, skillBonus, archetype);
            EnsureArchetypeSkills(pawn, rarity, archetype);
            float totalGearBudget = GearBudgetForRarity(rarity);
            float armorBudget = totalGearBudget * 0.65f;
            float weaponBudget = totalGearBudget - armorBudget;
            pawn.apparel.DestroyAll(DestroyMode.Vanish);
            AddTieredApparel(pawn, rarity, gearQuality, itemQuality, apparelCount, usedApparelMaterials, usedApparelDefs, ref armorBudget);
            float remainingSuppliesBudget = weaponBudget + armorBudget;
            if (archetype.PreferredWeaponStyle == WeaponStyle.None)
            {
                pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                if (archetype.Name == "Медик")
                {
                    AddMedicalSupplies(pawn, rarity, remainingSuppliesBudget);
                }
                else if (archetype.Name == "Псионик")
                {
                    GivePsycasts(pawn, rarity, usedPsycasts);
                }
            }
            else if (archetype.Name == "Инженер")
            {
                pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                return AddEngineerMechAndLink(pawn);
            }
            else
            {
                EquipTieredWeapon(pawn, rarity, gearQuality, itemQuality, remainingSuppliesBudget, archetype, usedWeapons);
            }

            return null;
        }

        private static Pawn AddEngineerMechAndLink(Pawn engineer)
        {
            HediffDef mechlinkDef = DefDatabase<HediffDef>.GetNamedSilentFail("MechlinkImplant");
            if (mechlinkDef == null)
            {
                return null;
            }

            string[] mechKindNames = new string[]
            {
                "Mech_Lifter",
                "Mech_Constructoid",
                "Mech_Agrihand",
                "Mech_Cleansweeper"
            };
            List<PawnKindDef> availableKinds = new List<PawnKindDef>();
            for (int i = 0; i < mechKindNames.Length; i++)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(mechKindNames[i]);
                if (kind != null)
                {
                    availableKinds.Add(kind);
                }
            }

            if (availableKinds.Count == 0)
            {
                return null;
            }

            PawnKindDef selectedKind = availableKinds[Rand.Range(0, availableKinds.Count)];
            Pawn mech = PawnGenerator.GeneratePawn(selectedKind, Faction.OfPlayer);
            if (mech == null)
            {
                return null;
            }

            if (engineer.health.hediffSet.GetFirstHediffOfDef(mechlinkDef) == null)
            {
                BodyPartRecord brain = engineer.health.hediffSet.GetBrain();
                if (brain == null)
                {
                    mech.Destroy(DestroyMode.Vanish);
                    return null;
                }

                Hediff mechlink = HediffMaker.MakeHediff(mechlinkDef, engineer, brain);
                engineer.health.AddHediff(mechlink, brain);
            }

            mech.relations.AddDirectRelation(PawnRelationDefOf.Overseer, engineer);
            return mech;
        }

        private static void ApplyRarityTraits(Pawn pawn, string rarity, GachaArchetype archetype, HashSet<TraitDef> usedTraits)
        {
            if (pawn.story == null || pawn.story.traits == null)
            {
                return;
            }

            List<Trait> existingTraits = new List<Trait>(pawn.story.traits.allTraits);
            for (int i = 0; i < existingTraits.Count; i++)
            {
                pawn.story.traits.RemoveTrait(existingTraits[i]);
            }

            int goodCount;
            int badCount;
            switch (rarity)
            {
                case "UR": goodCount = 3; badCount = 0; break;
                case "SSR": goodCount = 2; badCount = 1; break;
                case "SR": goodCount = 1; badCount = 1; break;
                default: goodCount = 0; badCount = 2; break;
            }

            List<RarityTraitCandidate> goodTraits = new List<RarityTraitCandidate>();
            List<RarityTraitCandidate> badTraits = new List<RarityTraitCandidate>();
            List<TraitDef> allDefs = DefDatabase<TraitDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                TraitDef def = allDefs[i];
                if (def.degreeDatas == null || def.degreeDatas.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < def.degreeDatas.Count; j++)
                {
                    TraitDegreeData degreeData = def.degreeDatas[j];
                    int score = degreeData.degree;
                    if (score == 0)
                    {
                        score = ClassifySingleDegreeTrait(def, degreeData);
                    }

                    if (score > 0)
                    {
                        goodTraits.Add(new RarityTraitCandidate(def, degreeData.degree));
                    }
                    else if (score < 0)
                    {
                        badTraits.Add(new RarityTraitCandidate(def, degreeData.degree));
                    }
                }
            }

            AddRarityTraitsFromPool(pawn, goodTraits, goodCount, archetype, usedTraits, true);
            AddRarityTraitsFromPool(pawn, badTraits, badCount, archetype, usedTraits, false);
        }

        private static int ClassifySingleDegreeTrait(TraitDef def, TraitDegreeData degreeData)
        {
            string name = def.defName;
            if (IsTraitName(name, "Kind", "Tough", "Nimble", "FastLearner", "GreatMemory", "QuickSleeper", "PerfectMemory", "Joyous", "BodyMastery"))
            {
                return 1;
            }

            if (IsTraitName(name, "Abrasive", "Greedy", "Jealous", "AnnoyingVoice", "CreepyBreathing", "Wimp", "SlowLearner", "Pyromaniac", "Delicate", "Recluse", "Disturbing", "VoidFascination"))
            {
                return -1;
            }

            FieldInfo offsetField = typeof(TraitDegreeData).GetField("marketValueFactorOffset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (offsetField != null)
            {
                object value = offsetField.GetValue(degreeData);
                if (value is float)
                {
                    float offset = (float)value;
                    if (offset > 0f) return 1;
                    if (offset < 0f) return -1;
                }
            }

            PropertyInfo offsetProperty = typeof(TraitDegreeData).GetProperty("marketValueFactorOffset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (offsetProperty != null)
            {
                object value = offsetProperty.GetValue(degreeData, null);
                if (value is float)
                {
                    float offset = (float)value;
                    if (offset > 0f) return 1;
                    if (offset < 0f) return -1;
                }
            }

            return 0;
        }

        private static bool IsTraitName(string name, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (name == names[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddRarityTraitsFromPool(Pawn pawn, List<RarityTraitCandidate> candidates, int count, GachaArchetype archetype, HashSet<TraitDef> usedTraits, bool preferArchetype)
        {
            int attempts = candidates.Count * 2;
            int added = 0;
            while (added < count && candidates.Count > 0 && attempts-- > 0)
            {
                List<int> unusedIndexes = new List<int>();
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (!usedTraits.Contains(candidates[i].Def))
                    {
                        unusedIndexes.Add(i);
                    }
                }

                List<int> archetypeIndexes = new List<int>();
                if (preferArchetype)
                {
                    for (int i = 0; i < unusedIndexes.Count; i++)
                    {
                        if (IsPreferredTrait(candidates[unusedIndexes[i]].Def.defName, archetype))
                        {
                            archetypeIndexes.Add(unusedIndexes[i]);
                        }
                    }
                }

                int index = archetypeIndexes.Count > 0 && Rand.Value < 0.65f
                    ? archetypeIndexes[Rand.Range(0, archetypeIndexes.Count)]
                    : unusedIndexes.Count > 0
                        ? unusedIndexes[Rand.Range(0, unusedIndexes.Count)]
                        : Rand.Range(0, candidates.Count);
                RarityTraitCandidate candidate = candidates[index];
                candidates.RemoveAt(index);

                try
                {
                    pawn.story.traits.GainTrait(new Trait(candidate.Def, candidate.Degree));
                    if (pawn.story.traits.HasTrait(candidate.Def))
                    {
                        usedTraits.Add(candidate.Def);
                        added++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[Gacha Colonists] Skipped incompatible trait " + candidate.Def.defName + ": " + ex.Message);
                }
            }
        }

        private static float GearBudgetForRarity(string rarity)
        {
            switch (rarity)
            {
                case "SR": return 1000f;
                case "SSR": return 3000f;
                case "UR": return 5000f;
                default: return 0f;
            }
        }

        private static void SetCommonPawnStatsAndRemoveGear(Pawn pawn, GachaArchetype archetype)
        {
            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                pawn.skills.skills[i].Level = Rand.Range(1, 3);
                if (!pawn.skills.skills[i].TotallyDisabled && IsPreferredSkill(pawn.skills.skills[i].def.defName, archetype) && Rand.Value < 0.7f)
                {
                    pawn.skills.skills[i].Level = 2;
                }
            }

            pawn.apparel.DestroyAll(DestroyMode.Vanish);
            pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            pawn.inventory.DestroyAll(DestroyMode.Vanish);
            pawn.carryTracker.DestroyCarriedThing();
        }

        private static void EnsureArchetypeSkills(Pawn pawn, string rarity, GachaArchetype archetype)
        {
            int primaryTarget;
            int secondaryTarget;
            switch (rarity)
            {
                case "UR": primaryTarget = 14; secondaryTarget = 11; break;
                case "SSR": primaryTarget = 10; secondaryTarget = 7; break;
                default: primaryTarget = 6; secondaryTarget = 4; break;
            }

            if (rarity == "R")
            {
                primaryTarget = 2;
                secondaryTarget = 1;
            }
            else if (archetype.Name == "Псионик")
            {
                primaryTarget = Math.Max(1, primaryTarget - 2);
                secondaryTarget = Math.Max(1, secondaryTarget - 1);
            }

            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                SkillRecord skill = pawn.skills.skills[i];
                if (skill.TotallyDisabled)
                {
                    continue;
                }

                if (skill.def.defName == archetype.PreferredSkills[0])
                {
                    skill.Level = Math.Max(skill.Level, primaryTarget);
                }
                else if (archetype.PreferredSkills.Length > 1 && skill.def.defName == archetype.PreferredSkills[1])
                {
                    skill.Level = Math.Max(skill.Level, secondaryTarget);
                }
            }
        }

        private static void AddMedicalSupplies(Pawn pawn, string rarity, float remainingBudget)
        {
            string desiredMedicine = rarity == "UR" ? "MedicineUltratech" : rarity == "SSR" ? "MedicineIndustrial" : "MedicineHerbal";
            ThingDef medicineDef = DefDatabase<ThingDef>.GetNamedSilentFail(desiredMedicine);
            if (medicineDef == null || !medicineDef.PlayerAcquirable)
            {
                medicineDef = DefDatabase<ThingDef>.GetNamedSilentFail("MedicineIndustrial");
            }

            if (medicineDef == null || !medicineDef.PlayerAcquirable)
            {
                medicineDef = DefDatabase<ThingDef>.GetNamedSilentFail("MedicineHerbal");
            }

            if (medicineDef == null || !medicineDef.PlayerAcquirable || medicineDef.BaseMarketValue <= 0f)
            {
                return;
            }

            int desiredCount = rarity == "UR" ? 8 : rarity == "SSR" ? 5 : 3;
            int countByBudget = (int)(remainingBudget / medicineDef.BaseMarketValue);
            int stackCount = Math.Min(desiredCount, countByBudget);
            if (stackCount <= 0)
            {
                return;
            }

            Thing medicine = ThingMaker.MakeThing(medicineDef);
            medicine.stackCount = stackCount;
            if (medicine.MarketValue <= remainingBudget && pawn.inventory.innerContainer.TryAdd(medicine))
            {
                return;
            }

            medicine.Destroy(DestroyMode.Vanish);
        }

        private static void GivePsycasts(Pawn pawn, string rarity, HashSet<AbilityDef> usedPsycasts)
        {
            HediffDef psylinkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PsychicAmplifier");
            BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
            if (psylinkDef == null || brain == null || pawn.abilities == null)
            {
                return;
            }

            int psylinkLevel;
            int abilityCount;
            int minimumAbilityLevel;
            int maximumAbilityLevel;
            switch (rarity)
            {
                case "UR": psylinkLevel = 5; abilityCount = 3; minimumAbilityLevel = 3; maximumAbilityLevel = 5; break;
                case "SSR": psylinkLevel = 3; abilityCount = 2; minimumAbilityLevel = 2; maximumAbilityLevel = 3; break;
                case "SR": psylinkLevel = 2; abilityCount = 1; minimumAbilityLevel = 1; maximumAbilityLevel = 2; break;
                default: return;
            }

            Hediff_Psylink psylink = pawn.health.hediffSet.GetFirstHediffOfDef(psylinkDef) as Hediff_Psylink;
            if (psylink == null)
            {
                psylink = HediffMaker.MakeHediff(psylinkDef, pawn, brain) as Hediff_Psylink;
                if (psylink == null)
                {
                    return;
                }

                pawn.health.AddHediff(psylink, brain);
            }

            int currentLevel = Math.Max(1, (int)psylink.Severity);
            if (currentLevel < psylinkLevel)
            {
                psylink.ChangeLevel(psylinkLevel - currentLevel, false);
            }

            List<Ability> existingAbilities = pawn.abilities.AllAbilitiesForReading;
            for (int i = existingAbilities.Count - 1; i >= 0; i--)
            {
                if (IsPsycastAbility(existingAbilities[i].def))
                {
                    pawn.abilities.RemoveAbility(existingAbilities[i].def);
                }
            }

            List<AbilityDef> candidates = new List<AbilityDef>();
            List<AbilityDef> allAbilityDefs = DefDatabase<AbilityDef>.AllDefsListForReading;
            for (int i = 0; i < allAbilityDefs.Count; i++)
            {
                AbilityDef def = allAbilityDefs[i];
                if (IsPsycastAbility(def) && def.level >= minimumAbilityLevel && def.level <= maximumAbilityLevel)
                {
                    candidates.Add(def);
                }
            }

            int count = Math.Min(abilityCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                List<AbilityDef> unusedCandidates = new List<AbilityDef>();
                for (int j = 0; j < candidates.Count; j++)
                {
                    if (!usedPsycasts.Contains(candidates[j]))
                    {
                        unusedCandidates.Add(candidates[j]);
                    }
                }

                List<AbilityDef> pool = unusedCandidates.Count > 0 ? unusedCandidates : candidates;
                AbilityDef abilityDef = pool[Rand.Range(0, pool.Count)];
                candidates.Remove(abilityDef);
                pawn.abilities.GainAbility(abilityDef);
                usedPsycasts.Add(abilityDef);
            }
        }

        private static List<string> PsycastLabelsFor(Pawn pawn)
        {
            List<string> labels = new List<string>();
            if (pawn.abilities == null)
            {
                return labels;
            }

            List<Ability> abilities = pawn.abilities.AllAbilitiesForReading;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (IsPsycastAbility(abilities[i].def))
                {
                    labels.Add(abilities[i].def.LabelCap);
                }
            }

            return labels;
        }

        private static bool IsPsycastAbility(AbilityDef def)
        {
            return def.IsPsycast ||
                (def.abilityClass != null && typeof(Psycast).IsAssignableFrom(def.abilityClass)) ||
                (def.category != null && def.category.defName == "Psychic");
        }

        private static void ImproveSkills(Pawn pawn, int skillCount, int bonus, GachaArchetype archetype)
        {
            List<SkillRecord> availableSkills = new List<SkillRecord>();
            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                if (!pawn.skills.skills[i].TotallyDisabled)
                {
                    availableSkills.Add(pawn.skills.skills[i]);
                }
            }

            int count = Math.Min(skillCount, availableSkills.Count);
            for (int i = 0; i < count; i++)
            {
                List<int> preferredIndexes = new List<int>();
                for (int j = 0; j < availableSkills.Count; j++)
                {
                    if (IsPreferredSkill(availableSkills[j].def.defName, archetype))
                    {
                        preferredIndexes.Add(j);
                    }
                }

                int index = preferredIndexes.Count > 0 && Rand.Value < 0.8f
                    ? preferredIndexes[Rand.Range(0, preferredIndexes.Count)]
                    : Rand.Range(0, availableSkills.Count);
                SkillRecord skill = availableSkills[index];
                skill.Level = Math.Min(20, skill.Level + bonus);
                availableSkills.RemoveAt(index);
            }
        }

        private static bool IsPreferredSkill(string skillName, GachaArchetype archetype)
        {
            for (int i = 0; i < archetype.PreferredSkills.Length; i++)
            {
                if (skillName == archetype.PreferredSkills[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPreferredTrait(string traitName, GachaArchetype archetype)
        {
            switch (archetype.Name)
            {
                case "Боец":
                    return IsTraitName(traitName, "Tough", "Nimble", "Brawler", "ShootingAccuracy", "Bloodlust");
                case "Медик":
                    return IsTraitName(traitName, "Kind", "FastLearner", "GreatMemory", "PerfectMemory", "Immunity");
                case "Инженер":
                    return IsTraitName(traitName, "Industriousness", "FastLearner", "GreatMemory", "PerfectMemory", "BodyMastery");
                case "Следопыт":
                    return IsTraitName(traitName, "SpeedOffset", "Nimble", "Animals", "Plants", "Undergrounder");
                case "Псионик":
                    return IsTraitName(traitName, "PsychicSensitivity", "NaturalMood", "Nerves", "PerfectMemory", "GreatMemory");
                default:
                    return IsTraitName(traitName, "Kind", "Beauty", "NaturalMood", "Nerves", "Joyous");
            }
        }

        private static void EquipTieredWeapon(Pawn pawn, string rarity, float startPercent, QualityCategory quality, float remainingBudget, GachaArchetype archetype, HashSet<ThingDef> usedWeapons)
        {
            pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
            List<ThingDef> candidates = new List<ThingDef>();
            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            int shootingSkill = pawn.skills.GetSkill(SkillDefOf.Shooting).Level;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                if (def.IsWeapon && def.equipmentType == EquipmentType.Primary && def.PlayerAcquirable && def.BaseMarketValue > 0f && def.BaseMarketValue <= remainingBudget && !HasUniqueWeaponComp(def))
                {
                    if (def.IsRangedWeapon && (shootingSkill < 4 || (shootingSkill < 8 && def.BaseMarketValue > 700f)))
                    {
                        continue;
                    }

                    if (!usedWeapons.Contains(def))
                    {
                        candidates.Add(def);
                    }
                }
            }

            if (candidates.Count == 0 && usedWeapons.Count > 0)
            {
                for (int i = 0; i < allDefs.Count; i++)
                {
                    ThingDef def = allDefs[i];
                    if (def.IsWeapon && def.equipmentType == EquipmentType.Primary && def.PlayerAcquirable && def.BaseMarketValue > 0f && def.BaseMarketValue <= remainingBudget && !HasUniqueWeaponComp(def))
                    {
                        if (def.IsRangedWeapon && (shootingSkill < 4 || (shootingSkill < 8 && def.BaseMarketValue > 700f)))
                        {
                            continue;
                        }

                        candidates.Add(def);
                    }
                }
            }

            if (archetype.PreferredWeaponStyle != WeaponStyle.Any)
            {
                List<ThingDef> preferredWeapons = new List<ThingDef>();
                for (int i = 0; i < candidates.Count; i++)
                {
                    bool matches = archetype.PreferredWeaponStyle == WeaponStyle.Melee
                        ? candidates[i].IsMeleeWeapon
                        : candidates[i].IsRangedWeapon;
                    if (matches)
                    {
                        preferredWeapons.Add(candidates[i]);
                    }
                }

                if (preferredWeapons.Count > 0)
                {
                    candidates = preferredWeapons;
                }
            }

            while (candidates.Count > 0)
            {
                candidates.Sort(delegate(ThingDef left, ThingDef right)
                {
                    return right.BaseMarketValue.CompareTo(left.BaseMarketValue);
                });
                int highValueOptions = Math.Min(5, candidates.Count);
                ThingDef weaponDef = candidates[Rand.Range(0, highValueOptions)];
                candidates.Remove(weaponDef);
                try
                {
                    ThingWithComps weapon = ThingMaker.MakeThing(weaponDef, GenStuff.RandomStuffFor(weaponDef)) as ThingWithComps;
                    if (weapon == null)
                    {
                        continue;
                    }

                    SetItemQuality(weapon, quality);
                    float marketValue = weapon.MarketValue;
                    if (marketValue > remainingBudget)
                    {
                        weapon.Destroy(DestroyMode.Vanish);
                        continue;
                    }

                    pawn.equipment.AddEquipment(weapon);
                    usedWeapons.Add(weaponDef);
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning("[Gacha Colonists] Skipped incompatible weapon " + weaponDef.defName + ": " + ex.Message);
                }
            }

            return;
        }

        private static bool HasUniqueWeaponComp(ThingDef def)
        {
            if (def.comps == null)
            {
                return false;
            }

            for (int i = 0; i < def.comps.Count; i++)
            {
                if (def.comps[i].compClass != null && def.comps[i].compClass.Name == "CompUniqueWeapon")
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddTieredApparel(Pawn pawn, string rarity, float startPercent, QualityCategory quality, int itemCount, HashSet<ThingDef> usedMaterials, HashSet<ThingDef> usedApparelDefs, ref float remainingBudget)
        {
            string[] preferredGroups = itemCount >= 3
                ? new string[] { "Torso", "FullHead", "Legs" }
                : new string[] { "Torso", "FullHead" };

            int added = 0;
            for (int i = 0; i < preferredGroups.Length && added < itemCount; i++)
            {
                if (TryAddTieredApparel(pawn, rarity, startPercent, quality, preferredGroups[i], usedMaterials, usedApparelDefs, ref remainingBudget))
                {
                    added++;
                }
            }

            while (added < itemCount && TryAddTieredApparel(pawn, rarity, startPercent, quality, null, usedMaterials, usedApparelDefs, ref remainingBudget))
            {
                added++;
            }
        }

        private static bool TryAddTieredApparel(Pawn pawn, string rarity, float startPercent, QualityCategory quality, string preferredGroup, HashSet<ThingDef> usedMaterials, HashSet<ThingDef> usedApparelDefs, ref float remainingBudget)
        {
            List<ThingDef> candidates = new List<ThingDef>();
            List<ThingDef> repeatedCandidates = new List<ThingDef>();
            List<ThingDef> preferredCandidates = new List<ThingDef>();
            List<ThingDef> focusedCandidates = new List<ThingDef>();
            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ThingDef def = allDefs[i];
                if (!def.IsApparel || !def.PlayerAcquirable || !IsProtectiveApparel(def) || def.BaseMarketValue <= 0f || def.BaseMarketValue > remainingBudget || !pawn.apparel.CanWearWithoutDroppingAnything(def) || !ApparelUtility.HasPartsToWear(pawn, def) || !IsDevelopmentStageAllowed(pawn, def))
                {
                    continue;
                }

                repeatedCandidates.Add(def);
                if (!usedApparelDefs.Contains(def))
                {
                    candidates.Add(def);
                }
                if (preferredGroup == null || CoversPreferredBodyGroup(def, preferredGroup))
                {
                    preferredCandidates.Add(def);
                    if (!CoversOtherOutfitGroups(def, preferredGroup))
                    {
                        focusedCandidates.Add(def);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                candidates = repeatedCandidates;
            }

            if (focusedCandidates.Count > 0)
            {
                candidates = focusedCandidates;
            }
            else if (preferredCandidates.Count > 0)
            {
                candidates = preferredCandidates;
            }

            while (candidates.Count > 0)
            {
                candidates.Sort(delegate(ThingDef left, ThingDef right)
                {
                    int valueOrder = right.BaseMarketValue.CompareTo(left.BaseMarketValue);
                    return valueOrder != 0 ? valueOrder : ApparelProtectionScore(right).CompareTo(ApparelProtectionScore(left));
                });
                int highValueOptions = Math.Min(5, candidates.Count);
                ThingDef apparelDef = candidates[Rand.Range(0, highValueOptions)];
                candidates.Remove(apparelDef);
                ThingDef stuff = SelectApparelMaterial(apparelDef, usedMaterials);
                try
                {
                    Apparel apparel = ThingMaker.MakeThing(apparelDef, stuff) as Apparel;
                    if (apparel == null)
                    {
                        continue;
                    }

                    SetItemQuality(apparel, quality);
                    float marketValue = apparel.MarketValue;
                    if (marketValue > remainingBudget)
                    {
                        apparel.Destroy(DestroyMode.Vanish);
                        continue;
                    }

                    pawn.apparel.Wear(apparel, false, false);
                    if (!pawn.apparel.WornApparel.Contains(apparel))
                    {
                        apparel.Destroy(DestroyMode.Vanish);
                        continue;
                    }

                    remainingBudget -= marketValue;
                    usedApparelDefs.Add(apparelDef);
                    if (stuff != null)
                    {
                        usedMaterials.Add(stuff);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Warning("[Gacha Colonists] Skipped incompatible apparel " + apparelDef.defName + ": " + ex.Message);
                }
            }

            return false;
        }

        private static bool IsDevelopmentStageAllowed(Pawn pawn, ThingDef apparelDef)
        {
            if (apparelDef.apparel == null || apparelDef.apparel.developmentalStageFilter == DevelopmentalStage.None)
            {
                return true;
            }

            return (apparelDef.apparel.developmentalStageFilter & pawn.DevelopmentalStage) != 0;
        }

        private static bool CoversPreferredBodyGroup(ThingDef def, string preferredGroup)
        {
            if (def.apparel == null || def.apparel.bodyPartGroups == null)
            {
                return false;
            }

            for (int i = 0; i < def.apparel.bodyPartGroups.Count; i++)
            {
                string groupName = def.apparel.bodyPartGroups[i].defName;
                if (groupName == preferredGroup || (preferredGroup == "FullHead" && groupName == "UpperHead"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CoversOtherOutfitGroups(ThingDef def, string preferredGroup)
        {
            if (preferredGroup == null || def.apparel == null || def.apparel.bodyPartGroups == null)
            {
                return false;
            }

            for (int i = 0; i < def.apparel.bodyPartGroups.Count; i++)
            {
                string groupName = def.apparel.bodyPartGroups[i].defName;
                bool isOtherOutfitGroup =
                    (groupName == "Torso" && preferredGroup != "Torso") ||
                    ((groupName == "FullHead" || groupName == "UpperHead") && preferredGroup != "FullHead") ||
                    (groupName == "Legs" && preferredGroup != "Legs");
                if (isOtherOutfitGroup)
                {
                    return true;
                }
            }

            return false;
        }

        private static ThingDef SelectApparelMaterial(ThingDef apparelDef, HashSet<ThingDef> usedMaterials)
        {
            List<ThingDef> allMaterials = new List<ThingDef>();
            List<ThingDef> commonMaterials = new List<ThingDef>();
            foreach (ThingDef material in GenStuff.AllowedStuffsFor(apparelDef))
            {
                if (material == null || !material.IsStuff)
                {
                    continue;
                }

                allMaterials.Add(material);
                if (material.PlayerAcquirable)
                {
                    commonMaterials.Add(material);
                }
            }

            List<ThingDef> options = commonMaterials.Count > 0 ? commonMaterials : allMaterials;
            List<ThingDef> unusedMaterials = new List<ThingDef>();
            for (int i = 0; i < options.Count; i++)
            {
                if (!usedMaterials.Contains(options[i]))
                {
                    unusedMaterials.Add(options[i]);
                }
            }

            if (unusedMaterials.Count > 0)
            {
                options = unusedMaterials;
            }

            if (options.Count == 0)
            {
                return null;
            }

            return options[Rand.Range(0, options.Count)];
        }

        private static ThingDef SelectTieredItem(List<ThingDef> candidates, string rarity, float startPercent, bool prioritizeProtection = false)
        {
            if (candidates.Count == 0)
            {
                return null;
            }

            candidates.Sort(delegate(ThingDef left, ThingDef right)
            {
                if (prioritizeProtection)
                {
                    int protectionOrder = ApparelProtectionScore(right).CompareTo(ApparelProtectionScore(left));
                    if (protectionOrder != 0)
                    {
                        return protectionOrder;
                    }
                }

                return right.BaseMarketValue.CompareTo(left.BaseMarketValue);
            });

            int start = (int)(candidates.Count * startPercent);
            int end = Math.Max(start + 1, (int)Math.Ceiling(candidates.Count * (startPercent + 0.25f)));
            if (rarity == "UR")
            {
                start = 0;
                end = Math.Max(1, (int)Math.Ceiling(candidates.Count * 0.25f));
            }

            end = Math.Min(end, candidates.Count);
            return candidates[Rand.Range(start, end)];
        }

        private static float ApparelProtectionScore(ThingDef def)
        {
            float cachedScore;
            if (ApparelProtectionCache.TryGetValue(def, out cachedScore))
            {
                return cachedScore;
            }

            float bestScore = GetProtectionScore(def, null);
            foreach (ThingDef stuff in GenStuff.AllowedStuffsFor(def))
            {
                bestScore = Math.Max(bestScore, GetProtectionScore(def, stuff));
            }

            ApparelProtectionCache[def] = bestScore;
            return bestScore;
        }

        private static bool IsProtectiveApparel(ThingDef def)
        {
            float baseProtection = GetProtectionScore(def, null);
            return baseProtection >= 0.25f || ApparelProtectionScore(def) >= 0.5f;
        }

        private static float GetProtectionScore(ThingDef def, ThingDef stuff)
        {
            float sharp = StatExtension.GetStatValueAbstract(def, StatDefOf.ArmorRating_Sharp, stuff);
            float blunt = StatExtension.GetStatValueAbstract(def, StatDefOf.ArmorRating_Blunt, stuff);
            float heat = StatExtension.GetStatValueAbstract(def, StatDefOf.ArmorRating_Heat, stuff);
            return sharp + blunt + heat;
        }

        private static void SetItemQuality(ThingWithComps item, QualityCategory quality)
        {
            CompQuality qualityComp = item.TryGetComp<CompQuality>();
            if (qualityComp != null)
            {
                qualityComp.SetQuality(quality, ArtGenerationContext.Outsider);
            }
        }

        private static void ConsumeSilver(List<Thing> silverStacks, int amount)
        {
            int remaining = amount;
            for (int i = 0; i < silverStacks.Count && remaining > 0; i++)
            {
                Thing stack = silverStacks[i];
                int take = stack.stackCount < remaining ? stack.stackCount : remaining;
                Thing spentSilver = stack.SplitOff(take);
                spentSilver.Destroy(DestroyMode.Vanish);
                remaining -= take;
            }
        }
    }
}





