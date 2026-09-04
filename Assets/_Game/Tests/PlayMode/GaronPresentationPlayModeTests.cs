using Eidren.AI;
using Eidren.Composition;
using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class GaronPresentationPlayModeTests
{
	/// <summary>
	/// Seit dem Gegner-Umbau traegt der Boss die 3D-Darstellung
	/// (CreatureMeshPresentation statt SpriteActorPresentation). Der Test
	/// prueft am LEBENDEN Boss in Zone_EmberRuins, dass alle neun
	/// Boss-Zustaende ueber ihre Sprite-Staemme in spielbare Clips
	/// aufgeloest werden — die Vorgaengerfassung pruefte dasselbe an den
	/// Sprite-Kacheln (Garon2D_LoadsAllBossStatesAtProductionScale).
	/// </summary>
	[UnityTest]
	public IEnumerator Garon3D_LoadsAllBossStatesAtProductionScale()
	{
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
		EidrenServiceRoot eidrenServiceRoot = EidrenServiceRoot.FindOrCreate();
		eidrenServiceRoot.GameSession.StartNewGame();
		eidrenServiceRoot.PlayerProgression.RecordEnemyDefeated(10525);
		yield return SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
		float timeout = Time.realtimeSinceStartup + 8f;
		BossController boss = null;
		while (boss == null && Time.realtimeSinceStartup < timeout)
		{
			boss = UnityEngine.Object.FindFirstObjectByType<BossAreaController>()?.ActiveBoss;
			yield return null;
		}
		Assert.That<BossController>(boss, (IResolveConstraint)(object)Is.Not.Null);

		// Keine Sprite-Darstellung mehr am Boss — ein Nebeneinander waere von
		// der Kindreihenfolge abhaengig (ActorPresentationLocator nimmt den
		// ersten Treffer).
		Assert.That<SpriteActorPresentation>(
			boss.GetComponentInChildren<SpriteActorPresentation>(true),
			(IResolveConstraint)(object)Is.Null);

		CreatureMeshPresentation presentation =
			boss.GetComponentInChildren<CreatureMeshPresentation>(true);
		SpriteActorAnimator animator = boss.GetComponentInChildren<SpriteActorAnimator>(true);
		Assert.That<CreatureMeshPresentation>(presentation, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<SpriteActorAnimator>(animator, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<float>(presentation.WorldHeight,
			(IResolveConstraint)(object)Is.EqualTo((object)4.5f).Within((object)0.001f));

		animator.enabled = false;
		presentation.SetFacing(ActorFacing8.NE);
		string[] states = new string[9] { "idle", "move", "front", "charge", "spin", "hit", "stagger", "return", "death" };
		string[] array = states;
		foreach (string state in array)
		{
			presentation.SetAuthoredState(state, 0.35f, loop: false, restart: true);
			yield return null;
			string clip = CreatureMeshPresentation.StemToClip(state);
			Assert.That<string>(clip, (IResolveConstraint)(object)Is.Not.Null, state, Array.Empty<object>());
			Assert.That<string>(presentation.CurrentClip, (IResolveConstraint)(object)Is.EqualTo((object)clip), state, Array.Empty<object>());
		}
		DynamicActorGroundShadow componentInChildren2 = boss.GetComponentInChildren<DynamicActorGroundShadow>();
		Assert.That<DynamicActorGroundShadow>(componentInChildren2, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Sprite>(componentInChildren2.ShadowRenderer.sprite, (IResolveConstraint)(object)Is.Not.Null);
	}
}
}
