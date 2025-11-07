using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Gameplay;
using UnityEngine;
using System.Collections.Generic;

namespace BeachcombingDetector
{
    internal sealed class Implementation : MelonMod
    {
        private const float SEARCH_RADIUS = 5f; // Search within 5 meters of spawn point
        private const float DISPLAY_DURATION = 10f; // Show results for 10 seconds
        
        private List<string> scanResults = new List<string>();
        private float displayTimer = 0f;
        private bool showResults = false;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Beachcombing Detector mod loaded!");
            MelonLogger.Msg("- Press F9 to scan for beachcombing items");
        }
        
        public override void OnUpdate()
        {
            // Manual scan with F9 key
            if (Input.GetKeyDown(KeyCode.F9))
            {
                MelonLogger.Msg("=== Scanning for Beachcombing Items ===");
                ScanBeachcombingLocations();
            }
            
            // Update display timer
            if (showResults)
            {
                displayTimer -= Time.deltaTime;
                if (displayTimer <= 0f)
                {
                    showResults = false;
                }
            }
        }
        
        public override void OnGUI()
        {
            if (!showResults || scanResults.Count == 0) return;
            
            // Create a semi-transparent background box
            float boxWidth = 500f;
            float boxHeight = 30f + (scanResults.Count * 25f); // Header + items
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
                      $"Beachcombing Items ({(int)displayTimer}s remaining)", headerStyle);
            
            // Items
            float yPos = boxY + 30f;
            foreach (string result in scanResults)
            {
                GUI.Label(new Rect(boxX + 10f, yPos, boxWidth - 20f, 25f), result, itemStyle);
                yPos += 25f;
            }
            
            GUI.color = Color.white;
        }
        
        private void ScanBeachcombingLocations()
        {
            try
            {
                // Clear previous results
                scanResults.Clear();
                
                // Find all BeachcombingSpawner instances in the scene
                BeachcombingSpawner[] spawners = GameObject.FindObjectsOfType<BeachcombingSpawner>();
                
                if (spawners == null || spawners.Length == 0)
                {
                    scanResults.Add("No beachcombing spawner found");
                    showResults = true;
                    displayTimer = DISPLAY_DURATION;
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
                
                if (scanResults.Count == 0)
                {
                    scanResults.Add("No items found on beach");
                }
                
                // Show results for 10 seconds
                showResults = true;
                displayTimer = DISPLAY_DURATION;
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
                
                List<string> foundItems = new List<string>();
                
                foreach (Collider collider in colliders)
                {
                    if (collider == null || collider.gameObject == null) continue;
                    
                    GameObject obj = collider.gameObject;
                    float distance = Vector3.Distance(position, obj.transform.position);
                    
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
                        
                        foundItems.Add($"• {itemName}");
                        continue;
                    }
                    
                    // Check for Container component
                    Container container = obj.GetComponent<Container>();
                    if (container != null)
                    {
                        foundItems.Add($"• Container ({obj.name})");
                        continue;
                    }
                }
                
                // Add found items to results
                if (foundItems.Count > 0)
                {
                    foreach (string item in foundItems)
                    {
                        scanResults.Add(item);
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