using RoR2;
using System.Reflection;
using UnityEngine;

namespace RoR2SkillSwapper
{
    static class Utils
    {
        public static T GetCopyOf<T>(this MonoBehaviour comp, T other) where T : MonoBehaviour
        {
            var type = comp.GetType();

            if (type != other.GetType())
                return null;

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var pinfos = type.GetProperties(flags);

            while (type != typeof(MonoBehaviour))
            {
                foreach (var pinfo in pinfos)
                {
                    if (!pinfo.CanWrite)
                        continue;

                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }

                var finfos = type.GetFields(flags);
                foreach (var finfo in finfos)
                {
                    finfo.SetValue(comp, finfo.GetValue(other));

                }

                type = type.BaseType;
            }

            return comp as T;
        }

        public static T AddComponent<T>(this GameObject ob, T toAdd) where T : MonoBehaviour
            => ob.AddComponent<T>().GetCopyOf(toAdd) as T;

        public static string SurvivorIndexToBodyString(SurvivorIndex index)
        {
            switch(index)
            {
                case SurvivorIndex.Commando:
                    return "CommandoBody";
                case SurvivorIndex.Engineer:
                    return "EngiBody";
                case SurvivorIndex.Huntress:
                    return "HuntressBody";
                case SurvivorIndex.Mage:
                    return "MageBody";
                case SurvivorIndex.Merc:
                    return "MercBody";
                case SurvivorIndex.Toolbot:
                    return "ToolbotBody";
                default:
                    return "";
            }
        }
    }
}
