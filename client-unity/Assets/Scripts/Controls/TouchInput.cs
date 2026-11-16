// UNTESTED. Mobile touch controls built from the SHARED layout (config/controls.layout.json).
// Joystick + 6 action buttons (jump/serve/pass/set/spike/block) + pause. Each HUD control registers with
// the Stage 1 tutorial ControlRegistry using its layout id, so tutorial arrows point at the real controls.
using System.Collections.Generic;
using NextPlay.Sim;
using NextPlay.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NextPlay.Controls
{
    // Aggregates all touch state into a sim PlayerInput once per sim tick.
    public sealed class TouchInput
    {
        public Vector2 Stick;                       // -1..1 (x = screen right, y = screen up)
        public bool JumpHeld;
        PlayerAction queued = PlayerAction.None;    // edge-triggered action (the sim buffers it for 0.3 s)
        bool jumpQueued;
        public void PressAction(PlayerAction a) { queued = a; }
        public void PressJump() { jumpQueued = true; }
        public PlayerInput Consume(System.Func<Vector2, Vector2> screenToCourt)
        {
            Vector2 c = screenToCourt(Stick);
            PlayerInput i = PlayerInput.Move(c.x, c.y);
            i.Jump = jumpQueued || JumpHeld; i.Action = queued; i.AimZ = c.y;
            queued = PlayerAction.None; jumpQueued = false;
            return i;
        }
    }

    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public TouchInput Target; public RectTransform Knob; public float Radius = 56f;
        RectTransform rt;
        void Awake() { rt = (RectTransform)transform; }
        public void OnPointerDown(PointerEventData e) { OnDrag(e); }
        public void OnDrag(PointerEventData e)
        {
            Vector2 local; RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out local);
            Vector2 v = Vector2.ClampMagnitude(local / Radius, 1f);
            Target.Stick = v; if (Knob != null) Knob.anchoredPosition = v * Radius;
        }
        public void OnPointerUp(PointerEventData e) { Target.Stick = Vector2.zero; if (Knob != null) Knob.anchoredPosition = Vector2.zero; }
    }

    public sealed class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public TouchInput Target; public string Action;   // layout "action": jump|serve|pass|set|spike|block|pause
        public System.Action OnPause;
        public void OnPointerDown(PointerEventData e)
        {
            switch (Action)
            {
                case "jump": Target.PressJump(); Target.JumpHeld = true; break;
                case "serve": Target.PressAction(PlayerAction.Serve); break;
                case "pass": Target.PressAction(PlayerAction.Pass); break;
                case "set": Target.PressAction(PlayerAction.Set); break;
                case "spike": Target.PressAction(PlayerAction.Spike); break;
                case "block": Target.PressAction(PlayerAction.Block); break;
                case "pause": if (OnPause != null) OnPause(); break;
            }
        }
        public void OnPointerUp(PointerEventData e) { if (Action == "jump") Target.JumpHeld = false; }
    }

    // Builds the HUD from the layout file (no hard-coded positions). Call Build() once with a Canvas.
    public static class ControlHud
    {
        public static TouchInput Build(Canvas canvas, LayoutFile layout, System.Action onPause)
        {
            TouchInput input = new TouchInput();
            RectTransform root = (RectTransform)canvas.transform;
            float dpToPx = Mathf.Max(1f, Screen.dpi / 160f) / canvas.scaleFactor;   // layout is in dp; canvas units are scaled px
            Rect safe = Screen.safeArea; safe = new Rect(safe.x / canvas.scaleFactor, safe.y / canvas.scaleFactor, safe.width / canvas.scaleFactor, safe.height / canvas.scaleFactor);
            foreach (ControlDef c in layout.controls)
            {
                Rect r = ControlLayout.Resolve(c, safe, dpToPx);
                GameObject go = new GameObject("HUD_" + c.id, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                RectTransform rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
                rt.anchoredPosition = new Vector2(r.x, r.y); rt.sizeDelta = new Vector2(r.width, r.height);
                go.GetComponent<Image>().color = new Color(1, 1, 1, c.type == "joystick" ? 0.18f : 0.35f);
                go.AddComponent<ControlRegistry>().controlId = c.id;       // Stage 1 tutorial overlay targets this
                GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(go.transform, false);
                Text tx = label.GetComponent<Text>(); tx.text = c.action; tx.alignment = TextAnchor.MiddleCenter; tx.color = Color.white;
                tx.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); tx.raycastTarget = false;
                RectTransform lr = (RectTransform)label.transform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
                if (c.type == "joystick")
                {
                    VirtualJoystick js = go.AddComponent<VirtualJoystick>(); js.Target = input; js.Radius = r.width * 0.35f;
                    tx.text = "";
                }
                else { TouchButton b = go.AddComponent<TouchButton>(); b.Target = input; b.Action = c.action; b.OnPause = onPause; }
            }
            return input;
        }
    }
}
