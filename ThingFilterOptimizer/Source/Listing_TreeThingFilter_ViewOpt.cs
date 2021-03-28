using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ThingFilterOptimizer
{
    public class Listing_TreeThingFilter_ViewOpt : Listing_Tree
    {
        private readonly ThingFilter filter;

        private readonly ThingFilter parentFilter;

        private List<SpecialThingFilterDef> hiddenSpecialFilters;

        private readonly List<ThingDef> forceHiddenDefs;

        private readonly List<SpecialThingFilterDef> tempForceHiddenSpecialFilters;

        private readonly List<ThingDef> suppressSmallVolumeTags;

        private Rect viewRect;

        private Vector2 scrollPosition;

        public Listing_TreeThingFilter_ViewOpt(ThingFilter filter, ThingFilter parentFilter,
            IEnumerable<ThingDef> forceHiddenDefs, IEnumerable<SpecialThingFilterDef> forceHiddenFilters,
            List<ThingDef> suppressSmallVolumeTags, Rect viewRect, ref Vector2 scrollPosition)
        {
            this.filter = filter;
            this.parentFilter = parentFilter;
            if (forceHiddenDefs != null) this.forceHiddenDefs = forceHiddenDefs.ToList();
            if (forceHiddenFilters != null) tempForceHiddenSpecialFilters = forceHiddenFilters.ToList();
            this.suppressSmallVolumeTags = suppressSmallVolumeTags;
            this.viewRect = viewRect;
            this.scrollPosition = scrollPosition;
        }

        public void DoCategoryChildren(TreeNode_ThingCategory node, int indentLevel, int openMask, Map map,
            bool isRoot = false)
        {
            // public void EndLine() => this.curY += this.lineHeight + this.verticalSpacing;
            if (isRoot)
            {
                foreach (var sfDef in node.catDef.ParentsSpecialThingFilterDefs)
                {
                    if (curY > listingRect.height)
                        break; // if Y > MAX HEIGHT
                    if (Visible_NewTemp(sfDef, node))
                        DoSpecialFilter(sfDef, indentLevel);
                }
            }

            var childSpecialFilters = node.catDef.childSpecialFilters;
            for (var i = 0; i < childSpecialFilters.Count; i++)
            {
                if (curY > listingRect.height)
                    break; // if Y > MAX HEIGHT
                if (Visible_NewTemp(childSpecialFilters[i], node))
                    DoSpecialFilter(childSpecialFilters[i], indentLevel);
            }

            foreach (var node2 in node.ChildCategoryNodes)
            {
                if (curY > listingRect.height)
                    break; // if Y > MAX HEIGHT
                if (Visible(node2))
                    DoCategory(node2, indentLevel, openMask, map);
            }

            foreach (var thingDef in from n in node.catDef.childThingDefs
                orderby n.label
                select n)
            {
                if (curY > listingRect.height)
                    break; // if Y > MAX HEIGHT
                if (Visible(thingDef))
                    DoThingDef(thingDef, indentLevel, map);
            }
        }

        private bool InViewArea => curY > (scrollPosition.y - listingRect.y) && curY < (scrollPosition.y - listingRect.y) + viewRect.height;
        //private bool InViewArea => true;

#region Drawers
        private void DoSpecialFilter(SpecialThingFilterDef sfDef, int nestLevel)
        {
            if (!sfDef.configurable) return;
            if (InViewArea)
            {
                LabelLeft("*" + sfDef.LabelCap, sfDef.description, nestLevel);
                var flag = filter.Allows(sfDef);
                var flag2 = flag;
                Widgets.Checkbox(new Vector2(LabelWidth, curY), ref flag, lineHeight, false, true);
                if (flag != flag2) filter.SetAllow(sfDef, flag);
            }
            EndLine();
        }

        public void DoCategory(TreeNode_ThingCategory node, int indentLevel, int openMask, Map map)
        {
            if (InViewArea)
            {
                OpenCloseWidget(node, indentLevel, openMask);
                LabelLeft(node.LabelCap, node.catDef.description, indentLevel);
                var multiCheckboxState = AllowanceStateOf(node);
                var multiCheckboxState2 = Widgets.CheckboxMulti(new Rect(LabelWidth, curY, lineHeight, lineHeight),
                    multiCheckboxState, true);
                if (multiCheckboxState != multiCheckboxState2)
                    filter.SetAllow(node.catDef, multiCheckboxState2 == MultiCheckboxState.On, forceHiddenDefs,
                        hiddenSpecialFilters);
            }
            EndLine();
            if (node.IsOpen(openMask)) DoCategoryChildren(node, indentLevel + 1, openMask, map);
        }

        private void DoThingDef(ThingDef tDef, int nestLevel, Map map)
        {
            if (InViewArea)
            {
                var obj = (suppressSmallVolumeTags == null || !suppressSmallVolumeTags.Contains(tDef)) &&
                          tDef.IsStuff &&
                          tDef.smallVolume;
                var text = tDef.DescriptionDetailed;
                if (obj) text += "\n\n" + "ThisIsSmallVolume".Translate(10.ToStringCached());
                var num = -4f;
                if (obj)
                {
                    var rect = new Rect(LabelWidth - 19f, curY, 19f, 20f);
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperRight;
                    GUI.color = Color.gray;
                    Widgets.Label(rect, "/" + 10.ToStringCached());
                    Text.Font = GameFont.Small;
                    GenUI.ResetLabelAlign();
                    GUI.color = Color.white;
                }

                num -= 19f;
                if (map != null)
                {
                    var count = map.resourceCounter.GetCount(tDef);
                    if (count > 0)
                    {
                        var text2 = count.ToStringCached();
                        var rect2 = new Rect(0f, curY, LabelWidth + num, 40f);
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.UpperRight;
                        GUI.color = new Color(0.5f, 0.5f, 0.1f);
                        Widgets.Label(rect2, text2);
                        num -= Text.CalcSize(text2).x;
                        GenUI.ResetLabelAlign();
                        Text.Font = GameFont.Small;
                        GUI.color = Color.white;
                    }
                }

                LabelLeft(tDef.LabelCap, text, nestLevel, num);
                var flag = filter.Allows(tDef);
                var flag2 = flag;
                Widgets.Checkbox(new Vector2(LabelWidth, curY), ref flag, lineHeight, false, true);
                if (flag != flag2) filter.SetAllow(tDef, flag);
            }
            EndLine();
        }
#endregion

#region Helper functions
        public MultiCheckboxState AllowanceStateOf(TreeNode_ThingCategory cat)
        {
            var num = 0;
            var num2 = 0;
            foreach (var thingDef in cat.catDef.DescendantThingDefs)
                if (Visible(thingDef))
                {
                    num++;
                    if (filter.Allows(thingDef)) num2++;
                }

            var flag = false;
            foreach (var sf in cat.catDef.DescendantSpecialThingFilterDefs)
                if (Visible_NewTemp(sf, cat) && !filter.Allows(sf))
                {
                    flag = true;
                    break;
                }

            if (num2 == 0) return MultiCheckboxState.Off;
            if (num == num2 && !flag) return MultiCheckboxState.On;
            return MultiCheckboxState.Partial;
        }

        private bool Visible(ThingDef td)
        {
            if (td.menuHidden) return false;
            if (forceHiddenDefs != null && forceHiddenDefs.Contains(td)) return false;
            if (parentFilter != null)
            {
                if (!parentFilter.Allows(td)) return false;
                if (parentFilter.IsAlwaysDisallowedDueToSpecialFilters(td)) return false;
            }

            return true;
        }

        private bool Visible(TreeNode_ThingCategory node)
        {
            return node.catDef.DescendantThingDefs.Any(Visible);
        }

        [Obsolete("Obsolete, only used to avoid errors when patching")]
        private bool Visible(SpecialThingFilterDef filter)
        {
            return Visible_NewTemp(filter, new TreeNode_ThingCategory(ThingCategoryDefOf.Root));
        }

        private bool Visible_NewTemp(SpecialThingFilterDef filter, TreeNode_ThingCategory node)
        {
            if (parentFilter != null && !parentFilter.Allows(filter)) return false;
            if (hiddenSpecialFilters == null) CalculateHiddenSpecialFilters(node);
            for (var i = 0; i < hiddenSpecialFilters.Count; i++)
                if (hiddenSpecialFilters[i] == filter)
                    return false;
            return true;
        }

        private void CalculateHiddenSpecialFilters(TreeNode_ThingCategory node)
        {
            hiddenSpecialFilters = new List<SpecialThingFilterDef>();
            if (tempForceHiddenSpecialFilters != null) hiddenSpecialFilters.AddRange(tempForceHiddenSpecialFilters);
            var enumerable =
                node.catDef.ParentsSpecialThingFilterDefs.Concat(node.catDef.DescendantSpecialThingFilterDefs);
            var enumerable2 = node.catDef.DescendantThingDefs;
            if (parentFilter != null)
                enumerable2 = from x in enumerable2
                    where parentFilter.Allows(x)
                    select x;
            foreach (var specialThingFilterDef in enumerable)
            {
                var flag = false;
                foreach (var def in enumerable2)
                    if (specialThingFilterDef.Worker.CanEverMatch(def))
                    {
                        flag = true;
                        break;
                    }

                if (!flag) hiddenSpecialFilters.Add(specialThingFilterDef);
            }
        }
#endregion
    }
}