using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using System.Collections.Generic;

namespace EnhancedBloodTrail
{
    // Static storage for tracking injured animals
    public static class InjuredAnimalTracker
    {
        public static Dictionary<GameObject, GameObject> TrackedMarkers = new Dictionary<GameObject, GameObject>();
        public static List<GameObject> TextObjects = new List<GameObject>();
        
        // Update text rotations to face camera
        public static void UpdateTextRotations()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;
            
            // Clean up null references
            TextObjects.RemoveAll(obj => obj == null);
            
            foreach (GameObject textObj in TextObjects)
            {
                if (textObj != null && textObj.transform != null)
                {
                    textObj.transform.rotation = mainCam.transform.rotation;
                }
            }
        }
    }

    // Patch to detect when blood trail starts (animal is injured) and enable wallhack rendering
    [HarmonyPatch(typeof(BloodTrail), nameof(BloodTrail.Awake))]
    internal class BloodTrail_Awake
    {
        private static void Postfix(BloodTrail __instance)
        {
            try
            {
                MelonLogger.Msg($"[EnhancedBloodTrail] BloodTrail.Awake called on {__instance.gameObject.name}");
                
                // The BloodTrail is attached to the injured animal
                EnableWallhackForAnimal(__instance.gameObject);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[EnhancedBloodTrail] Error in BloodTrail.Awake: {ex}");
            }
        }

        private static void EnableWallhackForAnimal(GameObject animalObject)
        {
            try
            {
                MelonLogger.Msg($"[EnhancedBloodTrail] EnableWallhackForAnimal called for: {animalObject.name}");

                // Don't create multiple markers for the same animal
                if (InjuredAnimalTracker.TrackedMarkers.ContainsKey(animalObject))
                {
                    MelonLogger.Msg($"[EnhancedBloodTrail] Animal already has a marker");
                    return;
                }

                // Get animal's health
                NPCCondition npcCondition = animalObject.GetComponent<NPCCondition>();
                float healthPercent = 100f;
                if (npcCondition != null)
                {
                    healthPercent = (npcCondition.m_CurrentHP / npcCondition.m_MaxHP) * 100f;
                }

                // Create a sphere marker above the animal
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "InjuredAnimalMarker";
                marker.transform.SetParent(animalObject.transform);
                marker.transform.localPosition = new Vector3(0, 2f, 0); // 2 meters above animal
                marker.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); // Small sphere
                
                // Remove collider so it doesn't interfere with gameplay
                UnityEngine.Object.Destroy(marker.GetComponent<Collider>());
                
                // Setup sphere material with wallhack shader
                Renderer markerRenderer = marker.GetComponent<Renderer>();
                if (markerRenderer != null)
                {
                    Material mat = markerRenderer.material;
                    
                    Shader textShader = Shader.Find("GUI/Text Shader");
                    if (textShader != null)
                    {
                        mat.shader = textShader;
                    }
                    
                    if (mat.HasProperty("_Cull"))
                        mat.SetInt("_Cull", 0);
                    
                    if (mat.HasProperty("_ZTest"))
                        mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                    
                    if (mat.HasProperty("_ZWrite"))
                        mat.SetInt("_ZWrite", 0);
                    
                    mat.color = new Color(1f, 0f, 0f, 1f);
                    mat.renderQueue = 5000;
                }
                
                // Create text showing health percentage
                GameObject textObj = new GameObject("HealthText");
                textObj.transform.SetParent(marker.transform);
                textObj.transform.localPosition = new Vector3(0, 0.5f, 0); // Above the sphere
                textObj.transform.localScale = Vector3.one * 0.1f;
                
                // Add TextMesh component
                TextMesh textMesh = textObj.AddComponent<TextMesh>();
                textMesh.text = $"{healthPercent:F0}%";
                textMesh.fontSize = 50;
                textMesh.color = Color.white;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                
                // Add to list for rotation updates
                InjuredAnimalTracker.TextObjects.Add(textObj);
                
                // Set text material to render through walls
                Renderer textRenderer = textObj.GetComponent<Renderer>();
                if (textRenderer != null && textRenderer.material != null)
                {
                    Material textMat = textRenderer.material;
                    
                    if (textMat.HasProperty("_ZTest"))
                        textMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                    
                    if (textMat.HasProperty("_ZWrite"))
                        textMat.SetInt("_ZWrite", 0);
                    
                    textMat.renderQueue = 5000;
                }
                
                // Track the marker
                InjuredAnimalTracker.TrackedMarkers[animalObject] = marker;
                
                MelonLogger.Msg($"[EnhancedBloodTrail] Marker with health text ({healthPercent:F0}%) created for {animalObject.name}");
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"[EnhancedBloodTrail] Error in EnableWallhackForAnimal: {e}");
            }
        }
    }

    internal sealed class Implementation : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Enhanced Blood Trail mod loaded!");
            MelonLogger.Msg("- Injured animals marked with red sphere and health percentage");
            MelonLogger.Msg("- Markers visible through walls (wallhack)");
        }
        
        public override void OnUpdate()
        {
            // Update text rotations every frame to face camera
            InjuredAnimalTracker.UpdateTextRotations();
        }
    }
}