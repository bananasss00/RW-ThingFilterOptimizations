﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ThingFilterOptimizer
{
    public class ThingFilterOptimizerMod : Mod
    {
        public ThingFilterOptimizerMod(ModContentPack content) : base(content)
        {
            var h = new Harmony("pirateby.thingfilteroptimizer");
            h.PatchAll();

            // MANUAL PATCHING VISIBLE METHODS, HARMONY NOT WANT PATCH ALL OVERLOADS WITH HarmonyPatch attribute //
            #if DEBUG
            var visibleMethod = new HarmonyMethod(typeof(Listing_TreeThingFilter_Patch), nameof(Listing_TreeThingFilter_Patch.VisibleDbg));
            #else
            var visibleMethod = new HarmonyMethod(typeof(Listing_TreeThingFilter_Patch), nameof(Listing_TreeThingFilter_Patch.Visible));
            #endif

            h.Patch(AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.Visible), new []{typeof(ThingDef)}), prefix: visibleMethod);
            h.Patch(AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.Visible), new []{typeof(TreeNode_ThingCategory)}), prefix: visibleMethod);
            h.Patch(AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.Visible_NewTemp)), prefix: visibleMethod);

            // unused Obsolete method
            //h.Patch(AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.Visible), new []{typeof(SpecialThingFilterDef)}), prefix: visibleMethod);
        }

        [Conditional("DEBUG")]
        public static void DbgLog(string text) => Log.Warning(text);
    }

    // Store scroll postion for Listing_TreeThingFilter patches
    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    public class ThingFilterUI_DoThingFilterConfigWindow_Patch
    {
        public static bool InDoThingFilterConfigWindow;

        [HarmonyPrefix]
        public static void DoThingFilterConfigWindow_Prefix(ref Rect rect, ref Vector2 scrollPosition)
        {
            InDoThingFilterConfigWindow = true;
            Listing_TreeThingFilter_Patch.ViewRect = rect;
            Listing_TreeThingFilter_Patch.ScrollPosition = scrollPosition;
            ThingFilterOptimizerMod.DbgLog("START");
        }

        [HarmonyPostfix]
        public static void DoThingFilterConfigWindow_Postfix()
        {
            InDoThingFilterConfigWindow = false;
            ThingFilterOptimizerMod.DbgLog("END");
        }
    }
    
    // Draw only visible elements
    [HarmonyPatch(typeof(Listing_TreeThingFilter))]
    public class Listing_TreeThingFilter_Patch
    {
        public static Rect ViewRect;

        public static Vector2 ScrollPosition;

        public static bool InDoThingFilterConfigWindow => ThingFilterUI_DoThingFilterConfigWindow_Patch.InDoThingFilterConfigWindow;

        public static bool InViewArea(Listing_TreeThingFilter l) => l.curY > (ScrollPosition.y - l.listingRect.y) && l.curY < (ScrollPosition.y - l.listingRect.y) + ViewRect.height;
        
        public static bool IsOutOfHeight(Listing_TreeThingFilter l) => l.curY > l.listingRect.height;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Listing_TreeThingFilter.DoSpecialFilter))]
        public static bool DoSpecialFilter(Listing_TreeThingFilter __instance, SpecialThingFilterDef sfDef)
        {
            if (!InDoThingFilterConfigWindow || !sfDef.configurable || InViewArea(__instance)) return true;
            ThingFilterOptimizerMod.DbgLog("SKIP");
            __instance.EndLine();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Listing_TreeThingFilter.DoCategory))]
        public static bool DoCategory(Listing_TreeThingFilter __instance, TreeNode_ThingCategory node, int indentLevel, int openMask, Map map)
        {
            if (!InDoThingFilterConfigWindow || InViewArea(__instance)) return true;
            ThingFilterOptimizerMod.DbgLog("SKIP");
            __instance.EndLine();
            if (node.IsOpen(openMask)) __instance.DoCategoryChildren(node, indentLevel + 1, openMask, map);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Listing_TreeThingFilter.DoThingDef))]
        public static bool DoThingDef(Listing_TreeThingFilter __instance)
        {
            if (!InDoThingFilterConfigWindow || InViewArea(__instance)) return true;
            ThingFilterOptimizerMod.DbgLog("SKIP");
            __instance.EndLine();
            return false;
        }

        /* SKIP DRAW OUT OF RANGE ELEMENTS IN METHOD DoCategoryChildren */
        public static bool Visible(Listing_TreeThingFilter __instance) => !InDoThingFilterConfigWindow || !IsOutOfHeight(__instance);

        public static bool VisibleDbg(Listing_TreeThingFilter __instance)
        {
            if (!InDoThingFilterConfigWindow)
                return true;

            bool result = !IsOutOfHeight(__instance);
            if (!result)
                ThingFilterOptimizerMod.DbgLog("SKIP OUT OF RANGE");

            return result;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Listing_TreeThingFilter.Visible), typeof(SpecialThingFilterDef))] // unused Obsolete method
        //public static bool Visible0(Listing_TreeThingFilter __instance) => !InDoThingFilterConfigWindow || !IsOutOfHeight(__instance);

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Listing_TreeThingFilter.Visible), typeof(ThingDef))]
        //public static bool Visible1(Listing_TreeThingFilter __instance) => !InDoThingFilterConfigWindow || !IsOutOfHeight(__instance);

        //[HarmonyPatch(nameof(Listing_TreeThingFilter.Visible), typeof(TreeNode_ThingCategory))]
        //public static bool Visible2(Listing_TreeThingFilter __instance) => !InDoThingFilterConfigWindow || !IsOutOfHeight(__instance);

        //[HarmonyPrefix]
        //[HarmonyPatch(nameof(Listing_TreeThingFilter.Visible_NewTemp))]
        //public static bool Visible_NewTemp(Listing_TreeThingFilter __instance) => !InDoThingFilterConfigWindow || !IsOutOfHeight(__instance);
    }
}
