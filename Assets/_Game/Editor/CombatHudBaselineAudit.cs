using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Eidren.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Editor
{
	/// <summary>HUD-100: liest den Iststand, ohne das autorierte Prefab zu speichern.</summary>
	public static class CombatHudBaselineAudit
	{
		public const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

		[Serializable]
		public sealed class Snapshot
		{
			public string utc;
			public string unityVersion;
			public string prefabSha256;
			public string evidence = "Prefab-Geometrie mit simulierter Safe Area; kein Geraete- oder Screenshotnachweis.";
			public Vector2 referenceResolution;
			public float matchWidthOrHeight;
			public int scaleMode;
			public int width, height;
			public Rect safeAreaPixels;
			public Rect safeAreaLocal;
			public List<Node> hierarchy = new List<Node>();
			public List<Binding> bindings = new List<Binding>();
			public List<Control> controls = new List<Control>();
		}

		[Serializable]
		public sealed class Node
		{
			public string path;
			public bool active;
			public Vector2 anchorMin, anchorMax, position, size, pivot;
		}

		[Serializable]
		public sealed class Binding
		{
			public string owner, field, target;
			public bool missing;
		}

		[Serializable]
		public sealed class Control
		{
			public string name, path;
			public Rect boundsInSafeArea;
		}

		public static Snapshot Read(int width, int height, Rect safeAreaPixels)
		{
			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				CombatHUD hud = root.GetComponent<CombatHUD>();
				CanvasScaler scaler = root.GetComponent<CanvasScaler>();
				var snapshot = new Snapshot
				{
					utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
					prefabSha256 = HashPrefab(), width = width, height = height,
					safeAreaPixels = safeAreaPixels, referenceResolution = scaler.referenceResolution,
					matchWidthOrHeight = scaler.matchWidthOrHeight, scaleMode = (int)scaler.uiScaleMode
				};
				foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
					snapshot.hierarchy.Add(new Node
					{
						path = PathOf(rect, root.transform), active = rect.gameObject.activeSelf,
						anchorMin = rect.anchorMin, anchorMax = rect.anchorMax,
						position = rect.anchoredPosition, size = rect.sizeDelta, pivot = rect.pivot
					});
				ReadBindings(root, snapshot);

				// Layout im isolierten Prefab-Inhalt auswerten. Kein Screen- oder
				// PlayerSettings-Eingriff, kein SaveAsPrefabAsset, keine Laufzeitmigration.
				scaler.enabled = false;
				hud.SafeArea.enabled = false;
				hud.Canvas.renderMode = RenderMode.WorldSpace;
				float scale = Mathf.Pow(width / scaler.referenceResolution.x, 1f - scaler.matchWidthOrHeight)
					* Mathf.Pow(height / scaler.referenceResolution.y, scaler.matchWidthOrHeight);
				RectTransform canvasRect = root.GetComponent<RectTransform>();
				canvasRect.localScale = Vector3.one;
				canvasRect.sizeDelta = new Vector2(width / scale, height / scale);
				RectTransform safeRect = hud.SafeArea.GetComponent<RectTransform>();
				SafeAreaPanel.CalculateAnchors(safeAreaPixels, width, height, out Vector2 min, out Vector2 max);
				safeRect.anchorMin = min;
				safeRect.anchorMax = max;
				safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
				canvasRect.ForceUpdateRectTransforms();
				snapshot.safeAreaLocal = safeRect.rect;
				var corners = new Vector3[4];
				foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
				{
					if (rect.GetComponent<Selectable>() == null && rect.GetComponent<InteractionButton>() == null) continue;
					rect.GetWorldCorners(corners);
					Vector3 lower = safeRect.InverseTransformPoint(corners[0]);
					Vector3 upper = safeRect.InverseTransformPoint(corners[2]);
					snapshot.controls.Add(new Control
					{
						name = rect.name, path = PathOf(rect, root.transform),
						boundsInSafeArea = Rect.MinMaxRect(lower.x, lower.y, upper.x, upper.y)
					});
				}
				return snapshot;
			}
			finally { PrefabUtility.UnloadPrefabContents(root); }
		}

		[MenuItem("Eidren/HUD/Export HUD-100 Baseline")]
		public static void Export()
		{
			string directory = Path.Combine("TestResults-Archiv", "HUD100", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
			Directory.CreateDirectory(directory);
			Write(directory, "16x9", 1920, 1080, new Rect(0, 0, 1920, 1080));
			Write(directory, "19_5x9-safearea", 2340, 1080, new Rect(100, 24, 2156, 1032));
			Write(directory, "20x9-safearea", 2400, 1080, new Rect(120, 24, 2184, 1032));
			Write(directory, "16x10", 1920, 1200, new Rect(0, 0, 1920, 1200));
			Debug.Log("HUD100 Baseline: " + Path.GetFullPath(directory));
		}

		private static void Write(string directory, string name, int width, int height, Rect safeArea)
		{
			File.WriteAllText(Path.Combine(directory, name + ".json"), JsonUtility.ToJson(Read(width, height, safeArea), true));
		}

		private static void ReadBindings(GameObject root, Snapshot snapshot)
		{
			foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
			{
				if (component == null) throw new InvalidOperationException("HUD-Prefab enthaelt ein fehlendes Script.");
				using (var serialized = new SerializedObject(component))
				{
					SerializedProperty property = serialized.GetIterator();
					while (property.NextVisible(true))
					{
						if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
						UnityEngine.Object target = property.objectReferenceValue;
						snapshot.bindings.Add(new Binding
						{
							owner = PathOf(component.transform, root.transform) + ":" + component.GetType().Name,
							field = property.propertyPath,
							target = target == null ? "" : target is Component bound ? PathOf(bound.transform, root.transform) : AssetDatabase.GetAssetPath(target) + ":" + target.name,
							missing = target == null && property.objectReferenceInstanceIDValue != 0
						});
					}
				}
			}
		}

		private static string PathOf(Transform target, Transform root)
		{
			string path = target.name;
			while (target != root && target.parent != null) { target = target.parent; path = target.name + "/" + path; }
			return path;
		}

		private static string HashPrefab()
		{
			using (var hash = SHA256.Create())
				return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(PrefabPath))).Replace("-", "").ToLowerInvariant();
		}
	}
}
