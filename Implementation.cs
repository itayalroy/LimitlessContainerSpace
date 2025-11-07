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
        private const float SEARCH_RADIUS = 5f; // Search within 5 meters of spawn point
        private const float DISTANCE_UPDATE_INTERVAL = 0.25f; // Update distances 4 times per second
        
        private List<BeachcombingItem> trackedItems = new List<BeachcombingItem>();
        private float distanceUpdateTimer = 0f;
        private bool showResults = false;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Beachcombing Detector mod loaded!");
            MelonLogger.Msg("- Press F9 to toggle beachcombing item overlay");
        }
        
        public override void OnUpdate()
        {
            // Toggle overlay with F9 key
            if (Input.GetKeyDown(KeyCode.F9))
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
                      $"Beachcombing Items (F9 to hide)", headerStyle);
            
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
                    
                    // Scan Big Item Locations
                    ScanBigItemLocations(spawner);
                    
                    // Scan Radial Spawners
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
        
        private void ScanBigItemLocations(BeachcombingSpawner spawner)
        {
            try
            {
                var bigItemLocations = spawner.m_BigItemLocations;
                
                if (bigItemLocations == null || bigItemLocations.Count == 0)
                {
                    return;
                }
                
                for (int i = 0; i < bigItemLocations.Count; i++)
                {
                    var location = bigItemLocations[i];
                    if (location == null) continue;
                    
                    // Get the position of this location
                    Vector3 locationPos = location.transform.position;
                    
                    // Search for nearby objects
                    SearchNearbyObjects(locationPos, $"Big Item Location #{i}");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in ScanBigItemLocations: {ex}");
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
                    
                    Vector3 spawnerPos = radialSpawner.transform.position;
                    
                    // Search for nearby objects
                    SearchNearbyObjects(spawnerPos, $"Radial Spawner #{i}");
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in ScanRadialSpawners: {ex}");
            }
        }
        
        private void SearchNearbyObjects(Vector3 position, string locationName)
        {
            try
            {
                // Find all colliders within radius
                Collider[] colliders = Physics.OverlapSphere(position, SEARCH_RADIUS);
                
                foreach (Collider collider in colliders)
                {
                    if (collider == null || collider.gameObject == null) continue;
                    
                    GameObject obj = collider.gameObject;
                    
                    // Check for GearItem component (items in the world)
                    GearItem gearItem = obj.GetComponent<GearItem>();
                    if (gearItem != null)
                    {
                        string itemName = gearItem.name;
                        try
                        {
                            // Try to get display name if available
                            string displayName = gearItem.DisplayName;
                            if (!string.IsNullOrEmpty(displayName))
                                itemName = displayName;
                        }
                        catch { }
                        
                        // Add to tracked items
                        trackedItems.Add(new BeachcombingItem
                        {
                            GameObject = obj,
                            DisplayName = itemName,
                            Distance = 0f, // Will be calculated in UpdateDistances
                            IsContainer = false
                        });
                        continue;
                    }
                    
                    // Check for Container component
                    Container container = obj.GetComponent<Container>();
                    if (container != null)
                    {
                        // Add to tracked items
                        trackedItems.Add(new BeachcombingItem
                        {
                            GameObject = obj,
                            DisplayName = $"Container ({obj.name})",
                            Distance = 0f, // Will be calculated in UpdateDistances
                            IsContainer = true
                        });
                        continue;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[BeachcombingDetector] Error in SearchNearbyObjects: {ex}");
            }
        }
    }
}