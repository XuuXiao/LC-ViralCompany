
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace ViralCompany.Configs {
    public class ViralCompanyConfig {
        public ConfigEntry<int> ConfigCameraCost { get; private set; }
        public ConfigEntry<bool> ConfigCameraEnabled { get; private set; }
        public ConfigEntry<string> ConfigCameraRarity { get; private set; }
        public ConfigEntry<bool> ConfigCameraScrapEnabled { get; private set; }   
        public ViralCompanyConfig(ConfigFile configFile) {
            ConfigCameraScrapEnabled = configFile.Bind("Scrap Options",
                                       "Camera Scrap | Enabled",
                                                false,
                                                "Enables/Disables the spawning of the scrap (sets rarity to 0 if false on all moons)");
            ConfigCameraRarity = configFile.Bind("Scrap Options",   
                                                "Camera Scrap | Rarity",  
                                                "Modded@1,ExperimentationLevel@1,AssuranceLevel@1,VowLevel@1,OffenseLevel@1,MarchLevel@1,RendLevel@1,DineLevel@1,TitanLevel@1", 
                                                "Rarity of Camera scrap appearing on every moon");
            ConfigCameraEnabled = configFile.Bind("Shop Options",   
                                                "Camera Item | Enabled",  
                                                true, 
                                                "Enables/Disables the Camera showing up in shop");
            ConfigCameraCost = configFile.Bind("Shop Options",   
                                                "Camera Item | Cost",
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