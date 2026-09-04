using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
    /// <summary>Rendert die sechs wirklich integrierten Produktionsstations-Prefabs.</summary>
    public static class ProductionStationSichtCapture
    {
        private const string PrefabFolder = "Assets/_Game/Prefabs/Buildings/Level01";
        private const int Width = 1100;
        private const int Height = 900;

        [MenuItem("Eidren/Mid-Poly/Phase 4/Sichtpruefung Produktionsstationen")]
        public static void Capture()
        {
            string folder = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
            if (string.IsNullOrWhiteSpace(folder))
                folder = "Documentation/Etappen/MidPoly/Phase4/InWorld/ProductionStationsFinal";
            Directory.CreateDirectory(folder);

            List<GameObject> created = new List<GameObject>();
            Material groundMaterial = null;
            try
            {
                Camera camera = New("ProductionStationCaptureCamera", created).AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.067f, 0.078f);
                camera.fieldOfView = 34f;
                Light key = New("ProductionStationCaptureKey", created).AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.32f;
                key.transform.rotation = Quaternion.Euler(43f, 146f, 0f);
                Light fill = New("ProductionStationCaptureFill", created).AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.50f;
                fill.transform.rotation = Quaternion.Euler(31f, -42f, 0f);
                Light rim = New("ProductionStationCaptureRim", created).AddComponent<Light>();
                rim.type = LightType.Directional;
                rim.intensity = 0.44f;
                rim.transform.rotation = Quaternion.Euler(16f, 216f, 0f);

                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "ProductionStationCaptureGround";
                ground.transform.position = new Vector3(0f, -0.07f, 0f);
                ground.transform.localScale = new Vector3(12f, 0.10f, 12f);
                created.Add(ground);
                groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                groundMaterial.color = new Color(0.095f, 0.11f, 0.12f);
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                CaptureOne(camera, folder, "Smelter", new Vector3(2.8f, 2.35f, 4.5f), new Vector3(0f, 0.95f, 0f), created);
                CaptureOne(camera, folder, "Sawmill", new Vector3(2.9f, 2.55f, 4.8f), new Vector3(0f, 1.02f, 0f), created);
                CaptureOne(camera, folder, "Stonecutter", new Vector3(2.8f, 2.15f, 4.2f), new Vector3(0f, 0.66f, 0f), created);
                CaptureOne(camera, folder, "Ropewalk", new Vector3(2.9f, 2.35f, 4.45f), new Vector3(0f, 0.78f, 0f), created);
                CaptureOne(camera, folder, "CookingPot", new Vector3(2.65f, 1.85f, 3.8f), new Vector3(0f, 0.48f, 0f), created);

                GameObject farm = Instance("FarmPlot", created);
                SetFarmState(farm, "VIS_Ready");
                Render(camera, folder, "farmplot_ready_integrated.png", new Vector3(3.45f, 3.0f, 4.55f), new Vector3(0f, 0.22f, 0f));
                farm.SetActive(false);

                string[] names = { "Smelter", "Sawmill", "Stonecutter", "Ropewalk", "CookingPot", "FarmPlot" };
                Vector3[] positions =
                {
                    new Vector3(-2.7f, 0f, 0.75f), new Vector3(-1.55f, 0f, 0.75f), new Vector3(-0.4f, 0f, 0.75f),
                    new Vector3(0.75f, 0f, 0.75f), new Vector3(1.9f, 0f, 0.75f), new Vector3(3.35f, 0f, 0.75f)
                };
                for (int index = 0; index < names.Length; index++)
                {
                    GameObject item = Instance(names[index], created);
                    item.transform.position = positions[index];
                    if (names[index] == "FarmPlot") SetFarmState(item, "VIS_Planted");
                }
                Render(camera, folder, "production_station_family_integrated.png",
                    new Vector3(7.8f, 4.4f, 9.2f), new Vector3(0.25f, 0.9f, 0.65f));
                Debug.Log("[MIDPOLY-000] Produktionsstations-Sichtpruefung geschrieben: " + folder);
            }
            finally
            {
                foreach (GameObject item in created)
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
                if (groundMaterial != null) UnityEngine.Object.DestroyImmediate(groundMaterial);
            }
        }

        private static void CaptureOne(Camera camera, string folder, string name, Vector3 cameraPosition,
            Vector3 target, List<GameObject> created)
        {
            GameObject item = Instance(name, created);
            Render(camera, folder, name.ToLowerInvariant() + "_integrated.png", cameraPosition, target);
            item.SetActive(false);
        }

        private static GameObject New(string name, List<GameObject> created)
        {
            GameObject item = new GameObject(name);
            created.Add(item);
            return item;
        }

        private static GameObject Instance(string name, List<GameObject> created)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/BLD_" + name + "_L01.prefab");
            if (prefab == null) throw new InvalidOperationException("Prefab fehlt: " + name);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = name + "_IntegratedCapture";
            LODGroup group = instance.GetComponent<LODGroup>();
            if (group != null) group.ForceLOD(0);
            created.Add(instance);
            return instance;
        }

        private static void SetFarmState(GameObject farm, string activeName)
        {
            Transform states = farm.transform.Find("Visual_A20");
            if (states == null) throw new InvalidOperationException("FarmPlot-Zustaende fehlen.");
            foreach (Transform child in states)
                child.gameObject.SetActive(child.name == activeName);
        }

        private static void Render(Camera camera, string folder, string filename, Vector3 position, Vector3 target)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            RenderTexture texture = new RenderTexture(Width, Height, 24);
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            Texture2D image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(folder, filename), image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
