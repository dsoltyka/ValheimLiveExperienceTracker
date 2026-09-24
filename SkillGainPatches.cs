using System;
using HarmonyLib;
using UnityEngine;

namespace LiveExperienceTracker
{
    /// <summary>One XP gain event for the local player.</summary>
    internal readonly struct SkillGain
    {
        public readonly Skills.SkillType Type;
        public readonly string Name;
        public readonly float Level;
        public readonly float Accumulator;
        public readonly float Required;
        public readonly float Gained;
        public readonly bool LeveledUp;

        public SkillGain(Skills.SkillType type, string name, float level, float accumulator, float required, float gained, bool leveledUp)
        {
            Type = type;
            Name = name;
            Level = level;
            Accumulator = accumulator;
            Required = required;
            Gained = gained;
            LeveledUp = leveledUp;
        }
    }

    /// <summary>
    /// Observes skill XP. The game funnels every gain through <c>Skills.RaiseSkill</c>, which calls
    /// <c>Skills.Skill.Raise</c> on the skill's data object. We remember which <c>Skills</c> instance is
    /// currently raising so the <c>Raise</c> postfix can tell whether it belongs to the local player.
    /// </summary>
    [HarmonyPatch]
    internal static class SkillGainPatches
    {
        /// <summary>Fired on the main thread after the game has applied an XP gain to the local player.</summary>
        public static event Action<SkillGain> SkillGained;

        private static readonly AccessTools.FieldRef<Skills, Player> PlayerRef =
            AccessTools.FieldRefAccess<Skills, Player>("m_player");

        private static readonly Func<Skills.Skill, float> NextLevelRequirement = CreateNextLevelRequirement();

        private static Skills s_raising;

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        [HarmonyPrefix]
        private static void RaiseSkill_Prefix(Skills __instance)
        {
            s_raising = __instance;
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        [HarmonyFinalizer]
        private static void RaiseSkill_Finalizer()
        {
            s_raising = null;
        }

        [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
        [HarmonyPrefix]
        private static void Raise_Prefix(Skills.Skill __instance, out float __state)
        {
            __state = __instance.m_level;
        }

        [HarmonyPatch(typeof(Skills.Skill), nameof(Skills.Skill.Raise))]
        [HarmonyPostfix]
        private static void Raise_Postfix(Skills.Skill __instance, float factor, bool __result, float __state)
        {
            if (s_raising == null || __instance.m_info == null)
            {
                return;
            }

            // Raise() is a no-op at the cap; there is nothing to show.
            if (__state >= 100f)
            {
                return;
            }

            Player owner = PlayerRef(s_raising);
            if (owner == null || owner != Player.m_localPlayer)
            {
                return;
            }

            float gained = __instance.m_info.m_increseStep * factor * Game.m_skillGainRate;
            if (gained <= 0f)
            {
                return;
            }

            Skills.SkillType type = __instance.m_info.m_skill;
            var gain = new SkillGain(
                type,
                ResolveName(type),
                __instance.m_level,
                __instance.m_accumulator,
                NextLevelRequirement(__instance),
                gained,
                __result);

            try
            {
                SkillGained?.Invoke(gain);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"SkillGained handler threw: {e}");
            }
        }

        /// <summary>Vanilla skills use $skill_name tokens; Jotunn registers $skill_&lt;id&gt; for custom skills. Both resolve here.</summary>
        private static string ResolveName(Skills.SkillType type)
        {
            string fallback = type.ToString();
            var localization = Localization.instance;
            if (localization == null)
            {
                return fallback;
            }

            string name = localization.Localize("$skill_" + fallback.ToLowerInvariant());
            // Unknown tokens come back as "[skill_xyz]".
            return string.IsNullOrEmpty(name) || name.StartsWith("[") ? fallback : name;
        }

        /// <summary>Use the game's own (private) formula when we can find it, otherwise the known one.</summary>
        private static Func<Skills.Skill, float> CreateNextLevelRequirement()
        {
            try
            {
                var method = AccessTools.Method(typeof(Skills.Skill), "GetNextLevelRequirement");
                if (method != null)
                {
                    return AccessTools.MethodDelegate<Func<Skills.Skill, float>>(method);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"Falling back to built-in level requirement formula: {e.Message}");
            }

            return skill => Mathf.Pow(Mathf.Floor(skill.m_level + 1f), 1.5f) * 0.5f + 0.5f;
        }
    }
}
