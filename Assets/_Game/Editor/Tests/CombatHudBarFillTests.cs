using Eidren.UI;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	public sealed class CombatHudBarFillTests
	{
		private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

		/// <summary>
		/// Macht die Ursache pruefbar: OnPopulateMesh der Image faellt ohne Sprite
		/// auf das volle Rechteck zurueck, egal welchen Fuellwert man setzt.
		/// </summary>
		private sealed class ProbeImage : Image
		{
			public float BuildMeshWidth()
			{
				VertexHelper helper = new VertexHelper();
				OnPopulateMesh(helper);
				List<UIVertex> vertices = new List<UIVertex>();
				helper.GetUIVertexStream(vertices);
				helper.Dispose();
				if (vertices.Count == 0)
				{
					return 0f;
				}
				float min = float.MaxValue;
				float max = float.MinValue;
				foreach (UIVertex vertex in vertices)
				{
					min = Mathf.Min(min, vertex.position.x);
					max = Mathf.Max(max, vertex.position.x);
				}
				return max - min;
			}
		}

		[Test]
		public void GefuellteGrafikOhneSprite_IgnoriertDenFuellwert()
		{
			Assert.That(ProbeWidth(withSprite: false), Is.EqualTo(200f).Within(0.5f),
				"Ohne Sprite zeichnet Unity trotz fillAmount 0,25 die volle Breite — der Fuellwert bleibt wirkungslos");
			Assert.That(ProbeWidth(withSprite: true), Is.EqualTo(50f).Within(0.5f),
				"Mit Sprite wirkt fillAmount 0,25 als Viertelbreite");
		}

		[Test]
		public void AlleBalkenFuellungen_BesitzenEinSprite()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(root, Is.Not.Null, "HUD-Prefab fehlt: " + PrefabPath);
			foreach (Image image in root.GetComponentsInChildren<Image>(includeInactive: true))
			{
				if (image.type == Image.Type.Filled)
				{
					Assert.That(image.sprite, Is.Not.Null,
						"Gefuellte Grafik ohne Sprite ignoriert fillAmount und steht dauerhaft voll: " + Path(image.transform, root.transform));
				}
			}
		}

		[Test]
		public void StatusBalken_LaufenHorizontalVonLinks()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
			Assert.That(root, Is.Not.Null);
			CombatHudStatusPresenter status = root.GetComponentInChildren<CombatHudStatusPresenter>(includeInactive: true);
			Assert.That(status, Is.Not.Null);
			foreach (string barPath in new string[4] { "Health/Fill", "Stamina/Fill", "Resonance/Fill", "ExperienceBack/ExperienceFill" })
			{
				Transform playerStatus = FindDeep(root.transform, "PlayerStatus");
				Assert.That(playerStatus, Is.Not.Null);
				Transform bar = playerStatus.Find(barPath);
				Assert.That(bar, Is.Not.Null, "Balken fehlt: " + barPath);
				Image image = bar.GetComponent<Image>();
				Assert.That(image.type, Is.EqualTo(Image.Type.Filled), "Statusbalken muss ein Fuellbalken sein: " + barPath);
				Assert.That(image.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal), "Statusbalken darf nicht radial laufen: " + barPath);
				Assert.That(image.fillOrigin, Is.EqualTo((int)Image.OriginHorizontal.Left), "Statusbalken leert sich nach links: " + barPath);
			}
		}

		private static float ProbeWidth(bool withSprite)
		{
			GameObject canvasObject = new GameObject("BarFillProbe_Canvas", typeof(Canvas));
			Texture2D texture = null;
			Sprite sprite = null;
			try
			{
				GameObject imageObject = new GameObject("BarFillProbe_Image");
				imageObject.transform.SetParent(canvasObject.transform, worldPositionStays: false);
				ProbeImage probe = imageObject.AddComponent<ProbeImage>();
				probe.rectTransform.sizeDelta = new Vector2(200f, 20f);
				if (withSprite)
				{
					texture = new Texture2D(4, 4);
					Color[] pixels = new Color[16];
					for (int index = 0; index < pixels.Length; index++)
					{
						pixels[index] = Color.white;
					}
					texture.SetPixels(pixels);
					texture.Apply();
					sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
					probe.sprite = sprite;
				}
				probe.type = Image.Type.Filled;
				probe.fillMethod = Image.FillMethod.Horizontal;
				probe.fillOrigin = (int)Image.OriginHorizontal.Left;
				probe.fillAmount = 0.25f;
				return probe.BuildMeshWidth();
			}
			finally
			{
				if (sprite != null)
				{
					Object.DestroyImmediate(sprite);
				}
				if (texture != null)
				{
					Object.DestroyImmediate(texture);
				}
				Object.DestroyImmediate(canvasObject);
			}
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

		private static string Path(Transform leaf, Transform root)
		{
			string path = leaf.name;
			for (Transform current = leaf.parent; current != null && current != root; current = current.parent)
			{
				path = current.name + "/" + path;
			}
			return path;
		}
	}
}
