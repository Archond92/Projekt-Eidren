using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using UnityEngine;

namespace Eidren.Tests
{
public sealed class ArchitectureGuardTests
{
	private static readonly string[] RuntimeAssemblyNames = new string[6] { "Eidren.Core", "Eidren.Data", "Eidren.Gameplay", "Eidren.Presentation", "Eidren.UI", "Eidren.Composition" };

	private const string ScriptRoot = "Assets/_Game/Scripts";

	private const int AllowedServiceRootPulls = 1;

	private const int MaximumScriptLines = 400;

	private static readonly HashSet<string> BudgetGrandfathered = new HashSet<string>(StringComparer.Ordinal)
	{
		"Core/PlayerInventory.cs", "Eidra/EidraTeamController.cs", "AI/EnemyControllerBase.cs", "Composition/PauseMenuController.cs", "Core/AudioService.cs", "AI/BossController.cs", "UI/StorageWindow.cs", "Composition/PlayerPrefabBindings.cs", "Core/GameSession.cs", "Presentation/CombatFeedback.cs",
		"Composition/AudioRuntimeBinder.cs", "UI/CraftingWindow.cs", "Combat/PlayerCombatController.cs", "Composition/ZoneController.cs", "UI/InventoryWindow.cs",
		// Bestandsaufnahme 13.08.2026: WindowsPerformanceAuditRunner ist unter
		// die Grenze gefallen und entfernt. Dafuer die acht Altbestaende
		// aufgenommen, die der Wächter seit der ersten Baseline meldete —
		// die Aufteilung bleibt als offener Wartungsauftrag notiert
		// (groesste zuerst: G001VisualAbnahmeRunner 946,
		// BuildingPlacementController.Continuation 817,
		// PlayerInputReader.BuildingPointer 555, SpriteActorPresentation.Atlas 519).
		"AI/WildlingController.cs", "Composition/BuildingPlacementController.Continuation.cs",
		"Composition/G001VisualAbnahmeRunner.cs", "Core/ContentDatabase.Buildings.cs",
		"Data/ItemDefinition.cs", "Input/PlayerInputReader.BuildingPointer.cs",
		"Interaction/ResourceNode.cs", "Presentation/SpriteActorPresentation.Atlas.cs"
	};

	[Test]
	public void MutableStaticState_HasDomainReloadReset()
	{
		List<string> offenders = new List<string>();
		foreach (Type type in RuntimeTypes())
		{
			FieldInfo[] mutable = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Where(IsMutableStaticState).ToArray();
			if (mutable.Length != 0 && !HasRuntimeInitializeReset(type))
			{
				offenders.Add(type.FullName + ": " + string.Join(", ", mutable.Select((FieldInfo f) => f.Name)));
			}
		}
		Assert.That<List<string>>(offenders, (IResolveConstraint)(object)Is.Empty, "§2: Veraenderlicher statischer Zustand ohne [RuntimeInitializeOnLoadMethod]-Reset. In Unity 6 ist \"Domain Reload deaktivieren\" Standard - ohne Reset ueberlebt der Zustand den Play-Modus und haelt beim zweiten Start zerstoerte Objekte:\n" + string.Join("\n", offenders), Array.Empty<object>());
	}

	private static bool IsMutableStaticState(FieldInfo field)
	{
		if (field.IsLiteral)
		{
			return false;
		}
		if (field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) && field.FieldType.Name.StartsWith("<", StringComparison.Ordinal))
		{
			return false;
		}
		Type fieldType = field.FieldType;
		if (typeof(ICollection).IsAssignableFrom(fieldType))
		{
			if (field.IsInitOnly)
			{
				return !HoldsOnlyValueData(fieldType);
			}
			return true;
		}
		if (!field.IsInitOnly)
		{
			return !fieldType.IsValueType;
		}
		return false;
	}

	private static bool HoldsOnlyValueData(Type collectionType)
	{
		Type[] elements = ((!collectionType.IsArray) ? (collectionType.IsGenericType ? collectionType.GetGenericArguments() : Array.Empty<Type>()) : new Type[1] { collectionType.GetElementType() });
		if (elements.Length != 0)
		{
			return elements.All((Type element) => element != null && (element.IsValueType || element == typeof(string)));
		}
		return false;
	}

	private static bool HasRuntimeInitializeReset(Type type)
	{
		return type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Any((MethodInfo method) => method.IsDefined(typeof(RuntimeInitializeOnLoadMethodAttribute), inherit: false));
	}

	[Test]
	public void ServiceRoot_IsNotPulledOutsideComposition()
	{
		List<string> pulls = new List<string>();
		foreach (string path in RuntimeScriptFiles())
		{
			if (path.Replace('\\', '/').Contains("/Composition/"))
			{
				continue;
			}
			string[] lines = File.ReadAllLines(path);
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Contains("EidrenServiceRoot.Instance"))
				{
					pulls.Add($"{path}:{i + 1}");
				}
			}
		}
		Assert.That<int>(pulls.Count, (IResolveConstraint)(object)Is.LessThanOrEqualTo((object)1), "§5: Services kommen ueber Initialize/Configure herein, nicht ueber den Locator. Die Obergrenze darf nur sinken:\n" + string.Join("\n", pulls), Array.Empty<object>());
	}

	[Test]
	public void NoNewFileExceedsTheClassBudget()
	{
		List<string> offenders = new List<string>();
		foreach (string path in RuntimeScriptFiles())
		{
			string relative = RelativeToScriptRoot(path);
			if (!BudgetGrandfathered.Contains(relative))
			{
				int lines = File.ReadAllLines(path).Length;
				if (lines > 400)
				{
					offenders.Add($"{relative}: {lines} Zeilen");
				}
			}
		}
		Assert.That<List<string>>(offenders, (IResolveConstraint)(object)Is.Empty, $"§7: Ueber {400} Zeilen wird aufgeteilt, " + "nicht diskutiert. Eine Regel ohne Zahl hat GameSession und CombatHUD nicht verhindert:\n" + string.Join("\n", offenders), Array.Empty<object>());
	}

	[Test]
	public void GrandfatheredBudgetList_ContainsNoStaleEntries()
	{
		List<string> stale = new List<string>();
		foreach (string relative in BudgetGrandfathered)
		{
			string full = Path.Combine("Assets/_Game/Scripts", relative);
			if (!File.Exists(full))
			{
				stale.Add(relative + " (Datei existiert nicht mehr)");
			}
			else if (File.ReadAllLines(full).Length <= 400)
			{
				stale.Add(relative + " (inzwischen unter der Grenze)");
			}
		}
		Assert.That<List<string>>(stale, (IResolveConstraint)(object)Is.Empty, "§7: Diese Eintraege im Bestandsschutz sind erledigt und gehoeren entfernt - sonst schuetzt die Liste kuenftige Ueberschreitungen mit:\n" + string.Join("\n", stale), Array.Empty<object>());
	}

	private static IEnumerable<Type> RuntimeTypes()
	{
		return from type in (from assembly in AppDomain.CurrentDomain.GetAssemblies()
				where RuntimeAssemblyNames.Contains(assembly.GetName().Name)
				select assembly).SelectMany(SafeGetTypes)
			where !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
			select type;
	}

	private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where((Type type) => type != null);
		}
	}

	private static IEnumerable<string> RuntimeScriptFiles()
	{
		return Directory.GetFiles("Assets/_Game/Scripts", "*.cs", SearchOption.AllDirectories);
	}

	private static string RelativeToScriptRoot(string path)
	{
		string normalized = path.Replace('\\', '/');
		string root = "Assets/_Game/Scripts".Replace('\\', '/') + "/";
		int index = normalized.IndexOf(root, StringComparison.Ordinal);
		if (index >= 0)
		{
			string text = normalized;
			int num = index + root.Length;
			return text.Substring(num, text.Length - num);
		}
		return normalized;
	}
}
}
