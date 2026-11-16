// UNTESTED. Builds the initial 3D court from the SAME constants as the sim (NextPlay.Sim.Court).
// Sim axes: x = court length (net at x=0), y = up, z = width. Unity uses the same mapping (1 unit = 1 m).
using NextPlay.Sim;
using UnityEngine;

namespace NextPlay.Gameplay
{
    public static class CourtBuilder
    {
        static Material Mat(string name, Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
            Material m = new Material(sh) { name = name, color = c };
            return m;
        }
        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m, bool collider = false)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        public static GameObject Build()
        {
            GameObject root = new GameObject("Court");
            float L = (float)Court.HalfLength, W = (float)Court.HalfWidth, NH = (float)Court.NetHeight;
            Material floor = Mat("Floor", new Color(0.78f, 0.55f, 0.32f)), line = Mat("Line", Color.white), zoneA = Mat("TeamA_Area", new Color(0.25f, 0.45f, 0.85f)), zoneB = Mat("TeamB_Area", new Color(0.85f, 0.3f, 0.3f));
            Material pole = Mat("Pole", new Color(0.2f, 0.2f, 0.2f)), net = Mat("Net", new Color(0.95f, 0.95f, 0.95f));

            Box(root.transform, "Ground", new Vector3(0, -0.05f, 0), new Vector3(30, 0.1f, 20), Mat("Ground", new Color(0.2f, 0.4f, 0.25f)), true);
            Box(root.transform, "CourtSurface", new Vector3(0, -0.01f, 0), new Vector3(2 * L + 4, 0.02f, 2 * W + 4), floor);
            // two team areas (tinted halves)
            Box(root.transform, "TeamArea_0", new Vector3(-L / 2, 0.002f, 0), new Vector3(L, 0.004f, 2 * W), zoneA);
            Box(root.transform, "TeamArea_1", new Vector3(L / 2, 0.002f, 0), new Vector3(L, 0.004f, 2 * W), zoneB);
            // boundary + centre lines (5 cm)
            float t = 0.05f, y = 0.008f;
            Box(root.transform, "Line_Left", new Vector3(-L, y, 0), new Vector3(t, 0.004f, 2 * W), line);
            Box(root.transform, "Line_Right", new Vector3(L, y, 0), new Vector3(t, 0.004f, 2 * W), line);
            Box(root.transform, "Line_Near", new Vector3(0, y, -W), new Vector3(2 * L, 0.004f, t), line);
            Box(root.transform, "Line_Far", new Vector3(0, y, W), new Vector3(2 * L, 0.004f, t), line);
            Box(root.transform, "Line_Centre", new Vector3(0, y, 0), new Vector3(t, 0.004f, 2 * W), line);
            // net and poles (net plane at x = 0, top at NetHeight)
            float poleZ = W + 0.5f;
            Box(root.transform, "Pole_A", new Vector3(0, NH / 2 + 0.1f, -poleZ), new Vector3(0.1f, NH + 0.2f, 0.1f), pole);
            Box(root.transform, "Pole_B", new Vector3(0, NH / 2 + 0.1f, poleZ), new Vector3(0.1f, NH + 0.2f, 0.1f), pole);
            Box(root.transform, "Net", new Vector3(0, NH - 0.5f, 0), new Vector3(0.03f, 1.0f, 2 * poleZ), net);
            Box(root.transform, "NetTape", new Vector3(0, NH, 0), new Vector3(0.05f, 0.07f, 2 * poleZ), line);

            // spawn markers (visual aids; real spawns come from Game: BaseX/BaseZ and serve spot)
            for (int team = 0; team < 2; team++)
                for (int i = 0; i < 2; i++)
                {
                    GameObject m = new GameObject("Spawn_Team" + team + "_P" + i); m.transform.SetParent(root.transform, false);
                    m.transform.localPosition = new Vector3((float)(Game.SideSign(team) * Game.BaseX[i]), 0, (float)Game.BaseZ[i]);
                }
            for (int team = 0; team < 2; team++)
            {
                GameObject s = new GameObject("BallSpawn_Team" + team); s.transform.SetParent(root.transform, false);
                s.transform.localPosition = new Vector3(Game.SideSign(team) * 9.4f, 1.1f, 0);
            }

            // lighting: one directional light (+ soft ambient)
            GameObject sun = new GameObject("Sun"); sun.transform.SetParent(root.transform, false);
            Light l = sun.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.1f; l.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52f, -30f, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);
            return root;
        }
    }
}
