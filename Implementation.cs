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
        }
        
        private void ScanBeachcombingLocations()
        {
            try
            {
                // Find all BeachcombingSpawner instances in the scene
                BeachcombingSpawner[] spawners = GameObject.FindObjectsOfType<BeachcombingSpawner>();
                
                if (spawners == null || spawners.Length == 0)
                {
                    MelonLogger.Msg("[BeachcombingDetector] No BeachcombingSpawner found in scene");
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
                
                MelonLogger.Msg("[BeachcombingDetector] Scan complete!");
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
                
                bool foundItems = false;
                
                foreach (Collider collider in colliders)
                {
                    if (collider == null || collider.gameObject == null) continue;
                    
                    GameObject obj = collider.gameObject;
                    float distance = Vector3.Distance(position, obj.transform.position);
                    
                    // Check for GearItem component (items in the world)
                    GearItem gearItem = obj.GetComponent<GearItem>();
                    if (gearItem != null)
                    {
                        if (!foundItems)
                        {
                            MelonLogger.Msg($"[BeachcombingDetector] {locationName} at {position}:");
                            foundItems = true;
                        }
                        
                        string itemName = gearItem.name;
                        try
                        {
                            // Try to get display name if available
                            string displayName = gearItem.DisplayName;
                            if (!string.IsNullOrEmpty(displayName))
                                itemName = displayName;
                        }
                        catch { }
                        
                        MelonLogger.Msg($"  -> ITEM: {itemName} ({obj.name}) - {distance:F2}m away");
                        continue;
                    }
                    
                    // Check for Container component
                    Container container = obj.GetComponent<Container>();
                    if (container != null)
                    {
                        if (!foundItems)
                        {
                            MelonLogger.Msg($"[BeachcombingDetector] {locationName} at {position}:");
                            foundItems = true;
                        }
                        
                        MelonLogger.Msg($"  -> CONTAINER: {obj.name} - {distance:F2}m away");
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