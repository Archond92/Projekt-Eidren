using Eidren.Presentation;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F31-008: Die Tür öffnet beim Annähern der Spielfigur, gibt den Weg
	/// physisch frei und schließt nach dem Entfernen wieder — der Nachweis,
	/// den es nie gab („Tür lässt sich bauen" hat den Fehler nicht gefunden).
	/// </summary>
	public sealed class Fixrunde031DoorTests
	{
		[UnityTest]
		public IEnumerator Tuer_OeffnetFuerDieFigurUndSchliesstDanach()
		{
			// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
			yield return Testumgebung.LeereWeltBereitstellen();
			GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
			ground.name = "F31008_Ground";
			ground.transform.position = new Vector3(0f, -0.5f, 0f);
			ground.transform.localScale = new Vector3(20f, 1f, 20f);
			Eidren.Data.BuildingCatalogDefinition katalog = Resources.Load<Eidren.Data.BuildingCatalogDefinition>("Data/BuildingCatalog_V01");
			Assert.That(katalog, Is.Not.Null, "Gebäudekatalog fehlt.");
			GameObject doorPrefab = null;
			foreach (Eidren.Data.BuildingCostDefinition building in katalog.Buildings)
			{
				if (building.Id == "building.door" && building.TryGetLevel(1, out _, out doorPrefab))
				{
					break;
				}
			}
			Assert.That(doorPrefab, Is.Not.Null, "Tür-Prefab fehlt im Katalog.");
			GameObject door = Object.Instantiate(doorPrefab, Vector3.zero, Quaternion.identity);
			GameObject playerObject = new GameObject("F31008_Player");
			try
			{
				BuildingDoorView view = door.GetComponentInChildren<BuildingDoorView>(includeInactive: true);
				Assert.That(view, Is.Not.Null, "Tür hat keine BuildingDoorView — Builder nicht gelaufen?");
				playerObject.transform.position = new Vector3(0f, 0f, 6f);
				CharacterController controller = playerObject.AddComponent<CharacterController>();
				controller.center = Vector3.up;
				yield return null;
				Assert.That(view.IsOpen, Is.False, "Tür muss anfangs geschlossen sein.");
				Assert.That(view.BlockingCollider.enabled, Is.True, "Geschlossene Tür muss sperren.");
				// Bewegung je Frame mit fester Schrittweite (deterministisch);
				// Wartezyklen in SPIELZEIT budgetiert — Batch-Frames haben
				// winzige deltaTime-Werte, Frame- und Echtzeitfristen kippen.
				const float schritt = 0.06f;
				int frames = 0;
				while (playerObject.transform.position.z > 1.6f && frames++ < 600)
				{
					controller.Move(new Vector3(0f, 0f, -schritt));
					yield return null;
				}
				float zeit = 0f;
				while (!view.IsOpen && zeit < 3f)
				{
					zeit += Time.deltaTime;
					yield return null;
				}
				Assert.That(view.IsOpen, Is.True, "Tür muss beim Annähern öffnen.");
				zeit = 0f;
				while (view.BlockingCollider.enabled && zeit < 3f)
				{
					zeit += Time.deltaTime;
					yield return null;
				}
				Assert.That(view.BlockingCollider.enabled, Is.False, "Offene Tür darf nicht sperren.");
				zeit = 0f;
				while (view.CurrentYaw < 90f && zeit < 3f)
				{
					zeit += Time.deltaTime;
					yield return null;
				}
				Assert.That(view.CurrentYaw, Is.GreaterThanOrEqualTo(90f), "Das Blatt muss aufschwingen.");
				// Durchgehen: von z=+1,6 nach z=-2 durch die Türlinie.
				frames = 0;
				while (playerObject.transform.position.z > -2f && frames++ < 600)
				{
					controller.Move(new Vector3(0f, 0f, -schritt));
					yield return null;
				}
				Assert.That(playerObject.transform.position.z, Is.LessThanOrEqualTo(-1.9f),
					"Die Figur muss durch die offene Tür kommen.");
				// Entfernen: Tür schließt und sperrt wieder.
				frames = 0;
				while (playerObject.transform.position.z > -6f && frames++ < 600)
				{
					controller.Move(new Vector3(0f, 0f, -schritt));
					yield return null;
				}
				zeit = 0f;
				while ((view.IsOpen || view.CurrentYaw > 0.5f) && zeit < 4f)
				{
					zeit += Time.deltaTime;
					yield return null;
				}
				Assert.That(view.IsOpen, Is.False, "Tür muss nach dem Verlassen schließen.");
				Assert.That(view.BlockingCollider.enabled, Is.True, "Geschlossene Tür muss wieder sperren.");
			}
			finally
			{
				Object.Destroy(door);
				Object.Destroy(playerObject);
				Object.Destroy(ground);
			}
			yield return null;
		}
	}
}
