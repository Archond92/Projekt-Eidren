using System.IO;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class UiVisualAssetBuilder
{
	private const string ArtFolder = "Assets/_Game/Resources/Art/UI/PhaseE";

	private const string PrefabFolder = "Assets/_Game/Prefabs/UI";

	private static readonly string[] ArtNames = new string[8] { "ui_tech_node_locked", "ui_tech_node_available", "ui_tech_node_unlocked", "ui_tech_connector", "ui_equipment_panel", "ui_progression_hud", "ui_build_menu_entry", "ui_death_bag_marker" };

	private static readonly string[] EquipmentSlotNames = new string[13]
	{
		"Weapon1", "Weapon2", "Head", "Chest", "Hands", "Legs", "Ring1", "Ring2", "Amulet", "Earring",
		"Backpack", "CatchDevice", "Battery"
	};

	[MenuItem("Eidren/Art/Build Phase E UI Prefabs")]
	public static void BuildUiPrefabs()
	{
		EnsureFolder("Assets/_Game/Prefabs", "UI");
		string[] artNames = ArtNames;
		foreach (string artName in artNames)
		{
			string path = "Assets/_Game/Resources/Art/UI/PhaseE/" + artName + ".png";
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("Phase-E UI art is missing: " + path);
			}
			ArtAssetImportUtility.ConfigureSprite(path, 1024);
		}
		BuildTechnologyNodePrefab();
		BuildImagePrefab("TechnologyConnector", "ui_tech_connector", new Vector2(640f, 96f));
		BuildEquipmentPanelPrefab();
		BuildProgressionHudPrefab();
		BuildBuildingMenuEntryPrefab();
		BuildImagePrefab("DeathBagWorldMarker", "ui_death_bag_marker", new Vector2(112f, 112f));
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Verify();
		Debug.Log("Eidren: built authored Phase-E UI prefabs. Capture uses the existing hold-interaction progress visualization.");
	}

	private static void BuildTechnologyNodePrefab()
	{
		string path = "Assets/_Game/Prefabs/UI/TechnologyNodeFrame.prefab";
		if (!(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null))
		{
			GameObject root = RectObject("TechnologyNodeFrame", new Vector2(128f, 128f));
			string[] states = new string[3] { "Locked", "Available", "Unlocked" };
			string[] sprites = new string[3] { "ui_tech_node_locked", "ui_tech_node_available", "ui_tech_node_unlocked" };
			for (int index = 0; index < states.Length; index++)
			{
				ImageChild(root.transform, states[index], Sprite(sprites[index])).SetActive(index == 1);
			}
			SaveAndDestroy(root, path);
		}
	}

	private static void BuildEquipmentPanelPrefab()
	{
		string path = "Assets/_Game/Prefabs/UI/EquipmentPanel.prefab";
		if (!(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null))
		{
			GameObject root = RectObject("EquipmentPanel", new Vector2(960f, 540f));
			root.GetComponent<Image>().sprite = Sprite("ui_equipment_panel");
			for (int index = 0; index < EquipmentSlotNames.Length; index++)
			{
				GameObject gameObject = RectObject(EquipmentSlotNames[index], new Vector2(84f, 84f));
				gameObject.transform.SetParent(root.transform, worldPositionStays: false);
				Image component = gameObject.GetComponent<Image>();
				component.color = new Color(1f, 1f, 1f, 0.001f);
				component.raycastTarget = true;
			}
			SaveAndDestroy(root, path);
		}
	}

	private static void BuildProgressionHudPrefab()
	{
		string path = "Assets/_Game/Prefabs/UI/ProgressionHud.prefab";
		if (!(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null))
		{
			GameObject root = RectObject("ProgressionHud", new Vector2(760f, 128f));
			root.GetComponent<Image>().sprite = Sprite("ui_progression_hud");
			GameObject gameObject = RectObject("ExperienceFill", new Vector2(470f, 34f));
			gameObject.transform.SetParent(root.transform, worldPositionStays: false);
			gameObject.GetComponent<Image>().color = new Color(0.16f, 0.88f, 0.62f, 0.92f);
			Image component = gameObject.GetComponent<Image>();
			component.type = Image.Type.Filled;
			component.fillMethod = Image.FillMethod.Horizontal;
			component.fillAmount = 0f;
			EmptyValueSocket(root.transform, "LevelValue");
			EmptyValueSocket(root.transform, "TechnologyPointValue");
			SaveAndDestroy(root, path);
		}
	}

	private static void BuildBuildingMenuEntryPrefab()
	{
		string path = "Assets/_Game/Prefabs/UI/BuildingMenuEntry_Smelter.prefab";
		if (!(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null))
		{
			GameObject gameObject = RectObject("BuildingMenuEntry_Smelter", new Vector2(720f, 240f));
			gameObject.GetComponent<Image>().sprite = Sprite("ui_build_menu_entry");
			ImageChild(gameObject.transform, "BuildingIcon", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Buildings/Level01/BLD_Smelter_L01.png"));
			ImageChild(gameObject.transform, "WoodCost", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_Wood.png"));
			ImageChild(gameObject.transform, "StoneCost", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Items/ITEM_TMP_Stone.png"));
			EmptyValueSocket(gameObject.transform, "LockedState");
			SaveAndDestroy(gameObject, path);
		}
	}

	private static void BuildImagePrefab(string prefabName, string spriteName, Vector2 size)
	{
		string path = "Assets/_Game/Prefabs/UI/" + prefabName + ".prefab";
		if (!(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null))
		{
			GameObject gameObject = RectObject(prefabName, size);
			gameObject.GetComponent<Image>().sprite = Sprite(spriteName);
			SaveAndDestroy(gameObject, path);
		}
	}

	private static GameObject RectObject(string name, Vector2 size)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		gameObject.GetComponent<RectTransform>().sizeDelta = size;
		gameObject.GetComponent<Image>().raycastTarget = false;
		return gameObject;
	}

	private static GameObject ImageChild(Transform parent, string name, Sprite sprite)
	{
		GameObject gameObject = RectObject(name, Vector2.zero);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = Vector2.zero;
		component.anchorMax = Vector2.one;
		component.offsetMin = Vector2.zero;
		component.offsetMax = Vector2.zero;
		gameObject.GetComponent<Image>().sprite = sprite;
		return gameObject;
	}

	private static void EmptyValueSocket(Transform parent, string name)
	{
		GameObject gameObject = RectObject(name, new Vector2(72f, 40f));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
	}

	private static Sprite Sprite(string name)
	{
		return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Resources/Art/UI/PhaseE/" + name + ".png");
	}

	private static void SaveAndDestroy(GameObject root, string path)
	{
		PrefabUtility.SaveAsPrefabAsset(root, path);
		UnityEngine.Object.DestroyImmediate(root);
	}

	private static void Verify()
	{
		string[] array = new string[6] { "TechnologyNodeFrame", "TechnologyConnector", "EquipmentPanel", "ProgressionHud", "BuildingMenuEntry_Smelter", "DeathBagWorldMarker" };
		foreach (string name in array)
		{
			string path = "Assets/_Game/Prefabs/UI/" + name + ".prefab";
			if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
			{
				throw new InvalidOperationException("UI prefab was not saved: " + path);
			}
		}
	}

	private static void EnsureFolder(string parent, string name)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + name))
		{
			AssetDatabase.CreateFolder(parent, name);
		}
	}
}
}
