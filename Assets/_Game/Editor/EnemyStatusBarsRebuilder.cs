using Eidren.UI;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Neuer Autor der Gegner-Statusleiste
	/// (StatusBars_A20) — ihr ursprünglicher Auftrag-20-Builder existiert
	/// nicht mehr, die Leisten lebten nur noch ausgerollt in den Prefabs.
	/// Baut je Feldgegner: Namenslabel über den Balken, Sprites in beiden
	/// Füllungen (ein Filled-Image OHNE Sprite zeichnet immer voll) und die
	/// Leistenhöhe über der Kapsel, damit sie bei großen Kreaturen nicht in
	/// der Figur steckt. Mehrfache Läufe erzeugen denselben Endzustand.
	/// </summary>
	public static class EnemyStatusBarsRebuilder
	{
		private const float ClearanceAboveHead = 0.45f;

		private static readonly string[] PrefabPaths = new string[]
		{
			"Assets/_Game/Prefabs/Enemies/Wildling.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/RootCharger.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/MoorThrower.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/GraniteShell.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/RiftGuardian.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/EmberEater.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/AshRunner.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/SealGuardian.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/ForgeGuardian.prefab",
			// #34: Der Kernwaechter traegt die Leiste seit dem Fund, dass
			// Treffer gegen 3000 HP ohne Anzeige als wirkungslos gelesen
			// werden; sein Aufbau lebt im EidraForgeEnemyContentBuilder.
			"Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab"
		};

		[MenuItem("Eidren/V0.3.1/Gegner-Statusleisten neu bauen")]
		public static void Rebuild()
		{
			Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
			if (uiSprite == null)
			{
				throw new InvalidOperationException("Builtin UISprite fehlt.");
			}
			foreach (string path in PrefabPaths)
			{
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					RebuildOne(root, path, uiSprite);
					PrefabUtility.SaveAsPrefabAsset(root, path);
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}
			AssetDatabase.SaveAssets();
			Debug.Log("Eidren: enemy status bars rebuilt for " + PrefabPaths.Length + " prefabs.");
		}

		private static void RebuildOne(GameObject root, string path, Sprite uiSprite)
		{
			WildlingStatusBars bars = root.GetComponentInChildren<WildlingStatusBars>(includeInactive: true);
			if (bars == null || bars.WorldCanvas == null)
			{
				throw new InvalidOperationException(path + ": StatusBars fehlen im Prefab.");
			}
			Canvas canvas = bars.WorldCanvas;
			// Beide Fuellungen brauchen ein Sprite, sonst ignoriert Unity den
			// fillAmount und zeichnet den Balken dauerhaft voll.
			foreach (Image image in canvas.GetComponentsInChildren<Image>(includeInactive: true))
			{
				if (image.sprite == null)
				{
					image.sprite = uiSprite;
				}
			}
			// Leiste ueber den Kopf der Kreatur heben.
			CapsuleCollider body = root.GetComponent<CapsuleCollider>();
			if (body == null)
			{
				throw new InvalidOperationException(path + ": Kapselkoerper fehlt.");
			}
			float headHeight = body.center.y + body.height * 0.5f;
			Vector3 canvasPosition = canvas.transform.localPosition;
			canvas.transform.localPosition = new Vector3(canvasPosition.x, headHeight + ClearanceAboveHead, canvasPosition.z);
			// Namenslabel ueber den Balken (im Canvas-Massstab 0,008:
			// 150 px = 1,2 m Breite, wie die Balken selbst).
			Transform existing = canvas.transform.Find("NameLabel");
			Text label;
			if (existing == null)
			{
				GameObject labelObject = new GameObject("NameLabel", typeof(RectTransform), typeof(Text), typeof(Outline));
				labelObject.transform.SetParent(canvas.transform, worldPositionStays: false);
				label = labelObject.GetComponent<Text>();
			}
			else
			{
				label = existing.GetComponent<Text>();
				if (existing.GetComponent<Outline>() == null)
				{
					existing.gameObject.AddComponent<Outline>();
				}
			}
			label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			label.fontSize = 13;
			label.fontStyle = FontStyle.Bold;
			label.alignment = TextAnchor.MiddleCenter;
			label.color = new Color(0.96f, 0.94f, 0.88f);
			label.horizontalOverflow = HorizontalWrapMode.Overflow;
			label.raycastTarget = false;
			Outline outline = label.GetComponent<Outline>();
			outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
			outline.effectDistance = new Vector2(1f, -1f);
			RectTransform rect = label.rectTransform;
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0f);
			rect.sizeDelta = new Vector2(220f, 18f);
			rect.anchoredPosition = new Vector2(0f, 12f);
			bars.ConfigureReferences(canvas, bars.HealthFill, bars.StaggerFill, FindProtectionLabel(canvas), label);
			EditorUtility.SetDirty(bars);
		}

		private static Text FindProtectionLabel(Canvas canvas)
		{
			Transform transform = canvas.transform.Find("ProtectionLabel");
			return (transform != null) ? transform.GetComponent<Text>() : null;
		}
	}
}
