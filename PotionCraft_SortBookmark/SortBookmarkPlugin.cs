using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PotionCraft.ManagersSystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace xiaoye97
{
    [BepInPlugin("me.xiaoye97.plugin.PotionCraft.SortBookmark", "SortBookmark", "1.4.0")]
    public class SortBookmarkPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<KeyCode> Hotkey;

        private void Awake()
        {
            Hotkey = Config.Bind<KeyCode>("config", "SortHotkey", KeyCode.Space);
            Harmony.CreateAndPatchAll(typeof(SortBookmarkPlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Managers), "OnEnable")]
        public static void Managers_OnEnable_Patch()
        {
            GameObject pluginGameObject = new GameObject("SortBookmarkPlugin");
            pluginGameObject.AddComponent<SortBookmark>();
            GameObject.DontDestroyOnLoad(pluginGameObject);
        }
    }
}