using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CreditLimitIncrease.Utils;
using HarmonyLib;
using SaveFileFramework.Utils;
using UnityEngine;

namespace CreditLimitIncrease
{
    // TODO Review this file and update to your own requirements.

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class CreditLimitIncreasePlugin : BaseUnityPlugin
    {
        // Mod specific details. MyGUID should be unique, and follow the reverse domain pattern
        // e.g.
        // com.mynameororg.pluginname
        // Version should be a valid version string.
        // e.g.
        // 1.0.0
        private const string MyGUID = "com.nanopoison.CreditLimitIncrease";
        private const string PluginName = "CreditLimitIncrease";
        private const string VersionString = "0.3.5";

        // Config entry key strings
        // These will appear in the config file created by BepInEx and can also be used
        // by the OnSettingsChange event to determine which setting has changed.
        public static string MantissaLengthKey = "Digits Before Exponent";
        public static string DebugCreditsIncreaseKey = "Increase Credits Shortcut";
        public static string DebugCreditsMultiplyKey = "Multiply Credits Shortcut";

        // Configuration entries. Static, so can be accessed directly elsewhere in code via
        // e.g.
        // float myFloat = CreditLimitIncreasePlugin.FloatExample.Value;
        // TODO Change this code or remove the code if not required.
        public static ConfigEntry<int> MantissaLength;
        public static ConfigEntry<KeyboardShortcut> DebugCreditsIncrease;
        public static ConfigEntry<KeyboardShortcut> DebugCreditsMultiply;

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        // config values
        public static int CreditsMantissaLength = 4;

        // strings for save/load
        private string mantissaString;
        private string expString;

        /// <summary>
        /// Initialise the configuration settings and patch methods
        /// </summary>
        private void Awake()
        {
            new BigCreditsManager(0);
            // Keyboard shortcut setting example
            // TODO Change this code or remove the code if not required.
            MantissaLength = Config.Bind("General",
                MantissaLengthKey,
                4,
                new ConfigDescription("Length of Mantissa",
                    new AcceptableValueRange<int>(1, 10)));

            DebugCreditsIncrease = Config.Bind("General",
                DebugCreditsIncreaseKey,
                new KeyboardShortcut());

            DebugCreditsMultiply = Config.Bind("General",
                DebugCreditsMultiplyKey,
                new KeyboardShortcut());

            // Add listeners methods to run if and when settings are changed by the player.
            // TODO Change this code or remove the code if not required.
            MantissaLength.SettingChanged += ConfigSettingChanged;
            DebugCreditsIncrease.SettingChanged += ConfigSettingChanged;
            DebugCreditsMultiply.SettingChanged += ConfigSettingChanged;

            // Apply all of our patches
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            // Sets up our static Log, so it can be used elsewhere in code.
            // .e.g.
            // CreditLimitIncreasePlugin.Log.LogDebug("Debug Message to BepInEx log file");
            Log = Logger;

            ModRegistry.RegisterMod(Info);

            mantissaString = ModRegistry.GetVariableString(this, "BigCreditsMantissa");
            expString = ModRegistry.GetVariableString(this, "BigCreditsExponent");
        }

        public void Save(ES3File file)
        {
            file.Save<double>(mantissaString, BigCreditsManager.Instance.credits.Mantissa);
            file.Save<long>(expString, BigCreditsManager.Instance.credits.Exponent);
        }

        public void Load(ES3File file)
        { 
            if (file.KeyExists(mantissaString) && file.KeyExists(expString))
            {
                double mant = file.Load<double>(mantissaString, 0);
                long expo = file.Load<long>(expString, 0);

                BigCreditsManager.Instance.credits = new BreakInfinity.BigDouble(mant, expo);
            } 
            else
            {
                BigCreditsManager.Instance.credits = file.Load<int>("PlayerGarden.instance.credits", 0);
                Log.LogInfo("First time using BigCreditsManager");
            }
        }

        /// <summary>
        /// Code executed every frame. See below for an example use case
        /// to detect keypress via custom configuration.
        /// </summary>
        // TODO - Add your code here or remove this section if not required.
        private void Update()
        {
            if (CreditLimitIncreasePlugin.DebugCreditsIncrease.Value.IsDown())
            {
                // Code here to do something on keypress
                if (BigCreditsManager.Instance == null)
                    return;
                BigCreditsManager.Instance.credits += int.MaxValue;
                CreditLimitIncreasePlugin.Log.LogInfo("Added credits!");
                PlayerGarden.instance.UpdateCreditsText();
            }
            if (CreditLimitIncreasePlugin.DebugCreditsMultiply.Value.IsDown())
            {
                // Code here to do something on keypress
                if (BigCreditsManager.Instance == null)
                    return;
                BigCreditsManager.Instance.credits *= BigCreditsManager.Instance.credits;
                CreditLimitIncreasePlugin.Log.LogInfo("Multiplied credits!");
                PlayerGarden.instance.UpdateCreditsText();
            }
        }

        /// <summary>
        /// Method to handle changes to configuration made by the player
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigSettingChanged(object sender, System.EventArgs e)
        {
            SettingChangedEventArgs settingChangedEventArgs = e as SettingChangedEventArgs;

            // Check if null and return
            if (settingChangedEventArgs == null)
            {
                return;
            }

            if (settingChangedEventArgs.ChangedSetting.Definition.Key == MantissaLengthKey)
            {
                CreditsMantissaLength = (int) settingChangedEventArgs.ChangedSetting.BoxedValue;
            }

            // Example Keyboard Shortcut setting changed handler
            if (settingChangedEventArgs.ChangedSetting.Definition.Key == DebugCreditsIncreaseKey)
            {
                KeyboardShortcut newValue = (KeyboardShortcut)settingChangedEventArgs.ChangedSetting.BoxedValue;

                // TODO - Add your code here or remove this section if not required.
                // Code here to do something with the new value
            }

            if (settingChangedEventArgs.ChangedSetting.Definition.Key == DebugCreditsMultiplyKey)
            {
                KeyboardShortcut newValue = (KeyboardShortcut)settingChangedEventArgs.ChangedSetting.BoxedValue;

                // TODO - Add your code here or remove this section if not required.
                // Code here to do something with the new value
            }
        }
    }
}
