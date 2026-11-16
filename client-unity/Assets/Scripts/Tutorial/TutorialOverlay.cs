// UNTESTED. Arrow/hand-pointer overlay. HUD buttons register by layout id; the arrow follows the REAL RectTransform.
using System.Collections.Generic; using UnityEngine;
namespace NextPlay.Tutorial {
  public class ControlRegistry : MonoBehaviour {
    static readonly Dictionary<string, RectTransform> map = new();
    public string controlId;                       // must equal an id in controls.layout.json
    void OnEnable() => map[controlId] = (RectTransform)transform;
    void OnDisable() => map.Remove(controlId);
    public static bool TryGet(string id, out RectTransform rt) => map.TryGetValue(id, out rt);
  }
  public class TutorialOverlay : MonoBehaviour {
    public RectTransform arrow, hand; public float bob = 12f;
    string target;
    public void Point(string controlId) { target = controlId; arrow.gameObject.SetActive(controlId != null); }
    void LateUpdate() {
      if (target == null) return;
      if (!ControlRegistry.TryGet(target, out var rt)) { Debug.LogError($"Tutorial target '{target}' has no HUD control"); return; }
      var p = rt.position + Vector3.up * (rt.rect.height*0.6f*rt.lossyScale.y + Mathf.Sin(Time.unscaledTime*6f)*bob);
      arrow.position = p; hand.position = rt.position;
    }
  }
}
