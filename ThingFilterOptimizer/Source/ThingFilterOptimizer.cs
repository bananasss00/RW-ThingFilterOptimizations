using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ThingFilterOptimizer
{
    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    public class ThingFilterOptimizerMod : Mod
    {
        public ThingFilterOptimizerMod(ModContentPack content) : base(content)
        {
            var h = new Harmony("pirateby.thingfilteroptimizer");
            h.PatchAll();

            var rsaPatch = AccessTools.Method("RSA.Core.ThingFilter_InjectFilter:Before_DoCategoryChildren");
            if (ModLister.GetActiveModWithIdentifier("Storage.Search.RSA") != null && rsaPatch != null)
            {
                h.Patch(typeof(Listing_TreeThingFilter_ViewOpt).GetMethod("DoCategoryChildren"), prefix: new HarmonyMethod(rsaPatch));
                Log.Message($"[ThingFilterOptimizer] RSA support patch");
            }
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DoThingFilterConfigWindow_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var listingCtor = AccessTools.Constructor(typeof(Listing_TreeThingFilter),
                new[]
                {
                    typeof(ThingFilter), typeof(ThingFilter), typeof(IEnumerable<ThingDef>),
                    typeof(IEnumerable<SpecialThingFilterDef>), typeof(List<ThingDef>)
                });
            var listingCtorMy = AccessTools.Constructor(typeof(Listing_TreeThingFilter_ViewOpt),
                new[]
                {
                    typeof(ThingFilter), typeof(ThingFilter), typeof(IEnumerable<ThingDef>),
                    typeof(IEnumerable<SpecialThingFilterDef>), typeof(List<ThingDef>),
                    typeof(Rect), typeof(Vector2).MakeByRefType()
                });
            var doCategoryChildren = AccessTools.Method(typeof(Listing_TreeThingFilter), nameof(Listing_TreeThingFilter.DoCategoryChildren));
            var doCategoryChildrenMy = AccessTools.Method(typeof(Listing_TreeThingFilter_ViewOpt), nameof(Listing_TreeThingFilter_ViewOpt.DoCategoryChildren));

            /***
             * Replace class in code Listing_TreeThingFilter => Listing_TreeThingFilter_ViewOpt with new parameter 'scrollPosition':
             *   Listing_TreeThingFilter listing_TreeThingFilter = new Listing_TreeThingFilter(filter, parentFilter, forceHiddenDefs, forceHiddenFilters, suppressSmallVolumeTags);
			 *   listing_TreeThingFilter.Begin(rect3);
			 *   listing_TreeThingFilter.DoCategoryChildren(node, 0, openMask, map, true);
			 *   listing_TreeThingFilter.End();
             */
            var myTypeLocalVar = ilGen.DeclareLocal(typeof(Listing_TreeThingFilter_ViewOpt));
            int successPatches = 0;
            int replacedVars = 0;
            foreach (var ci in instructions)
            {

                if (ci.operand is LocalBuilder local && local.LocalType == typeof(Listing_TreeThingFilter))
                {
                    ci.operand = myTypeLocalVar; // replace to var with type: Listing_TreeThingFilter_ViewOpt
                    replacedVars++;
                }
                else if (ci.opcode == OpCodes.Newobj && ci.operand == listingCtor)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // add argument 'rect' for my class
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // add argument 'scrollPosition' for my class
                    ci.operand = listingCtorMy; // replace ctor
                    successPatches++;
                }
                else if (ci.opcode == OpCodes.Callvirt && ci.operand == doCategoryChildren)
                {
                    ci.operand = doCategoryChildrenMy; // replace method
                    successPatches++;
                }
                yield return ci;
            }

            //Log.Message($"DoThingFilterConfigWindow replaced vars: {replacedVars}");

            if (successPatches != 2)
            {
                Log.Error($"Outdated transpiler ThingFilterUI:DoThingFilterConfigWindow. successPatches: {successPatches}");
            }
        }
    }
}
