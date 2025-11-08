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
        public GameObject? GameObject;
        public Vector3? Position; // For pending spawns that don't have GameObject yet
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
                      $"Beachcombing Items ({trackedItems.Count}) - F3 to hide", headerStyle);
            
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
                    else if (item.Position.HasValue)
                    {
                        // For pending spawns, use stored position
                        item.Distance = Vector3.Distance(playerPos, item.Position.Value);
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
                // Method 1: Check m_OldBigItems (legacy items)
                var oldBigItems = spawner.m_OldBigItems;
                
                if (oldBigItems != null && oldBigItems.Count > 0)
                {
                    for (int i = 0; i < oldBigItems.Count; i++)
                    {
                        var customSpawnedItem = oldBigItems[i];
                        if (customSpawnedItem == null) continue;
                        
                        AddBigItemToTracked(customSpawnedItem, "(old)");
                    }
                }
                
                // Method 2: Check each big item location's Child property
                var bigItemLocations = spawner.m_BigItemLocations;
                
                if (bigItemLocations != null && bigItemLocations.Count > 0)
                {
                    for (int i = 0; i < bigItemLocations.Count; i++)
                    {
                        var location = bigItemLocations[i];
                        if (location == null) continue;
                        
                        // Check if this location has a spawned child
                        var child = location.Child;
                        if (child == null) continue;
                        
                        AddBigItemToTracked(child, null);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in ScanSpawnedBigItems: {ex}");
            }
        }
        
        private void AddBigItemToTracked(CustomSpawnedItem customSpawnedItem, string suffix)
        {
            try
            {
                // Get the GameObject from the CustomSpawnedItem component
                GameObject item = customSpawnedItem.gameObject;
                if (item == null) return;
                
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
                    
                    if (!string.IsNullOrEmpty(suffix))
                        itemName += " " + suffix;
                    
                    trackedItems.Add(new BeachcombingItem
                    {
                        GameObject = item,
                        DisplayName = itemName,
                        Distance = 0f,
                        IsContainer = false
                    });
                    return;
                }
                
                // Check for Container component
                Container container = item.GetComponent<Container>();
                if (container != null)
                {
                    string containerName = $"Container ({item.name})";
                    if (!string.IsNullOrEmpty(suffix))
                        containerName += " " + suffix;
                    
                    trackedItems.Add(new BeachcombingItem
                    {
                        GameObject = item,
                        DisplayName = containerName,
                        Distance = 0f,
                        IsContainer = true
                    });
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in AddBigItemToTracked: {ex}");
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
                    
                    // Force spawn items without visibility checks
                    try
                    {
                        radialSpawner.SpawnAttemptAllNoVisChecks();
                        MelonLogger.Msg($"Forced spawn attempt on radial spawner #{i}");
                    }
                    catch (System.Exception ex)
                    {
                        MelonLogger.Warning($"Could not force spawn on radial spawner #{i}: {ex.Message}");
                    }
                    
                    // Check spawned objects from this radial spawner
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
                // Scan already spawned objects
                var spawns = radialSpawner.m_Spawns;
                
                if (spawns != null && spawns.Count > 0)
                {
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
                
                // Scan pending spawns (items that will spawn when player gets close)
                var pendingSpawns = radialSpawner.m_PendingSpawns;
                
                if (pendingSpawns != null && pendingSpawns.Count > 0)
                {
                    for (int i = 0; i < pendingSpawns.Count; i++)
                    {
                        var pendingSpawn = pendingSpawns[i];
                        if (pendingSpawn == null) continue;
                        
                        // Use prefab name as display name (pending items don't have GameObject yet)
                        string prefabName = pendingSpawn.m_PrefabName;
                        if (string.IsNullOrEmpty(prefabName)) continue;
                        
                        // Create a placeholder item for pending spawn
                        // We'll use position for distance calculation
                        trackedItems.Add(new BeachcombingItem
                        {
                            GameObject = null, // No GameObject yet, item not spawned
                            Position = pendingSpawn.m_Position, // Store position for distance calc
                            DisplayName = $"{prefabName} (not spawned)",
                            Distance = 0f,
                            IsContainer = false
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