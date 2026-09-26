using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ValheimModShared;

namespace LiveExperienceTracker
{
    /// <summary>
    /// Entry point. Binds config, installs the skill-gain patches and spawns the HUD tracker.
    /// Client-side only: nothing here touches the network or needs to be on the server.
    /// </summary>
    [BepInPlugin(PluginGuid, BuildInfo.Name, BuildInfo.Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dsoltyka.LiveExperienceTracker";

        internal static ManualLogSource Log { get; private set; }
        internal static Settings Settings { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Settings = new Settings(Config);
            ConfigWatcher.Watch(Config, Log);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SkillGainPatches));

            gameObject.AddComponent<TrackerHud>();

            Log.LogInfo($"{BuildInfo.Name} {BuildInfo.Version} loaded");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
