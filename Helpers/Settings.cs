using BepInEx.Configuration;
using System.Collections.Generic;

namespace DrakiaXYZ.QuickMoveToContainer.Helpers
{
    internal class Settings
    {
        public const string GeneralSectionTitle = "1. General";

        public static ConfigFile Config;

        public static ConfigEntry<bool> AllOpenContainers;

        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public static void Init(ConfigFile Config)
        {
            Settings.Config = Config;

            ConfigEntries.Add(AllOpenContainers = Config.Bind(
                GeneralSectionTitle,
                "Target All Open Containers",
                true,
                new ConfigDescription(
                    "Whether to cascade through all open containers, in order, looking for a target",
                    null,
                    new ConfigurationManagerAttributes { })));

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
            int settingOrder = ConfigEntries.Count;
            foreach (var entry in ConfigEntries)
            {
                ConfigurationManagerAttributes attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
    }
}
