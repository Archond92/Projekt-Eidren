using Eidren.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
    /// <summary>Rendert die wirklich integrierten Weltkisten in geschlossenem und geoeffnetem Zustand.</summary>
    public static class WorldChestSichtCapture
    {
        private const int Width = 1200;
        private const int Height = 760;
        private static readonly string[] Names = { "Common", "Guarded", "Hidden" };

        [MenuItem("Eidren/Mid-Poly/Phase 4/Sichtpruefung Weltkisten")]
        public static void Capture()
        {
            string folder = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
            if (string.IsNullOrWhiteSpace(folder))
                folder = "Documentation/Etappen/MidPoly/Phase4/InWorld/WorldChestsFinal";
            Directory.CreateDirectory(folder);

            GameObject cameraObject = new GameObject("WorldChestCaptureCamera");
            GameObject keyObject = new GameObject("WorldChestCaptureKey");
            GameObject fillObject = new GameObject("WorldChestCaptureFill");
            GameObject rimObject = new GameObject("WorldChestCaptureRim");
            List<GameObject> instances = new List<GameObject>();
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.065f, 0.078f);
                camera.fieldOfView = 34f;
                Light key = keyObject.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.25f;
                keyObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                Light fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.48f;
                fillObject.transform.rotation = Quaternion.Euler(28f, 142f, 0f);
                Light rim = rimObject.AddComponent<Light>();
                rim.type = LightType.Directional;
                rim.intensity = 0.38f;
                rimObject.transform.rotation = Quaternion.Euler(62f, 205f, 0f);

                for (int index = 0; index < Names.Length; index++)
                {
                    string name = Names[index];
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_" + name + ".prefab");
                    GameObject instance = UnityEngine.Object.Instantiate(prefab);
                    instance.name = "Capture_" + name;
                    instance.transform.position = new Vector3((index - 1) * 1.72f, 0f, 0f);
                    LODGroup group = instance.GetComponent<LODGroup>();
                    if (group != null) group.ForceLOD(0);
                    instance.GetComponent<WorldChestVisual>().Apply(WorldChestVisualState.Closed);
                    instances.Add(instance);
                }
                Render(camera, folder, "world_chest_family_integrated.png",
                    new Vector3(4.5f, 3.1f, -6.4f), new Vector3(0f, 0.34f, 0f));

                for (int index = 0; index < instances.Count; index++)
                {
                    for (int other = 0; other < instances.Count; other++)
                        instances[other].SetActive(other == index);
                    GameObject instance = instances[index];
                    instance.transform.position = Vector3.zero;
                    WorldChestVisual visual = instance.GetComponent<WorldChestVisual>();
                    visual.Apply(WorldChestVisualState.Closed);
                    Render(camera, folder, "world_chest_" + Names[index].ToLowerInvariant() + "_closed.png",
                        new Vector3(1.55f, 1.18f, -2.35f), new Vector3(0f, 0.34f, 0f));
                    visual.Apply(WorldChestVisualState.Opened);
                    Render(camera, folder, "world_chest_" + Names[index].ToLowerInvariant() + "_open.png",
                        new Vector3(1.75f, 1.52f, -2.55f), new Vector3(0f, 0.50f, 0f));
                    visual.Apply(WorldChestVisualState.Closed);
                }
                Debug.Log("[MIDPOLY-000] Weltkisten-Sichtpruefung geschrieben: " + folder);
            }
            finally
            {
                foreach (GameObject instance in instances) UnityEngine.Object.DestroyImmediate(instance);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
                UnityEngine.Object.DestroyImmediate(rimObject);
            }
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
