using Eidren.Combat;
using Eidren.Gameplay.Presentation;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class V02PlayerPresentationBuilder
{
	private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";

	private const string SpearSpritePath = "Assets/_Game/Art/Items/ITEM_TMP_CopperSpear.png";

	private const string ItemArt = "Assets/_Game/Art/Items/ITEM_TMP_";

	[MenuItem("Eidren/V0.2/Build Player Spear Presentation")]
	public static void Build()
	{
		GameObject root = PrefabUtility.LoadPrefabContents("Assets/_Game/Prefabs/Player/Player.prefab");
		try
		{
			Transform driver = root.transform.Find("WeaponDriver");
			if (driver == null)
			{
				throw new InvalidOperationException("Player prefab has no WeaponDriver.");
			}
			PlayerWeaponVisual presentation = driver.GetComponent<PlayerWeaponVisual>();
			if (presentation == null)
			{
				throw new InvalidOperationException("WeaponDriver has no PlayerWeaponVisual.");
			}
			Transform spear = driver.Find("Spear");
			if (spear == null)
			{
				spear = new GameObject("Spear").transform;
				spear.SetParent(driver, worldPositionStays: false);
			}
			spear.localPosition = new Vector3(0.44f, 0.93f, 0f);
			spear.localRotation = Quaternion.Euler(0f, 0f, -34f);
			spear.localScale = Vector3.one * 0.31f;
			SpriteRenderer renderer = spear.GetComponent<SpriteRenderer>();
			if (renderer == null)
			{
				renderer = spear.gameObject.AddComponent<SpriteRenderer>();
			}
			renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_CopperSpear.png");
			SpriteRenderer hammerRenderer = ((presentation.HammerRoot != null) ? presentation.HammerRoot.GetComponent<SpriteRenderer>() : null);
			if (hammerRenderer != null)
			{
				renderer.sharedMaterial = hammerRenderer.sharedMaterial;
			}
			renderer.sortingOrder = 14;
			presentation.Configure(presentation.HammerRoot, presentation.DaggersRoot, spear.gameObject);
			presentation.ConfigureNamedSprites(LoadSprite("Sealbreaker"), LoadSprite("AshFangs"), LoadSprite("EmberThorn"));
			ConfigureHarvestPresentation(root, hammerRenderer);
			EditorUtility.SetDirty(presentation);
			PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Player/Player.prefab");
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("Built v0.2 player spear presentation.");
	}

	private static void ConfigureHarvestPresentation(GameObject root, SpriteRenderer materialSource)
	{
		PlayerHarvestVisual harvest = root.GetComponent<PlayerHarvestVisual>();
		if (harvest == null)
		{
			harvest = root.AddComponent<PlayerHarvestVisual>();
		}
		Transform tool = root.transform.Find("HarvestToolPresentation");
		if (tool == null)
		{
			tool = new GameObject("HarvestToolPresentation").transform;
			tool.SetParent(root.transform, worldPositionStays: false);
		}
		SpriteRenderer renderer = tool.GetComponent<SpriteRenderer>();
		if (renderer == null)
		{
			renderer = tool.gameObject.AddComponent<SpriteRenderer>();
		}
		renderer.sharedMaterial = ((materialSource != null) ? materialSource.sharedMaterial : null);
		renderer.sortingOrder = 14;
		tool.localScale = Vector3.one * 0.28f;
		string[] ids = new string[9] { "axe", "scythe", "pickaxe", "copper_axe", "copper_scythe", "copper_pickaxe", "iron_axe", "iron_scythe", "iron_pickaxe" };
		string[] names = new string[9] { "Axe", "Scythe", "Pickaxe", "CopperAxe", "CopperScythe", "CopperPickaxe", "IronAxe", "IronScythe", "IronPickaxe" };
		Sprite[] sprites = new Sprite[names.Length];
		for (int index = 0; index < names.Length; index++)
		{
			sprites[index] = LoadSprite(names[index]);
		}
		harvest.ConfigurePresentation(renderer, ids, sprites);
		EditorUtility.SetDirty(harvest);
	}

	private static Sprite LoadSprite(string assetName)
	{
		return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_" + assetName + ".png");
	}
}
}
