using RoR2;
using RoR2.Skills;

namespace RoR2SkillSwapper
{
    static class Utils
    {
        public static int StringToSlot(string s)
        {
            switch (s)
            {
                case "0":
                case "left":
                case "p":
                case "primary":
                    return 0;
                case "1":
                case "right":
                case "s":
                case "secondary":
                    return 1;
                case "2":
                case "shift":
                case "u":
                case "utility":
                    return 2;
                case "3":
                case "r":
                case "sp":
                case "special":
                    return 3;
                default:
                    return -1;
            }
        }

        public static void Override(object source, GenericSkill slot, SkillDef skillDef)
        {
            if (slot != null && skillDef != null)
                slot.SetSkillOverride(source, skillDef, GenericSkill.SkillOverridePriority.Replacement);
        }

        public static void Remove(object source, GenericSkill slot, SkillDef toRemove) =>
            slot.UnsetSkillOverride(source, toRemove, GenericSkill.SkillOverridePriority.Replacement);

        public static GenericSkill GetSlot(int i)
        {
            var locator = GetBody()?.skillLocator;

            if (locator == null)
            {
                Chat.AddMessage("Couldn't find the player's body");
                return null;
            }

            switch (i)
            {
                case 0:
                    return locator.primary;
                case 1:
                    return locator.secondary;
                case 2:
                    return locator.utility;
                case 3:
                    return locator.special;
                default:
                    return null;
            }
        }

        public static CharacterBody GetBody()
        {
            var localId = LocalUserManager.GetFirstLocalUser().currentNetworkUser.Network_id;

            foreach (var master in PlayerCharacterMasterController.instances)
            {
                if (master.networkUser.Network_id.Equals(localId))
                    return master.master.GetBody();
            }

            return null;
        }
    }
}
