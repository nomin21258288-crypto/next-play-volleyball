// UNTESTED. Loads the SAME config/controls.layout.json the JS tests validate (copied to StreamingAssets).
using System.Collections.Generic; using System.IO; using Newtonsoft.Json; using UnityEngine;
namespace NextPlay.Tutorial {
  public class ControlDef { public string id, type, anchor, action, labelKey; public float[] offset, size; }
  public class LayoutFile { public int version; public List<ControlDef> controls; }
  public static class ControlLayout {
    public static LayoutFile Load() {
      // Stage 3: on Android StreamingAssets lives inside the APK and File.ReadAllText cannot open it, so read the Resources copy first.
      TextAsset ta = Resources.Load<TextAsset>("Config/controls.layout");
      if (ta != null) return JsonConvert.DeserializeObject<LayoutFile>(ta.text);
      return JsonConvert.DeserializeObject<LayoutFile>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "controls.layout.json")));
    }
    // Same math as packages/tutorial-core/src/layout.js (dp -> px via Screen.dpi/160, safe area as insets).
    public static Rect Resolve(ControlDef c, Rect safe, float dpToPx) {
      float ox = c.offset[0]*dpToPx, oy = c.offset[1]*dpToPx, w = c.size[0]*dpToPx, h = c.size[1]*dpToPx;
      var a = c.anchor.Split('-'); // Unity y is bottom-up
      float x = a[1]=="left" ? safe.xMin+ox : safe.xMax-ox-w;
      float y = a[0]=="bottom" ? safe.yMin+oy : safe.yMax-oy-h;
      return new Rect(x, y, w, h);
    }
  }
}
