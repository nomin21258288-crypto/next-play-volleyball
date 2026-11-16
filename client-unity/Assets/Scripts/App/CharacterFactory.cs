// UNTESTED in Unity. Builds one 3D volleyball player from primitives (feet at y=0, facing +z, ~1.83 m tall).
// FACE: the character's OWN original photo (cropped + soft mask by tools/make_branding_assets.py, nothing repainted) is projected
// onto a front cap of the head sphere. No face is generated, edited, swapped or shared between characters.
// This is a game-ready placeholder likeness, not a final scan: a human-approved 3D likeness workflow is still open.
// Clothing: volleyball kit only, NO numbers.
using System.Collections.Generic;
using NextPlay.Core;
using UnityEngine;

namespace NextPlay.App
{
    public static class FaceLibrary
    {
        static readonly Dictionary<string, Texture2D> faces = new Dictionary<string, Texture2D>();
        public static Texture2D Get(string name)
        {
            Texture2D t; if (faces.TryGetValue(name, out t) && t != null) return t;
            t = Resources.Load<Texture2D>("Characters/" + name);
            if (t != null) faces[name] = t; else Debug.LogError("NextPlay: missing face texture Resources/Characters/" + name);
            return t;
        }
        public static void Put(string name, Texture2D t) { faces[name] = t; }
    }

    public static class CharacterFactory
    {
        static Mesh faceCap;

        static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }

        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            Kill(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        static Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            GameObject g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = pos; return g.transform;
        }

        // Front hemisphere of Unity's sphere mesh with planar (front-projected) UVs: u = x + 0.5, v = y + 0.5.
        static Mesh FaceCap()
        {
            if (faceCap != null) return faceCap;
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh src = tmp.GetComponent<MeshFilter>().sharedMesh; Kill(tmp);
            Vector3[] v = src.vertices; Vector3[] n = src.normals; int[] t = src.triangles;
            int[] map = new int[v.Length]; for (int i = 0; i < map.Length; i++) map[i] = -1;
            List<Vector3> nv = new List<Vector3>(); List<Vector3> nn = new List<Vector3>(); List<Vector2> uv = new List<Vector2>(); List<int> nt = new List<int>();
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                if (v[t[i]].z < 0.02f || v[t[i + 1]].z < 0.02f || v[t[i + 2]].z < 0.02f) continue;
                for (int k = 0; k < 3; k++)
                {
                    int s = t[i + k];
                    if (map[s] < 0) { map[s] = nv.Count; nv.Add(v[s]); nn.Add(n[s]); uv.Add(new Vector2(v[s].x + 0.5f, v[s].y + 0.5f)); }
                    nt.Add(map[s]);
                }
            }
            Mesh m = new Mesh(); m.name = "FaceCap";
            m.SetVertices(nv); m.SetNormals(nn); m.SetUVs(0, uv); m.SetTriangles(nt, 0);
            m.RecalculateBounds();
            faceCap = m; return m;
        }

        public static GameObject Build(CharacterDef def, Texture2D face, Color jersey, Color accent)
        {
            Color skin = UiKit.Hex(def.skinColor, new Color(0.6f, 0.4f, 0.3f)), hair = UiKit.Hex(def.hairColor, new Color(0.15f, 0.1f, 0.1f));
            Material mJersey = ShaderLib.Lit(jersey), mAccent = ShaderLib.Lit(accent), mShorts = ShaderLib.Lit(new Color(0.04f, 0.07f, 0.2f)),
                     mSkin = ShaderLib.Lit(skin, 0.3f), mHair = ShaderLib.Lit(hair, 0.3f), mShoe = ShaderLib.Lit(new Color(0.95f, 0.95f, 0.95f));

            GameObject root = new GameObject("Character_" + def.id + "_" + def.roleLabel);
            CharacterRig rig = root.AddComponent<CharacterRig>();
            Transform hips = Pivot(root.transform, "Hips", new Vector3(0, 0.92f, 0)); rig.Hips = hips;
            Transform spine = Pivot(hips, "Spine", Vector3.zero); rig.Spine = spine;

            // torso (jersey) + chest sash. NO numbers or text on the kit.
            Prim(PrimitiveType.Capsule, spine, "Torso", new Vector3(0, 0.29f, 0), new Vector3(0.40f, 0.31f, 0.23f), mJersey);
            Prim(PrimitiveType.Cube, spine, "Sash", new Vector3(0, 0.34f, 0.113f), new Vector3(0.31f, 0.045f, 0.012f), mAccent);
            Prim(PrimitiveType.Cylinder, spine, "Neck", new Vector3(0, 0.63f, 0), new Vector3(0.10f, 0.05f, 0.10f), mSkin);

            Transform head = Pivot(spine, "Head", new Vector3(0, 0.78f, 0)); rig.Head = head;
            Vector3 headScale = new Vector3(0.20f, 0.26f, 0.23f);
            Prim(PrimitiveType.Sphere, head, "Skull", Vector3.zero, headScale, mHair);
            GameObject cap = new GameObject("FaceCap", typeof(MeshFilter), typeof(MeshRenderer)); cap.transform.SetParent(head, false);
            cap.transform.localScale = headScale * 1.012f;
            cap.GetComponent<MeshFilter>().sharedMesh = FaceCap();
            MeshRenderer cr = cap.GetComponent<MeshRenderer>(); cr.sharedMaterial = ShaderLib.FaceDecal(face); cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // arms: shoulder -> elbow -> hand
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                Transform arm = Pivot(spine, side == 0 ? "ArmL" : "ArmR", new Vector3(0.23f * sx, 0.53f, 0));
                Prim(PrimitiveType.Capsule, arm, "Sleeve", new Vector3(0, -0.07f, 0), new Vector3(0.11f, 0.08f, 0.11f), mJersey);
                Prim(PrimitiveType.Capsule, arm, "UpperArm", new Vector3(0, -0.19f, 0), new Vector3(0.085f, 0.11f, 0.085f), mSkin);
                Transform elbow = Pivot(arm, side == 0 ? "ElbowL" : "ElbowR", new Vector3(0, -0.30f, 0));
                Prim(PrimitiveType.Capsule, elbow, "Forearm", new Vector3(0, -0.14f, 0), new Vector3(0.07f, 0.14f, 0.07f), mSkin);
                Prim(PrimitiveType.Sphere, elbow, "Hand", new Vector3(0, -0.30f, 0), new Vector3(0.085f, 0.095f, 0.085f), mSkin);
                if (side == 0) { rig.ArmL = arm; rig.ElbowL = elbow; } else { rig.ArmR = arm; rig.ElbowR = elbow; }
            }
            // legs: hip -> knee -> foot
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                Transform hip = Pivot(hips, side == 0 ? "HipL" : "HipR", new Vector3(0.10f * sx, 0, 0));
                Prim(PrimitiveType.Capsule, hip, "Thigh", new Vector3(0, -0.20f, 0), new Vector3(0.155f, 0.21f, 0.155f), mShorts);
                Transform knee = Pivot(hip, side == 0 ? "KneeL" : "KneeR", new Vector3(0, -0.42f, 0));
                Prim(PrimitiveType.Capsule, knee, "Shin", new Vector3(0, -0.22f, 0), new Vector3(0.105f, 0.22f, 0.105f), mSkin);
                Prim(PrimitiveType.Cube, knee, "Shoe", new Vector3(0, -0.465f, 0.06f), new Vector3(0.11f, 0.07f, 0.26f), mShoe);
                if (side == 0) { rig.HipL = hip; rig.KneeL = knee; } else { rig.HipR = hip; rig.KneeR = knee; }
            }
            return root;
        }
    }
}
