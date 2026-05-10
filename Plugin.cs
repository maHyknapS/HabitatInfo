/*
 * HabitatInfo - Voodoo Fishin' Mod
 * Displays the current habitat tags when fishing
 *
 * Author: mahyknaps
 * Assisted by: Claude (Anthropic AI)
 * Version: 1.0.0
 */

using BepInEx;
using BepInEx.Unity.IL2CPP;
using Fishing;
using HarmonyLib;
using Riverboat.Players;
using Riverboat.UI;
using System.Reflection;
using UnityEngine;

namespace HabitatInfo
{
    [BepInPlugin("com.habitat.info", "HabitatInfo", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public static Plugin? Instance;
        public static string CurrentTags = "";

        public override void Load()
        {
            Instance = this;
            Log.LogInfo("HabitatInfo loaded!");
            var harmony = new Harmony("com.habitat.info");
            harmony.PatchAll();
        }

        public static string LoadTagsFromArea(FishingArea area)
        {
            try
            {
                var getter = typeof(FishingArea).GetMethod("get_EnvironmentTags",
                    BindingFlags.Public | BindingFlags.Instance);

                if (getter != null)
                {
                    var rawList = getter.Invoke(area, null);
                    if (rawList != null)
                    {
                        var listType = rawList.GetType();
                        int count = (int)(listType.GetProperty("Count")?.GetValue(rawList) ?? 0);
                        var indexer = listType.GetProperty("Item");
                        var names = new System.Collections.Generic.List<string>();

                        for (int i = 0; i < count; i++)
                        {
                            var tag = indexer?.GetValue(rawList, new object[] { i });
                            if (tag == null) continue;
                            var tagName = tag.GetType()
                                .GetProperty("tagName", BindingFlags.Public | BindingFlags.Instance)
                                ?.GetValue(tag)?.ToString();
                            if (!string.IsNullOrEmpty(tagName))
                                names.Add(tagName!);
                        }

                        if (names.Count > 0)
                            return string.Join(" | ", names);
                    }
                }
            }
            catch { }

            return "";
        }
    }

    [HarmonyPatch(typeof(PlayerCasting), "Update")]
    public class PlayerCastingUpdatePatch
    {
        static bool _wasInWater = false;

        static void Postfix(PlayerCasting __instance)
        {
            try
            {
                bool hasB = __instance.HasBobber();
                if (!hasB && _wasInWater)
                {
                    _wasInWater = false;
                    Plugin.CurrentTags = "";
                    GameNotification.Instance?.HideNotification();
                    return;
                }

                var bobber = __instance.Bobber;
                if (bobber == null) return;

                bool inWater = bobber.IsInWater();

                if (inWater && !_wasInWater)
                {
                    _wasInWater = true;

                    var manager = Object.FindObjectOfType<FishingManager>();
                    if (manager != null)
                    {
                        var area = manager.FindFishingArea(bobber.transform.position);
                        if (area != null)
                            Plugin.CurrentTags = Plugin.LoadTagsFromArea(area);
                    }

                    string display = string.IsNullOrEmpty(Plugin.CurrentTags)
                        ? "Loading habitat..."
                        : Plugin.CurrentTags;

                    GameNotification.Instance?.ShowPersistentNotification(display);
                }
                else if (!inWater && _wasInWater)
                {
                    _wasInWater = false;
                    Plugin.CurrentTags = "";
                    GameNotification.Instance?.HideNotification();
                }
            }
            catch { }
        }
    }
}