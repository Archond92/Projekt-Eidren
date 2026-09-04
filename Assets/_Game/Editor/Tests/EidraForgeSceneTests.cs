using Eidren.Composition;
using Eidren.Editor;
using NUnit.Framework;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Haelt fest, dass die Schmiedeszene frei von geerbtem Emberruinen-Inhalt ist.
/// Die Szene entstand als Kopie von Zone_EmberRuins und trug Aussengelaende,
/// Felsen, Bewuchs und eine Kopie des Eingangsportals mit; weil das NavMesh mit
/// CollectObjects.All gebacken wird, war das begehbare Flaeche.
/// </summary>
public sealed class EidraForgeSceneTests
{
	/// <summary>
	/// Der schaerfste Schutz gegen Requisiten, die einen Gang zumauern: es muss
	/// einen vollstaendigen Weg vom Startpunkt bis in die Essenkammer geben.
	/// Der Duesenstein steht mit einer Oeffnung von 2,5 mitten in der Duese -
	/// dass ein NavMesh existiert, sagt darueber nichts.
	/// </summary>
	[Test]
	public void VomWindfang_FuehrtEinDurchgehenderWegBisInDieGrube()
	{
		EditorSceneManager.OpenScene(EidraForgeSceneBuilder.ScenePath, OpenSceneMode.Single);
		try
		{
			Assert.That(NavMesh.SamplePosition(new Vector3(0f, 0.2f, -35f), out NavMeshHit start, 4f, NavMesh.AllAreas),
				Is.True, "Am Startpunkt liegt kein NavMesh.");
			/* Zielpunkt bewusst abseits der Esse: sie steht bei (0|32) und ist am
			   Fuss 8 breit, ihr Mesh wird vom NavMesh mitgebacken. Ein Ziel unter
			   dem Ofen misst dessen Huelle, nicht den Grubenboden. */
			Assert.That(NavMesh.SamplePosition(new Vector3(-7f, -6f, 25f), out NavMeshHit ziel, 4f, NavMesh.AllAreas),
				Is.True, "Auf dem Grubenboden liegt kein NavMesh.");

			/* Zweistufig, damit die Meldung sagt, wo der Weg reisst: erst bis auf
			   die Galerie (dort steht der Duesenstein im Gang), dann hinunter in
			   die Grube (dort muessen die Treppen tragen). */
			Assert.That(NavMesh.SamplePosition(new Vector3(0f, 0f, 18f), out NavMeshHit galerie, 4f, NavMesh.AllAreas),
				Is.True, "Auf der Galerie liegt kein NavMesh.");

			NavMeshPath bisGalerie = new NavMeshPath();
			NavMesh.CalculatePath(start.position, galerie.position, NavMesh.AllAreas, bisGalerie);
			Assert.That(bisGalerie.status, Is.EqualTo(NavMeshPathStatus.PathComplete),
				"Der Weg vom Windfang bis zur Galerie ist unterbrochen - eine Requisite versperrt einen Gang.");

			NavMeshPath bisGrube = new NavMeshPath();
			NavMesh.CalculatePath(galerie.position, ziel.position, NavMesh.AllAreas, bisGrube);
			Assert.That(bisGrube.status, Is.EqualTo(NavMeshPathStatus.PathComplete),
				"Von der Galerie fuehrt kein Weg in die Grube - die Treppen tragen nicht.");
		}
		finally
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		}
	}

	[Test]
	public void Szene_TraegtKeinGeerbtesAussengelaendeMehr()
	{
		Scene szene = EditorSceneManager.OpenScene(EidraForgeSceneBuilder.ScenePath, OpenSceneMode.Single);
		try
		{
			Assert.That(szene.GetRootGameObjects().Any(w => w.name == "EidraForgeEntrance"), Is.False,
				"Das Eingangsportal gehoert nach Zone_EmberRuins, nicht in die Schmiede.");

			ZoneController zone = szene.GetRootGameObjects()
				.Select(w => w.GetComponentInChildren<ZoneController>(includeInactive: true))
				.First(z => z != null);

			Assert.That(zone.EnvironmentRoot.Find("AreaArt"), Is.Null,
				"AreaArt mit Felsen und Bewuchs steht noch in der Szene.");

			Assert.That(zone.WalkableGround, Is.Not.Null, "walkableGround ist pflichtig.");
			Renderer[] bodenRenderer = zone.WalkableGround.GetComponentsInChildren<Renderer>(includeInactive: true);
			Assert.That(bodenRenderer.All(r => !r.enabled), Is.True,
				"WalkableGround darf nicht sichtbar sein - der Boden kommt aus den Kacheln.");

			Assert.That(zone.GetValidationErrors(), Is.Empty);
		}
		finally
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		}
	}
}
