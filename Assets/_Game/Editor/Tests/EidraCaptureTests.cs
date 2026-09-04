using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class EidraCaptureTests
{
	private sealed class OpenSpace : IZonePlacementSpace
	{
		public bool IsPlaceable(Vector3 position, float clearance)
		{
			return true;
		}
	}

	private GameObject _databaseObject;

	private ContentDatabase _database;

	private PlayerEquipment _equipment;

	private PlayerInventory _inventory;

	private CaptureEquipment _capture;

	private PlayerProgressionService _playerProgression;

	private TechnologyUnlockService _technology;

	private EidraRoster _roster;

	private EidraRosterService _rosterService;

	[SetUp]
	public void SetUp()
	{
		_databaseObject = new GameObject("EidraCapture_Test");
		_database = _databaseObject.AddComponent<ContentDatabase>();
		_equipment = new PlayerEquipment(_database);
		_inventory = new PlayerInventory(_database);
		_capture = new CaptureEquipment(_equipment, _database, _inventory);
		ProgressionCurveDefinition curve = AssetDatabase.LoadAssetAtPath<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset");
		Assert.That<ProgressionCurveDefinition>(curve, (IResolveConstraint)(object)Is.Not.Null);
		_playerProgression = new PlayerProgressionService(curve);
		TechnologyTreeDefinition tree = AssetDatabase.LoadAssetAtPath<TechnologyTreeDefinition>("Assets/_Game/Resources/Data/Progression/TechnologyTree_V01.asset");
		Assert.That<TechnologyTreeDefinition>(tree, (IResolveConstraint)(object)Is.Not.Null);
		_technology = new TechnologyUnlockService(tree, _playerProgression);
		_roster = new EidraRoster();
		_rosterService = new EidraRosterService(_roster, _technology);
	}

	[TearDown]
	public void TearDown()
	{
		if (_databaseObject != null)
		{
			UnityEngine.Object.DestroyImmediate(_databaseObject);
		}
	}

	[Test]
	public void ConsumedBatteryReloadsOneReplacementFromTheBackpack()
	{
		EquipBattery();
		Assert.That<int>(_inventory.Add("battery", 2), (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(_capture.TryConsumeBattery(out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
		Assert.That<bool>(_equipment.TryGetSlot(EquipmentSlot.Battery, out var replacement), (IResolveConstraint)(object)Is.True, "Der Batterieplatz ist die Kammer; der Rucksack hält Ersatzbatterien.", Array.Empty<object>());
		Assert.That<int>(replacement.Quantity, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(_inventory.GetTotalAmount("battery"), (IResolveConstraint)(object)Is.EqualTo((object)1), "Genau eine Ersatzbatterie rückt nach.", Array.Empty<object>());
	}

	[Test]
	public void WithoutABattery_TheCaptureIsBlockedAndNamesTheReason()
	{
		EquipDevice();
		CaptureCheck check = CaptureResolution.Evaluate(Terrock().Capture, 0.2f, _capture.ReadLoadout());
		Assert.That<bool>(check.CanCapture, (IResolveConstraint)(object)Is.False);
		Assert.That<string>(check.BlockedReason, (IResolveConstraint)(object)Is.EqualTo((object)"BATTERIE FEHLT"), "Der Grund steht VOR dem Halten in der Interaktionsanzeige (M8.3).", Array.Empty<object>());
	}

	[Test]
	public void WithoutTheDevice_TheCaptureIsBlockedAndNamesTheReason()
	{
		EquipBattery();
		CaptureCheck check = CaptureResolution.Evaluate(Terrock().Capture, 0.2f, _capture.ReadLoadout());
		Assert.That<bool>(check.CanCapture, (IResolveConstraint)(object)Is.False);
		Assert.That<string>(check.BlockedReason, (IResolveConstraint)(object)Is.EqualTo((object)"FANGGERÄT FEHLT"));
	}

	[Test]
	public void WithTooLittleCharge_TheCaptureIsBlockedAndNamesBothLevers()
	{
		EquipDevice();
		EquipBattery();
		CaptureCheck check = CaptureResolution.Evaluate(Terrock().Capture, 1f, _capture.ReadLoadout());
		Assert.That<bool>(check.CanCapture, (IResolveConstraint)(object)Is.False, "Ein Terrock bei vollen Lebenspunkten verlangt mehr als eine T1-Batterie liefert.", Array.Empty<object>());
		Assert.That<float>(check.Charge, (IResolveConstraint)(object)Is.LessThan((object)check.Requirement));
		Assert.That<string>(check.BlockedReason, (IResolveConstraint)(object)Does.Contain("LADUNG"));
		Assert.That<string>(check.BlockedReason, (IResolveConstraint)(object)Does.Contain("BEDARF"));
	}

	[Test]
	public void WithEnoughCharge_TheCaptureSucceedsEverySingleTime()
	{
		EquipDevice();
		EquipBattery();
		EidraCaptureProfile profile = Terrock().Capture;
		CaptureLoadout loadout = _capture.ReadLoadout();
		for (int attempt = 0; attempt < 200; attempt++)
		{
			CaptureCheck check = CaptureResolution.Evaluate(in profile, 0.3f, in loadout);
			Assert.That<bool>(check.CanCapture, (IResolveConstraint)(object)Is.True, $"Versuch {attempt}: M8.3 verbietet den Wurf.", Array.Empty<object>());
			Assert.That<string>(check.BlockedReason, (IResolveConstraint)(object)Is.Empty);
		}
	}

	[Test]
	public void TheCaptureDemandFallsWithTheTargetsHealth()
	{
		EidraCaptureProfile profile = Terrock().Capture;
		float num = CaptureResolution.RequirementAt(in profile, 1f);
		float atHalf = CaptureResolution.RequirementAt(in profile, 0.5f);
		float atFloor = CaptureResolution.RequirementAt(in profile, 0f);
		Assert.That<float>(num, (IResolveConstraint)(object)Is.GreaterThan((object)atHalf));
		Assert.That<float>(atHalf, (IResolveConstraint)(object)Is.GreaterThan((object)atFloor));
		Assert.That<float>(num, (IResolveConstraint)(object)Is.EqualTo((object)profile.RequirementAtFullHealth).Within((object)0.001f));
		Assert.That<float>(atFloor, (IResolveConstraint)(object)Is.EqualTo((object)profile.MinimumRequirement).Within((object)0.001f), "Der Bedarf faellt bis zu einem Mindestwert -- sonst waere die Batteriestufe bedeutungslos, sobald man weit genug herunterpruegelt.", Array.Empty<object>());
	}

	[Test]
	public void NoCapturePathUsesUnityRandom()
	{
		string[] array = new string[6] { "Assets/_Game/Scripts/Core/CaptureResolution.cs", "Assets/_Game/Scripts/Core/CaptureEquipment.cs", "Assets/_Game/Scripts/Core/EidraRoster.cs", "Assets/_Game/Scripts/Core/EidraRosterService.cs", "Assets/_Game/Scripts/AI/EidraWildController.cs", "Assets/_Game/Scripts/Interaction/EidraCaptureTarget.cs" };
		foreach (string path in array)
		{
			Assert.That<bool>(File.Exists(path), (IResolveConstraint)(object)Is.True, path, Array.Empty<object>());
			Assert.That<IEnumerable<string>>(from line in (from line in File.ReadAllLines(path)
					where !IsComment(line)
					select line).ToArray()
				where line.Contains("Random")
				select line, (IResolveConstraint)(object)Is.Empty, "M1.2/M8.3: " + path + " darf nichts wuerfeln.", Array.Empty<object>());
		}
	}

	private static bool IsComment(string line)
	{
		string trimmed = line.TrimStart();
		if (!trimmed.StartsWith("//", StringComparison.Ordinal) && !trimmed.StartsWith("*", StringComparison.Ordinal))
		{
			return trimmed.StartsWith("/*", StringComparison.Ordinal);
		}
		return true;
	}

	[Test]
	public void DamageNeverPushesAnEidraBelowItsFleeThreshold()
	{
		EidraCaptureProfile profile = Terrock().Capture;
		float floor = CaptureResolution.FleeHealthFloor(in profile, 150f);
		float health = 150f;
		for (int hit = 0; hit < 20; hit++)
		{
			float applied = CaptureResolution.ClampDamageAboveFlee(in profile, 150f, health, 999f);
			health -= applied;
			Assert.That<float>(health, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)(floor - 0.001f)), $"Schlag {hit}: Die Lebenspunkte duerfen die " + "Fluchtschwelle nie unterschreiten.", Array.Empty<object>());
			Assert.That<float>(health, (IResolveConstraint)(object)Is.GreaterThan((object)0f), "Es gibt keinen Pfad, auf dem ein Eidra stirbt.", Array.Empty<object>());
		}
	}

	[Test]
	public void CapturableEidraCarryNoLootTable()
	{
		EnemyDefinition[] array = CapturableEnemies();
		foreach (EnemyDefinition enemy in array)
		{
			Assert.That<LootTableDefinition>(enemy.LootTable, (IResolveConstraint)(object)Is.Null, enemy.name + ": M8.3 kennt keine Eidra-Beutetabelle und keinen 'toeten statt fangen'-Pfad.", Array.Empty<object>());
			Assert.That<string[]>(enemy.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty, enemy.name, Array.Empty<object>());
		}
	}

	[Test]
	public void OnlyTheCommitPointEverConsumesTheBattery()
	{
		Assert.That<string[]>((from path in Directory.GetFiles("Assets/_Game/Scripts", "*.cs", SearchOption.AllDirectories)
			where File.ReadAllText(path).Contains("TryConsumeBattery")
			select path.Replace('\\', '/') into path
			where !path.EndsWith("Core/CaptureEquipment.cs", StringComparison.Ordinal)
			select path).ToArray(), (IResolveConstraint)(object)Is.EquivalentTo((IEnumerable)new string[1] { "Assets/_Game/Scripts/Interaction/EidraCaptureTarget.cs".Replace('\\', '/') }), "M8.3: Die Batterie wird nur bei Erfolg verbraucht. Sie darf an genau einer Stelle haengen -- dem Commit-Punkt.", Array.Empty<object>());
		string text = File.ReadAllText("Assets/_Game/Scripts/Interaction/EidraCaptureTarget.cs");
		int cancel = text.IndexOf("public void CancelInteraction", StringComparison.Ordinal);
		int complete = text.IndexOf("public void CompleteInteraction", StringComparison.Ordinal);
		Assert.That<int>(cancel, (IResolveConstraint)(object)Is.GreaterThan((object)0));
		Assert.That<int>(complete, (IResolveConstraint)(object)Is.GreaterThan((object)cancel));
		int num = cancel;
		Assert.That<string>(text.Substring(num, complete - num), (IResolveConstraint)(object)Does.Not.Contain("TryConsumeBattery"), "Ein Abbruch kostet nichts.", Array.Empty<object>());
	}

	[Test]
	public void TheSameSpeciesCanBeCapturedTwiceAsTwoInstances()
	{
		UnlockSecondSlot();
		Assert.That<bool>(_rosterService.TryCapture("terrock", out var first, out var error), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_rosterService.TryCapture("terrock", out var second, out error), (IResolveConstraint)(object)Is.True);
		Assert.That<EidraInstanceState[]>(_roster.GetAll(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)2));
		Assert.That<string>(first.InstanceId, (IResolveConstraint)(object)Is.Not.EqualTo((object)second.InstanceId), "Zwei Faenge derselben Art sind zwei Instanzen, kein Flag.", Array.Empty<object>());
		Assert.That<IEnumerable<string>>(from instance in _roster.GetAll()
			select instance.EidraId, (IResolveConstraint)(object)Is.All.EqualTo((object)"terrock"));
	}

	[Test]
	public void TheRosterHoldsInstancesNotSpeciesFlags()
	{
		Assert.That<bool>(typeof(SavePlayerData).GetFields().Any((FieldInfo field) => field.FieldType == typeof(bool)), (IResolveConstraint)(object)Is.False, "Der Spielstand haelt eine Liste von Instanzen (M8.3).", Array.Empty<object>());
		Assert.That<FieldInfo>(typeof(SavePlayerData).GetField("eidraRoster"), (IResolveConstraint)(object)Is.Not.Null);
	}

	[Test]
	public void AtMostTwoEidraAreActiveAtOnce()
	{
		UnlockSecondSlot();
		for (int index = 0; index < 4; index++)
		{
			Assert.That<bool>(_rosterService.TryCapture("terrock", out var _, out var _), (IResolveConstraint)(object)Is.True);
		}
		Assert.That<EidraInstanceState[]>(_roster.GetAll(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)4));
		Assert.That<string[]>(_roster.GetActiveInstanceIds(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)2), "Vier gefangene Eidra, zwei aktive -- der Rest bleibt im Bestand und verstaerkt spaeter Produktionsstaetten (M6).", Array.Empty<object>());
		string[] three = (from eidraInstanceState in _roster.GetAll().Take(3)
			select eidraInstanceState.InstanceId).ToArray();
		Assert.That<bool>(_rosterService.TrySetActiveTeam(three, out var error2), (IResolveConstraint)(object)Is.False, "Drei aktive Eidra sind kein gueltiger Zustand.", Array.Empty<object>());
		Assert.That<string>(error2, (IResolveConstraint)(object)Is.Not.Empty);
	}

	[Test]
	public void TheSecondSlotStaysShutUntilItsTechnologyNode()
	{
		Assert.That<int>(_rosterService.ActiveSlotCapacity, (IResolveConstraint)(object)Is.EqualTo((object)1), "Ohne technology.12 gibt es genau einen Platz.", Array.Empty<object>());
		Assert.That<bool>(_rosterService.TryCapture("terrock", out var instance, out var error), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(_rosterService.TryCapture("noctarion", out instance, out error), (IResolveConstraint)(object)Is.True);
		Assert.That<string[]>(_roster.GetActiveInstanceIds(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1), "Das zweite Eidra liegt im Bestand, nicht im Team.", Array.Empty<object>());
		UnlockSecondSlot();
		Assert.That<int>(_rosterService.ActiveSlotCapacity, (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void TwoRunsWithTheSameSeedPlaceEidraIdentically()
	{
		ZoneDefinition zone = Zone("Zone_Quarry");
		ZoneLayout first = Generate(zone, 4711);
		ZoneLayout second = Generate(zone, 4711);
		Assert.That<IReadOnlyList<ZoneEidraPlacement>>(first.EidraPlacements, (IResolveConstraint)(object)Is.Not.Empty, "Der Steinbruch ist Terrocks Heimatgebiet (M8.3).", Array.Empty<object>());
		Assert.That<int>(second.EidraPlacements.Count, (IResolveConstraint)(object)Is.EqualTo((object)first.EidraPlacements.Count));
		for (int index = 0; index < first.EidraPlacements.Count; index++)
		{
			Assert.That<Vector3>(second.EidraPlacements[index].Position, (IResolveConstraint)(object)Is.EqualTo((object)first.EidraPlacements[index].Position));
			Assert.That<string>(second.EidraPlacements[index].InstanceId, (IResolveConstraint)(object)Is.EqualTo((object)first.EidraPlacements[index].InstanceId));
		}
	}

	[Test]
	public void EachSpeciesHasExactlyOneHomeZone()
	{
		EnemyDefinition[] array = CapturableEnemies();
		foreach (EnemyDefinition enemy in array)
		{
			string[] zones = (from zone in AssetDatabase.FindAssets("t:ZoneDefinition", new string[1] { "Assets/_Game/Data/Zones" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ZoneDefinition>)
				where zone != null && zone.EnemyAllocations.Any((ZoneEnemyAllocation allocation) => allocation.Definition == enemy && allocation.Count > 0)
				select zone.Id).ToArray();
			Assert.That<string[]>(zones, (IResolveConstraint)(object)((enemy.CapturableEidra.Id == "ignivar") ? ((ConstraintExpression)Has.Length).EqualTo((object)0) : ((ConstraintExpression)Has.Length).EqualTo((object)1)), enemy.name + " steht in: " + string.Join(", ", zones), Array.Empty<object>());
		}
	}

	[Test]
	public void EidraPlacementsDoNotDisturbTheNodeLayout()
	{
		ZoneLayout withEidra = Generate(Zone("Zone_Quarry"), 99);
		ZoneLayout again = Generate(Zone("Zone_Quarry"), 99);
		Assert.That<int>(withEidra.Placements.Count, (IResolveConstraint)(object)Is.EqualTo((object)again.Placements.Count));
		for (int index = 0; index < withEidra.Placements.Count; index++)
		{
			Assert.That<Vector3>(again.Placements[index].Position, (IResolveConstraint)(object)Is.EqualTo((object)withEidra.Placements[index].Position));
		}
	}

	private static EnemyDefinition[] CapturableEnemies()
	{
		EnemyDefinition[] array = (from enemy in AssetDatabase.FindAssets("t:EnemyDefinition", new string[1] { "Assets/_Game/Data/Enemies" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<EnemyDefinition>)
			where enemy != null && enemy.IsCapturableEidra
			select enemy).ToArray();
		Assert.That<EnemyDefinition[]>(array, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)3), "v0.2 kennt Terrock, Noctarion und Ignivar (M8.1).", Array.Empty<object>());
		return array;
	}

	private static ZoneLayout Generate(ZoneDefinition zone, int seed)
	{
		ZoneState state = new ZoneState(zone.Id, seed, 2);
		return ZoneLayoutGenerator.Generate(zone, state, new Bounds(Vector3.zero, new Vector3(68f, 0f, 68f)), new OpenSpace());
	}

	private static ZoneDefinition Zone(string assetName)
	{
		ZoneDefinition zoneDefinition = AssetDatabase.LoadAssetAtPath<ZoneDefinition>("Assets/_Game/Data/Zones/" + assetName + ".asset");
		Assert.That<ZoneDefinition>(zoneDefinition, (IResolveConstraint)(object)Is.Not.Null, assetName, Array.Empty<object>());
		return zoneDefinition;
	}

	private static EidraData Terrock()
	{
		EidraData eidraData = AssetDatabase.LoadAssetAtPath<EidraData>("Assets/_Game/Data/Eidren/Terrock.asset");
		Assert.That<EidraData>(eidraData, (IResolveConstraint)(object)Is.Not.Null);
		return eidraData;
	}

	private void EquipDevice()
	{
		Assert.That<bool>(_equipment.TryEquip(EquipmentSlot.CatchDevice, new ItemStack("catch_device", 1), out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
	}

	private void EquipBattery()
	{
		Assert.That<bool>(_equipment.TryEquip(EquipmentSlot.Battery, new ItemStack("battery", 1), out var error), (IResolveConstraint)(object)Is.True, error, Array.Empty<object>());
	}

	private void UnlockSecondSlot()
	{
		_technology.Restore(new string[13]
		{
			"technology.01.workbench", "technology.02.axe", "technology.03.storage_chest", "technology.04.hammer", "technology.05.scythe", "technology.06.daggers", "technology.07.structures", "technology.08.farm_plot", "technology.09.pickaxe", "technology.10.smelter",
			"technology.13.sawmill", "technology.14.ropewalk", "technology.11.catch_device"
		});
		Assert.That<bool>(_technology.IsFeatureUnlocked("feature.eidra_slot_2"), (IResolveConstraint)(object)Is.True);
	}
}
}
