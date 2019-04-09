using Harmony12;
using RoR2;

namespace RoR2SkillSwapper.Patches
{
    [HarmonyPatch(typeof(DisableIfGameModded), "OnEnable")]
    static class DisableIfGameModdedPatch
    {
        static void Prefix(DisableIfGameModded __instance)
        {
            __instance.gameObject.SetActive(false);
        }
    }
}
