using MelonLoader;
using HarmonyLib;
using Il2Cpp;

namespace LimitlessContainerSpace
{
    [HarmonyPatch(typeof(Container), "Awake")]
    internal class ContainerSpace
    {
        private static void Postfix(Container __instance)
        {
            var currentCapacity = __instance.m_Capacity;
            var prevCapacity = (long)currentCapacity.m_Units / 1000000000.0;
            currentCapacity.m_Units = 10000L * 1000000000L;
            __instance.m_Capacity = currentCapacity;
            MelonLogger.Msg($"[LimitlessContainerSpace] Container capacity modified: {prevCapacity:F1} kg → {10000.0:F1} kg");
        }
    }

    internal sealed class Implementation : MelonMod
    {
        public override void OnInitializeMelon()
        {

        }
    }
}