using Eidren.Composition;
using Eidren.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using UnityEditor;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Editor
{
public static class PauseSettingsContentBuilder
{
	private const string MixerPath = "Assets/_Game/Resources/Audio/EidrenAudioMixer.mixer";

	private const string PrefabPath = "Assets/_Game/Resources/UI/PauseMenu.prefab";

	[MenuItem("Eidren/Pause & Settings/Build V0.1")]
	public static void Build()
	{
		EnsureDirectory("Assets/_Game/Resources/Audio");
		EnsureDirectory("Assets/_Game/Resources/UI");
		BuildMixer();
		BuildPausePrefab();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static void BuildMixer()
	{
		if (!(AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Game/Resources/Audio/EidrenAudioMixer.mixer") != null))
		{
			Type controllerType = FindEditorType("UnityEditor.Audio.AudioMixerController");
			Type exposedType = FindEditorType("UnityEditor.Audio.ExposedAudioParameter");
			if (controllerType == null || exposedType == null)
			{
				throw new InvalidOperationException("Unity AudioMixer editor types are unavailable.");
			}
			object controller = controllerType.GetMethod("CreateMixerControllerAtPath", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[1] { "Assets/_Game/Resources/Audio/EidrenAudioMixer.mixer" });
			if (controller == null)
			{
				throw new InvalidOperationException("Unity could not create the Eidren AudioMixer.");
			}
			object master = controllerType.GetProperty("masterGroup", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(controller);
			if (master == null)
			{
				throw new InvalidOperationException("Created AudioMixer has no master group.");
			}
			((UnityEngine.Object)master).name = "Master";
			MethodInfo createGroup = controllerType.GetMethod("CreateNewGroup", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			MethodInfo addChild = controllerType.GetMethod("AddChildToParent", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			object music = CreateGroup(controller, master, "Music", createGroup, addChild);
			object sfx = CreateGroup(controller, master, "SFX", createGroup, addChild);
			CreateGroup(controller, master, "UI", createGroup, addChild);
			SetExposedParameters(controller, controllerType, exposedType, new(object, string)[3]
			{
				(master, "MasterVolume"),
				(music, "MusicVolume"),
				(sfx, "SFXVolume")
			});
			EditorUtility.SetDirty((UnityEngine.Object)controller);
		}
	}

	private static object CreateGroup(object controller, object parent, string name, MethodInfo create, MethodInfo add)
	{
		object group = create.Invoke(controller, new object[2] { name, false });
		add.Invoke(controller, new object[2] { group, parent });
		return group;
	}

	private static void SetExposedParameters(object controller, Type controllerType, Type exposedType, (object group, string name)[] definitions)
	{
		Array exposed = Array.CreateInstance(exposedType, definitions.Length);
		FieldInfo guidField = exposedType.GetField("guid", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		FieldInfo nameField = exposedType.GetField("name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		for (int index = 0; index < definitions.Length; index++)
		{
			object group = definitions[index].group;
			MethodInfo getGuid = group.GetType().GetMethod("GetGUIDForVolume", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			object value = Activator.CreateInstance(exposedType);
			guidField.SetValue(value, getGuid.Invoke(group, null));
			nameField.SetValue(value, definitions[index].name);
			exposed.SetValue(value, index);
		}
		controllerType.GetProperty("exposedParameters", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(controller, exposed);
	}

	private static void BuildPausePrefab()
	{
		GameObject root = new GameObject("PauseMenu", typeof(RectTransform), typeof(PauseMenuController));
		GameObject canvasObject = new GameObject("CanvasRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasObject.transform.SetParent(root.transform, worldPositionStays: false);
		Canvas component = canvasObject.GetComponent<Canvas>();
		component.renderMode = RenderMode.ScreenSpaceOverlay;
		component.sortingOrder = 1500;
		CanvasScaler component2 = canvasObject.GetComponent<CanvasScaler>();
		component2.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		component2.referenceResolution = new Vector2(1920f, 1080f);
		component2.matchWidthOrHeight = 1f;
		RectTransform safe = CreateRect(canvasObject.transform, "SafeArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
		safe.gameObject.AddComponent<SafeAreaPanel>();
		Button pauseButton = CreateButton(safe, "PauseButton", "II", new Vector2(0.93f, 0.91f), new Vector2(0.98f, 0.99f), Vector2.zero, Vector2.zero, 30);
		GameObject pausePanel = CreateOverlay(safe, "PausePanel");
		RectTransform parent = CreateCard(pausePanel.transform, new Vector2(620f, 720f));
		CreateText(parent, "Title", "PAUSE", 48, new Vector2(0f, 275f), new Vector2(520f, 70f));
		Button resume = CardButton(parent, "ResumeButton", "FORTSETZEN", 175f);
		Button settings = CardButton(parent, "SettingsButton", "EINSTELLUNGEN", 75f);
		Button home = CardButton(parent, "HomeButton", "ZUR HEIMATBASIS", -25f);
		Button mainMenu = CardButton(parent, "MainMenuButton", "HAUPTMENÜ", -125f);
		Button quit = CardButton(parent, "QuitButton", "SPIEL BEENDEN", -225f);
		GameObject settingsPanel = CreateOverlay(safe, "SettingsPanel");
		RectTransform settingsCard = CreateCard(settingsPanel.transform, new Vector2(980f, 850f));
		CreateText(settingsCard, "Title", "EINSTELLUNGEN", 42, new Vector2(0f, 350f), new Vector2(760f, 60f));
		Text masterValue;
		Slider masterSlider = CreateSliderRow(settingsCard, "Master", "GESAMTLAUTSTÄRKE", 245f, out masterValue);
		Text musicValue;
		Slider musicSlider = CreateSliderRow(settingsCard, "Music", "MUSIKLAUTSTÄRKE", 145f, out musicValue);
		Text sfxValue;
		Slider sfxSlider = CreateSliderRow(settingsCard, "SFX", "EFFEKTLAUTSTÄRKE", 45f, out sfxValue);
		Dropdown quality = CreateDropdownRow(settingsCard, "Quality", "GRAFIKQUALITÄT", -75f, new string[3] { "Low", "Medium", "High" });
		Dropdown frameRate = CreateDropdownRow(settingsCard, "FrameRate", "FRAMERATE", -185f, new string[2] { "30 FPS", "60 FPS" });
		Button settingsBack = CardButton(settingsCard, "BackButton", "ZURÜCK", -310f);
		GameObject confirmationPanel = CreateOverlay(safe, "ConfirmationPanel");
		RectTransform parent2 = CreateCard(confirmationPanel.transform, new Vector2(760f, 430f));
		Text confirmationText = CreateText(parent2, "Message", "Bestätigen?", 32, new Vector2(0f, 100f), new Vector2(650f, 130f));
		Text feedback = CreateText(parent2, "Feedback", string.Empty, 22, new Vector2(0f, 10f), new Vector2(650f, 65f));
		Button confirm = CreateButton(parent2, "ConfirmButton", "BESTÄTIGEN", new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.3f), Vector2.zero, Vector2.zero, 25);
		Button cancel = CreateButton(parent2, "CancelButton", "ABBRECHEN", new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.3f), Vector2.zero, Vector2.zero, 25);
		pausePanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		confirmationPanel.SetActive(value: false);
		root.GetComponent<PauseMenuController>().ConfigureReferences(canvasObject, pauseButton, pausePanel, resume, settings, home, mainMenu, quit, settingsPanel, masterSlider, musicSlider, sfxSlider, masterValue, musicValue, sfxValue, quality, frameRate, settingsBack, confirmationPanel, confirmationText, feedback, confirm, cancel);
		PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Resources/UI/PauseMenu.prefab");
		UnityEngine.Object.DestroyImmediate(root);
	}

	private static GameObject CreateOverlay(Transform parent, string name)
	{
		RectTransform rectTransform = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
		rectTransform.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.025f, 0.035f, 0.88f);
		return rectTransform.gameObject;
	}

	private static RectTransform CreateCard(Transform parent, Vector2 size)
	{
		RectTransform rectTransform = CreateRect(parent, "Card", Vector2.one * 0.5f, Vector2.one * 0.5f, -size * 0.5f, size * 0.5f);
		rectTransform.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.11f, 0.13f, 0.99f);
		return rectTransform;
	}

	private static Button CardButton(Transform parent, string name, string label, float y)
	{
		return CreateButton(parent, name, label, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(-235f, y - 38f), new Vector2(235f, y + 38f), 26);
	}

	private static Slider CreateSliderRow(Transform parent, string name, string label, float y, out Text valueText)
	{
		CreateText(parent, name + "Label", label, 24, new Vector2(-250f, y), new Vector2(360f, 55f));
		GameObject gameObject = DefaultControls.CreateSlider(default(DefaultControls.Resources));
		gameObject.name = name + "Slider";
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		Vector2 anchorMin = (component.anchorMax = Vector2.one * 0.5f);
		component.anchorMin = anchorMin;
		component.anchoredPosition = new Vector2(120f, y);
		component.sizeDelta = new Vector2(420f, 38f);
		Slider component2 = gameObject.GetComponent<Slider>();
		component2.minValue = 0f;
		component2.maxValue = 1f;
		component2.value = 1f;
		valueText = CreateText(parent, name + "Value", "100 %", 23, new Vector2(390f, y), new Vector2(130f, 50f));
		return component2;
	}

	private static Dropdown CreateDropdownRow(Transform parent, string name, string label, float y, string[] options)
	{
		CreateText(parent, name + "Label", label, 24, new Vector2(-250f, y), new Vector2(360f, 55f));
		GameObject gameObject = DefaultControls.CreateDropdown(default(DefaultControls.Resources));
		gameObject.name = name + "Dropdown";
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		Vector2 anchorMin = (component.anchorMax = Vector2.one * 0.5f);
		component.anchorMin = anchorMin;
		component.anchoredPosition = new Vector2(190f, y);
		component.sizeDelta = new Vector2(360f, 60f);
		Dropdown component2 = gameObject.GetComponent<Dropdown>();
		component2.ClearOptions();
		component2.AddOptions(new List<string>(options));
		return component2;
	}

	private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize)
	{
		RectTransform rectTransform = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
		Image image = rectTransform.gameObject.AddComponent<Image>();
		image.color = new Color(0.12f, 0.28f, 0.29f, 0.98f);
		Button button = rectTransform.gameObject.AddComponent<Button>();
		button.targetGraphic = image;
		RectTransform rectTransform2 = CreateText(rectTransform, "Label", label, fontSize, Vector2.zero, Vector2.zero).rectTransform;
		rectTransform2.anchorMin = Vector2.zero;
		rectTransform2.anchorMax = Vector2.one;
		rectTransform2.offsetMin = Vector2.zero;
		rectTransform2.offsetMax = Vector2.zero;
		return button;
	}

	private static Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
	{
		Text text = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, position - size * 0.5f, position + size * 0.5f).gameObject.AddComponent<Text>();
		text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		text.text = value;
		text.fontSize = fontSize;
		text.alignment = TextAnchor.MiddleCenter;
		text.color = new Color(0.94f, 0.94f, 0.88f, 1f);
		text.raycastTarget = false;
		return text;
	}

	private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMin = anchorMin;
		component.anchorMax = anchorMax;
		component.offsetMin = offsetMin;
		component.offsetMax = offsetMax;
		return component;
	}

	private static Type FindEditorType(string fullName)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			Type type = assemblies[i].GetType(fullName);
			if (type != null)
			{
				return type;
			}
		}
		return null;
	}

	private static void EnsureDirectory(string path)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}
}
}
