using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTallyCore.Utils.Assets;
using TootTallyCore.Utils.TootTallyModules;
using TootTallySettings;
using UnityEngine;

namespace TootTallyAccounts
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTallyCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("TootTallySettings", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "TootTally.cfg";
        public const string DEFAULT_APIKEY = "SignUpOnTootTally.com";
        public static string GetAPIKey => Instance.option.APIKey.Value;
        internal Options option;
        private Harmony _harmony;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }

        //Change this name to whatever you want
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }

        public static TootTallySettingPage settingPage;

        public static void LogInfo(string msg) => Instance.Logger.LogInfo(msg);
        public static void LogError(string msg) => Instance.Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            _harmony = new Harmony(Info.Metadata.GUID);

            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTallyCore.Plugin.Instance.Config.Bind("Modules", "<insert module name here>", true, "<insert module description here>");
            TootTallyModuleManager.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                APIKey = Config.Bind("API Setup", "API Key", DEFAULT_APIKEY, "API Key for Score Submissions."),
                ShowLoginPanel = Config.Bind("API Setup", "Show Login Panel", true, "Show login panel when not logged in.")
            };
            TootTallySettings.Plugin.MainTootTallySettingPage.AddButton("OpenLoginPage", new Vector2(400, 60), "Open Login Page", TootTallyUser.OpenLoginPanel);
            TootTallySettings.Plugin.MainTootTallySettingPage.AddToggle("Show Login Panel", option.ShowLoginPanel);

            var assetsPath = Path.Combine(Path.GetDirectoryName(Instance.Info.Location), "Assets");
            AssetManager.LoadAssets(assetsPath);
            AssetBundleManager.LoadAssets(Path.Combine(assetsPath, "loginassets"));
            _harmony.PatchAll(typeof(TootTallyUser));
            _harmony.PatchAll(typeof(UserStatusUpdater));
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            _harmony.UnpatchSelf();
            settingPage.Remove();
            LogInfo($"Module unloaded!");
        }

        public class Options
        {
            public ConfigEntry<string> APIKey { get; set; }
            public ConfigEntry<bool> ShowLoginPanel { get; set; }
        }
    }
}