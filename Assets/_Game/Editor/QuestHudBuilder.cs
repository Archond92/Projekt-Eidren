using Eidren.UI;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// F31-006: Baut das Aufgaben-Label der Tutorial-Questkette ins
	/// Kampf-HUD-Prefab. UI wird autoriert, nicht zur Laufzeit gebastelt;
	/// mehrfache Läufe erzeugen denselben Endzustand.
	/// </summary>
	public static class QuestHudBuilder
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

		private const string LabelName = "QuestLabel";

		[MenuItem("Eidren/V0.3.1/F31-006 Aufgaben-Label ins Kampf-HUD einbauen")]
		public static void Build()
		{
			GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
			try
			{
				Transform safeArea = FindDeep(root.transform, "SafeArea");
				if (safeArea == null)
				{
					throw new InvalidOperationException("SafeArea fehlt im HUD.");
				}
				Transform previous = FindDeep(safeArea, LabelName);
				if (previous != null)
				{
					UnityEngine.Object.DestroyImmediate(previous.gameObject);
				}
				GameObject labelObject = new GameObject(LabelName, typeof(RectTransform), typeof(Text), typeof(Outline), typeof(QuestHudPresenter));
				labelObject.transform.SetParent(safeArea, worldPositionStays: false);
				Text text = labelObject.GetComponent<Text>();
				text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				text.fontSize = 20;
				text.alignment = TextAnchor.UpperLeft;
				text.color = new Color(0.95f, 0.9f, 0.72f);
				text.horizontalOverflow = HorizontalWrapMode.Overflow;
				text.raycastTarget = false;
				Outline outline = labelObject.GetComponent<Outline>();
				outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
				outline.effectDistance = new Vector2(1f, -1f);
				RectTransform rect = labelObject.GetComponent<RectTransform>();
				rect.anchorMin = new Vector2(0f, 1f);
				rect.anchorMax = new Vector2(0f, 1f);
				rect.pivot = new Vector2(0f, 1f);
				rect.sizeDelta = new Vector2(640f, 30f);
				rect.anchoredPosition = new Vector2(18f, -86f);
				labelObject.GetComponent<QuestHudPresenter>().ConfigureReferences(text);
				if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
				{
					throw new InvalidOperationException("CombatHUD.prefab ließ sich nicht speichern.");
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			AssetDatabase.SaveAssets();
			Debug.Log("Eidren: quest label built into combat HUD.");
		}

		private static Transform FindDeep(Transform root, string name)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child.name == name)
				{
					return child;
				}
			}
			return null;
		}
	}
}
