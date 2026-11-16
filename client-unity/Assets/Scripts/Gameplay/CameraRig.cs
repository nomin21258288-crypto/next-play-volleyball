// UNTESTED. Basic broadcast-style camera behind team 0's baseline (human side), gently tracking the ball in z.
using UnityEngine;

namespace NextPlay.Gameplay
{
    public sealed class CameraRig : MonoBehaviour
    {
        public Transform Ball; public Vector3 Offset = new Vector3(-17f, 8f, 0f); public Vector3 LookAt = new Vector3(0, 1.5f, 0); public float Follow = 0.15f;
        public static CameraRig Create(Transform ball)
        {
            Camera cam = Camera.main; if (cam == null) { GameObject go = new GameObject("Main Camera", typeof(Camera)); go.tag = "MainCamera"; cam = go.GetComponent<Camera>(); }
            cam.fieldOfView = 55f; cam.clearFlags = CameraClearFlags.Skybox;
            CameraRig r = cam.gameObject.AddComponent<CameraRig>(); r.Ball = ball; return r;
        }
        void LateUpdate()
        {
            float bz = Ball != null ? Ball.position.z * Follow * 3f : 0f;
            transform.position = Offset + new Vector3(0, 0, bz);
            transform.LookAt(LookAt + new Vector3(0, 0, bz * 0.5f));
        }
    }
}
