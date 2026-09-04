using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Eidren.Tests.EditMode
{
	public sealed class EnemyFeedbackTests
	{
		private const string WildEidraPath = "Assets/_Game/Prefabs/Enemies/WildEidra.prefab";

		private static readonly string[] EnemyPrefabs = new string[10]
		{
			WildEidraPath,
			"Assets/_Game/Prefabs/Enemies/TierTwo/GraniteShell.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/MoorThrower.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/RiftGuardian.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/Riftling.prefab",
			"Assets/_Game/Prefabs/Enemies/TierTwo/RootCharger.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/AshRunner.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/EmberEater.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/ForgeGuardian.prefab",
			"Assets/_Game/Prefabs/Enemies/Forge/SealGuardian.prefab"
		};

		[Test]
		public void JedeGegneranzeige_BesitztEineFuellgrafik()
		{
			foreach (string path in EnemyPrefabs)
			{
				GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				Assert.That(root, Is.Not.Null, "Gegner-Prefab fehlt: " + path);
				foreach (Image image in root.GetComponentsInChildren<Image>(includeInactive: true))
				{
					if (image.type == Image.Type.Filled)
					{
						Assert.That(image.sprite, Is.Not.Null,
							$"Ohne Fuellgrafik ignoriert die Anzeige den Wert und steht dauerhaft voll: {path} → {image.name}");
					}
				}
			}
		}

		[Test]
		public void Staggerbalken_StartetLeer()
		{
			foreach (string path in EnemyPrefabs)
			{
				GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				Assert.That(root, Is.Not.Null, "Gegner-Prefab fehlt: " + path);
				Transform stagger = FindDeep(root.transform, "StaggerFill");
				Assert.That(stagger, Is.Not.Null, "StaggerFill fehlt: " + path);
				Assert.That(stagger.GetComponent<Image>().fillAmount, Is.Zero,
					"Der Staggerbalken muss vor dem ersten Treffer leer sein: " + path);
			}
		}

		/// <summary>
		/// Die Schadenszahl gehoert in die gemeinsame Basis. Mehrere Aufrufer
		/// erzeugen sonst doppelte Zahlen fuer denselben Treffer, einzelne
		/// Controller ohne Aufruf gar keine.
		/// </summary>
		[Test]
		public void Schadenszahl_EntstehtNurInDerGemeinsamenBasis()
		{
			List<string> callers = new List<string>();
			foreach (string file in Directory.GetFiles("Assets/_Game/Scripts", "*.cs", SearchOption.AllDirectories))
			{
				if (File.ReadAllText(file).Contains("CombatFeedback.SpawnDamage"))
				{
					callers.Add(file.Replace('\\', '/'));
				}
			}
			Assert.That(callers.Count, Is.EqualTo(1),
				"Genau ein Aufrufer erwartet, gefunden: " + string.Join(", ", callers));
			Assert.That(callers[0], Does.EndWith("Scripts/AI/EnemyControllerBase.cs"),
				"Der Aufruf gehoert in die gemeinsame Gegnerbasis");
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
	}
}
