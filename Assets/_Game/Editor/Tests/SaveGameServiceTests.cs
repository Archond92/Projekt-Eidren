using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class SaveGameServiceTests
{
	private sealed class MemorySaveFileSystem : ISaveFileSystem
	{
		private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.Ordinal);

		public bool FailReplace { get; set; }

		public int WriteCount { get; private set; }

		public bool FileExists(string path)
		{
			return _files.ContainsKey(path);
		}

		public string ReadAllText(string path)
		{
			return _files[path];
		}

		public void WriteAllText(string path, string contents)
		{
			WriteCount++;
			_files[path] = contents;
		}

		public void Copy(string source, string destination, bool overwrite)
		{
			if (!overwrite && _files.ContainsKey(destination))
			{
				throw new InvalidOperationException();
			}
			_files[destination] = _files[source];
		}

		public void Move(string source, string destination)
		{
			_files[destination] = _files[source];
			_files.Remove(source);
		}

		public void Replace(string source, string destination)
		{
			if (FailReplace)
			{
				throw new InvalidOperationException("Injected failure.");
			}
			_files[destination] = _files[source];
			_files.Remove(source);
		}

		public void Delete(string path)
		{
			_files.Remove(path);
		}

		public void CreateDirectory(string path)
		{
		}

		public void Set(string path, string contents)
		{
			_files[path] = contents;
		}

		public string Get(string path)
		{
			return _files[path];
		}
	}

	private GameObject _root;

	private ContentDatabase _content;

	private GameSession _session;

	private SaveGameService _service;

	private MemorySaveFileSystem _files;

	private PlayerProgressionService _progression;

	private TechnologyUnlockService _technology;

	[SetUp]
	public void SetUp()
	{
		_root = new GameObject("SaveGameService_Test");
		_content = _root.AddComponent<ContentDatabase>();
		_content.Configure(new WeaponData[2]
		{
			Load<WeaponData>("Assets/_Game/Data/Weapons/Hammer.asset"),
			Load<WeaponData>("Assets/_Game/Data/Weapons/Daggers.asset")
		}, new EidraData[2]
		{
			Load<EidraData>("Assets/_Game/Data/Eidren/Terrock.asset"),
			Load<EidraData>("Assets/_Game/Data/Eidren/Noctarion.asset")
		}, Array.Empty<AbilityData>(), Array.Empty<BossData>());
		_session = _root.AddComponent<GameSession>();
		_session.Initialize();
		_session.ConfigureContentDatabase(_content);
		_session.StartNewGame();
		_service = _root.AddComponent<SaveGameService>();
		_files = new MemorySaveFileSystem();
		ProgressionCurveDefinition curve = Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
		_progression = new PlayerProgressionService(curve);
		TechnologyTreeDefinition tree = Load<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
		_technology = new TechnologyUnlockService(tree, _progression);
		_service.Initialize(_session, _content, null, _files, "save-tests", _progression, _technology);
	}

	[TearDown]
	public void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(_root);
	}

	[Test]
	public void RoundTrip_RestoresAllRequiredSessionDataAtHomeBase()
	{
		_session.PlayerInventory.Add("wood", 7);
		int stoneStack = _content.GetItem("stone").MaximumStackSize;
		_session.SetStorageState(new StorageContainerState("home_base.storage.main", CreateStorageSlots("stone", stoneStack)));
		_session.WeaponProgression.TryApply("hammer", 2, 1.15f, 1.1f);
		_session.SetActiveWeaponId("daggers");
		_session.SetActiveEidraId("noctarion");
		_session.SelectWorldMapNode("zone_ember_ruins");
		_session.TrySetProgressFlag("garon_defeated");
		_session.MarkWorldMapNodeCompleted("zone_ember_ruins");
		Assert.That<bool>(_service.SaveNow(SaveRequestReason.HomeBaseEntered, out var saveError), (IResolveConstraint)(object)Is.True, saveError, Array.Empty<object>());
		_session.StartNewGame();
		Assert.That<bool>(_service.TryLoad(out var loadError), (IResolveConstraint)(object)Is.True, loadError, Array.Empty<object>());
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)7));
		Assert.That<int>(_session.WeaponProgression.GetLevel("hammer"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<string>(_session.ActiveWeaponId, (IResolveConstraint)(object)Is.EqualTo((object)"daggers"));
		Assert.That<string>(_session.ActiveEidraId, (IResolveConstraint)(object)Is.EqualTo((object)"noctarion"));
		Assert.That<bool>(_session.HasProgressFlag("garon_defeated"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_session.IsWorldMapNodeCompleted("zone_ember_ruins"), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(_session.CurrentWorldMapNodeId, (IResolveConstraint)(object)Is.EqualTo((object)"home_base"));
		Assert.That<string>(_session.LastSafeSceneKey, (IResolveConstraint)(object)Is.EqualTo((object)"HomeBase"));
		Assert.That<bool>(_session.TryGetStorageState("home_base.storage.main", out var storage), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(storage.Slots[0].Quantity, (IResolveConstraint)(object)Is.EqualTo((object)stoneStack));
	}

	[Test]
	public void ProgressionPointsAndFirstFlags_SurviveRoundTrip()
	{
		_progression.RecordEnemyDefeated(400);
		Assert.That<int>(_progression.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<bool>(_progression.TrySpendTechnologyPoints(1), (IResolveConstraint)(object)Is.True);
		_progression.RecordRecipeCrafted("recipe.saved");
		_progression.RecordBuildingConstructed("building.saved");
		AssertSave();
		_progression.ResetForNewGame();
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(_progression.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(_progression.State.AvailableTechnologyPoints, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_progression.State.SpentTechnologyPoints, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_progression.RecordRecipeCrafted("recipe.saved"), (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_progression.RecordBuildingConstructed("building.saved"), (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void UnlockedTechnologyAndSpentPoint_SurviveRoundTrip()
	{
		_progression.RecordEnemyDefeated(175);
		Assert.That<TechnologyUnlockResult>(_technology.TryUnlock("technology.01.workbench"), (IResolveConstraint)(object)Is.EqualTo((object)TechnologyUnlockResult.Success));
		AssertSave();
		_progression.ResetForNewGame();
		_technology.ResetForNewGame();
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<bool>(_technology.IsBuildingUnlocked("building.workbench"), (IResolveConstraint)(object)Is.True);
		Assert.That<int>(_progression.State.SpentTechnologyPoints, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_progression.State.AvailableTechnologyPoints, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void BuildingPositionRotationAndLevel_SurviveRoundTrip()
	{
		BuildingInstanceState building = new BuildingInstanceState("building-instance-save-test", "building.workbench", new Vector3(4f, 0.25f, -7f), 3, 1);
		Assert.That<bool>(_session.Buildings.TryAdd(building), (IResolveConstraint)(object)Is.True);
		AssertSave();
		_session.StartNewGame();
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<bool>(_session.Buildings.TryGet(building.InstanceId, out var loaded), (IResolveConstraint)(object)Is.True);
		Assert.That<string>(loaded.BuildingId, (IResolveConstraint)(object)Is.EqualTo((object)"building.workbench"));
		Assert.That<Vector3>(loaded.Position, (IResolveConstraint)(object)Is.EqualTo((object)building.Position));
		Assert.That<int>(loaded.QuarterTurns, (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(loaded.Level, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void EidraRosterAndActiveTeam_SurviveRoundTrip()
	{
		EidraInstanceState first = new EidraInstanceState("terrock.001", "terrock");
		EidraInstanceState second = new EidraInstanceState("terrock.002", "terrock");
		Assert.That<bool>(_session.Eidra.TryAdd(first), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_session.Eidra.TryAdd(second), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_session.Eidra.TrySetActiveTeam(new string[2] { first.InstanceId, second.InstanceId }, 2, out var teamError), (IResolveConstraint)(object)Is.True, teamError, Array.Empty<object>());
		AssertSave();
		_session.StartNewGame();
		Assert.That<EidraInstanceState[]>(_session.Eidra.GetAll(), (IResolveConstraint)(object)Is.Empty);
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		EidraInstanceState[] loaded = _session.Eidra.GetAll();
		Assert.That<EidraInstanceState[]>(loaded, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)2));
		Assert.That<IEnumerable<string>>(loaded.Select((EidraInstanceState instance) => instance.InstanceId), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[2] { "terrock.001", "terrock.002" }));
		Assert.That<IEnumerable<string>>(loaded.Select((EidraInstanceState instance) => instance.EidraId), (IResolveConstraint)(object)Is.All.EqualTo((object)"terrock"));
		Assert.That<string[]>(_session.Eidra.GetActiveInstanceIds(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)2));
	}

	[Test]
	public void VersionEightSave_MigratesTheSingleActiveEidraIntoTheRoster()
	{
		AssertSave();
		Assert.That<bool>(_service.TryReadSave(out var data, out var _, out var readError), (IResolveConstraint)(object)Is.True, readError, Array.Empty<object>());
		data.saveVersion = 8;
		data.player.activeEidraId = "terrock";
		data.player.eidraRoster = null;
		data.player.activeEidraInstanceIds = null;
		string legacyJson = JsonUtility.ToJson(data, prettyPrint: true);
		_files.Set(_service.MainPath, legacyJson);
		_files.Set(_service.BackupPath, legacyJson);
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		EidraInstanceState[] all = _session.Eidra.GetAll();
		Assert.That<EidraInstanceState[]>(all, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		Assert.That<string>(all[0].EidraId, (IResolveConstraint)(object)Is.EqualTo((object)"terrock"));
		Assert.That<string>(all[0].InstanceId, (IResolveConstraint)(object)Is.EqualTo((object)"terrock.001"));
		Assert.That<string[]>(_session.Eidra.GetActiveInstanceIds(), (IResolveConstraint)(object)Is.EqualTo((object)new string[1] { "terrock.001" }));
	}

	[Test]
	public void VersionEightSaveWithoutAnEidra_MigratesToAnEmptyRoster()
	{
		AssertSave();
		Assert.That<bool>(_service.TryReadSave(out var data, out var _, out var readError), (IResolveConstraint)(object)Is.True, readError, Array.Empty<object>());
		data.saveVersion = 8;
		data.player.activeEidraId = string.Empty;
		data.player.eidraRoster = null;
		data.player.activeEidraInstanceIds = null;
		string legacyJson = JsonUtility.ToJson(data, prettyPrint: true);
		_files.Set(_service.MainPath, legacyJson);
		_files.Set(_service.BackupPath, legacyJson);
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<EidraInstanceState[]>(_session.Eidra.GetAll(), (IResolveConstraint)(object)Is.Empty);
		Assert.That<string[]>(_session.Eidra.GetActiveInstanceIds(), (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void VersionSixSave_MigratesWithNoBuildings()
	{
		AssertSave();
		Assert.That<bool>(_service.TryReadSave(out var data, out var _, out var readError), (IResolveConstraint)(object)Is.True, readError, Array.Empty<object>());
		data.saveVersion = 6;
		data.buildings = null;
		string legacyJson = JsonUtility.ToJson(data, prettyPrint: true);
		_files.Set(_service.MainPath, legacyJson);
		_files.Set(_service.BackupPath, legacyJson);
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<BuildingInstanceState[]>(_session.Buildings.GetAll(), (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void VersionFiveSave_MigratesWithInitialToolTechnologies()
	{
		AssertSave();
		Assert.That<bool>(_service.TryReadSave(out var data, out var _, out var readError), (IResolveConstraint)(object)Is.True, readError, Array.Empty<object>());
		data.saveVersion = 5;
		data.progression.unlockedTechnologyNodeIds = null;
		string legacyJson = JsonUtility.ToJson(data, prettyPrint: true);
		_files.Set(_service.MainPath, legacyJson);
		_files.Set(_service.BackupPath, legacyJson);
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<string[]>(_technology.CaptureUnlockedNodeIds(), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[3] { "technology.02.axe", "technology.05.scythe", "technology.09.pickaxe" }));
		Assert.That<int>(_progression.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	[Test]
	public void MaximumLevelOverflow_IsAbsentAfterRoundTrip()
	{
		_progression.RecordEnemyDefeated(100000);
		Assert.That<int>(_progression.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)24));
		Assert.That<int>(_progression.State.Experience, (IResolveConstraint)(object)Is.EqualTo((object)_progression.State.ExperienceToNextLevel));
		AssertSave();
		_progression.ResetForNewGame();
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(_progression.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)24));
		Assert.That<int>(_progression.State.Experience, (IResolveConstraint)(object)Is.EqualTo((object)_progression.State.ExperienceToNextLevel));
		Assert.That<int>(_progression.RecordEnemyDefeated(50), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void VersionFourSave_MigratesToStageOneLevelOneNoPoints()
	{
		AssertSave();
		Assert.That<bool>(_service.TryReadSave(out var data, out var _, out var readError), (IResolveConstraint)(object)Is.True, readError, Array.Empty<object>());
		data.saveVersion = 4;
		data.progression = null;
		string legacyJson = JsonUtility.ToJson(data, prettyPrint: true);
		_files.Set(_service.MainPath, legacyJson);
		_files.Set(_service.BackupPath, legacyJson);
		_progression.RecordEnemyDefeated(5000);
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(_progression.State.Stage, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_progression.State.Level, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_progression.State.AvailableTechnologyPoints, (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_progression.State.SpentTechnologyPoints, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void CorruptMain_LoadsValidBackup()
	{
		_session.PlayerInventory.Add("wood", 2);
		AssertSave();
		_session.PlayerInventory.Add("stone", 3);
		AssertSave();
		_files.Set(_service.MainPath, "{broken");
		_session.StartNewGame();
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("stone"), (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void CorruptMainAndBackup_FailsWithoutDestroyingSession()
	{
		_files.Set(_service.MainPath, "{broken");
		_files.Set(_service.BackupPath, "[]");
		int potions = _session.PlayerInventory.GetTotalAmount("healing_potion");
		Assert.That<bool>(_service.TryLoad(out var _), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("healing_potion"), (IResolveConstraint)(object)Is.EqualTo((object)potions));
	}

	[Test]
	public void UnknownItem_IsQuarantinedAndSkipped()
	{
		AssertSave();
		Assert.That<bool>(_service.TryReadSave(out var data, out var _, out var readError), (IResolveConstraint)(object)Is.True, readError, Array.Empty<object>());
		data.player.inventory[0] = new SaveItemStackData
		{
			itemId = "removed_item",
			quantity = 5
		};
		_files.Set(_service.MainPath, JsonUtility.ToJson(data, prettyPrint: true));
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("removed_item"), (IResolveConstraint)(object)Is.Zero);
		Assert.That<int>(_service.LastQuarantine.Length, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<string>(_service.LastQuarantine[0].stableId, (IResolveConstraint)(object)Is.EqualTo((object)"removed_item"));
	}

	[Test]
	public void FailedAtomicReplace_PreservesPreviousMainSave()
	{
		AssertSave();
		string original = _files.Get(_service.MainPath);
		_session.PlayerInventory.Add("wood", 1);
		_files.FailReplace = true;
		Assert.That<bool>(_service.SaveNow(SaveRequestReason.CraftingCompleted, out var _), (IResolveConstraint)(object)Is.False);
		Assert.That<string>(_files.Get(_service.MainPath), (IResolveConstraint)(object)Is.EqualTo((object)original));
		Assert.That<bool>(_files.FileExists(_service.TempPath), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void FailedRepairAfterBackupLoad_PreservesValidBackup()
	{
		_session.PlayerInventory.Add("wood", 2);
		AssertSave();
		_session.PlayerInventory.Add("stone", 3);
		AssertSave();
		string validBackup = _files.Get(_service.BackupPath);
		_files.Set(_service.MainPath, "{broken");
		Assert.That<bool>(_service.TryLoad(out var loadError), (IResolveConstraint)(object)Is.True, loadError, Array.Empty<object>());
		_files.FailReplace = true;
		Assert.That<bool>(_service.SaveNow(SaveRequestReason.HomeBaseEntered, out var _), (IResolveConstraint)(object)Is.False);
		Assert.That<string>(_files.Get(_service.BackupPath), (IResolveConstraint)(object)Is.EqualTo((object)validBackup));
	}

	[Test]
	public void BeginNewGame_PreservesOldSaveUntilFirstSuccessfulSave()
	{
		_session.PlayerInventory.Add("wood", 4);
		AssertSave();
		string oldSave = _files.Get(_service.MainPath);
		_service.BeginNewGame();
		_session.StartNewGame();
		Assert.That<string>(_files.Get(_service.MainPath), (IResolveConstraint)(object)Is.EqualTo((object)oldSave));
		Assert.That<bool>(_service.SaveNow(SaveRequestReason.HomeBaseEntered, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<string>(_files.Get(_service.MainPath), (IResolveConstraint)(object)Is.Not.EqualTo((object)oldSave));
	}

	[Test]
	public void FutureVersion_IsRejectedAndDoesNotEnableContinue()
	{
		SaveGameData data = new SaveGameData
		{
			saveVersion = 15
		};
		_files.Set(_service.MainPath, JsonUtility.ToJson(data));
		Assert.That<bool>(_service.HasAnySaveFile(), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_service.HasRecoverableSave(), (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(_service.TryLoad(out var _), (IResolveConstraint)(object)Is.False);
	}

	[Test]
	public void Durability_SurvivesSaveAndLoad()
	{
		ItemStack[] slots = _session.PlayerInventory.ExportSlots();
		slots[0] = new ItemStack("hammer", 1, "hammer-instance", 42);
		Assert.That<bool>(_session.PlayerInventory.TryImportSlots(slots, out var importError), (IResolveConstraint)(object)Is.True, importError, Array.Empty<object>());
		AssertSave();
		_session.StartNewGame();
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(_session.PlayerInventory.ExportSlots()[0].Durability, (IResolveConstraint)(object)Is.EqualTo((object)42));
	}

	[Test]
	public void SaveWithoutDurabilityField_MigratesWithDefaultValues()
	{
		_session.PlayerInventory.Add("wood", 3);
		AssertSave();
		Assert.That<bool>(_service.TryReadSave(out var data, out var _, out var readError), (IResolveConstraint)(object)Is.True, readError, Array.Empty<object>());
		data.saveVersion = 1;
		string legacyJson = JsonUtility.ToJson(data).Replace(",\"durability\":0", string.Empty);
		Assert.That<string>(legacyJson, (IResolveConstraint)(object)Does.Not.Contain("durability"));
		_files.Set(_service.MainPath, legacyJson);
		_files.Set(_service.BackupPath, legacyJson);
		_session.StartNewGame();
		Assert.That<bool>(_service.TryLoad(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<int>(_session.PlayerInventory.GetTotalAmount("wood"), (IResolveConstraint)(object)Is.EqualTo((object)3));
		Assert.That<int>(_session.PlayerInventory.ExportSlots()[0].Durability, (IResolveConstraint)(object)Is.Zero);
	}

	[Test]
	public void ReentrantSaveRequest_IsRejectedWithoutSecondWrite()
	{
		bool nestedResult = true;
		string nestedError = string.Empty;
		_service.SaveCompleted += TryNestedSave;
		try
		{
			Assert.That<bool>(_service.SaveNow(SaveRequestReason.HomeBaseEntered, out var outerError), (IResolveConstraint)(object)Is.True, outerError, Array.Empty<object>());
		}
		finally
		{
			_service.SaveCompleted -= TryNestedSave;
		}
		Assert.That<bool>(nestedResult, (IResolveConstraint)(object)Is.False);
		StringAssert.Contains("already in progress", nestedError);
		Assert.That<int>(_files.WriteCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		void TryNestedSave(SaveRequestReason _)
		{
			nestedResult = _service.SaveNow(SaveRequestReason.CraftingCompleted, out nestedError);
		}
	}

	private void AssertSave()
	{
		Assert.That<bool>(_service.SaveNow(SaveRequestReason.HomeBaseEntered, out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
	}

	private static ItemStack[] CreateStorageSlots(string itemId, int quantity)
	{
		ItemStack[] array = new ItemStack[24];
		array[0] = new ItemStack(itemId, quantity);
		return array;
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		Assert.That<T>(val, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		return val;
	}
}
}
