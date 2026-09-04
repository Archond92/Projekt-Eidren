using Eidren.Composition;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class MobileReadinessTests
{
	[Test]
	public void BuildScenes_StartWithBootstrapThenMainMenu()
	{
		string[] array = (from scene in EditorBuildSettings.scenes
			where scene.enabled
			select scene.path).ToArray();
		Assert.That<int>(array.Length, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)2));
		Assert.That<string>(array[0], (IResolveConstraint)(object)Is.EqualTo((object)"Assets/_Game/Scenes/Bootstrap.unity"));
		Assert.That<string>(array[1], (IResolveConstraint)(object)Is.EqualTo((object)"Assets/_Game/Scenes/MainMenu.unity"));
	}

	[TestCase(new object[] { 1920, 1080, 0, 0, 1920, 1080 })]
	[TestCase(new object[] { 2160, 1080, 80, 0, 2000, 1080 })]
	[TestCase(new object[] { 2340, 1080, 100, 0, 2140, 1080 })]
	[TestCase(new object[] { 2560, 1600, 48, 0, 2464, 1600 })]
	public void SafeArea_ProducesBoundedLandscapeAnchors(int width, int height, int x, int y, int safeWidth, int safeHeight)
	{
		SafeAreaPanel.CalculateAnchors(new Rect(x, y, safeWidth, safeHeight), width, height, out var min, out var max);
		Assert.That<float>(min.x, (IResolveConstraint)(object)Is.InRange((IComparable)0f, (IComparable)1f));
		Assert.That<float>(min.y, (IResolveConstraint)(object)Is.InRange((IComparable)0f, (IComparable)1f));
		Assert.That<float>(max.x, (IResolveConstraint)(object)Is.InRange((IComparable)0f, (IComparable)1f));
		Assert.That<float>(max.y, (IResolveConstraint)(object)Is.InRange((IComparable)0f, (IComparable)1f));
		Assert.That<float>(max.x, (IResolveConstraint)(object)Is.GreaterThan((object)min.x));
		Assert.That<float>(max.y, (IResolveConstraint)(object)Is.GreaterThan((object)min.y));
	}

	[Test]
	public void BackRouting_StopsAfterHighestPriorityConsumer()
	{
		GameObject root = new GameObject("MenuInputService_Test");
		MenuInputService input = root.AddComponent<MenuInputService>();
		List<string> calls = new List<string>();
		Func<bool> pause = delegate
		{
			calls.Add("pause");
			return true;
		};
		Func<bool> submenu = delegate
		{
			calls.Add("submenu");
			return true;
		};
		input.RegisterBackHandler(pause, 100);
		input.RegisterBackHandler(submenu, 200);
		input.PressBack();
		Assert.That<List<string>>(calls, (IResolveConstraint)(object)Is.EqualTo((object)new string[1] { "submenu" }));
		UnityEngine.Object.DestroyImmediate(root);
	}

	[TestCase("Assets/_Game/Resources/UI/PauseMenu.prefab")]
	[TestCase("Assets/_Game/Resources/UI/InventoryWindow.prefab")]
	[TestCase("Assets/_Game/Resources/UI/CraftingWindow.prefab")]
	[TestCase("Assets/_Game/Resources/UI/StorageWindow.prefab")]
	[TestCase("Assets/_Game/Resources/UI/BossEncounterView.prefab")]
	[TestCase("Assets/_Game/Resources/UI/CombatHUD.prefab")]
	[TestCase("Assets/_Game/Resources/UI/BuildingMenuWindow.prefab")]
	[TestCase("Assets/_Game/UI/WorldMap/Prefabs/WorldMapCanvas.prefab")]
	public void MobileUiPrefab_HasSafeArea(string path)
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
		Assert.That<SafeAreaPanel>(gameObject.GetComponentInChildren<SafeAreaPanel>(includeInactive: true), (IResolveConstraint)(object)Is.Not.Null, path, Array.Empty<object>());
	}
}
}
