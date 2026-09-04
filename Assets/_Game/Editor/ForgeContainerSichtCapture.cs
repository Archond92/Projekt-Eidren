using Eidren.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
    /// <summary>Rendert die tatsaechlich integrierten Schmiedecontainer-Prefabs.</summary>
    public static class ForgeContainerSichtCapture
    {
        private const int Width = 1400;
        private const int Height = 850;

        [MenuItem("Eidren/Mid-Poly/Phase 4/Sichtpruefung Schmiedecontainer")]
        public static void Capture()
        {
            string folder = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
            if (string.IsNullOrWhiteSpace(folder))
                folder = "Documentation/Etappen/MidPoly/Phase4/InWorld/ForgeContainersFinal";
            Directory.CreateDirectory(folder);

            GameObject cameraObject = new GameObject("ForgeContainerCaptureCamera");
            GameObject keyObject = new GameObject("ForgeContainerCaptureKey");
            GameObject fillObject = new GameObject("ForgeContainerCaptureFill");
            GameObject rimObject = new GameObject("ForgeContainerCaptureRim");
            List<GameObject> instances = new List<GameObject>();
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.043f, 0.055f);
                camera.fieldOfView = 34f;
                Light key = keyObject.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.35f;
                keyObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                Light fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.52f;
                fillObject.transform.rotation = Quaternion.Euler(26f, 138f, 0f);
                Light rim = rimObject.AddComponent<Light>();
                rim.type = LightType.Directional;
                rim.intensity = 0.46f;
                rimObject.transform.rotation = Quaternion.Euler(62f, 208f, 0f);

                for (int index = 0; index < MidpolyForgeContainerMigration.Names.Length; index++)
                {
                    string name = MidpolyForgeContainerMigration.Names[index];
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        MidpolyForgeContainerMigration.PrefabPathFor(name));
                    GameObject instance = UnityEngine.Object.Instantiate(prefab);
                    instance.name = "Capture_" + name;
                    int row = index / 4;
                    int column = index % 4;
                    instance.transform.position = new Vector3((column - 1.5f) * 1.72f, 0f, row * 1.72f);
                    LODGroup group = instance.GetComponent<LODGroup>();
                    if (group != null)
                    {
                        group.ForceLOD(0);
                        // Camera.Render() wertet im headless Editor die LODGroup
                        // nicht verlaesslich gegen die manuelle Kamera aus. Fuer
                        // die reine Sichtpruefung wird deshalb LOD0 explizit
                        // geschaltet; am gespeicherten Prefab aendert sich nichts.
                        group.enabled = false;
                        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                            renderer.enabled = HasHierarchyToken(renderer.transform, "LOD0")
                                || HasHierarchyToken(renderer.transform, "ContactShadow");
                    }
                    instance.GetComponent<WorldChestVisual>().Apply(WorldChestVisualState.Closed);
                    instances.Add(instance);
                }

                Render(camera, folder, "forge_container_family_integrated_closed.png",
                    new Vector3(6.7f, 4.5f, -8.7f), new Vector3(0f, 0.48f, 0.85f));
                foreach (GameObject instance in instances)
                    instance.GetComponent<WorldChestVisual>().Apply(WorldChestVisualState.Opened);
                Render(camera, folder, "forge_container_family_integrated_open.png",
                    new Vector3(7.1f, 5.6f, -9.5f), new Vector3(0f, 0.72f, 0.85f));

                for (int index = 0; index < instances.Count; index++)
                {
                    for (int other = 0; other < instances.Count; other++) instances[other].SetActive(other == index);
                    GameObject instance = instances[index];
                    instance.transform.position = Vector3.zero;
                    WorldChestVisual visual = instance.GetComponent<WorldChestVisual>();
                    visual.Apply(WorldChestVisualState.Closed);
                    Render(camera, folder, "forge_container_" + MidpolyForgeContainerMigration.Names[index].ToLowerInvariant() + "_closed.png",
                        new Vector3(1.9f, 1.45f, -2.75f), new Vector3(0f, 0.42f, 0f));
                    visual.Apply(WorldChestVisualState.Opened);
                    Render(camera, folder, "forge_container_" + MidpolyForgeContainerMigration.Names[index].ToLowerInvariant() + "_open.png",
                        new Vector3(2.1f, 1.85f, -3.05f), new Vector3(0f, 0.60f, 0f));
                }
                Debug.Log("[MIDPOLY-000] Schmiedecontainer-Sichtpruefung geschrieben: " + folder);
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

        private static bool HasHierarchyToken(Transform transform, string token)
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (current.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
