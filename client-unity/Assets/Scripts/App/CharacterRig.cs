// UNTESTED in Unity. Procedural animation for the primitive-built characters: no Animator controller or clips needed.
// Driven by PlayerView (speed / grounded / sim action events) in a match, or set to Lineup (arms crossed) on the loading screen and lobby.
// Angles are degrees. Unity rotation about +X swings a hanging limb BACKWARD; negative swings it forward/up.
using System.Collections.Generic;
using NextPlay.Gameplay;
using UnityEngine;

namespace NextPlay.App
{
    public sealed class CharacterRig : MonoBehaviour, IPlayerAnimDriver
    {
        public Transform Hips, Spine, Head, ArmL, ArmR, ElbowL, ElbowR, HipL, HipR, KneeL, KneeR;
        public bool Lineup;            // crossed-arms stance (loading screen); false = relaxed idle
        public float PhaseOffset;      // de-synchronises idle breathing between characters
        public float HipsRestY = 0.92f;

        const int LEAN = 0, HEADX = 1, ALX = 2, ALZ = 3, ARX = 4, ARZ = 5, EL = 6, ER = 7, HL = 8, HR = 9, KL = 10, KR = 11, CROUCH = 12, N = 13;
        static float[] P(float lean, float head, float alx, float alz, float arx, float arz, float el, float er, float hl, float hr, float kl, float kr, float crouch)
        { return new[] { lean, head, alx, alz, arx, arz, el, er, hl, hr, kl, kr, crouch }; }

        sealed class Key { public float T; public float[] Pose; public Key(float t, float[] p) { T = t; Pose = p; } }
        sealed class Act { public float Dur; public Key[] Keys; }

        static readonly float[] Idle = P(0, 0, -4, -6, -4, 6, -8, -8, 0, 0, 3, 3, 0);
        static readonly float[] Crossed = P(0, 0, -35, 28, -35, -28, -105, -105, 0, 0, 2, 2, 0);
        static readonly float[] Air = P(4, -4, -150, -12, -150, 12, -15, -15, -12, -12, 35, 35, 0);

        static readonly Dictionary<string, Act> Acts = new Dictionary<string, Act>
        {
            { "serve", new Act { Dur = 0.7f, Keys = new[] { new Key(0f, Idle), new Key(0.2f, P(-5, -10, -160, -10, -40, 8, -10, -90, 0, 0, 5, 5, 0.02f)), new Key(0.38f, P(8, 5, -150, -10, -175, 10, -5, -5, 0, 0, 0, 0, 0)), new Key(0.55f, P(14, 10, -40, -10, -60, 10, -20, -20, 0, 0, 0, 0, 0)), new Key(0.7f, Idle) } } },
            { "pass",  new Act { Dur = 0.5f, Keys = new[] { new Key(0f, Idle), new Key(0.15f, P(18, 10, -50, 10, -50, -10, 0, 0, -10, -10, 25, 25, 0.12f)), new Key(0.35f, P(12, 8, -70, 10, -70, -10, 0, 0, -8, -8, 20, 20, 0.08f)), new Key(0.5f, Idle) } } },
            { "set",   new Act { Dur = 0.45f, Keys = new[] { new Key(0f, Idle), new Key(0.12f, P(-5, -8, -150, -15, -150, 15, -70, -70, 0, 0, 5, 5, 0.03f)), new Key(0.3f, P(-2, -5, -165, -12, -165, 12, -30, -30, 0, 0, 3, 3, 0.0f)), new Key(0.45f, Idle) } } },
            { "spike", new Act { Dur = 0.5f, Keys = new[] { new Key(0f, Air), new Key(0.12f, P(-8, 0, -150, -20, -210, 15, -10, -80, -10, -10, 30, 30, 0)), new Key(0.25f, P(18, 8, -60, -12, -170, 10, -10, -5, -10, -10, 25, 25, 0)), new Key(0.4f, P(20, 10, -30, -10, -40, 8, -10, -20, -8, -8, 25, 25, 0)), new Key(0.5f, Air) } } },
            { "block", new Act { Dur = 0.6f, Keys = new[] { new Key(0f, Idle), new Key(0.1f, P(5, -5, -175, -8, -175, 8, 0, 0, 0, 0, 10, 10, 0)), new Key(0.45f, P(5, -5, -175, -8, -175, 8, 0, 0, 0, 0, 10, 10, 0)), new Key(0.6f, Idle) } } },
        };

        readonly float[] cur = new float[N];
        float speed; bool grounded = true; float runPhase;
        Act act; float actT;
        bool inited;

        public void Drive(float speed, bool grounded, float height) { this.speed = speed; this.grounded = grounded; }

        public void Trigger(string action)
        {
            if (string.IsNullOrEmpty(action)) return;
            Act a; if (!Acts.TryGetValue(action.ToLowerInvariant(), out a)) return;   // "Jump" has no clip: the airborne pose covers it
            act = a; actT = 0f;
        }

        float[] BasePose()
        {
            float[] b = new float[N];
            float[] src = Lineup ? Crossed : (grounded ? Idle : Air);
            for (int i = 0; i < N; i++) b[i] = src[i];
            if (grounded && !Lineup && speed > 0.4f)
            {
                float amp = Mathf.Clamp01(speed / 5.2f), s = Mathf.Sin(runPhase);
                b[HL] = -s * 40f * amp; b[HR] = s * 40f * amp;
                b[KL] = Mathf.Max(0f, -s) * 55f * amp + 6f; b[KR] = Mathf.Max(0f, s) * 55f * amp + 6f;
                b[ALX] = s * 35f * amp; b[ARX] = -s * 35f * amp; b[EL] = b[ER] = -35f * amp - 8f;
                b[LEAN] = 10f * amp;
            }
            return b;
        }

        float[] ActPose(Act a, float t)
        {
            float[] r = new float[N];
            int k = 0; while (k < a.Keys.Length - 2 && t > a.Keys[k + 1].T) k++;
            Key k0 = a.Keys[k], k1 = a.Keys[k + 1];
            float f = Mathf.Clamp01((t - k0.T) / Mathf.Max(1e-4f, k1.T - k0.T));
            for (int i = 0; i < N; i++) r[i] = Mathf.Lerp(k0.Pose[i], k1.Pose[i], f);
            return r;
        }

        void LateUpdate()
        {
            if (Hips == null) return;
            float dt = Time.deltaTime;
            runPhase += dt * (6f + speed * 1.2f);
            float[] target = BasePose();
            if (act != null)
            {
                actT += dt;
                if (actT >= act.Dur) act = null;
                else
                {
                    float w = Mathf.Clamp01(Mathf.Min(actT / 0.06f, (act.Dur - actT) / 0.08f));
                    float[] ap = ActPose(act, actT);
                    for (int i = 0; i < N; i++) target[i] = Mathf.Lerp(target[i], ap[i], w);
                }
            }
            float blend = inited ? 1f - Mathf.Exp(-16f * dt) : 1f;
            inited = true;
            for (int i = 0; i < N; i++) cur[i] += (target[i] - cur[i]) * blend;

            float bob = Mathf.Sin(Time.time * 1.8f + PhaseOffset) * 0.004f;
            Hips.localPosition = new Vector3(0f, HipsRestY - cur[CROUCH] + bob, 0f);
            Spine.localRotation = Quaternion.Euler(cur[LEAN], 0f, 0f);
            Head.localRotation = Quaternion.Euler(cur[HEADX], 0f, 0f);
            ArmL.localRotation = Quaternion.Euler(cur[ALX], 0f, cur[ALZ]);
            ArmR.localRotation = Quaternion.Euler(cur[ARX], 0f, cur[ARZ]);
            ElbowL.localRotation = Quaternion.Euler(cur[EL], 0f, 0f);
            ElbowR.localRotation = Quaternion.Euler(cur[ER], 0f, 0f);
            HipL.localRotation = Quaternion.Euler(cur[HL], 0f, 0f);
            HipR.localRotation = Quaternion.Euler(cur[HR], 0f, 0f);
            KneeL.localRotation = Quaternion.Euler(cur[KL], 0f, 0f);
            KneeR.localRotation = Quaternion.Euler(cur[KR], 0f, 0f);
        }
    }
}
