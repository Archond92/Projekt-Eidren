using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests
{
public sealed class NoctarionPresentationPlayModeTests
{
	[UnityTest]
	public IEnumerator Noctarion3D_LoadsAllCompanionStates()
	{
		// Leere Welt erzwingen: Dieser Test baut am Weltursprung auf.
		yield return Eidren.Tests.PlayMode.Testumgebung.LeereWeltBereitstellen();
		GameObject cameraObject = new GameObject("Main Camera");
		cameraObject.tag = "MainCamera";
		cameraObject.AddComponent<Camera>().orthographic = true;
		cameraObject.transform.SetPositionAndRotation(new Vector3(5f, 7f, -7f), Quaternion.Euler(35f, -35f, 0f));
		GameObject lightObject = new GameObject("Main Light");
		lightObject.AddComponent<Light>().type = LightType.Directional;
		lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
		GameObject prefab = Resources.Load<GameObject>("Prefabs/Actors/3D/Noctarion_3D");
		Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null);
		GameObject instance = Object.Instantiate(prefab);
		yield return null;
		Assert.That<SpriteActorPresentation>(
			instance.GetComponentInChildren<SpriteActorPresentation>(true),
			(IResolveConstraint)(object)Is.Null);
		CreatureMeshPresentation presentation = instance.GetComponentInChildren<CreatureMeshPresentation>(true);
		SpriteActorAnimator animator = instance.GetComponentInChildren<SpriteActorAnimator>(true);
		Assert.That<CreatureMeshPresentation>(presentation, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<SpriteActorAnimator>(animator, (IResolveConstraint)(object)Is.Not.Null);
		presentation.SetFacing(ActorFacing8.SW);
		animator.enabled = false;
		string[] states = new string[10]
		{
			"idle", "move", "shadowstep", "backmark", "hit",
			"appear", "telegraph", "wildattack", "stagger", "flee"
		};
		foreach (string state in states)
		{
			presentation.SetAuthoredState(state, 0f, loop: false, restart: true);
			yield return null;
			string clip = CreatureMeshPresentation.StemToClip(state);
			Assert.That<string>(clip, (IResolveConstraint)(object)Is.Not.Null, state);
			Assert.That<string>(presentation.CurrentClip, (IResolveConstraint)(object)Is.EqualTo((object)clip), state);
		}
		DynamicActorGroundShadow shadow = instance.GetComponentInChildren<DynamicActorGroundShadow>(true);
		Assert.That<DynamicActorGroundShadow>(shadow, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<Sprite>(shadow.ShadowRenderer.sprite, (IResolveConstraint)(object)Is.Not.Null);
		Object.Destroy(instance);
		Object.Destroy(lightObject);
		Object.Destroy(cameraObject);
		yield return null;
	}
}
}
