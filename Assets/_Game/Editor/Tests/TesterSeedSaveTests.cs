using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Tester-Spielstand: Der ausgelieferte Voll-Ausbau-Stand (Maximallevel,
	/// komplette Eisenausruestung, alle Gebiete und Technologien) muss ueber
	/// den echten Ladepfad wiederherstellbar sein — nicht nur als Textdatei
	/// existieren.
	/// </summary>
	public sealed class TesterSeedSaveTests
	{
		// Nicht unter Resources/ — sonst laege der Voll-Ausbau-Stand in jedem
		// oeffentlichen Build. Er bleibt hier pruefbar und wird nur fuer
		// Tester-Pakete nach Resources kopiert.
		private const string SeedAssetPath = "Assets/_Game/Editor/TesterSeed/TesterSeedSave.json";
		private const string TreePath = "Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset";
		private const string CurvePath = "Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset";
		private const string BuildingFolder = "Assets/_Game/Data/Buildings";

		private sealed class MemoryFiles : ISaveFileSystem
		{
			private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.Ordinal);

			public bool FileExists(string path) => _files.ContainsKey(path);

			public string ReadAllText(string path) => _files[path];

			public void WriteAllText(string path, string contents) => _files[path] = contents;

			public void Copy(string source, string destination, bool overwrite) => _files[destination] = _files[source];

			public void Move(string source, string destination)
			{
				_files[destination] = _files[source];
				_files.Remove(source);
			}

			public void Replace(string source, string destination)
			{
				_files[destination] = _files[source];
				_files.Remove(source);
			}

			public void Delete(string path) => _files.Remove(path);

			public void CreateDirectory(string path)
			{
			}
		}

		private GameObject _root;
		private ContentDatabase _content;
		private GameSession _session;
		private SaveGameService _service;
		private MemoryFiles _files;
		private PlayerProgressionService _progression;
		private TechnologyUnlockService _technology;

		[SetUp]
		public void SetUp()
		{
			_root = new GameObject("TesterSeedSave_Test");
			_content = _root.AddComponent<ContentDatabase>();
			_content.Configure(
				new[]
				{
					Load<WeaponData>("Assets/_Game/Data/Weapons/IronSpear.asset"),
					Load<WeaponData>("Assets/_Game/Data/Weapons/IronDaggers.asset")
				},
				new[]
				{
					Load<EidraData>("Assets/_Game/Data/Eidren/Terrock.asset"),
					Load<EidraData>("Assets/_Game/Data/Eidren/Noctarion.asset"),
					Load<EidraData>("Assets/_Game/Data/Eidren/Ignivar.asset")
				},
				Array.Empty<AbilityData>(), Array.Empty<BossData>());
			_session = _root.AddComponent<GameSession>();
			_session.Initialize();
			_session.ConfigureContentDatabase(_content);
			_session.StartNewGame();
			_service = _root.AddComponent<SaveGameService>();
			_files = new MemoryFiles();
			_progression = new PlayerProgressionService(Load<ProgressionCurveDefinition>(CurvePath));
			_technology = new TechnologyUnlockService(Load<TechnologyTreeDefinition>(TreePath), _progression,
				_session.HasProgressFlag);
			_service.Initialize(_session, _content, null, _files, "seed-tests", _progression, _technology);
		}

		[TearDown]
		public void TearDown()
		{
			UnityEngine.Object.DestroyImmediate(_root);
		}

		[Test]
		public void SeedSpielstand_StelltMaximalstufeUndAlleTechnologienWiederHer()
		{
			LoadSeed();

			ProgressionCurveDefinition curve = Load<ProgressionCurveDefinition>(CurvePath);
			Assert.That(curve.TryGetStage(2, out ProgressionStageCurve stageTwo), Is.True);
			PlayerProgressionSnapshot state = _progression.State;
			Assert.That(state.Stage, Is.EqualTo(2), "Der Seed muss auf der hoechsten Stufe stehen");
			Assert.That(state.Level, Is.EqualTo(stageTwo.MaximumLevel), "Der Seed muss das Maximallevel tragen");

			TechnologyTreeDefinition tree = Load<TechnologyTreeDefinition>(TreePath);
			string[] unlockable = UnlockableNodeIds(tree);
			Assert.That(unlockable, Is.Not.Empty);
			foreach (string nodeId in unlockable)
			{
				Assert.That(_technology.GetNodeState(nodeId), Is.EqualTo(TechnologyNodeState.Unlocked),
					"Technologie nicht freigeschaltet: " + nodeId);
			}
		}

		[Test]
		public void SeedSpielstand_OeffnetAlleGebiete()
		{
			LoadSeed();

			// Die drei Tier-2-Gebiete haengen am Fortschrittsflag, die uebrigen
			// sind von Haus aus offen.
			Assert.That(_session.HasProgressFlag("tier_2_unlocked"), Is.True, "tier_2_unlocked fehlt");
			Assert.That(_session.HasProgressFlag("garon_defeated"), Is.True, "garon_defeated fehlt");
			foreach (string nodeId in new[]
			{
				"home_base", "zone_greenwood", "zone_quarry", "zone_marsh",
				"zone_ember_ruins", "zone_twilight_grove", "zone_veil_marsh", "zone_grey_rifts"
			})
			{
				Assert.That(_session.IsWorldMapNodeVisited(nodeId), Is.True, "Gebiet nicht bereist: " + nodeId);
			}
		}

		[Test]
		public void SeedSpielstand_TraegtKompletteEisenausruestung()
		{
			LoadSeed();

			AssertEquipped(EquipmentSlot.Head, "armor_iron_helmet");
			AssertEquipped(EquipmentSlot.Chest, "armor_iron_chest");
			AssertEquipped(EquipmentSlot.Hands, "armor_iron_gloves");
			AssertEquipped(EquipmentSlot.Legs, "armor_iron_legs");
			AssertEquipped(EquipmentSlot.Weapon1, "iron_spear");
			AssertEquipped(EquipmentSlot.Weapon2, "iron_daggers");
		}

		[Test]
		public void SeedSpielstand_EnthaeltMaterialFuerJedenGebaeudetyp()
		{
			LoadSeed();
			var totals = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (string guid in AssetDatabase.FindAssets("t:BuildingCostDefinition", new[] { BuildingFolder }))
			{
				BuildingCostDefinition building = AssetDatabase.LoadAssetAtPath<BuildingCostDefinition>(AssetDatabase.GUIDToAssetPath(guid));
				Assert.That(building, Is.Not.Null);
				foreach (CraftingIngredient ingredient in building.Cost)
				{
					totals.TryGetValue(ingredient.ItemId, out int current);
					totals[ingredient.ItemId] = current + ingredient.Amount;
				}
			}
			Assert.That(totals, Is.Not.Empty);
			foreach (KeyValuePair<string, int> total in totals)
				Assert.That(_session.PlayerInventory.GetTotalAmount(total.Key), Is.GreaterThanOrEqualTo(total.Value), total.Key);
		}

		[Test]
		public void SeedSpielstand_BringtAlleDreiEidraMit()
		{
			LoadSeed();

			string[] roster = _session.Eidra.GetAll().Select(instance => instance.EidraId).ToArray();
			Assert.That(roster, Is.EquivalentTo(new[] { "terrock", "noctarion", "ignivar" }));
			Assert.That(_session.Eidra.GetActiveInstanceIds().Length, Is.EqualTo(2),
				"Beide Eidra-Plaetze sollen belegt sein");
		}

		[Test]
		public void SeedSpielstand_LaesstDieStillgelegtenKnotenAus()
		{
			// F31-007: Stillgelegte Knoten sind aus dem Baum entfernt; der Seed
			// darf weder das Flag noch die alten IDs enthalten.
			SaveGameData seed = ReadSeed();
			TechnologyTreeDefinition tree = Load<TechnologyTreeDefinition>(TreePath);
			Assert.That(tree.Nodes.Where(node => string.Equals(node.RequiredProgressFlag, "retired.never", StringComparison.Ordinal)),
				Is.Empty, "Seit F31-007 gibt es keine stillgelegten Knoten mehr.");
			foreach (string nodeId in new[] { "technology.12.second_eidra_slot", "technology.19.t1_weapons", "technology.21.cloth_coat", "technology.22.cloth_bracers", "technology.23.cloth_shoes" })
			{
				Assert.That(seed.progression.unlockedTechnologyNodeIds, Does.Not.Contain(nodeId));
			}
		}

		[Test]
		public void SeedEinspielen_SchreibtNurWennNochKeinSpielstandDaIst()
		{
			TextAsset seed = AssetDatabase.LoadAssetAtPath<TextAsset>(SeedAssetPath);
			Assert.That(seed, Is.Not.Null, SeedAssetPath);

			Assert.That(_service.TrySeedFromTemplate(seed.text, out string error), Is.True, error);
			Assert.That(_files.FileExists(_service.MainPath), Is.True, "Seed wurde nicht geschrieben");

			// Ein vorhandener Spielstand des Testers darf nie ueberschrieben werden.
			_files.WriteAllText(_service.MainPath, "{\"saveVersion\":13}");
			Assert.That(_service.TrySeedFromTemplate(seed.text, out error), Is.False,
				"Vorhandener Spielstand wurde ueberschrieben");
			Assert.That(_files.ReadAllText(_service.MainPath), Is.EqualTo("{\"saveVersion\":13}"));
		}

		[Test]
		public void NeuesSpiel_UebernimmtDenTesterSpielstandDirekt()
		{
			// Wer bereits einen eigenen Spielstand hat, erreicht den
			// Voll-Ausbau-Stand ueber "NEUES SPIEL" — ohne Dateiarbeit.
			TextAsset seed = AssetDatabase.LoadAssetAtPath<TextAsset>(SeedAssetPath);
			Assert.That(seed, Is.Not.Null, SeedAssetPath);
			_session.StartNewGame();

			Assert.That(_service.TryApplyTemplate(seed.text, out string error), Is.True, error);

			Assert.That(_progression.State.Level, Is.EqualTo(40));
			Assert.That(_session.HasProgressFlag("tier_2_unlocked"), Is.True);
			AssertEquipped(EquipmentSlot.Chest, "armor_iron_chest");
		}

		[Test]
		public void SeedSpielstand_LiegtNichtUnterResources()
		{
			// Unter Resources/ wandert die Datei in JEDEN Build und schaltet dem
			// Spieler beim ersten Start alles frei — Maximallevel, alle Gebiete,
			// komplette Eisenausruestung. Fuer das v0.2-Testpaket war das
			// gewollt, fuer ein oeffentliches Release ist es falsch.
			Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(
					"Assets/_Game/Resources/Data/TesterSeedSave.json"),
				Is.Null,
				"Der Voll-Ausbau-Spielstand liegt wieder unter Resources und wuerde ausgeliefert");
		}

		[Test]
		public void SeedEinspielen_LehntUnbrauchbareVorlageAb()
		{
			Assert.That(_service.TrySeedFromTemplate("kein json", out string error), Is.False);
			Assert.That(error, Is.Not.Empty);
			Assert.That(_files.FileExists(_service.MainPath), Is.False);
		}

		// ------------------------------------------------------------------

		private void LoadSeed()
		{
			TextAsset seed = AssetDatabase.LoadAssetAtPath<TextAsset>(SeedAssetPath);
			Assert.That(seed, Is.Not.Null, "Seed-Spielstand fehlt: " + SeedAssetPath);
			_files.WriteAllText(_service.MainPath, seed.text);
			Assert.That(_service.TryLoad(out string error), Is.True, "Seed laedt nicht: " + error);
			Assert.That(_service.LastQuarantine, Is.Empty, "Seed enthaelt unbrauchbare Eintraege");
		}

		private static SaveGameData ReadSeed()
		{
			TextAsset seed = AssetDatabase.LoadAssetAtPath<TextAsset>(SeedAssetPath);
			Assert.That(seed, Is.Not.Null, "Seed-Spielstand fehlt: " + SeedAssetPath);
			return JsonUtility.FromJson<SaveGameData>(seed.text);
		}

		private void AssertEquipped(EquipmentSlot slot, string itemId)
		{
			Assert.That(_session.PlayerEquipment.TryGetSlot(slot, out ItemStack stack), Is.True,
				"Slot leer: " + slot);
			Assert.That(stack.ItemId, Is.EqualTo(itemId), slot.ToString());
			Assert.That(stack.Durability, Is.GreaterThan(0), "Ausruestung ohne Haltbarkeit: " + itemId);
		}

		private static string[] UnlockableNodeIds(TechnologyTreeDefinition tree)
		{
			return tree.Nodes
				.Where(node => !string.Equals(node.RequiredProgressFlag, "retired.never", StringComparison.Ordinal))
				.Select(node => node.Id)
				.ToArray();
		}

		private static T Load<T>(string path) where T : UnityEngine.Object
		{
			T asset = AssetDatabase.LoadAssetAtPath<T>(path);
			Assert.That(asset, Is.Not.Null, path);
			return asset;
		}
	}
}
