using System;
using BepInEx.Configuration;
using UnityEngine;

namespace LiveExperienceTracker
{
    /// <summary>All user-facing configuration. Every value can be changed at runtime (ConfigurationManager friendly).</summary>
    internal sealed class Settings
    {
        public ConfigEntry<float> DisplaySeconds { get; }
        public ConfigEntry<float> FadeSeconds { get; }
        public ConfigEntry<int> DecimalPlaces { get; }

        public ConfigEntry<int> FontSize { get; }
        public ConfigEntry<float> BarWidth { get; }
        public ConfigEntry<float> BarHeight { get; }
        public ConfigEntry<float> ElementSpacing { get; }
        public ConfigEntry<float> RowSpacing { get; }
        public ConfigEntry<float> OffsetX { get; }
        public ConfigEntry<float> OffsetY { get; }

        public ConfigEntry<Color> SkillNameColor { get; }
        public ConfigEntry<Color> ValueColor { get; }
        public ConfigEntry<Color> GainColor { get; }
        public ConfigEntry<Color> BarFillColor { get; }
        public ConfigEntry<Color> BarBackgroundColor { get; }

        /// <summary>Raised whenever any setting changes so the HUD can rebuild itself.</summary>
        public event Action Changed;

        public Settings(ConfigFile config)
        {
            const string general = "1 - General";
            const string layout = "2 - Layout";
            const string colors = "3 - Colors";

            DisplaySeconds = config.Bind(general, "DisplaySeconds", 5f,
                new ConfigDescription("How long a skill row stays visible after its last XP gain, in seconds.",
                    new AcceptableValueRange<float>(0.5f, 60f)));
            FadeSeconds = config.Bind(general, "FadeSeconds", 0.5f,
                new ConfigDescription("How long a row spends fading out at the end of its display time. 0 disables the fade.",
                    new AcceptableValueRange<float>(0f, 5f)));
            DecimalPlaces = config.Bind(general, "DecimalPlaces", 2,
                new ConfigDescription("Decimal places used for XP numbers.",
                    new AcceptableValueRange<int>(0, 4)));

            FontSize = config.Bind(layout, "FontSize", 16,
                new ConfigDescription("Font size of the row text.", new AcceptableValueRange<int>(8, 40)));
            BarWidth = config.Bind(layout, "BarWidth", 120f,
                new ConfigDescription("Width of the progress bar in pixels.", new AcceptableValueRange<float>(20f, 600f)));
            BarHeight = config.Bind(layout, "BarHeight", 10f,
                new ConfigDescription("Height of the progress bar in pixels.", new AcceptableValueRange<float>(2f, 40f)));
            ElementSpacing = config.Bind(layout, "ElementSpacing", 6f,
                new ConfigDescription("Horizontal gap between the name, bar and numbers.", new AcceptableValueRange<float>(0f, 40f)));
            RowSpacing = config.Bind(layout, "RowSpacing", 2f,
                new ConfigDescription("Vertical gap between skill rows.", new AcceptableValueRange<float>(0f, 40f)));
            OffsetX = config.Bind(layout, "OffsetX", 0f,
                "Horizontal offset from the bottom-left corner of the hotbar, in pixels. Positive moves right.");
            OffsetY = config.Bind(layout, "OffsetY", 4f,
                "Vertical gap below the hotbar, in pixels. Positive moves down.");

            SkillNameColor = config.Bind(colors, "SkillName", new Color(1f, 0.85f, 0.2f), "Color of the skill name.");
            ValueColor = config.Bind(colors, "Values", Color.white, "Color of the (current / needed) XP text.");
            GainColor = config.Bind(colors, "Gain", new Color(0.4f, 1f, 0.4f), "Color of the +XP text.");
            BarFillColor = config.Bind(colors, "BarFill", new Color(1f, 0.75f, 0.15f), "Fill color of the progress bar.");
            BarBackgroundColor = config.Bind(colors, "BarBackground", new Color(0f, 0f, 0f, 0.6f), "Background color of the progress bar.");

            config.SettingChanged += (_, __) => Changed?.Invoke();
        }
    }
}
