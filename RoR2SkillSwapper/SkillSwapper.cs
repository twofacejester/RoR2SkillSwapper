using Harmony12;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using static UnityModManagerNet.UnityModManager;

namespace RoR2SkillSwapper
{
    public class SkillSwapper
    {
        public static List<GenericSkill> Skills;
        private static ModEntry.ModLogger _logger;

        public static void Load(ModEntry modEntry)
        {
            _logger = modEntry.Logger;
            var harmony = HarmonyInstance.Create("com.twoface.swapper");

            harmony.PatchAll();

            RoR2Application.isModded = true;

            LoadSkills();
        }

        public static void Log(string s)
        {
            _logger.Log(s);
        }

        public static GameObject GetPrefab(SurvivorIndex index)
        {
            var prefabs = Traverse.Create(typeof(BodyCatalog)).Field("bodyPrefabs").GetValue<GameObject[]>();
            var intIndex = BodyCatalog.FindBodyIndex(Utils.SurvivorIndexToBodyString(index));
            return prefabs[intIndex];
        }

        public static void ReplaceSkill(GameObject prefab, GenericSkill skill, SkillSlot slot)
        {
            var charBody = prefab.GetComponent<CharacterBody>();
            var locator = prefab.GetComponent<SkillLocator>();

            var replaced = locator.GetSkill(slot);
            var addedSkill = prefab.AddComponent(skill);

            switch(slot)
            {
                case SkillSlot.Primary: locator.primary = addedSkill; break;
                case SkillSlot.Secondary: locator.secondary = addedSkill; break;
                case SkillSlot.Utility: locator.utility = addedSkill; break;
                case SkillSlot.Special: locator.special = addedSkill; break;
            }

            addedSkill.stateMachine = replaced.stateMachine;
        }

        private static void LoadSkills()
        {
            Skills = new List<GenericSkill>();

            var survivors = new SurvivorIndex[]
            {
                SurvivorIndex.Commando,
                SurvivorIndex.Engineer,
                SurvivorIndex.Huntress,
                SurvivorIndex.Mage,
                SurvivorIndex.Merc,
                SurvivorIndex.Toolbot
            };

            foreach (var index in survivors)
            {
                var body = SurvivorCatalog.GetSurvivorDef(index);
                var skills = body?.bodyPrefab.GetComponents<GenericSkill>();
                skills.Do(s => Log(s.skillName));
                Skills.AddRange(skills);
            }

            var bandit = BodyCatalog.FindBodyPrefab("BanditBody")?.GetComponents<GenericSkill>();
            bandit.Do(s => Log(s.skillName));
            Skills.AddRange(bandit);
        }
    }
}
