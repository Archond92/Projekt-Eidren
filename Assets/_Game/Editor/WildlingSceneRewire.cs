using Eidren.AI;
using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eidren.Editor
{
	/// <summary>
	/// Stellt die SZENEN-Wildlinge von Sprite- auf 3D-Darstellung um.
	///
	/// Der Wildling ist der einzige Gegner ohne eigenes Prefab: sein
	/// WildlingContentBuilder bricht seit jeher am fehlenden
	/// Wildling_2D.prefab ab, und die einst gebauten Instanzen liegen
	/// AUSGEROLLT in den (binaer serialisierten) Zonenszenen. Ein
	/// Prefab-Rewire greift deshalb nicht — dieses Werkzeug oeffnet die
	/// Szenen selbst und baut jede Instanz um, mit derselben Logik wie
	/// CreatureEnemyRewire: Sprite-Darstellung entfernen (der Locator sucht
	/// includeInactive und nimmt die erste — ein Nebeneinander waere von der
	/// Kindreihenfolge abhaengig), Wildling_3D als Mesh3D einhaengen,
	/// SpriteActorAnimator.presentationBehaviour umhaengen, Beweis per
	/// ActorPresentationLocator.
	///
	/// Idempotent: mehrfache Laeufe erzeugen denselben Endzustand.
	/// </summary>
	public static class WildlingSceneRewire
	{
		private static readonly string[] Szenen =
		{
			"Assets/_Game/Scenes/Zone_Greenwood.unity",
			"Assets/_Game/Scenes/Zone_Quarry.unity",
			"Assets/_Game/Scenes/Zone_Marsh.unity",
			"Assets/_Game/Scenes/Zone_EmberRuins.unity",
			"Assets/_Game/Scenes/Zone_GreyRifts.unity",
			"Assets/_Game/Scenes/Zone_TwilightGrove.unity",
			"Assets/_Game/Scenes/Zone_VeilMarsh.unity",
			"Assets/_Game/Scenes/HomeBase.unity",
			"Assets/_Game/Scenes/EidraForge.unity",
			"Assets/_Game/Scenes/Bootstrap.unity",
			"Assets/_Game/Scenes/MainMenu.unity",
			"Assets/_Game/Scenes/WorldMap/WorldMap.unity"
		};

		/// <summary>
		/// Nur ansehen, nichts aendern: wie liegen die Wildlinge in den Szenen?
		/// </summary>
		[MenuItem("Eidren/V0.2/Wildling-Szenen inventarisieren")]
		public static void Inventar()
		{
			foreach (string pfad in Szenen)
			{
				Scene szene = EditorSceneManager.OpenScene(pfad, OpenSceneMode.Single);
				WildlingController[] wildlinge = FindeWildlinge(szene);
				Debug.Log("[WL3D] " + szene.name + ": " + wildlinge.Length + " Wildling(e)");
				foreach (WildlingController w in wildlinge)
				{
					var teile = new List<string>();
					foreach (Component c in w.GetComponentsInChildren<Component>(true))
					{
						if (c is SpriteActorPresentation || c is CreatureMeshPresentation
							|| c is SpriteActorAnimator || c is Renderer)
						{
							teile.Add(c.GetType().Name + "@" + c.gameObject.name);
						}
					}
					Debug.Log("[WL3D]   " + Pfad(w.transform) + "  ->  " + string.Join(", ", teile));
				}
			}
			Debug.Log("[WL3D] Inventar fertig.");
		}

		/// <summary>
		/// Definition-Id zu Kreaturname. Das Inventar hat gezeigt, dass die
		/// TierTwo-Zonen neben den Wildlingen auch AUSGEROLLTE Kopien anderer
		/// Gegner tragen (Szenennamen sind die displayName-Werte: Rissling,
		/// Wurzelstuermer, Moorwerfer, Granitpanzer) — keine Prefab-Instanzen,
		/// der Prefab-Umbau erreicht sie nicht. Jeder bekommt SEIN Modell;
		/// eine unbekannte Id bricht ab, statt still das falsche einzuhaengen.
		/// </summary>
		private static readonly Dictionary<string, string> DefinitionZuKreatur =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				{ "enemy.wildling", "Wildling" },
				{ "enemy.riftling", "Riftling" },
				{ "enemy.root_charger", "RootCharger" },
				{ "enemy.moor_thrower", "MoorThrower" },
				{ "enemy.granite_shell", "GraniteShell" },
				{ "enemy.rift_guardian", "RiftGuardian" }
			};

		/// <summary>Alle Szenen umstellen. Wirft beim ersten Fehler.</summary>
		[MenuItem("Eidren/V0.2/Wildling-Szenen auf 3D umstellen")]
		public static void RewireAlleSzenen()
		{
			var prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
			int gesamt = 0;
			foreach (string pfad in Szenen)
			{
				Scene szene = EditorSceneManager.OpenScene(pfad, OpenSceneMode.Single);
				WildlingController[] wildlinge = FindeWildlinge(szene);
				if (wildlinge.Length == 0)
				{
					continue;
				}
				foreach (WildlingController w in wildlinge)
				{
					string id = (w.Definition != null) ? w.Definition.Id : null;
					if (id == null || !DefinitionZuKreatur.TryGetValue(id, out string kreatur))
					{
						throw new InvalidOperationException("Unbekannte Gegner-Definition '"
							+ id + "' an " + Pfad(w.transform) + " — kein Modell zugeordnet.");
					}
					if (!prefabs.TryGetValue(kreatur, out GameObject visualPrefab))
					{
						visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
							CreatureMeshBuilder.VisualPrefabPath(kreatur));
						if (visualPrefab == null)
						{
							throw new InvalidOperationException(kreatur + "_3D.prefab fehlt — erst "
								+ "CreatureMeshBuilder.BuildAll laufen lassen.");
						}
						prefabs[kreatur] = visualPrefab;
					}
					RewireEinzeln(w, visualPrefab);
					Debug.Log("[WL3D]   " + szene.name + ": " + Pfad(w.transform)
						+ " (" + id + ") -> " + kreatur + "_3D");
					gesamt++;
				}
				EditorSceneManager.MarkSceneDirty(szene);
				EditorSceneManager.SaveScene(szene);
				Debug.Log("[WL3D] " + szene.name + ": " + wildlinge.Length + " umgestellt, gespeichert.");
			}
			Debug.Log("[WL3D] fertig: " + gesamt + " Szenen-Gegner auf 3D umgestellt.");
		}

		private static void RewireEinzeln(WildlingController wildling, GameObject visualPrefab)
		{
			Transform visual = wildling.transform.Find(CreatureEnemyRewire.VisualNodeName);
			if (visual == null)
			{
				SpriteActorAnimator animator =
					wildling.GetComponentInChildren<SpriteActorAnimator>(true);
				visual = (animator != null) ? animator.transform : null;
			}
			if (visual == null)
			{
				throw new InvalidOperationException("Kein Visual-Knoten am Wildling "
					+ Pfad(wildling.transform));
			}

			// Alten Einbau entfernen — Wiederholbarkeit.
			Transform vorhanden = visual.Find(CreatureEnemyRewire.MeshNodeName);
			if (vorhanden != null)
			{
				UnityEngine.Object.DestroyImmediate(vorhanden.gameObject);
			}

			// Sprite-Darstellung entfernen (Knoten, wenn eigenstaendig; sonst
			// nur die Komponente) — gleiche Regel wie im Prefab-Rewire.
			foreach (SpriteActorPresentation sprite in
				wildling.GetComponentsInChildren<SpriteActorPresentation>(true))
			{
				if (sprite == null)
				{
					continue;
				}
				GameObject knoten = sprite.gameObject;
				if (knoten == wildling.gameObject || knoten == visual.gameObject)
				{
					UnityEngine.Object.DestroyImmediate(sprite);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(knoten);
				}
			}

			var mesh = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, visual);
			mesh.name = CreatureEnemyRewire.MeshNodeName;
			mesh.transform.localPosition = Vector3.zero;
			mesh.transform.localRotation = Quaternion.identity;
			mesh.transform.localScale = Vector3.one;

			CreatureMeshPresentation schicht =
				mesh.GetComponentInChildren<CreatureMeshPresentation>(true);
			if (schicht == null)
			{
				throw new InvalidOperationException("Keine CreatureMeshPresentation im Wildling_3D.");
			}

			foreach (SpriteActorAnimator animator in
				wildling.GetComponentsInChildren<SpriteActorAnimator>(true))
			{
				var serialized = new SerializedObject(animator);
				SerializedProperty feld = serialized.FindProperty("presentationBehaviour");
				feld.objectReferenceValue = schicht;
				serialized.ApplyModifiedPropertiesWithoutUndo();
			}

			IActorPresentation gefunden = ActorPresentationLocator.Find(wildling.gameObject);
			if (!(gefunden is CreatureMeshPresentation))
			{
				throw new InvalidOperationException("Locator liefert "
					+ ((gefunden == null) ? "nichts" : gefunden.GetType().Name)
					+ " statt CreatureMeshPresentation: " + Pfad(wildling.transform));
			}
		}

		private static WildlingController[] FindeWildlinge(Scene szene)
		{
			var alle = new List<WildlingController>();
			foreach (GameObject wurzel in szene.GetRootGameObjects())
			{
				alle.AddRange(wurzel.GetComponentsInChildren<WildlingController>(true));
			}
			return alle.ToArray();
		}

		private static string Pfad(Transform t)
		{
			string p = t.name;
			while (t.parent != null)
			{
				t = t.parent;
				p = t.name + "/" + p;
			}
			return p;
		}
	}
}
