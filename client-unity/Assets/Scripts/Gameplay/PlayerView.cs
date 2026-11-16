// UNTESTED. Placeholder player: capsule "Visual" under a logic root. Animation-ready: if an Animator with a controller
// is assigned later (final characters), parameters Speed(float) Grounded(bool) Jump/Serve/Pass/Set/Spike/Block(triggers) are driven.
using NextPlay.Sim;
using UnityEngine;

namespace NextPlay.Gameplay
{
    public sealed class TeamIdentity : MonoBehaviour
    {
        public int Team; public string PlayerId; public bool IsHuman;
        public static readonly Color[] TeamColors = { new Color(0.2f, 0.45f, 0.95f), new Color(0.95f, 0.3f, 0.25f) };
    }

    // Stage 3: lets a procedurally animated character (or any other rig) be driven without Animator controllers.
    public interface IPlayerAnimDriver
    {
        void Drive(float speed, bool grounded, float height);
        void Trigger(string action);   // "Serve" | "Pass" | "Set" | "Spike" | "Block" | "Jump"
    }

    public sealed class PlayerView : MonoBehaviour
    {
        public Player Sim; public Transform Visual; public Animator Anim; public IPlayerAnimDriver Rig;
        Vector3 lastPos; float yaw;
        int shownEvents;

        public static PlayerView Create(Player p, Transform parent, GameObject prefabOrNull)
        {
            return CreateWithVisual(p, parent, prefabOrNull != null ? Instantiate(prefabOrNull) : null);
        }

        // visualOrNull: an already-built character (feet at its local origin, facing +z). null = the Stage 2 capsule placeholder.
        public static PlayerView CreateWithVisual(Player p, Transform parent, GameObject visualOrNull)
        {
            GameObject root = new GameObject("Player_" + p.Id);
            root.transform.SetParent(parent, false);
            bool placeholder = visualOrNull == null;
            GameObject visual = placeholder ? GameObject.CreatePrimitive(PrimitiveType.Capsule) : visualOrNull;
            visual.name = "Visual"; visual.transform.SetParent(root.transform, false);
            if (placeholder)
            {
                visual.transform.localPosition = new Vector3(0, 0.9f, 0); visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                Object.Destroy(visual.GetComponent<Collider>());
                Renderer r = visual.GetComponent<Renderer>(); r.material.color = TeamIdentity.TeamColors[p.Team];
                GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube); nose.name = "FacingMarker"; nose.transform.SetParent(visual.transform, false);
                nose.transform.localPosition = new Vector3(0, 0.35f, 0.55f); nose.transform.localScale = new Vector3(0.3f, 0.2f, 0.5f);
                Object.Destroy(nose.GetComponent<Collider>()); nose.GetComponent<Renderer>().material.color = Color.white;
            }
            TeamIdentity id = root.AddComponent<TeamIdentity>(); id.Team = p.Team; id.PlayerId = p.Id; id.IsHuman = p.Human;
            PlayerView v = root.AddComponent<PlayerView>(); v.Sim = p; v.Visual = visual.transform; v.Anim = visual.GetComponentInChildren<Animator>(); v.Rig = visual.GetComponentInChildren<IPlayerAnimDriver>();
            v.yaw = p.Team == 0 ? 90f : -90f;   // face the net
            v.lastPos = new Vector3((float)p.X, 0, (float)p.Z);
            return v;
        }

        public void Sync(Game g)
        {
            Vector3 pos = new Vector3((float)Sim.X, (float)Sim.Y, (float)Sim.Z);
            Vector3 d = pos - lastPos; d.y = 0;
            if (d.sqrMagnitude > 1e-6f) yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0.25f);   // rotation
            transform.position = pos; transform.rotation = Quaternion.Euler(0, yaw, 0);
            bool animated = Anim != null && Anim.runtimeAnimatorController != null;
            if (animated) { SetF("Speed", d.magnitude / (float)Const.DT); SetB("Grounded", Sim.Y <= 0.001); }
            if (animated || Rig != null)
            {
                for (; shownEvents < g.Events.Count; shownEvents++)
                {
                    GameEvent e = g.Events[shownEvents];
                    if (e.Type == "action" && e.Player == Sim.Id && e.Ok == true)
                    {
                        string a = char.ToUpper(e.Action[0]) + e.Action.Substring(1);
                        if (animated) Trig(a);
                        if (Rig != null) Rig.Trigger(a);
                    }
                }
                if (Sim.Vy > 0.1) { if (animated) Trig("Jump"); if (Rig != null) Rig.Trigger("Jump"); }
            }
            if (Rig != null) Rig.Drive(d.magnitude / Mathf.Max(Time.deltaTime, 1e-4f), Sim.Y <= 0.001, (float)Sim.Y);
            lastPos = pos;
        }
        void SetF(string n, float v) { if (Has(n)) Anim.SetFloat(n, v); }
        void SetB(string n, bool v) { if (Has(n)) Anim.SetBool(n, v); }
        void Trig(string n) { if (Has(n)) Anim.SetTrigger(n); }
        bool Has(string n) { foreach (AnimatorControllerParameter p in Anim.parameters) if (p.name == n) return true; return false; }
    }
}
