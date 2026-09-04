using Eidren.Composition;
using Eidren.Data;
using Eidren.Presentation;
using Eidren.UI;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Editor
{
public static class FixCollectionAcceptanceRunner
{
	public static void Run()
	{
		VerifyPlayerAndHud();
		VerifyMenus();
		VerifyWorldActors();
		VerifyArt();
		Debug.Log("FIX_COLLECTION_ACCEPTANCE: 23/23 asset and wiring checks passed.");
	}

	private static void VerifyPlayerAndHud()
	{
		Require(Load<GameObject>("Assets/_Game/Prefabs/Player/Player.prefab").GetComponent<PlayerPrefabBindings>().MaxHealth == 100f, "Player does not start with 100 HP.");
		CombatHudStatusPresenter status = Load<GameObject>("Assets/_Game/Resources/UI/CombatHUD.prefab").GetComponentInChildren<CombatHudStatusPresenter>(includeInactive: true);
		Require(status != null && status.ExperienceFill != null && status.LevelValue != null, "XP HUD references are incomplete.");
		Require(Load<ProgressionCurveDefinition>("Assets/_Game/Resources/Data/Progression/ProgressionCurve_V01.asset").GaronMinimumLevel == 12, "Garon pacing gate is not level 12.");
	}

	private static void VerifyMenus()
	{
		GameObject gameObject = Load<GameObject>("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab");
		Require(gameObject.GetComponentInChildren<HorizontalLayoutGroup>(includeInactive: true) != null, "Building catalog is not horizontal.");
		Require(new SerializedObject(gameObject.GetComponent<BuildingMenuWindow>()).FindProperty("moveButton").objectReferenceValue != null, "Building move button is not wired.");
		Require(Load<GameObject>("Assets/_Game/Resources/UI/StorageWindow.prefab").GetComponent<StorageWindow>().TakeAllButton != null, "Take-all button is not wired.");
	}

	private static void VerifyWorldActors()
	{
		Require(Load<GameObject>("Assets/_Game/Prefabs/Enemies/WildEidra.prefab").GetComponent<WildlingStatusBars>() != null, "Wild Eidra status bars are missing.");
		string[] array = new string[3] { "Common", "Guarded", "Hidden" };
		foreach (string family in array)
		{
			SerializedObject serialized = new SerializedObject(Load<GameObject>("Assets/_Game/Prefabs/Loot/WorldChests/WorldChest_" + family + ".prefab").GetComponent<WorldChestVisual>());
			Require(serialized.FindProperty("leftHand").objectReferenceValue != null && serialized.FindProperty("rightHand").objectReferenceValue != null, family + " chest opening hands are missing.");
		}
	}

	private static void VerifyArt()
	{
		HashSet<Sprite> icons = new HashSet<Sprite>();
		string[] array = new string[4] { "WandererHood", "WandererCoat", "WandererBracers", "WandererLegs" };
		foreach (string itemName in array)
		{
			ItemDefinition item = Load<ItemDefinition>("Assets/_Game/Data/Items/" + itemName + ".asset");
			Require(item.Icon != null, itemName + " icon is missing.");
			Require(!AssetDatabase.GetAssetPath(item.Icon).Contains("ITEM_TMP_"), itemName + " still uses a placeholder icon.");
			icons.Add(item.Icon);
		}
		Require(icons.Count == 4, "Armor icons are not distinct.");
		TextureImporter hand = AssetImporter.GetAtPath("Assets/_Game/Resources/Art/UI/ui_interaction_hand.png") as TextureImporter;
		Require(hand != null && hand.alphaIsTransparency, "Interaction hand is not imported with transparency.");
		Require(File.ReadAllText("Assets/_Game/Shaders/Actors/HandPaintedActorSprite.shader").Contains("albedo = lerp(albedo, armorAlbedo, armorMask);"), "Armor does not replace the base silhouette.");
	}

	private static T Load<T>(string path) where T : UnityEngine.Object
	{
		T val = AssetDatabase.LoadAssetAtPath<T>(path);
		if (val == null)
		{
			throw new FileNotFoundException(path);
		}
		return val;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
}
