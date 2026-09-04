using Eidren.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Baut die 3D-Darstellungs-Wrapper der Eidra (Noctarion, Terrock, Ignivar).
	///
	/// Die drei haben kein eigenes Gegner-Prefab: EidraWildController und
	/// EidraTeamController laden ihre Darstellung zur Laufzeit per
	/// Resources.Load und erwarten im geladenen Prefab einen SpriteActorAnimator
	/// (der Wild-Controller castet darauf und ruft Bind). Der Wrapper muss dem
	/// 2D-Prefab deshalb strukturell gleichen — Animator, Sprite-VFX, Anker und
	/// Bodenschatten — nur das Aktorbild ist die Kreaturenschicht.
	///
	/// Gebaut wird er GENAU SO: das 2D-Prefab wird kopiert und mit denselben
	/// Schritten wie ein Gegner-Prefab umgestellt (CreatureEnemyRewire-Helfer).
	/// Damit erben die Wrapper jede Feldkonfiguration des Originals — Stems,
	/// Partikelfarben, Schattenmasse — statt sie hier zu duplizieren.
	///
	/// Idempotent: der Wrapper wird je Lauf vollstaendig neu erzeugt.
	/// </summary>
	public static class EidraVisual3DBuilder
	{
		private const string TargetFolder = "Assets/_Game/Resources/Prefabs/Actors/3D";

		/// <summary>Eidra-Kennung, 2D-Quelle, 3D-Ziel (beide als Asset-Pfade).</summary>
		public static readonly (string Creature, string SourcePrefab, string TargetPrefab)[] Targets =
		{
			("Noctarion", "Assets/_Game/Resources/Prefabs/Actors/2D/Noctarion_2D.prefab",
				TargetFolder + "/Noctarion_3D.prefab"),
			("Terrock", "Assets/_Game/Resources/Prefabs/Actors/2D/Terrock_2D.prefab",
				TargetFolder + "/Terrock_3D.prefab"),
			("Ignivar", "Assets/_Game/Resources/Prefabs/Actors/Forge/Ignivar_2D.prefab",
				TargetFolder + "/Ignivar_3D.prefab")
		};

		[MenuItem("Eidren/V0.2/Eidra-Darstellung auf 3D bauen")]
		public static void BuildAll()
		{
			var fehler = new List<string>();
			foreach ((string creature, string source, string target) in Targets)
			{
				try
				{
					Build(creature, source, target);
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
				Debug.LogError("[K3D] " + fehler.Count + " Eidra-Wrapper nicht gebaut:\n"
					+ string.Join("\n", fehler));
				return;
			}
			Debug.Log("[K3D] " + Targets.Length + " Eidra-Wrapper gebaut.");
		}

		public static void Build(string creature, string sourcePrefabPath, string targetPrefabPath)
		{
			GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
			if (source == null)
			{
				throw new InvalidOperationException("2D-Quelle fehlt: " + sourcePrefabPath);
			}
			string visualPrefabPath = CreatureMeshBuilder.VisualPrefabPath(creature);
			GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualPrefabPath);
			if (visualPrefab == null)
			{
				throw new InvalidOperationException("Darstellungs-Prefab fehlt: " + visualPrefabPath);
			}

			Directory.CreateDirectory(TargetFolder);
			// Kopie ohne Prefab-Verbindung: der Wrapper ist ein eigenstaendiges
			// Prefab, keine Variante — sonst zoege jede 2D-Aenderung still in
			// die 3D-Fassung ein.
			GameObject root = UnityEngine.Object.Instantiate(source);
			try
			{
				root.name = creature + "_3D";
				CreatureEnemyRewire.RemoveSpritePresentations(root, root.transform);
				CreatureMeshPresentation presentation = CreatureEnemyRewire.AttachMesh(
					root.transform, visualPrefab, visualPrefabPath);
				CreatureEnemyRewire.RewireAnimator(root, presentation);
				CreatureEnemyRewire.RemoveDuplicateDrivers(root);

				if (!(ActorPresentationLocator.Find(root) is CreatureMeshPresentation))
				{
					throw new InvalidOperationException(
						"Nach dem Bau liefert der Locator nicht die Kreaturenschicht: " + targetPrefabPath);
				}
				PrefabUtility.SaveAsPrefabAsset(root, targetPrefabPath);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}
	}
}
