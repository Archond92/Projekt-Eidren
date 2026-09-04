using Eidren.Presentation;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// F31-008: Rüstet das Tür-Prefab mit Blatt-Angel, Spieler-Sensor und
	/// <see cref="BuildingDoorView"/> aus. Das Blatt (Planken, Strebe, Griff,
	/// Schloss, Scharniere) wandert unter einen Angel-Pivot an der
	/// Scharnierkante; Rahmen, Joints, Caps und Kontaktschatten bleiben an
	/// der Wurzel.
	/// </summary>
	public static class BuildingDoorContentBuilder
	{
		private const string PrefabPath = "Assets/_Game/Prefabs/Buildings/Level01/BLD_Door_L01.prefab";

		private static readonly string[] BladeParts =
		{
			"DoorPlank_0", "DoorPlank_1", "DoorPlank_2", "DoorPlank_3", "DoorPlank_4",
			"DiagonalBrace", "Griff", "LockPlate", "Scharnierband_0", "Scharnierband_1"
		};

		[MenuItem("Eidren/Buildings/Build Door Behaviour (F31-008)")]
		public static void BuildDoorBehaviour()
		{
			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				// Fabrik-Vertrag: alle Geometrieteile bleiben unter
				// Geometry_A14 (VisualAssetTests zaehlt dort) — die Angel
				// haengt deshalb IN der Gruppe, nicht an der Wurzel.
				Transform geometry = FindDeep(root.transform, "Geometry_A14");
				if (geometry == null)
				{
					throw new InvalidDataException("Tür hat keine Geometry_A14-Gruppe.");
				}
				Transform hinge = FindDeep(root.transform, "DoorBlade");
				if (hinge == null)
				{
					GameObject hingeObject = new GameObject("DoorBlade");
					hinge = hingeObject.transform;
				}
				if (hinge.parent != geometry)
				{
					hinge.SetParent(geometry, worldPositionStays: false);
				}
				// Angel an der Scharnierkante: Tür spannt x -0,5..0,5,
				// die Scharnierbänder sitzen bei x = -0,35.
				hinge.localPosition = new Vector3(-0.5f, 0f, 0f);
				hinge.localRotation = Quaternion.identity;
				foreach (string partName in BladeParts)
				{
					Transform part = FindDeep(root.transform, partName);
					if (part == null)
					{
						throw new InvalidDataException("Türteil fehlt: " + partName);
					}
					if (part.parent != hinge)
					{
						part.SetParent(hinge, worldPositionStays: true);
					}
				}
				BoxCollider blocking = root.GetComponent<BoxCollider>();
				if (blocking == null)
				{
					throw new InvalidDataException("Tür hat keinen Sperr-Collider an der Wurzel.");
				}
				Transform sensorTransform = root.transform.Find("DoorSensor");
				if (sensorTransform == null)
				{
					GameObject sensorObject = new GameObject("DoorSensor");
					sensorTransform = sensorObject.transform;
					sensorTransform.SetParent(root.transform, worldPositionStays: false);
				}
				sensorTransform.localPosition = Vector3.zero;
				SphereCollider sensor = sensorTransform.GetComponent<SphereCollider>();
				if (sensor == null)
				{
					sensor = sensorTransform.gameObject.AddComponent<SphereCollider>();
				}
				sensor.isTrigger = true;
				sensor.center = new Vector3(0f, 1f, 0f);
				sensor.radius = 2.1f;
				BuildingDoorView view = sensorTransform.GetComponent<BuildingDoorView>();
				if (view == null)
				{
					view = sensorTransform.gameObject.AddComponent<BuildingDoorView>();
				}
				view.ConfigureReferences(hinge, blocking);
				EditorUtility.SetDirty(view);
				PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			AssetDatabase.SaveAssets();
			Debug.Log("Eidren: Türverhalten autoriert — Blatt-Angel, Sensor, BuildingDoorView.");
		}

		private static Transform FindDeep(Transform root, string childName)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child.name == childName)
				{
					return child;
				}
			}
			return null;
		}
	}
}
