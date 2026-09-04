using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Stellt ein Gegner-Prefab von der Sprite- auf die 3D-Darstellung um.
	///
	/// Der Umbau ist eng begrenzt: er fasst NUR den Visual-Knoten an. Der
	/// Bodenschatten (DynamicActorGroundShadow) bringt einen eigenen Renderer
	/// mit und zielt auf die Wurzel, ActorSpriteVfx erzeugt seine Renderer zur
	/// Laufzeit selbst — beide haengen nicht am Aktorbild und bleiben unberuehrt.
	/// Ebenso die Anker (AbilityAnchor, BodyAnchor, FootAnchor) und die
	/// Trefferzonen.
	///
	/// WARUM DIE SPRITE-DARSTELLUNG ENTFERNT WIRD UND NICHT NUR DEAKTIVIERT:
	/// ActorPresentationLocator.Find sucht mit includeInactive: true und liefert
	/// die ERSTE gefundene Darstellung im Baum. Bliebe die SpriteActorPresentation
	/// stehen, entschiede die Kindreihenfolge darueber, welche Darstellung der
	/// Gegner ansteuert — ein Nebeneinander waere nicht stabil, sondern zufaellig.
	///
	/// Idempotent: mehrfache Laeufe erzeugen denselben Endzustand.
	/// </summary>
	public static class CreatureEnemyRewire
	{
		/// <summary>Knoten, unter dem die Darstellung eines Gegners haengt.</summary>
		public const string VisualNodeName = "Visual";

		/// <summary>Name, unter dem das 3D-Prefab eingehaengt wird.</summary>
		public const string MeshNodeName = "Mesh3D";

		/// <summary>
		/// Kreatur zu Gegner-Prefab. Elf der vierzehn Figuren haben ein eigenes
		/// Prefab. Ignivar, Noctarion und Terrock haben keins — sie laufen ueber
		/// Prefabs/Enemies/WildEidra.prefab, das erst geprueft werden muss, bevor
		/// es hier aufgenommen wird.
		/// </summary>
		public static readonly (string Creature, string EnemyPrefab)[] Targets =
		{
			("Riftling",      "Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab"),
			("MoorThrower",   "Assets/_Game/Prefabs/Enemies/TierTwo/MoorThrower.prefab"),
			("RiftGuardian",  "Assets/_Game/Prefabs/Enemies/TierTwo/RiftGuardian.prefab"),
			("RootCharger",   "Assets/_Game/Prefabs/Enemies/TierTwo/RootCharger.prefab"),
			("GraniteShell",  "Assets/_Game/Prefabs/Enemies/TierTwo/GraniteShell.prefab"),
			("ForgeGuardian", "Assets/_Game/Prefabs/Enemies/Forge/ForgeGuardian.prefab"),
			("SealGuardian",  "Assets/_Game/Prefabs/Enemies/Forge/SealGuardian.prefab"),
			("CoreGuardian",  "Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab"),
			("EmberEater",    "Assets/_Game/Prefabs/Enemies/Forge/EmberEater.prefab"),
			("AshRunner",     "Assets/_Game/Prefabs/Enemies/Forge/AshRunner.prefab"),
			("Garon",         "Assets/_Game/Prefabs/Bosses/Garon.prefab")
		};

		public static string EnemyPrefabPath(string creature)
		{
			foreach ((string name, string prefab) in Targets)
			{
				if (string.Equals(name, creature, StringComparison.Ordinal))
				{
					return prefab;
				}
			}
			return null;
		}

		[MenuItem("Eidren/V0.2/Gegner auf 3D umstellen — Riftling (Referenzfall)")]
		public static void RewireRiftling()
		{
			Rewire("Riftling", EnemyPrefabPath("Riftling"));
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("[K3D] Riftling.prefab auf 3D umgestellt.");
		}

		[MenuItem("Eidren/V0.2/Gegner auf 3D umstellen — alle elf")]
		public static void RewireAll()
		{
			var fehler = new List<string>();
			foreach ((string creature, string prefab) in Targets)
			{
				try
				{
					Rewire(creature, prefab);
				}
				catch (Exception ex)
				{
					fehler.Add(creature + ": " + ex.Message);
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			if (fehler.Count > 0)
			{
				Debug.LogError("[K3D] " + fehler.Count + " Gegner nicht umgestellt:\n"
					+ string.Join("\n", fehler));
				return;
			}
			Debug.Log("[K3D] " + Targets.Length + " Gegner auf 3D umgestellt.");
		}

		/// <summary>
		/// Stellt einen Gegner um. Wirft, wenn etwas nicht stimmt — ein halb
		/// umgebautes Prefab waere schlimmer als ein nicht umgebautes.
		/// </summary>
		public static void Rewire(string creature, string enemyPrefabPath)
		{
			if (string.IsNullOrEmpty(enemyPrefabPath))
			{
				throw new InvalidOperationException("Kein Gegner-Prefab fuer " + creature + " hinterlegt.");
			}

			string visualPrefabPath = CreatureMeshBuilder.VisualPrefabPath(creature);
			GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualPrefabPath);
			if (visualPrefab == null)
			{
				throw new InvalidOperationException("Darstellungs-Prefab fehlt: " + visualPrefabPath
					+ " — erst CreatureMeshBuilder laufen lassen.");
			}
			if (AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath) == null)
			{
				throw new InvalidOperationException("Gegner-Prefab fehlt: " + enemyPrefabPath);
			}

			GameObject root = PrefabUtility.LoadPrefabContents(enemyPrefabPath);
			try
			{
				Transform visual = FindVisualNode(root);
				if (visual == null)
				{
					throw new InvalidOperationException("Kein Darstellungsknoten ('" + VisualNodeName
						+ "' oder Knoten mit SpriteActorAnimator) in " + enemyPrefabPath);
				}

				RemoveExistingMeshNode(visual);
				RemoveSpritePresentations(root, visual);
				CreatureMeshPresentation presentation = AttachMesh(visual, visualPrefab, visualPrefabPath);
				RewireAnimator(root, presentation);
				RemoveDuplicateDrivers(root);
				VerifyLocator(root, enemyPrefabPath);

				PrefabUtility.SaveAsPrefabAsset(root, enemyPrefabPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		/// <summary>
		/// Der Einhaengepunkt der Darstellung. Bei den Gegnern heisst er
		/// "Visual", beim Boss "Garon_Visual" — verlaesslicher als der Name ist
		/// der SpriteActorAnimator, der in jedem Fall auf diesem Knoten sitzt.
		/// </summary>
		private static Transform FindVisualNode(GameObject root)
		{
			Transform benannt = root.transform.Find(VisualNodeName);
			if (benannt != null)
			{
				return benannt;
			}
			SpriteActorAnimator animator =
				root.GetComponentInChildren<SpriteActorAnimator>(includeInactive: true);
			return (animator != null) ? animator.transform : null;
		}

		/// <summary>
		/// Die Forge-Gegner betten ihr 2D-Prefab als eigenen Knoten ein und
		/// tragen dadurch ZWEI SpriteActorAnimator und zwei ActorSpriteVfx.
		/// Nach dem Umbau wuerden beide Animatoren dieselbe Kreaturenschicht
		/// ansteuern — der Controller bindet nur den ersten, der zweite liefe
		/// unkoordiniert daneben. Es bleibt je Typ genau der erste (der am
		/// Visual-Knoten, den GetComponentInChildren zuerst liefert; derselbe,
		/// den auch WildlingController.Bind findet).
		/// </summary>
		internal static void RemoveDuplicateDrivers(GameObject root)
		{
			SpriteActorAnimator[] animators =
				root.GetComponentsInChildren<SpriteActorAnimator>(includeInactive: true);
			for (int i = 1; i < animators.Length; i++)
			{
				UnityEngine.Object.DestroyImmediate(animators[i]);
			}
			ActorSpriteVfx[] vfx = root.GetComponentsInChildren<ActorSpriteVfx>(includeInactive: true);
			for (int i = 1; i < vfx.Length; i++)
			{
				UnityEngine.Object.DestroyImmediate(vfx[i]);
			}
		}

		/// <summary>Alten Einbau entfernen — das macht den Lauf wiederholbar.</summary>
		private static void RemoveExistingMeshNode(Transform visual)
		{
			Transform vorhanden = visual.Find(MeshNodeName);
			if (vorhanden != null)
			{
				UnityEngine.Object.DestroyImmediate(vorhanden.gameObject);
			}
		}

		/// <summary>
		/// Entfernt jede Sprite-Darstellung unter der Wurzel. Sitzt sie auf einem
		/// eigenen Knoten (der Regelfall: "HandPaintedSprite"), faellt der ganze
		/// Knoten weg — er traegt nur den SpriteRenderer des Aktorbilds. Sitzt sie
		/// dagegen auf dem Visual-Knoten oder der Wurzel selbst, faellt nur die
		/// Komponente weg, damit Geschwister und Anker stehen bleiben.
		/// </summary>
		internal static void RemoveSpritePresentations(GameObject root, Transform visual)
		{
			SpriteActorPresentation[] sprites =
				root.GetComponentsInChildren<SpriteActorPresentation>(includeInactive: true);
			foreach (SpriteActorPresentation sprite in sprites)
			{
				if (sprite == null)
				{
					continue;
				}
				GameObject knoten = sprite.gameObject;
				if (knoten == root || knoten == visual.gameObject)
				{
					UnityEngine.Object.DestroyImmediate(sprite);
					continue;
				}
				UnityEngine.Object.DestroyImmediate(knoten);
			}
		}

		internal static CreatureMeshPresentation AttachMesh(Transform visual, GameObject visualPrefab,
			string visualPrefabPath)
		{
			var mesh = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, visual);
			mesh.name = MeshNodeName;
			mesh.transform.localPosition = Vector3.zero;
			mesh.transform.localRotation = Quaternion.identity;
			mesh.transform.localScale = Vector3.one;

			CreatureMeshPresentation presentation =
				mesh.GetComponentInChildren<CreatureMeshPresentation>(includeInactive: true);
			if (presentation == null)
			{
				throw new InvalidOperationException("Keine CreatureMeshPresentation in "
					+ visualPrefabPath);
			}
			return presentation;
		}

		/// <summary>
		/// Haengt SpriteActorAnimator.presentationBehaviour um. Der Animator
		/// arbeitet trotz seines Namens ausschliesslich gegen IActorPresentation
		/// und IAuthoredStatePresentation — beide traegt die Kreaturenschicht.
		/// Das Feld wird ueber SerializedObject gesetzt, damit die Aenderung im
		/// Prefab landet.
		/// </summary>
		internal static void RewireAnimator(GameObject root, CreatureMeshPresentation presentation)
		{
			SpriteActorAnimator[] animators =
				root.GetComponentsInChildren<SpriteActorAnimator>(includeInactive: true);
			foreach (SpriteActorAnimator animator in animators)
			{
				var serialized = new SerializedObject(animator);
				SerializedProperty feld = serialized.FindProperty("presentationBehaviour");
				if (feld == null)
				{
					throw new InvalidOperationException(
						"Feld 'presentationBehaviour' nicht am SpriteActorAnimator gefunden.");
				}
				feld.objectReferenceValue = presentation;
				serialized.ApplyModifiedPropertiesWithoutUndo();
			}
		}

		/// <summary>
		/// Beweis statt Hoffnung: der Locator muss jetzt die Kreaturenschicht
		/// liefern. Diese Pruefung ist der eigentliche Zweck des Umbaus — sie
		/// faellt hier auf, nicht erst als stumm stehende Figur im Spiel.
		/// </summary>
		private static void VerifyLocator(GameObject root, string enemyPrefabPath)
		{
			IActorPresentation gefunden = ActorPresentationLocator.Find(root);
			if (gefunden is CreatureMeshPresentation)
			{
				return;
			}
			string was = (gefunden == null) ? "gar keine" : gefunden.GetType().Name;
			throw new InvalidOperationException("Nach dem Umbau liefert der Locator " + was
				+ " statt CreatureMeshPresentation: " + enemyPrefabPath);
		}
	}
}
