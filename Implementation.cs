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
            __instance.m_CapacityKG = 10000;
        }
    }

    internal sealed class Implementation : MelonMod
    {
        public override void OnInitializeMelon()
        {

        }
    }
}