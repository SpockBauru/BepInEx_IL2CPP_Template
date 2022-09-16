using System;
using System.Collections;

// From BepInEx.core.dll in BepInEx/core folder
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

//from 0Harmony.dll in BepInEx/core folder
using HarmonyLib;

// From BepInEx.IL2CPP.dll in BepInEx/core folder
using BepInEx.IL2CPP;
using BepInEx.IL2CPP.Utils.Collections;

// From UnhollowerBaseLib.dll  BepInEx/core folder
using UnhollowerRuntimeLib;

// From UnityEngine.CoreModule.dll in BepInEx\unhollowed folder
using UnityEngine;
using UnityEngine.SceneManagement;

// Also make a reference in your library to Il2Cppmscorlib.dll, from BepInEx\unhollowed folder

namespace PluginCode
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class Plugin : BasePlugin
    {
        /// <summary>
        /// Human-readable name of the plugin. In general, it should be short and concise.
        /// This is the name that is shown to the users who run BepInEx and to modders that inspect BepInEx logs. 
        /// </summary>
        public const string PluginName = "IL2CPP Plugin";

        /// <summary>
        /// Unique ID of the plugin. Will be used as the default config file name.
        /// This must be a unique string that contains only characters a-z, 0-9 underscores (_) and dots (.)
        /// When creating Harmony patches or any persisting data, it's best to use this ID for easier identification.
        /// </summary>
        public const string GUID = "com.yourName.pluginName";

        /// <summary>
        /// Version of the plugin. Must be in form <major>.<minor>.<build>.<revision>.
        /// Major and minor versions are mandatory, but build and revision can be left unspecified.
        /// </summary>
        public const string Version = "1.0.0";

        internal static new ManualLogSource Log;

        private ConfigEntry<bool> _exampleConfigEntry;

        /// <summary>
        /// Host your MonoBehaviour components in an same the same GameObject
        /// that is shared between all your projects
        /// </summary>
        public GameObject YourName;

        public override void Load()
        {
            Log = base.Log;

            _exampleConfigEntry = Config.Bind("General",
                                              "Enable this plugin",
                                              true,
                                              "If false, this plugin will do nothing");

            if (_exampleConfigEntry.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }

            // IL2CPP don't automatically inherits MonoBehaviour, so needs to add a component separatelly
            ClassInjector.RegisterTypeInIl2Cpp<MonoBehaviourExamples>();

            // Add the monobehavior component to your personal GameObject. Try to not duplicate.
            YourName = GameObject.Find("YourName");
            if (YourName == null)
            {
                YourName = new GameObject("YourName");
                GameObject.DontDestroyOnLoad(YourName);
                YourName.hideFlags = HideFlags.HideAndDontSave;
                YourName.AddComponent<MonoBehaviourExamples>();
            }
            else YourName.AddComponent<MonoBehaviourExamples>();
        }

        private static class Hooks
        {
            // [HarmonyPrefix]
            // [HarmonyPatch(typeof(SomeClass), nameof(SomeClass.SomeInstanceMethod))]
            // private static void SomeMethodPrefix(SomeClass __instance, int someParameter, ref int __result)
            // {
            //     ...
            // }
        }
    }

    public class MonoBehaviourExamples : MonoBehaviour
    {
        // Constructor needed to use Start, Update, etc...
        public MonoBehaviourExamples(IntPtr handle) : base(handle) { }

        private string textInUpdate = "Monobehavior Update";
        private float timer = 0;
        private WaitForSeconds fiveSeconds = new WaitForSeconds(5f);

        private void Start()
        {
            Plugin.Log.LogMessage("Monobehavior Start");

            //Coroutines are done differently, use WrapToIl2Cpp() from BepInEx.IL2CPP.Utils.Collections
            StartCoroutine(MonoCoroutine().WrapToIl2Cpp());

            //SceneManager.SceneLoaded is also done differently
            SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>((s, lsm) => OnSceneLoaded()));
        }

        private IEnumerator MonoCoroutine()
        {
            // In IL2CPP, coroutines don't always have the expected behavior. You need to test if its working as expected.
            while (true)
            {
                Plugin.Log.LogDebug("Monobehavior Coroutine");
                yield return fiveSeconds;
            }
        }

        private void OnSceneLoaded()
        {
            Plugin.Log.LogMessage("Monobehavior Sceneloaded");
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < 6) return;

            Plugin.Log.LogInfo(textInUpdate);
            timer = 0;
        }

        //private void OnGui()
        //{
        //     GUI is The Achilles' Heel from IL2CPP
        //     GUIStyle references sometimes work, sometimes don't
        //     Many, MANY BUGS
        //     Nothing is documented
        //     WELCOME TO IL2CPP HELL
        //     (recommended soundtrack: https://www.youtube.com/watch?v=Jm932Sqwf5E )
        //}
    }
}
