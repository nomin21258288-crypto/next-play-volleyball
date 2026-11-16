// UNTESTED in Unity. Camera for the loading screen and lobby: frames the six-player lineup for ANY aspect ratio
// (landscape or portrait phones) and eases between the intro framing and the lobby framing.
using UnityEngine;

namespace NextPlay.App
{
    public sealed class ArenaCamera : MonoBehaviour
    {
        public Vector3 LookAt = new Vector3(-4.7f, 1.35f, 0f);
        public float Distance = 7.0f, Height = 1.55f, HalfWidth = 3.9f, HalfHeight = 1.9f, Ease = 3f, MaxFov = 70f;
        Camera cam; float fov = 30f; Vector3 pos;
        public Vector3 DesiredPosition { get { return new Vector3(LookAt.x - Distance, Height, LookAt.z); } }

        public float FitFov(float aspect)
        {
            float dist = Mathf.Max(0.5f, Distance);
            float half = Mathf.Max(HalfHeight / dist, HalfWidth / (dist * Mathf.Max(0.3f, aspect)));
            return Mathf.Min(MaxFov, 2f * Mathf.Atan(half) * Mathf.Rad2Deg);
        }

        void Awake() { cam = GetComponent<Camera>(); }
        void Start() { pos = DesiredPosition; fov = FitFov(cam.aspect); Apply(); }
        void LateUpdate()
        {
            float k = 1f - Mathf.Exp(-Ease * Time.unscaledDeltaTime);
            pos = Vector3.Lerp(pos, DesiredPosition, k); fov = Mathf.Lerp(fov, FitFov(cam.aspect), k);
            Apply();
        }
        void Apply() { transform.position = pos; transform.LookAt(LookAt); cam.fieldOfView = fov; }
    }
}
