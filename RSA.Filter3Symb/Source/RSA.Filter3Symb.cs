using System.Diagnostics;
using HarmonyLib;
using RSA.Core;
using Verse;

namespace RSA
{
    [HarmonyPatch(typeof(SearchTerm), nameof(SearchTerm.FilterNodes))]
    public class Filter3Symb : Mod
    {
        public Filter3Symb(ModContentPack content) : base(content)
        {
            new Harmony("pirateby.rsa.filter3symb").PatchAll();
        }

        [HarmonyPrefix]
        public static bool FilterNodes_Prefix(SearchTerm __instance, TreeNode_ThingCategory node, ref TreeNode_ThingCategory __result)
        {
            //if (!string.IsNullOrEmpty(__instance.Value) && __instance.Value.Length >= 3)
            //    return true; // call original

            var s = __instance.Value;
            
            if (!s?.Equals(_prevValue) ?? true)
            {
                _sw.Restart();
                _prevValue = s;
            }

            if (string.IsNullOrEmpty(s))
                return true;

            if (s.Length >= 3 || _sw.Elapsed.TotalMilliseconds > 500)
                return true;

            __result = node;
            return false;
        }

        private static Stopwatch _sw = new Stopwatch();
        private static string _prevValue/* = String.Empty*/;
    }
}
