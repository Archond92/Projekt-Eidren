using System.Collections.Generic;
using System.IO;
using Eidren.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/* G-004: Vertrag statt Konvention. Diese Klasse wird rot, sobald ein
	   Builder-Lauf die Erdung eines Weltobjekts wegwirft — genau der Fehler,
	   der die G-003-Kontaktdecals im Stilumbau restlos gekostet hat: sie waren
	   korrekt gebaut, hingen aber als nachtraegliche Dekoration an Prefabs, die
	   die Etappen 1 bis 5 neu geschrieben haben. */
	public sealed class GroundContactTests
	{
		private static IEnumerable<string> AllePrefabs()
		{
			foreach (string ordner in GroundContactAudit.Ordner)
			{
				if (!Directory.Exists(ordner))
				{
					continue;
				}
				foreach (string datei in Directory.GetFiles(ordner, "*.prefab"))
				{
					yield return datei.Replace('\\', '/');
				}
			}
		}

		/* NUR MeshRenderer — dieselbe Regel wie WorldContactShadowBuilder.Attach.
		   Sprite-Glyphen (F-005: OpeningHand_Left/Right an den Weltkisten) und
		   Partikel zaehlen nicht zur festen Geometrie; ueber alle Renderer
		   gemessen ergaeben die Kisten 11,4 x 6,15 x 8,26 statt 1,2 x 0,59 x 0,79. */
		private static Bounds Rendererbounds(GameObject prefab, out int anzahl)
		{
			MeshRenderer[] renderer = prefab.GetComponentsInChildren<MeshRenderer>(true);
			anzahl = renderer.Length;
			if (anzahl == 0)
			{
				return new Bounds(Vector3.zero, Vector3.zero);
			}
			Bounds b = renderer[0].bounds;
			for (int i = 1; i < anzahl; i++)
			{
				b.Encapsulate(renderer[i].bounds);
			}
			return b;
		}

		[Test]
		[TestCaseSource(nameof(AllePrefabs))]
		public void Weltprefab_ErfuelltDieAufnahmeregel(string pfad)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
			Assert.That(prefab, Is.Not.Null, "Prefab nicht ladbar: " + pfad);

			Bounds bounds = Rendererbounds(prefab, out int rendererZahl);
			Transform decal = prefab.transform.Find(WorldContactShadowBuilder.ChildName);

			if (rendererZahl == 0 || bounds.size.y <= WorldContactShadowBuilder.MindestHoehe)
			{
				Assert.That(decal, Is.Null,
					$"{Path.GetFileName(pfad)} liegt mit {bounds.size.y:F3} m unter der " +
					$"Aufnahmeregel ({WorldContactShadowBuilder.MindestHoehe} m), " +
					"traegt aber ein Kontaktdecal.");
				return;
			}

			Assert.That(decal, Is.Not.Null,
				$"{Path.GetFileName(pfad)} steht {bounds.size.y:F3} m hoch, " +
				"hat aber kein Kontaktdecal (ContactShadow).");

			MeshRenderer renderer = decal.GetComponent<MeshRenderer>();
			Assert.That(renderer, Is.Not.Null, "ContactShadow ohne MeshRenderer: " + pfad);
			Assert.That(renderer.sharedMaterial, Is.Not.Null, "ContactShadow ohne Material: " + pfad);
			Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_World_ContactShadow"),
				"ContactShadow nutzt ein fremdes Material: " + pfad);
			Assert.That(renderer.shadowCastingMode,
				Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off),
				"Das Decal darf selbst keinen Schatten werfen: " + pfad);
		}

		[Test]
		[TestCaseSource(nameof(AllePrefabs))]
		public void Weltprefab_HatHoechstensEinKontaktdecal(string pfad)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
			Assert.That(prefab, Is.Not.Null, "Prefab nicht ladbar: " + pfad);
			int treffer = 0;
			foreach (Transform kind in prefab.transform)
			{
				if (kind.name == WorldContactShadowBuilder.ChildName)
				{
					treffer++;
				}
			}
			Assert.That(treffer, Is.LessThanOrEqualTo(1),
				$"{Path.GetFileName(pfad)} traegt {treffer} Kontaktdecals — " +
				"ein wiederholter Builder-Lauf hat verdoppelt.");
		}
	}
}
