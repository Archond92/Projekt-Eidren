using Eidren.Interaction;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class CombatHudPrefabTests
{
	private sealed class FakeInteractable : IInteractable
	{
		public string InteractionId => "test.hold";

		public InteractionType Type => InteractionType.WorldObject;

		public string DisplayText => "HALTEN";

		public Sprite Icon => null;

		public float InteractionRange => 2f;

		public Eidren.Interaction.InteractionMode Mode => Eidren.Interaction.InteractionMode.Hold;

		public float HoldDuration => 2f;

		public int Priority => 1;

		public Vector3 InteractionPosition => InteractionObject.transform.position;

		public GameObject InteractionObject { get; }

		public FakeInteractable(GameObject target)
		{
			InteractionObject = target;
		}

		public bool CanInteract(in InteractionContext context, out string blockedReason)
		{
			blockedReason = string.Empty;
			return true;
		}

		public void BeginInteraction(in InteractionContext context)
		{
		}

		public void UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
		}

		public void CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
		}

		public void CompleteInteraction(in InteractionContext context)
		{
		}

		bool IInteractable.CanInteract(in InteractionContext context, out string blockedReason)
		{
			return CanInteract(in context, out blockedReason);
		}

		void IInteractable.BeginInteraction(in InteractionContext context)
		{
			BeginInteraction(in context);
		}

		void IInteractable.UpdateInteraction(in InteractionContext context, float normalizedProgress)
		{
			UpdateInteraction(in context, normalizedProgress);
		}

		void IInteractable.CancelInteraction(in InteractionContext context, InteractionCancelReason reason)
		{
			CancelInteraction(in context, reason);
		}

		void IInteractable.CompleteInteraction(in InteractionContext context)
		{
			CompleteInteraction(in context);
		}
	}

	private const string PrefabPath = "Assets/_Game/Resources/UI/CombatHUD.prefab";

	[Test]
	public void AuthoredPrefab_HasEveryRequiredViewAndOneInteraction()
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/UI/CombatHUD.prefab");
		Assert.That<GameObject>(gameObject, (IResolveConstraint)(object)Is.Not.Null);
		CombatHUD component = gameObject.GetComponent<CombatHUD>();
		Assert.That<CombatHUD>(component, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Canvas>(component.Canvas, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<SafeAreaPanel>(component.SafeArea, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<CombatHudActionPresenter>(component.Actions, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<CombatHudInteractionPresenter>(component.InteractionView, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<CombatHudInputPresenter>(component.InputView, (IResolveConstraint)(object)Is.Not.Null);
		CombatHudStatusPresenter componentInChildren = gameObject.GetComponentInChildren<CombatHudStatusPresenter>(includeInactive: true);
		Assert.That<Image>(componentInChildren.ExperienceFill, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Text>(componentInChildren.LevelValue, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<InteractionButton[]>(gameObject.GetComponentsInChildren<InteractionButton>(includeInactive: true), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		Assert.That<GameObject>(Find(gameObject, "EidraSkill1"), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<GameObject>(Find(gameObject, "EidraSkill2"), (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<AbilityRangePreview[]>(gameObject.GetComponentsInChildren<AbilityRangePreview>(includeInactive: true), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)2));
	}

	[Test]
	public void RuntimeHudCode_ContainsNoLayoutFactories()
	{
		string root = "Assets/_Game/Scripts/UI";
		string[] hudFiles = Directory.GetFiles(root, "CombatHud*.cs", SearchOption.TopDirectoryOnly).Append(Path.Combine(root, "CombatHUD.cs")).ToArray();
		string[] forbidden = new string[12]
		{
			"BuildCanvas", "BuildPlayerPanel", "BuildBossPanel", "BuildObjective", "BuildJoystick", "BuildActions", "BuildResultPanel", "RoundButton", "PillButton", "CreateShapeSprite",
			"GetBuiltinResource<Font>", "new GameObject"
		};
		string[] array = hudFiles;
		foreach (string path in array)
		{
			string source = File.ReadAllText(path);
			string[] array2 = forbidden;
			foreach (string token in array2)
			{
				Assert.That<string>(source, (IResolveConstraint)(object)Does.Not.Contain(token), path + " still contains runtime UI factory '" + token + "'.", Array.Empty<object>());
			}
		}
	}

	[Test]
	public void ActionCluster_TouchRectsDoNotOverlapOrLeaveBounds()
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/UI/CombatHUD.prefab");
		RectTransform cluster = Find(prefab, "ActionCluster").GetComponent<RectTransform>();
		Assert.That<Vector2>(cluster.anchorMin, (IResolveConstraint)(object)Is.EqualTo((object)new Vector2(1f, 0f)));
		Assert.That<Vector2>(cluster.anchorMax, (IResolveConstraint)(object)Is.EqualTo((object)new Vector2(1f, 0f)));
		Assert.That<Vector2>(cluster.pivot, (IResolveConstraint)(object)Is.EqualTo((object)new Vector2(1f, 0f)));
		string[] names = new string[9] { "AttackAction", "DodgeAction", "EidraSkill1", "EidraSkill2", "WeaponSwitch", "EidraSwitch", "PotionAction", "FoodAction", "InteractionAction" };
		List<Rect> rectangles = names.Select((string name) => RelativeRect(Find(prefab, name).GetComponent<RectTransform>(), cluster)).ToList();
		Rect bounds = new Rect(0f, 0f, 580f, 480f);
		for (int i = 0; i < rectangles.Count; i++)
		{
			Assert.That<bool>(Contains(bounds, rectangles[i]), (IResolveConstraint)(object)Is.True, names[i] + " leaves the 580x480 thumb cluster.", Array.Empty<object>());
			for (int j = i + 1; j < rectangles.Count; j++)
			{
				Assert.That<bool>(rectangles[i].Overlaps(rectangles[j]), (IResolveConstraint)(object)Is.False, names[i] + " overlaps " + names[j] + ".", Array.Empty<object>());
			}
		}
		Assert.That<float>(rectangles[0].width, (IResolveConstraint)(object)Is.EqualTo((object)144f));
		Assert.That<IEnumerable<float>>(from value in rectangles.Skip(4).Take(2)
			select value.width, (IResolveConstraint)(object)Has.All.EqualTo((object)72f));
		Assert.That<IEnumerable<float>>(from value in rectangles.Skip(1).Take(3).Concat(rectangles.Skip(6).Take(3))
			select value.width, (IResolveConstraint)(object)Has.All.EqualTo((object)100f));
	}

	[TestCase(new object[] { 1920, 1080, 0, 1920 })]
	[TestCase(new object[] { 2340, 1080, 100, 2140 })]
	[TestCase(new object[] { 2560, 1600, 48, 2464 })]
	public void ActionCluster_FitsEveryRequiredSafeArea(int width, int height, int safeX, int safeWidth)
	{
		float scale = (float)height / 1080f;
		Assert.That<float>((float)safeWidth / scale, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)628f), $"Safe area {width}x{height} at x={safeX} is too narrow.", Array.Empty<object>());
	}

	[Test]
	public void InteractionAtHalfProgress_HasHalfFilledRing()
	{
		GameObject instance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/UI/CombatHUD.prefab")) as GameObject;
		GameObject target = new GameObject("InteractionTarget");
		try
		{
			CombatHudInteractionPresenter presenter = instance.GetComponent<CombatHUD>().InteractionView;
			MethodInfo method = typeof(CombatHudInteractionPresenter).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
			FakeInteractable interactable = new FakeInteractable(target);
			method.Invoke(presenter, new object[1]
			{
				new InteractionSnapshot(InteractionState.Holding, interactable, canInteract: true, 0.5f, string.Empty)
			});
			Assert.That<float>(presenter.ProgressRing.fillAmount, (IResolveConstraint)(object)Is.EqualTo((object)0.5f).Within((object)0.001f));
			Assert.That<InteractionButton>(presenter.Control, (IResolveConstraint)(object)Is.Not.Null);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(instance);
			UnityEngine.Object.DestroyImmediate(target);
		}
	}

	[Test]
	public void MobilePrefab_HasNoForbiddenPermanentLabels()
	{
		string yaml = File.ReadAllText("Assets/_Game/Resources/UI/CombatHUD.prefab");
		string[] array = new string[6] { "ANGRIFF", "DASH AUSWEICHEN", "SKILL 1", "SKILL 2", "BUFFFOOD", "NICHT VERFÜGBAR" };
		foreach (string value in array)
		{
			Assert.That<string>(yaml, (IResolveConstraint)(object)Does.Not.Contain(value));
		}
	}

	private static GameObject Find(GameObject root, string name)
	{
		return root.GetComponentsInChildren<Transform>(includeInactive: true).FirstOrDefault((Transform value) => value.name == name)?.gameObject;
	}

	private static Rect RelativeRect(RectTransform target, RectTransform parent)
	{
		Vector2 parentSize = parent.sizeDelta;
		return new Rect(target.anchorMin * parentSize + target.anchoredPosition - target.pivot * target.sizeDelta, target.sizeDelta);
	}

	private static bool Contains(Rect outer, Rect inner)
	{
		if (inner.xMin >= outer.xMin && inner.yMin >= outer.yMin && inner.xMax <= outer.xMax)
		{
			return inner.yMax <= outer.yMax;
		}
		return false;
	}
}
}
