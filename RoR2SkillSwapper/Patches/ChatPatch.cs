using Harmony12;
using RoR2;
using RoR2.UI;
using System;
using System.Linq;

namespace RoR2SkillSwapper.Patches
{
    [HarmonyPatch(typeof(ChatBox), "SubmitChat")]
    static class ChatPatch
    {
        static void Prefix(ChatBox __instance)
        {
            var field = __instance.inputField;
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
                }

                field.text = "";
            }
        }

        private static void Replace(string survivorName, string slotString, string skillName)
        {
            if (!Enum.TryParse(survivorName, out SurvivorIndex survivor))
            {
                Chat.AddMessage("Invalid survivor name");
                return;
            }
            var prefab = SkillSwapper.GetPrefab(survivor);
            var skill = SkillSwapper.Skills.Where(s => s.skillName == skillName).FirstOrDefault();

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

            SkillSwapper.ReplaceSkill(prefab, skill, slot);
        }
    }
}
