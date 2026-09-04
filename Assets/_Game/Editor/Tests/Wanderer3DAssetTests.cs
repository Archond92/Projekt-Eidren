using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class Wanderer3DAssetTests
	{
		private const string GlbPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb";

		private static readonly string[] ExpectedNodes = new string[20]
		{
			"Basis", "Haare",
			"Helm_Stoff", "Helm_Kupfer", "Helm_Eisen",
			"Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
			"Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
			"Haende_Stoff", "Haende_Kupfer", "Haende_Eisen",
			"Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer",
			"Waffe_Axt", "Waffe_Spitzhacke", "Waffe_Sense"
		};

		private static readonly string[] ExpectedClips = new string[19]
		{
			"Ruhe_Ohne", "Gehen_Ohne", "Laufen_Ohne",
			"Ruhe_Speer", "Ruhe_Dolche", "Ruhe_Hammer",
			"Gehen_Speer", "Gehen_Dolche", "Gehen_Hammer",
			"Laufen_Speer", "Laufen_Dolche", "Laufen_Hammer",
			"Angriff_Speer", "Angriff_Dolche", "Angriff_Hammer",
			// W-001/W-009: Abbau je Werkzeug und die Kistenoeffnung.
			"Abbau_Axt", "Abbau_Spitzhacke", "Abbau_Sense", "Oeffnen"
		};

		[Test]
		public void Glb_EnthaeltAlleSlotWerkzeugUndWaffenknoten()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
			Assert.That(root, Is.Not.Null, "GLB nicht importiert: " + GlbPath);
			foreach (string node in ExpectedNodes)
			{
				Transform child = FindDeep(root.transform, node);
				Assert.That(child, Is.Not.Null, "Knoten fehlt: " + node);
				Assert.That(child.GetComponentsInChildren<Renderer>(true), Is.Not.Empty,
					"Mid-Poly-Knoten ohne Renderer: " + node);
			}
		}

		[Test]
		public void Glb_EnthaeltAlleZwoelfClips()
		{
			var clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath).OfType<AnimationClip>()
				.Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
			foreach (string expected in ExpectedClips)
			{
				Assert.That(clips.Any(clip => clip.name == expected), Is.True, "Clip fehlt: " + expected);
			}
			// Sieben Haltungen mit Ruhe/Gehen/Laufen plus sechs Aktionen und Oeffnen.
			Assert.That(clips.Length, Is.EqualTo(28), "Unerwartete Clip-Anzahl");
		}

		[Test]
		public void WandererShader_ExistiertUndHatTintProperty()
		{
			Shader shader = Shader.Find("Eidren/Actors/WandererVertexLit");
			Assert.That(shader, Is.Not.Null, "Shader Eidren/Actors/WandererVertexLit fehlt");
			var material = new Material(shader);
			Assert.That(material.HasProperty("_Tint"), Is.True, "_Tint-Property fehlt");
			Object.DestroyImmediate(material);
		}

		private static Transform FindDeep(Transform root, string childName)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
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
