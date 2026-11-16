// Unity-only (uses UnityEngine.Rect). Checks the C# layout resolver against the same rules as the JS tests.
using System.Collections.Generic;
using NextPlay.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace NextPlay.Tests
{
    public class ControlLayoutTests
    {
        static readonly Vector2[] Screens = { new Vector2(640, 360), new Vector2(732, 412), new Vector2(800, 360), new Vector2(915, 412) };
        static readonly string[] RequiredActions = { "move", "jump", "serve", "pass", "set", "spike", "block" };

        [Test] public void EveryRequiredActionHasExactlyOneControl()
        {
            LayoutFile l = ControlLayout.Load();
            foreach (string a in RequiredActions) Assert.AreEqual(1, l.controls.FindAll(c => c.action == a).Count, a);
        }
        [Test] public void ControlsInBoundsNonOverlappingAndTouchSized_OnAllReferenceScreens()
        {
            LayoutFile l = ControlLayout.Load();
            foreach (Vector2 s in Screens)
            {
                Rect safe = new Rect(0, 0, s.x, s.y); List<Rect> rs = new List<Rect>();
                foreach (ControlDef c in l.controls)
                {
                    Rect r = ControlLayout.Resolve(c, safe, 1f); rs.Add(r);
                    Assert.GreaterOrEqual(r.xMin, 0, c.id); Assert.GreaterOrEqual(r.yMin, 0, c.id);
                    Assert.LessOrEqual(r.xMax, s.x, c.id); Assert.LessOrEqual(r.yMax, s.y, c.id);
                    Assert.GreaterOrEqual(r.width, 44f, c.id); Assert.GreaterOrEqual(r.height, 44f, c.id);
                }
                for (int i = 0; i < rs.Count; i++) for (int j = i + 1; j < rs.Count; j++) Assert.IsFalse(rs[i].Overlaps(rs[j]), l.controls[i].id + " overlaps " + l.controls[j].id + " @" + s);
            }
        }
    }
}
