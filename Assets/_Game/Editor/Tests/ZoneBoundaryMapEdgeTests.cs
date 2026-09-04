using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// N04-001-Nachtrag: Das "Ende der Karte" auf der Minimap kommt aus der
	/// Zonengeometrie, nicht aus der Bewegungsgrenze. Alle Zonen sind mit
	/// MovementBoundaryShape.None gebaut (EidrenSceneStructureBuilder) — die
	/// Bewegungsgrenze ist dort immer Unbounded, das Zonenrechteck steckt
	/// trotzdem in rectangleSize. Genau diese Luecke hat die Randlinie in der
	/// Exe verschluckt: Die Presenter-Tests setzten sich ihre Grenze selbst.
	/// </summary>
	public sealed class ZoneBoundaryMapEdgeTests
	{
		[Test]
		public void Kartenrand_KommtAuchOhneHarteBewegungsgrenze()
		{
			GameObject zone = new GameObject("MapEdgeTest_Zone");
			try
			{
				ZoneBoundarySettings settings = zone.AddComponent<ZoneBoundarySettings>();
				settings.Configure(MovementBoundaryShape.None, Vector3.zero, 0f, new Vector2(80f, 80f), MapSideFlags.All, Vector3.zero, new Vector2(76f, 76f));

				PlayerMovementBoundary edge = settings.CreateMapEdgeBoundary();

				Assert.That(edge.HasHardBoundary, Is.True, "Das Zonenrechteck muss auch bei Shape None als Kartenrand herauskommen — sonst zeichnet die Minimap nie eine Linie");
				Assert.That(edge.Shape, Is.EqualTo(MovementBoundaryShape.Rectangle), "Ohne harte Grenze ist das Rechteck der Kartenrand");
				Assert.That(edge.Size, Is.EqualTo(new Vector2(80f, 80f)), "Der Rand muss das konfigurierte Zonenmass tragen");
			}
			finally
			{
				Object.DestroyImmediate(zone);
			}
		}

		[Test]
		public void Kartenrand_UebernimmtEineEchteHarteGrenzeUnveraendert()
		{
			GameObject zone = new GameObject("MapEdgeTest_Zone");
			try
			{
				ZoneBoundarySettings settings = zone.AddComponent<ZoneBoundarySettings>();
				settings.Configure(MovementBoundaryShape.Circle, Vector3.zero, 30f, new Vector2(80f, 80f), MapSideFlags.None, Vector3.zero, new Vector2(76f, 76f));

				PlayerMovementBoundary edge = settings.CreateMapEdgeBoundary();

				Assert.That(edge.Shape, Is.EqualTo(MovementBoundaryShape.Circle), "Eine echte harte Grenze ist selbst der Kartenrand");
				Assert.That(edge.Radius, Is.EqualTo(30f), "Der Kreisradius muss unveraendert durchkommen");
			}
			finally
			{
				Object.DestroyImmediate(zone);
			}
		}

		[Test]
		public void EidraForge_KartenrandDecktDenGanzenGrundriss()
		{
			// Die Schmiede entstand als Szenenkopie von Zone_EmberRuins und behielt
			// deren Zonenquadrat um den Ursprung. Der Grundriss reicht aber von
			// X -36..36 und Z -38..50 (EidraForgeLayout) — mit dem geerbten Rand
			// zeichnet die Minimap das Kartenende mitten durch das Verlies.
			EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity", OpenSceneMode.Single);
			try
			{
				ZoneBoundarySettings settings = Object.FindFirstObjectByType<ZoneBoundarySettings>();
				Assert.That(settings, Is.Not.Null, "Die Schmiede braucht ZoneBoundarySettings");

				PlayerMovementBoundary edge = settings.CreateMapEdgeBoundary();

				Assert.That(edge.HasHardBoundary, Is.True, "Die Schmiede muss einen zeichenbaren Kartenrand liefern");
				Assert.That(edge.Shape, Is.EqualTo(MovementBoundaryShape.Rectangle), "Der Kartenrand des Verlieses ist das Grundriss-Rechteck");
				Assert.That(edge.Center.x, Is.EqualTo(0f).Within(0.01f), "Der Grundriss liegt in X mittig");
				Assert.That(edge.Center.z, Is.EqualTo(6f).Within(0.01f), "Der Grundriss liegt in Z bei +6, nicht am Ursprung");
				Assert.That(edge.Size, Is.EqualTo(new Vector2(72f, 88f)), "Der Rand muss den Grundriss X -36..36, Z -38..50 umschliessen");
			}
			finally
			{
				EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			}
		}

		[Test]
		public void Greenwood_TraegtEinenZeichenbarenKartenrand()
		{
			// Die Zonenszenen sind binaer serialisiert — was in ihnen steht, laesst
			// sich nur durch Laden beweisen, nicht durch Textsuche. Genau diese
			// Pruefung fehlte, als die Randlinie gruen getestet und trotzdem in
			// jeder echten Zone unsichtbar war.
			EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_Greenwood.unity", OpenSceneMode.Single);
			try
			{
				ZoneBoundarySettings settings = Object.FindFirstObjectByType<ZoneBoundarySettings>();
				Assert.That(settings, Is.Not.Null, "Greenwood braucht ZoneBoundarySettings");

				PlayerMovementBoundary edge = settings.CreateMapEdgeBoundary();

				Assert.That(edge.HasHardBoundary, Is.True, "Die echte Szene muss einen zeichenbaren Kartenrand liefern");
				Assert.That(edge.Size.x, Is.GreaterThan(0f), "Das Zonenrechteck der Szene darf nicht leer sein");
			}
			finally
			{
				EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			}
		}
	}
}
