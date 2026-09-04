using Eidren.Composition;
using NUnit.Framework;
using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Nach-Release-Fix (18.08.2026): Der Verlies-Eingang in den Glutruinen
	/// stammt vom EidraForgeSceneBuilder und wurde vom Szenen-Rebuild der
	/// Fixrunde abgeworfen — dritter Fall der Rebuild-Falle (nach AreaArt
	/// und BuildSettings). Diese Wache hält den Eingang in der Szene fest.
	/// </summary>
	public sealed class EmberRuinsEntranceTests
	{
		[Test]
		public void Glutruinen_TragenDenVerliesEingang()
		{
			Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_EmberRuins.unity", OpenSceneMode.Single);
			EidraForgeEntrance entrance = null;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				entrance = root.GetComponentInChildren<EidraForgeEntrance>(true);
				if (entrance != null)
				{
					break;
				}
			}
			Assert.That(entrance, Is.Not.Null,
				"Zone_EmberRuins hat keinen EidraForgeEntrance — das Verlies ist unerreichbar. " +
				"Eidren/Scenes/Build Eidra Forge (oder den Eingangs-Einstieg) laufen lassen.");
			Assert.That(entrance.gameObject.activeSelf, Is.True);
		}

		/// <summary>
		/// Tester-Runde 19.08.2026: Der Eingang ist ein Felsportal — zwei
		/// flankierende Felsformationen, ein AUFRECHTER Steinsturz und eine
		/// dunkle Öffnung mit Glutlicht. Die alte Fassung war eine rote
		/// Scheibe mit einem Quader, den die Eltern-Skalierung (0,16 auf Y)
		/// auf Bankhöhe plattdrückte.
		/// </summary>
		[Test]
		public void VerliesEingang_IstEinFelsportal()
		{
			Scene scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Zone_EmberRuins.unity", OpenSceneMode.Single);
			EidraForgeEntrance entrance = null;
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				entrance = root.GetComponentInChildren<EidraForgeEntrance>(true);
				if (entrance != null)
				{
					break;
				}
			}
			Assert.That(entrance, Is.Not.Null, "Kein Verlies-Eingang in den Glutruinen.");
			Transform wurzel = entrance.transform;
			Assert.That(wurzel.localScale, Is.EqualTo(Vector3.one),
				"Die Portalwurzel muss unskaliert sein — Kinder unter einer gequetschten Wurzel verlieren ihre Höhe.");
			int felsen = 0;
			foreach (Transform kind in wurzel)
			{
				if (kind.name.StartsWith("PortalFels", StringComparison.Ordinal))
				{
					felsen++;
				}
			}
			Assert.That(felsen, Is.GreaterThanOrEqualTo(2), "Das Felsportal braucht mindestens zwei flankierende Felsen.");
			Transform sturz = wurzel.Find("PortalSturz");
			Assert.That(sturz, Is.Not.Null, "Das Felsportal hat keinen Steinsturz.");
			Assert.That(sturz.position.y, Is.GreaterThanOrEqualTo(2.5f),
				$"Der Sturz muss über der Öffnung liegen — er sitzt auf Höhe {sturz.position.y:0.00}.");
			Assert.That(wurzel.Find("PortalOeffnung"), Is.Not.Null, "Die dunkle Portalöffnung fehlt.");
			Assert.That(wurzel.GetComponentInChildren<Light>(true), Is.Not.Null, "Das Glutlicht des Portals fehlt.");
			Assert.That(wurzel.GetComponentInChildren<Collider>(true), Is.Not.Null,
				"Das Portal braucht einen Trigger-Collider für die Interaktion.");
		}
	}
}
