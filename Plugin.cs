/*
 * HabitatInfo - Voodoo Fishin' Mod
 * Displays the current habitat tags when fishing
 *
 * Author: mahyknaps
 * Assisted by: Claude (Anthropic AI)
 * Version: 1.0.2
 */

using BepInEx;
using BepInEx.Unity.IL2CPP;
using Fishing;
using HarmonyLib;
using Riverboat.Players;
using Riverboat.UI;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HabitatInfo
{
    [BepInPlugin("com.habitat.info", "HabitatInfo", "1.0.2")]
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

        // Strips rich text tags, unicode escapes and descriptions from localized tag names
        public static string CleanTagName(string raw)
        {
            raw = Regex.Unescape(raw);
            raw = Regex.Replace(raw, "<.*?>", "");

            // Remove description after separator (varies by language)
            var separators = new string[] { " - ", " — ", " \u2013 ", " \u2014 ", " ÔÇô " };
            foreach (var sep in separators)
            {
                int idx = raw.IndexOf(sep);
                if (idx >= 0)
                {
                    raw = raw.Substring(0, idx);
                    break;
                }
            }

            return raw.TrimEnd('-', ' ').Trim();
        }

        // Reads environment tags from a FishingArea and returns them as a formatted string
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

                            string? tagName = null;

                            // Try localized name first so it matches the player's language
                            try
                            {
                                var getLocalizedName = tag.GetType().GetMethod("GetLocalizedName",
                                    BindingFlags.Public | BindingFlags.Instance);
                                var raw = getLocalizedName?.Invoke(tag, null)?.ToString();
                                if (!string.IsNullOrEmpty(raw))
                                    tagName = CleanTagName(raw);
                            }
                            catch { }

                            // Fall back to raw tag name if localization fails
                            if (string.IsNullOrEmpty(tagName))
                            {
                                var tagNameProp = tag.GetType().GetProperty("tagName",
                                    BindingFlags.Public | BindingFlags.Instance);
                                tagName = tagNameProp?.GetValue(tag)?.ToString();
                            }

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

        // Checks if a PlayerCasting instance belongs to the local player
        public static bool IsLocalPlayer(PlayerCasting instance)
        {
            try
            {
                var prop = typeof(PlayerCasting).GetProperty("IsLocalPlayer",
                    BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                    return (bool)(prop.GetValue(instance) ?? false);
            }
            catch { }
            return false;
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
                if (!Plugin.IsLocalPlayer(__instance)) return;

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

                    // Find the fishing area at the bobber's position and load its tags
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