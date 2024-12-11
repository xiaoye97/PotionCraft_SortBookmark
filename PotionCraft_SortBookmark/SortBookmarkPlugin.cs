using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using UnityEngine;

namespace xiaoye97
{
    [BepInPlugin("me.xiaoye97.plugin.PotionCraft.SortBookmark", "SortBookmark", "2.0.1")]
    public class SortBookmarkPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<KeyCode> Hotkey;

        private void Awake()
        {
            Hotkey = Config.Bind<KeyCode>("config", "SortHotkey", KeyCode.Space);
            Harmony.CreateAndPatchAll(typeof(SortBookmarkPlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RecipeBook), "Awake")]
        public static void RecipeBook_Awake_Patch()
        {
            GameObject pluginGameObject = new GameObject("SortBookmarkPlugin");
            pluginGameObject.AddComponent<SortBookmark>();
            GameObject.DontDestroyOnLoad(pluginGameObject);
        }
    }
}