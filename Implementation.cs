using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using System.Collections.Generic;

namespace EnhancedBloodTrail
{
    // Static storage for our custom persistent blood decals
    internal static class PersistentBloodDecals
    {
        public static List<DecalProjectorInstance> CustomDecals = new List<DecalProjectorInstance>();
    }

    // Patch to reduce distance between blood drops for easier tracking
    [HarmonyPatch(typeof(BloodTrail), "Awake")]
    internal class BloodTrail_Awake
    {
        private static void Postfix(BloodTrail __instance)
        {
            // Make blood drops occur much more frequently by reducing the distance
            __instance.m_BloodDropDistanceMin = 0.3f;  // Original is probably around 2-3
            __instance.m_BloodDropDistanceMax = 0.5f;  // Original is probably around 4-5
            __instance.m_BloodDropDistanceSlowMin = 0.3f;  // For when bleeding slows
            __instance.m_BloodDropDistanceSlowMax = 0.5f;
            
            MelonLogger.Msg($"[EnhancedBloodTrail] Blood drop distances reduced for easier tracking");
        }
    }

    // NEW APPROACH: Create our own persistent blood decal whenever the game creates one
    [HarmonyPatch(typeof(DynamicDecalsManager), "CreateDecal")]
    internal class DynamicDecalsManager_CreateDecal
    {
        // Flag to prevent infinite recursion
        private static bool isCreatingCustomDecal = false;

        private static void Postfix(DynamicDecalsManager __instance, DecalProjectorInstance __result, Vector3 pos, DecalProjectorType projectorType)
        {
            try
            {
                // Prevent infinite recursion - don't create custom decals while we're already creating one
                if (isCreatingCustomDecal)
                    return;

                // Only create custom decals for blood
                if (__result == null || (projectorType != DecalProjectorType.AnimalBlood && projectorType != DecalProjectorType.AnimalBloodPersistent))
                    return;

                // Set flag to prevent re-entry
                isCreatingCustomDecal = true;

                // Create our own custom blood decal at this position
                DecalProjectorInstance customDecal = __instance.CreateDecal(
                    pos,                                // position
                    0f,                                 // rotation angle
                    Vector3.down,                       // normal (pointing down onto ground)
                    0,                                  // uvRectangleIndex
                    Vector3.one,                        // scale
                    DecalProjectorType.AnimalBlood,    // type
                    false                              // indoors
                );

                if (customDecal != null)
                {
                    // Configure our custom decal to be large, visible, and permanent
                    customDecal.m_LifeTimeHours = 999999f;
                    customDecal.m_FadeOverEntireLifetime = false;
                    customDecal.m_Alpha = 1.0f;
                    customDecal.m_Scale = customDecal.m_Scale * 3.0f; // Make it 3x bigger
                    customDecal.m_HoursAtCreateTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();

                    // Store in our custom list so the game can't remove it
                    PersistentBloodDecals.CustomDecals.Add(customDecal);
                    
                    if (PersistentBloodDecals.CustomDecals.Count % 10 == 0)
                    {
                        MelonLogger.Msg($"[EnhancedBloodTrail] Created {PersistentBloodDecals.CustomDecals.Count} custom persistent blood decals");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[EnhancedBloodTrail] Error creating custom blood decal: {ex}");
            }
            finally
            {
                // Always clear the flag
                isCreatingCustomDecal = false;
            }
        }
    }

    // Add our custom decals to the rendering list so they actually get drawn
    [HarmonyPatch(typeof(DynamicDecalsManager), "RenderDynamicScreenSpaceDecals")]
    internal class DynamicDecalsManager_RenderDynamicScreenSpaceDecals
    {
        private static void Prefix(Il2CppSystem.Collections.Generic.List<DecalProjectorInstance> instances, ref int renderedDecalCount)
        {
            try
            {
                int addedCount = 0;
                
                // Add all our custom persistent blood decals to the BEGINNING of the instances list
                foreach (var customDecal in PersistentBloodDecals.CustomDecals)
                {
                    if (customDecal != null && customDecal.m_Alpha > 0)
                    {
                        // Keep the decal properties locked
                        customDecal.m_Alpha = 1.0f;
                        customDecal.m_LifeTimeHours = 999999f;
                        
                        // Insert at the beginning of the list if not already there
                        if (!instances.Contains(customDecal))
                        {
                            instances.Insert(0, customDecal);
                            addedCount++;
                        }
                    }
                }
                
                // Increase the render count by the number of custom decals we added
                renderedDecalCount += addedCount;
                
                if (addedCount > 0)
                {
                    MelonLogger.Msg($"[EnhancedBloodTrail] Added {addedCount} custom blood decals to render queue (total: {PersistentBloodDecals.CustomDecals.Count})");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[EnhancedBloodTrail] Error adding custom decals to render list: {ex}");
            }
        }
    }

    internal sealed class Implementation : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Enhanced Blood Trail mod loaded!");
            MelonLogger.Msg("- Blood drops occur every 0.3-0.5 meters (very frequent)");
            MelonLogger.Msg("- Custom persistent blood decals created for each drop");
            MelonLogger.Msg("- Blood decals are 3x larger and never fade");
            MelonLogger.Msg("- Decals stored independently from game's cleanup system");
        }
    }
}