using BepInEx;
using Harmony;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RoR2SkillSwapper
{
    [BepInPlugin("twoface-skillswapper", "Skill Swapper", "0.3.0")]
    public class SkillSwapper : BaseUnityPlugin
    {
        public List<GenericSkill> Skills;

        public SkillSwapper() {}

        public void Awake() => On.RoR2.UI.ChatBox.SubmitChat += ChatHook;

        private void ChatHook(On.RoR2.UI.ChatBox.orig_SubmitChat orig, ChatBox chatbox)
        {
            var field = chatbox.inputField;
            var text = field.text;
            var args = text.Split(' ');

            if (text.StartsWith("/"))
            {
                text = text.Substring(1);

                if (text.StartsWith("swap"))
                {
                    if (args.Length > 3)
                    {
                        Replace(args[1], args[2], args[3]);
                    }
                    else
                        Chat.AddMessage("Usage: swap survivor-name 0|1|2|3 skill-name");
                    field.text = "";
                }
                else if (text.StartsWith("dump"))
                {
                    Dump();
                    field.text = "";
                }
            }

            orig.Invoke(chatbox);
        }

        private void Log(string s) => Logger.LogDebug(s);

        private GameObject GetPrefab(SurvivorIndex index)
        {
            var prefabs = Traverse.Create(typeof(BodyCatalog)).Field("bodyPrefabs").GetValue<GameObject[]>();
            var intIndex = BodyCatalog.FindBodyIndex(Utils.SurvivorIndexToBodyString(index));
            return prefabs[intIndex];
        }

        private void ReplaceSkill(GameObject prefab, GenericSkill skill, SkillSlot slot)
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

        private void Replace(string survivorName, string slotString, string skillName)
        {
            if (Skills == null)
                LoadSkills();

            if (!Enum.TryParse(survivorName, out SurvivorIndex survivor))
            {
                Chat.AddMessage("Invalid survivor name");
                return;
            }
            var prefab = GetPrefab(survivor);
            var skill = Skills.Where(s => s.skillName == skillName).FirstOrDefault();

            if (!int.TryParse(slotString, out var slotNum))
            {
                Chat.AddMessage("Invalid slot number");
                return;
            }
            if (slotNum > 3 || slotNum < 0)
            {
                Chat.AddMessage("Invalid slot number");
                return;
            }
            var slot = (SkillSlot)slotNum;

            if (skill == null)
            {
                Chat.AddMessage($"Unknown skill: {skillName}");
                return;
            }

            ReplaceSkill(prefab, skill, slot);
            Chat.AddMessage($"Swapped {Enum.GetName(typeof(SurvivorIndex), survivor)}'s skill for {skillName}");
        }

        private void LoadSkills()
        {
            Skills = new List<GenericSkill>();

            var bodyPrefabs = new string[]
            {
                "CommandoBody",
                "HuntressBody",
                "ToolbotBody",
                "EngiBody",
                "MageBody",
                "MercBody",
                "BanditBody",
                "GreaterWispBody",
                "TitanGoldBody",
                "VagrantBody",
                "WispBody",
            };

            foreach (var bodyName in bodyPrefabs)
            {
                var skills = BodyCatalog.FindBodyPrefab(bodyName)?.GetComponents<GenericSkill>();
                skills.Do(s => Log(s.skillName));
                Skills.AddRange(skills);
            }
        }

        private void Dump()
        {
            using (var writer = new StreamWriter("dump.txt"))
            {
                var bodies = BodyCatalog.allBodyPrefabs;

                foreach (var body in bodies)
                {
                    writer.WriteLine($"\"body\": {body.name}");
                    body.GetComponents<GenericSkill>().Do(s => 
                    {
                        writer.WriteLine($"\t\"{s.skillName}\"");
                    });
                }

                writer.Flush();
            }
        }
    }
}
