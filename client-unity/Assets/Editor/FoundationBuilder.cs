// UNTESTED editor utility: NextPlay > Create Foundation (prefabs + materials + Court scene).
// Generates assets through Unity's own APIs because hand-written .prefab/.unity YAML would be unreliable.
using NextPlay.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NextPlay.EditorTools
{
    public static class FoundationBuilder
    {
        [MenuItem("NextPlay/Create Foundation (prefabs + Court scene)")]
        public static void Create()
        {
            Material[] mats = new Material[2];
            for (int t = 0; t < 2; t++)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
                mats[t] = new Material(sh) { color = TeamIdentity.TeamColors[t] };
                AssetDatabase.CreateAsset(mats[t], "Assets/Materials/Team" + t + ".mat");
            }
            GameObject[] players = new GameObject[2];
            for (int t = 0; t < 2; t++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = "PlayerPlaceholder_Team" + t;
                go.transform.localPosition = new Vector3(0, 0.9f, 0); go.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = mats[t];
                go.AddComponent<Animator>();   // animation slot: assign an AnimatorController when final characters exist
                players[t] = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/PlayerPlaceholder_Team" + t + ".prefab"); Object.DestroyImmediate(go);
            }
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name = "Ball"; ball.transform.localScale = Vector3.one * 0.21f;
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            GameObject ballPrefab = PrefabUtility.SaveAsPrefabAsset(ball, "Assets/Prefabs/Ball.prefab"); Object.DestroyImmediate(ball);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject m = new GameObject("Match"); MatchController mc = m.AddComponent<MatchController>();
            mc.PlayerPrefabTeam0 = players[0]; mc.PlayerPrefabTeam1 = players[1]; mc.BallPrefab = ballPrefab;
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Court.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Court.unity", true) };
            AssetDatabase.SaveAssets(); Debug.Log("NextPlay foundation created. Press Play in Assets/Scenes/Court.unity.");
        }
    }
}
