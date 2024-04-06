
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace ViralCompany.Configs {
    public class ViralCompanyConfig {
        public ConfigEntry<int> ConfigCameraCost { get; private set; }
        public ConfigEntry<bool> ConfigCameraShipUpgradeEnabled { get; private set; }        
        public ViralCompanyConfig(ConfigFile configFile) {
            ConfigCameraShipUpgradeEnabled = configFile.Bind("Scrap Options",
                                                "Camera Scrap | Enabled",
                                                true,
                                                "Enables/Disables the Camera from being a Ship Upgrade");
            ConfigCameraCost = configFile.Bind("Shop Options",
                                                "Camera ShipUpgrade | Cost",  
                                                60, 
                                                "Cost of Camera");
            ClearUnusedEntries(configFile);
            Plugin.Logger.LogInfo("Setting up config for Viral Company plugin...");
        }
        private void ClearUnusedEntries(ConfigFile configFile) {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = configFile.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(configFile, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            configFile.Save(); // Save the config file to save these changes
        }
    }
}