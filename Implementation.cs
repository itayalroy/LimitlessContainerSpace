using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Gameplay;
using UnityEngine;
using System.Collections.Generic;

namespace BeachcombingDetector
{
    // Class to store tracked beachcombing items
    internal class BeachcombingItem
    {
        public GameObject GameObject;
        public string DisplayName;
        public float Distance;
        public bool IsContainer;
    }
    
    internal sealed class Implementation : MelonMod
    {
        private const float DISTANCE_UPDATE_INTERVAL = 0.25f; // Update distances 4 times per second
        
        private List<BeachcombingItem> trackedItems = new List<BeachcombingItem>();
        private float distanceUpdateTimer = 0f;
        private bool showResults = false;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Beachcombing Detector mod loaded!");
            MelonLogger.Msg("- Press F3 to toggle beachcombing item overlay");
        }
        
        public override void OnUpdate()
        {
            // Toggle overlay with F3 key
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (showResults)
                {
                    // Hide overlay if already showing
                    showResults = false;
                    MelonLogger.Msg("=== Beachcombing overlay hidden ===");
                }
                else
                {
                    // Scan and show overlay
                    MelonLogger.Msg("=== Scanning for Beachcombing Items ===");
                    ScanBeachcombingLocations();
                }
            }
            
            // Update distances periodically when results are shown
            if (showResults && trackedItems.Count > 0)
            {
                distanceUpdateTimer += Time.deltaTime;
                if (distanceUpdateTimer >= DISTANCE_UPDATE_INTERVAL)
                {
                    distanceUpdateTimer = 0f;
                    UpdateDistances();
                }
            }
        }
        
        public override void OnGUI()
        {
            if (!showResults || trackedItems.Count == 0) return;
            
            // Create a semi-transparent background box
            float boxWidth = 500f;
            float boxHeight = 30f + (trackedItems.Count * 25f); // Header + items
            float boxX = 20f; // Left side of screen
            float boxY = 100f; // Top of screen
            
            // Draw background
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "");
            
            // Draw text
            GUI.color = Color.white;
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.yellow;
            
            GUIStyle itemStyle = new GUIStyle(GUI.skin.label);
            itemStyle.fontSize = 14;
            itemStyle.normal.textColor = Color.white;
            
            // Header
            GUI.Label(new Rect(boxX + 10f, boxY + 5f, boxWidth - 20f, 25f), 
                      $"Beachcombing Items (F3 to hide)", headerStyle);
            
            // Items with distances
            float yPos = boxY + 30f;
            foreach (BeachcombingItem item in trackedItems)
            {
                string displayText = $"• {item.DisplayName} - {item.Distance:F0}m";
                GUI.Label(new Rect(boxX + 10f, yPos, boxWidth - 20f, 25f), displayText, itemStyle);
                yPos += 25f;
            }
            
            GUI.color = Color.white;
        }
        
        private void UpdateDistances()
        {
            try
            {
                // Get player position
                GameObject playerObject = GameManager.GetPlayerObject();
                if (playerObject == null) return;
                
                Vector3 playerPos = playerObject.transform.position;
                
                // Update distance for each tracked item
                foreach (BeachcombingItem item in trackedItems)
                {
                    if (item.GameObject != null)
                    {
                        item.Distance = Vector3.Distance(playerPos, item.GameObject.transform.position);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in UpdateDistances: {ex}");
            }
        }
        
        private void ScanBeachcombingLocations()
        {
            try
            {
                // Clear previous results
                trackedItems.Clear();
                
                // Find all BeachcombingSpawner instances in the scene
                BeachcombingSpawner[] spawners = GameObject.FindObjectsOfType<BeachcombingSpawner>();
                
                if (spawners == null || spawners.Length == 0)
                {
                    showResults = false;
                    MelonLogger.Msg("No beachcombing spawner found");
                    return;
                }
                
                foreach (BeachcombingSpawner spawner in spawners)
                {
                    if (spawner == null) continue;
                    
                    // Check already-spawned big items
                    ScanSpawnedBigItems(spawner);
                    
                    // Scan Radial Spawners (for small items)
                    ScanRadialSpawners(spawner);
                }
                
                if (trackedItems.Count == 0)
                {
                    showResults = false;
                    MelonLogger.Msg("No items found on beach");
                }
                else
                {
                    // Show results and update distances immediately
                    showResults = true;
                    UpdateDistances();
                    MelonLogger.Msg($"Found {trackedItems.Count} beachcombing item(s)");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in ScanBeachcombingLocations: {ex}");
            }
        }
        
        private void ScanSpawnedBigItems(BeachcombingSpawner spawner)
        {
            try
            {
                var oldBigItems = spawner.m_OldBigItems;
                
                if (oldBigItems == null || oldBigItems.Count == 0)
                {
                    return;
                }
                
                for (int i = 0; i < oldBigItems.Count; i++)
                {
                    var customSpawnedItem = oldBigItems[i];
                    if (customSpawnedItem == null) continue;
                    
                    // Get the GameObject from the CustomSpawnedItem component
                    GameObject item = customSpawnedItem.gameObject;
                    if (item == null) continue;
                    
                    // Check for GearItem component
                    GearItem gearItem = item.GetComponent<GearItem>();
                    if (gearItem != null)
                    {
                        string itemName = gearItem.name;
                        try
                        {
                            string displayName = gearItem.DisplayName;
                            if (!string.IsNullOrEmpty(displayName))
                                itemName = displayName;
                        }
                        catch { }
                        
                        trackedItems.Add(new BeachcombingItem
                        {
                            GameObject = item,
                            DisplayName = itemName + " (spawned)",
                            Distance = 0f,
                            IsContainer = false
                        });
                        continue;
                    }
                    
                    // Check for Container component
                    Container container = item.GetComponent<Container>();
                    if (container != null)
                    {
                        trackedItems.Add(new BeachcombingItem
                        {
                            GameObject = item,
                            DisplayName = $"Container ({item.name}) (spawned)",
                            Distance = 0f,
                            IsContainer = true
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in ScanSpawnedBigItems: {ex}");
            }
        }
        
        private void ScanRadialSpawners(BeachcombingSpawner spawner)
        {
            try
            {
                var childSpawners = spawner.m_ChildSpawners;
                
                if (childSpawners == null || childSpawners.Count == 0)
                {
                    return;
                }
                
                for (int i = 0; i < childSpawners.Count; i++)
                {
                    var radialSpawner = childSpawners[i];
                    if (radialSpawner == null) continue;
                    
                    // Check already spawned objects from this radial spawner
                    ScanRadialSpawnerSpawns(radialSpawner);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in ScanRadialSpawners: {ex}");
            }
        }
        
        private void ScanRadialSpawnerSpawns(RadialObjectSpawner radialSpawner)
        {
            try
            {
                var spawns = radialSpawner.m_Spawns;
                
                if (spawns == null || spawns.Count == 0)
                {
                    return;
                }
                
                for (int i = 0; i < spawns.Count; i++)
                {
                    GameObject spawnedObj = spawns[i];
                    if (spawnedObj == null) continue;
                    
                    // Check for GearItem component
                    GearItem gearItem = spawnedObj.GetComponent<GearItem>();
                    if (gearItem != null)
                    {
                        string itemName = gearItem.name;
                        try
                        {
                            string displayName = gearItem.DisplayName;
                            if (!string.IsNullOrEmpty(displayName))
                                itemName = displayName;
                        }
                        catch { }
                        
                        trackedItems.Add(new BeachcombingItem
                        {
                            GameObject = spawnedObj,
                            DisplayName = itemName,
                            Distance = 0f,
                            IsContainer = false
                        });
                        continue;
                    }
                    
                    // Check for Container component
                    Container container = spawnedObj.GetComponent<Container>();
                    if (container != null)
                    {
                        trackedItems.Add(new BeachcombingItem
                        {
                            GameObject = spawnedObj,
                            DisplayName = $"Container ({spawnedObj.name})",
                            Distance = 0f,
                            IsContainer = true
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in ScanRadialSpawnerSpawns: {ex}");
            }
        }
    }
}