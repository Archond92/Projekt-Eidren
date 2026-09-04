using Eidren.Data;
using Eidren.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
    /// <summary>Rendert die zwei wirklich integrierten Beutel-Prefabs in LOD0.</summary>
    public static class WorldLootSichtCapture
    {
        private const int Width = 1400;
        private const int Height = 900;

        [MenuItem("Eidren/Mid-Poly/Phase 4/Sichtpruefung Weltbeutel")]
        public static void Capture()
        {
            string folder = Environment.GetEnvironmentVariable("EIDREN_CAPTURE_ORDNER");
            if (string.IsNullOrWhiteSpace(folder))
                folder = "Documentation/Etappen/MidPoly/Phase4/InWorld/WorldLootFinal";
            Directory.CreateDirectory(folder);

            List<GameObject> created = new List<GameObject>();
            try
            {
                GameObject cameraObject = New("WorldLootCaptureCamera", created);
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.043f, 0.055f);
                camera.fieldOfView = 32f;

                GameObject keyObject = New("WorldLootCaptureKey", created);
                Light key = keyObject.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.35f;
                keyObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                GameObject fillObject = New("WorldLootCaptureFill", created);
                Light fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.55f;
                fillObject.transform.rotation = Quaternion.Euler(26f, 140f, 0f);
                GameObject rimObject = New("WorldLootCaptureRim", created);
                Light rim = rimObject.AddComponent<Light>();
                rim.type = LightType.Directional;
                rim.intensity = 0.44f;
                rimObject.transform.rotation = Quaternion.Euler(64f, 205f, 0f);

                GameObject world = UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(MidpolyWorldLootMigration.WorldItemPrefabPath));
                created.Add(world);
                world.name = "Capture_SingleWorldItemPouch";
                world.transform.position = new Vector3(-0.62f, 0f, 0f);
                ForceLod0(world);
                ItemDefinition wood = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/Wood.asset");
                world.GetComponent<WorldItemController>().Initialize(wood, 3, "capture.world-item");

                GameObject death = UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(MidpolyWorldLootMigration.DeathBagPrefabPath));
                created.Add(death);
                death.name = "Capture_SingleDeathBag";
                death.transform.position = new Vector3(0.62f, 0f, 0.05f);
                ForceLod0(death);

                Render(camera, folder, "world_loot_integrated_pair.png", created, world,
                    new Vector3(2.15f, 1.48f, -3.10f), new Vector3(0f, 0.34f, 0f));
                death.SetActive(false);
                world.transform.position = Vector3.zero;
                Render(camera, folder, "world_item_single_pouch_integrated.png", created, world,
                    new Vector3(1.10f, 0.86f, -1.65f), new Vector3(0f, 0.20f, 0f));
                death.SetActive(true);
                world.SetActive(false);
                death.transform.position = Vector3.zero;
                Render(camera, folder, "death_bag_single_integrated.png", created, null,
                    new Vector3(1.55f, 1.20f, -2.25f), new Vector3(0f, 0.35f, 0f));
                Debug.Log("[MIDPOLY-000] Weltbeutel-Sichtpruefung geschrieben: " + folder);
            }
            finally
            {
                foreach (GameObject item in created.Where(item => item != null))
                    UnityEngine.Object.DestroyImmediate(item);
            }
        }

        private static GameObject New(string name, ICollection<GameObject> created)
        {
            GameObject result = new GameObject(name);
            created.Add(result);
            return result;
        }

        private static void ForceLod0(GameObject instance)
        {
            LODGroup group = instance.GetComponent<LODGroup>();
            if (group == null) return;
            group.ForceLOD(0);
            group.enabled = false;
            Transform geometry = instance.transform.Find("Geometry_A14");
            foreach (Renderer renderer in geometry.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = HasHierarchyToken(renderer.transform, "LOD0");
        }

        private static void Render(Camera camera, string folder, string filename,
            IEnumerable<GameObject> created, GameObject billboardRoot, Vector3 position, Vector3 target)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            if (billboardRoot != null)
            {
                Transform icon = billboardRoot.transform.Find("ItemIcon");
                Transform quantity = billboardRoot.transform.Find("Quantity");
                if (icon != null) icon.rotation = camera.transform.rotation;
                if (quantity != null) quantity.rotation = camera.transform.rotation;
            }
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
