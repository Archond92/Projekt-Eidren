using Eidren.Composition;
using Eidren.Input;
using Eidren.UI;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
public sealed class CombatHudIntegrationTests
{
	[UnityTest]
	public IEnumerator HomeBase_UsesOneAuthoredHudAndSwitchesDisplayOnly()
	{
		yield return Load("HomeBase");
		PlayerPrefabBindings player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
		PlayerInputReader input = Object.FindFirstObjectByType<PlayerInputReader>();
		Assert.That<PlayerPrefabBindings>(player, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<PlayerInputReader>(input, (IResolveConstraint)(object)Is.Not.Null);
		CombatHUD hud = player.CombatHud;
		Assert.That<CombatHUD>(hud, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<CombatHUD[]>(Object.FindObjectsByType<CombatHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		Assert.That<InteractionButton[]>(Object.FindObjectsByType<InteractionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		int instanceId = hud.GetInstanceID();
		input.SetDisplayFamily(InputDisplayFamily.Touch);
		yield return null;
		Assert.That<bool>(hud.InputView.JoystickRoot.activeSelf, (IResolveConstraint)(object)Is.True);
		input.SetDisplayFamily(InputDisplayFamily.KeyboardMouse);
		yield return null;
		Assert.That<bool>(hud.InputView.JoystickRoot.activeSelf, (IResolveConstraint)(object)Is.False);
		Assert.That<bool>(hud.InputView.KeyboardHints.activeSelf, (IResolveConstraint)(object)Is.True);
		input.SetDisplayFamily(InputDisplayFamily.Gamepad);
		yield return null;
		Assert.That<bool>(hud.InputView.GamepadHints.activeSelf, (IResolveConstraint)(object)Is.True);
		Assert.That<int>(hud.GetInstanceID(), (IResolveConstraint)(object)Is.EqualTo((object)instanceId));
	}

	[UnityTest]
	public IEnumerator TouchButtons_ReachEveryExistingInputEvent()
	{
		yield return Load("HomeBase");
		PlayerInputReader playerInputReader = Object.FindFirstObjectByType<PlayerInputReader>();
		CombatHUD hud = Object.FindFirstObjectByType<CombatHUD>();
		Assert.That<PlayerInputReader>(playerInputReader, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<CombatHUD>(hud, (IResolveConstraint)(object)Is.Not.Null);
		int attack = 0;
		int dodge = 0;
		int weapon = 0;
		int eidra = 0;
		int firstSkill = 0;
		int secondSkill = 0;
		int potion = 0;
		int food = 0;
		playerInputReader.AttackPressed += delegate
		{
			attack++;
		};
		playerInputReader.DodgePressed += delegate
		{
			dodge++;
		};
		playerInputReader.SwitchWeaponPressed += delegate
		{
			weapon++;
		};
		playerInputReader.SwitchEidraPressed += delegate
		{
			eidra++;
		};
		playerInputReader.SkillPressed += delegate(int index)
		{
			if (index == 0)
			{
				firstSkill++;
			}
			if (index == 1)
			{
				secondSkill++;
			}
		};
		playerInputReader.ConsumablePressed += delegate(int index)
		{
			if (index == 0)
			{
				potion++;
			}
			if (index == 1)
			{
				food++;
			}
		};
		Press(hud, "AttackAction");
		Press(hud, "DodgeAction");
		Press(hud, "WeaponSwitch");
		Press(hud, "EidraSwitch");
		Press(hud, "EidraSkill1");
		Press(hud, "EidraSkill2");
		Press(hud, "PotionAction");
		Press(hud, "FoodAction");
		yield return null;
		Assert.That<int>(attack, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(dodge, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(weapon, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(eidra, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(firstSkill, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(secondSkill, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(potion, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<int>(food, (IResolveConstraint)(object)Is.EqualTo((object)1));
	}

	private static void Press(CombatHUD hud, string name)
	{
		hud.GetComponentsInChildren<Transform>(includeInactive: true).First((Transform value) => value.name == name).GetComponent<Button>()
			.onClick.Invoke();
	}

	private static IEnumerator Load(string scene)
	{
		AsyncOperation load = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
		Assert.That<AsyncOperation>(load, (IResolveConstraint)(object)Is.Not.Null);
		while (!load.isDone)
		{
			yield return null;
		}
		for (int i = 0; i < 4; i++)
		{
			yield return null;
		}
	}
}
}
