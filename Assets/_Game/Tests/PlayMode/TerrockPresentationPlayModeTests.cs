using Eidren.Gameplay.Presentation;
using Eidren.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests
{
public sealed class TerrockPresentationPlayModeTests
{
	[UnityTest]
	public IEnumerator Terrock3D_LoadsAllProofStatesAtRuntime()
	{
		yield return Eidren.Tests.PlayMode.Testumgebung.LeereWeltBereitstellen();
		GameObject cameraObject = new GameObject("Main Camera");
		Camera camera = cameraObject.AddComponent<Camera>();
		cameraObject.tag = "MainCamera";
		cameraObject.transform.SetPositionAndRotation(new Vector3(5f, 7f, -7f), Quaternion.Euler(35f, -35f, 0f));
		camera.orthographic = true;
		GameObject lightObject = new GameObject("Main Light");
		lightObject.AddComponent<Light>().type = LightType.Directional;
		lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
		GameObject prefab = Resources.Load<GameObject>("Prefabs/Actors/3D/Terrock_3D");
		Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null);
		GameObject instance = UnityEngine.Object.Instantiate(prefab);
		yield return null;
		Assert.That<SpriteActorPresentation>(
			instance.GetComponentInChildren<SpriteActorPresentation>(true),
			(IResolveConstraint)(object)Is.Null);
		CreatureMeshPresentation presentation = instance.GetComponentInChildren<CreatureMeshPresentation>(true);
		SpriteActorAnimator animator = instance.GetComponentInChildren<SpriteActorAnimator>(true);
		Assert.That<CreatureMeshPresentation>(presentation, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<SpriteActorAnimator>(animator, (IResolveConstraint)(object)Is.Not.Null);
		animator.enabled = false;
		presentation.SetFacing(ActorFacing8.NW);
		string[] states = new string[10]
		{
			"idle", "move", "rockbreaker", "stonehide", "hit",
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
		UnityEngine.Object.Destroy(instance);
		UnityEngine.Object.Destroy(lightObject);
		UnityEngine.Object.Destroy(cameraObject);
		yield return null;
	}

	[UnityTest]
	public IEnumerator Terrock3D_RendersInGreenwoodUnderTwoLights()
	{
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			Assert.Ignore("Greenwood captures require a real graphics device.");
		}
		yield return SceneManager.LoadSceneAsync("Zone_Greenwood", LoadSceneMode.Single);
		Light[] array = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (Light existing in array)
		{
			if (existing.type == LightType.Directional)
			{
				existing.enabled = false;
			}
		}
		GameObject prefab = Resources.Load<GameObject>("Prefabs/Actors/3D/Terrock_3D");
		Assert.That<GameObject>(prefab, (IResolveConstraint)(object)Is.Not.Null);
		GameObject actor = UnityEngine.Object.Instantiate(prefab, new Vector3(-4.5f, 0.05f, 9.5f), Quaternion.identity);
		SetLayerRecursively(actor, 31);
		Assert.That<SpriteActorPresentation>(
			actor.GetComponentInChildren<SpriteActorPresentation>(true),
			(IResolveConstraint)(object)Is.Null);
		CreatureMeshPresentation componentInChildren = actor.GetComponentInChildren<CreatureMeshPresentation>(true);
		Assert.That<CreatureMeshPresentation>(componentInChildren, (IResolveConstraint)(object)Is.Not.Null);
		componentInChildren.SetFacing(ActorFacing8.SE);
		componentInChildren.SetAuthoredState("idle", 0f, loop: true, restart: true);
		GameObject lightObject = new GameObject("__TerrockTest_MainLight");
		Light mainLight = lightObject.AddComponent<Light>();
		mainLight.type = LightType.Directional;
		mainLight.intensity = 1.35f;
		mainLight.shadows = LightShadows.Soft;
		GameObject cameraObject = new GameObject("__TerrockTest_Camera")
		{
			tag = "MainCamera"
		};
		Camera camera = cameraObject.AddComponent<Camera>();
		camera.orthographic = true;
		camera.orthographicSize = 4.4f;
		camera.clearFlags = CameraClearFlags.Color;
		camera.backgroundColor = Color.clear;
		camera.cullingMask = int.MinValue;
		camera.transform.position = actor.transform.position + new Vector3(-6.5f, 8.5f, -6.5f);
		camera.transform.rotation = Quaternion.Euler(48f, 45f, 0f);
		yield return null;
		mainLight.color = new Color(1f, 0.78f, 0.56f);
		lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
		Color warmAverage = CaptureAverage(camera);
		mainLight.color = new Color(0.52f, 0.72f, 1f);
		lightObject.transform.rotation = Quaternion.Euler(58f, 142f, 0f);
		Color coolAverage = CaptureAverage(camera);
		Assert.That<float>(Vector3.Distance(new Vector3(warmAverage.r, warmAverage.g, warmAverage.b), new Vector3(coolAverage.r, coolAverage.g, coolAverage.b)), (IResolveConstraint)(object)Is.GreaterThan((object)0.01f), "The actor view must react visibly to changed world light.", Array.Empty<object>());
		UnityEngine.Object.Destroy(actor);
		UnityEngine.Object.Destroy(lightObject);
		UnityEngine.Object.Destroy(cameraObject);
		yield return null;
	}

	private static Color CaptureAverage(Camera camera)
	{
		RenderTexture target = (camera.targetTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32));
		camera.Render();
		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = target;
		Texture2D image = new Texture2D(1280, 720, TextureFormat.RGBA32, mipChain: false);
		image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
		image.Apply();
		Color[] pixels = image.GetPixels(image.width / 4, image.height / 4, image.width / 2, image.height / 2);
		Color average = Color.black;
		int samples = 0;
		for (int index = 0; index < pixels.Length; index += 4)
		{
			if (!(pixels[index].a <= 0.05f))
			{
				average += pixels[index];
				samples++;
			}
		}
		Assert.That<int>(samples, (IResolveConstraint)(object)Is.GreaterThan((object)0));
		average /= (float)Mathf.Max(1, samples);
		RenderTexture.active = previous;
		camera.targetTexture = null;
		UnityEngine.Object.Destroy(image);
		UnityEngine.Object.Destroy(target);
		return average;
	}

	private static void SetLayerRecursively(GameObject root, int layer)
	{
		root.layer = layer;
		foreach (Transform item in root.transform)
		{
			SetLayerRecursively(item.gameObject, layer);
		}
	}
}
}
