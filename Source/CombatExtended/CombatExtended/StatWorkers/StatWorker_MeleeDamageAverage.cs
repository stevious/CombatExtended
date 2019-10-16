﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_MeleeDamageAverage : StatWorker_MeleeDamageBase
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess)
        {
            var skilledDamageVariationMin = damageVariationMin;
            var skilledDamageVariationMax = damageVariationMax;

            if (req.Thing?.ParentHolder is Pawn_EquipmentTracker tracker)
            {
                skilledDamageVariationMin = GetDamageVariationMin(tracker.pawn);
                skilledDamageVariationMax = GetDamageVariationMax(tracker.pawn);
            }

            var tools = (req.Def as ThingDef)?.tools;
            if (tools.NullOrEmpty())
            {
                return 0;
            }
            if (tools.Any(x => !(x is ToolCE)))
            {
                Log.Error($"Trying to get stat MeleeDamageAverage from {req.Def.defName} which has no support for Combat Extended.");
                return 0;
            }

            var totalSelectionWeight = 0f;
            foreach (var tool in tools)
            {
                totalSelectionWeight += tool.chanceFactor;
            }
            var totalDPS = 0f;
            foreach (var tool in tools)
            {
                var minDPS = tool.power / tool.cooldownTime * skilledDamageVariationMin;
                var maxDPS = tool.power / tool.cooldownTime * skilledDamageVariationMax;
                var weightFactor = tool.chanceFactor / totalSelectionWeight;
                totalDPS += weightFactor * ((minDPS + maxDPS) / 2f);
            }
            return totalDPS;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var skilledDamageVariationMin = damageVariationMin;
            var skilledDamageVariationMax = damageVariationMax;
            var meleeSkillLevel = -1;

            if (req.Thing?.ParentHolder is Pawn_EquipmentTracker tracker && tracker.pawn != null)
            {
                var pawnHolder = tracker.pawn;
                skilledDamageVariationMin = GetDamageVariationMin(pawnHolder);
                skilledDamageVariationMax = GetDamageVariationMax(pawnHolder);

                if (pawnHolder.skills != null)
                    meleeSkillLevel = pawnHolder.skills.GetSkill(SkillDefOf.Melee).Level;
            }

            var tools = (req.Def as ThingDef)?.tools;

            if (tools.NullOrEmpty())
            {
                return base.GetExplanationUnfinalized(req, numberSense);
            }

            var stringBuilder = new StringBuilder();

            if (meleeSkillLevel >= 0)
            {
                stringBuilder.AppendLine("Wielder skill level: " + meleeSkillLevel);
            }
            stringBuilder.AppendLine(string.Format("Damage variation: {0}% - {1}%",
                (100 * skilledDamageVariationMin).ToStringByStyle(ToStringStyle.FloatMaxTwo),
                (100 * skilledDamageVariationMax).ToStringByStyle(ToStringStyle.FloatMaxTwo)));
            stringBuilder.AppendLine("");

            foreach (ToolCE tool in tools)
            {
                var minDPS = tool.power / tool.cooldownTime * skilledDamageVariationMin;
                var maxDPS = tool.power / tool.cooldownTime * skilledDamageVariationMax;

                var maneuvers = DefDatabase<ManeuverDef>.AllDefsListForReading.Where(d => tool.capacities.Contains(d.requiredCapacity));
                var maneuverString = "(";
                foreach (var maneuver in maneuvers)
                {
                    maneuverString += maneuver.ToString() + "/";
                }
                maneuverString = maneuverString.TrimmedToLength(maneuverString.Length - 1) + ")";

                stringBuilder.AppendLine("  Tool: " + tool.ToString() + " " + maneuverString);
                stringBuilder.AppendLine("    Base damage: " + tool.power.ToStringByStyle(ToStringStyle.FloatMaxTwo));
                stringBuilder.AppendLine("    Cooldown: " + tool.cooldownTime.ToStringByStyle(ToStringStyle.FloatMaxTwo) + " seconds");
                stringBuilder.AppendLine(string.Format("    Damage variation: {0} - {1}",
                    minDPS.ToStringByStyle(ToStringStyle.FloatMaxTwo),
                    maxDPS.ToStringByStyle(ToStringStyle.FloatMaxTwo)));
                stringBuilder.AppendLine("    Average damage: " + ((minDPS + maxDPS) / 2f).ToStringByStyle(ToStringStyle.FloatMaxTwo));
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

    }
}