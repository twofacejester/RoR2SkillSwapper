using BepInEx;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RoR2SkillSwapper
{
    [BepInPlugin("twoface.skillswapper", "Skill Swapper", "0.3.2")]
    public class SkillSwapper : BaseUnityPlugin
    {
        public static List<GenericSkill> Skills = new List<GenericSkill>();

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
                else if (text.StartsWith("reset"))
                {
                    Reset();
                    Chat.AddMessage("Reset survivors");
                    field.text = "";
                }
            }

            orig.Invoke(chatbox);
        }

        private void Log(string s) => Logger.LogInfo(s);
        private void Debug(string s) => Logger.LogDebug(s);

        // TODO replace this with a better method
        // Reloading the prefabs from the resource files causes issues with the character select (and fails to actually reset the skills)
        // This may be due to how it was coded at the time
        private void Reset()
        {
            var slots = new SkillSlot[]
            {
                SkillSlot.Primary,
                SkillSlot.Secondary,
                SkillSlot.Utility,
                SkillSlot.Special
            };

            var survivors = new (SurvivorIndex, string[])[] 
            {
                (SurvivorIndex.Commando, new string[] { "FirePistol", "FireFMJ", "Roll", "Barrage" }),
                (SurvivorIndex.Engineer, new string[] { "FireGrenade", "PlaceMine", "PlaceBubbleShield", "PlaceTurret" }),
                (SurvivorIndex.Huntress, new string[] { "FireSeekingArrow", "Glaive", "Blink", "ArrowRain" }),
                (SurvivorIndex.Mage, new string[] { "FireFirebolt", "NovaBomb", "Wall", "Flamethrower" }),
                (SurvivorIndex.Merc, new string[] { "GroundLight", "Whirlwind", "Dash", "Evis" }),
                (SurvivorIndex.Toolbot, new string[] { "FireNailgun", "StunDrone", "ToolbotDash", "Swap" })
            };

            foreach ((var surv, var skills) in survivors)
            {
                var prefab = GetPrefab(surv);
                
                for (var i = 0; i < slots.Length; i++)
                {
                    var skill = Skills.FirstOrDefault(s => s.skillName == skills[i]);
                    var slot = slots[i];

                    ReplaceSkill(prefab, skill, slot);
                }
            }
        }

        private GameObject GetPrefab(SurvivorIndex index)
        {
            var prefabs = Traverse.Create(typeof(BodyCatalog)).Field("bodyPrefabs").GetValue<GameObject[]>();
            var intIndex = BodyCatalog.FindBodyIndex(Utils.SurvivorIndexToBodyString(index));
            return prefabs[intIndex];
        }

        private void ReplaceSkill(GameObject prefab, GenericSkill skill, SkillSlot slot)
        {
            var locator = prefab.GetComponent<SkillLocator>();

            var replaced = locator.GetSkill(slot);
            var addedSkill = GetSkill(prefab, skill.skillName) ?? prefab.AddComponent(skill);

            switch (slot)
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
            if (Skills.Count == 0)
                LoadSkills();

            if (!Enum.TryParse(survivorName, out SurvivorIndex survivor))
            {
                Chat.AddMessage("Invalid survivor name");
                return;
            }

            var prefab = GetPrefab(survivor);

            var skill = Skills.FirstOrDefault(s => s.skillName == skillName);

            if (skill == null)
            {
                Chat.AddMessage($"Unknown skill: {skillName}");
                return;
            }

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

            ReplaceSkill(prefab, skill, slot);
            Chat.AddMessage($"Swapped {Enum.GetName(typeof(SurvivorIndex), survivor)}'s skill for {skillName}");
        }

        private GenericSkill GetSkill(GameObject body, string skillName) =>
            body.GetComponents<GenericSkill>().FirstOrDefault(s => s.skillName == skillName);

        private void LoadSkills()
        {
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
                Debug($"Loading {bodyName}");
                var skills = BodyCatalog.FindBodyPrefab(bodyName)?.GetComponents<GenericSkill>();
                skills.Do(s => Debug(s.skillName));
                Skills.AddRange(skills);
                Debug($"Finished loading {bodyName}");
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
