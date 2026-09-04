using Eidren.AI;
using Eidren.Composition;
using Eidren.Core.Services;
using Eidren.Data;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections;
using System;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.PlayMode
{
public sealed class AudioServicePlayModeTests
{
	[UnityTest]
	public IEnumerator OneShotPool_EnforcesMaximumAndDoesNotLeakSources()
	{
		GameObject root = new GameObject("AudioPool_Test");
		AudioEventDefinition definition = ScriptableObject.CreateInstance<AudioEventDefinition>();
		AudioEventCatalogDefinition catalog = ScriptableObject.CreateInstance<AudioEventCatalogDefinition>();
		AudioClip clip = AudioClip.Create("AudioPoolTestClip", 22050, 1, 22050, stream: false);
		definition.Configure("test.pool_limit", new AudioClip[1] { clip }, Vector2.one, Vector2.one, AudioMixerBus.Sfx, AudioEventPlaybackType.OneShot, isSpatial: false, 2, 0f);
		catalog.Configure(new AudioEventDefinition[1] { definition }, Array.Empty<SceneAudioProfile>());
		SettingsService settings = root.AddComponent<SettingsService>();
		settings.Initialize(null, null, "audio-pool-tests");
		AudioService audio = root.AddComponent<AudioService>();
		audio.Initialize(catalog, settings, 4);
		int sourceCount = root.GetComponentsInChildren<AudioSource>(includeInactive: true).Length;
		Assert.That<bool>(audio.PlayOneShot("test.pool_limit"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(audio.PlayOneShot("test.pool_limit"), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(audio.PlayOneShot("test.pool_limit"), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(audio.CountActiveInstances("test.pool_limit"), (IResolveConstraint)(object)Is.EqualTo((object)2));
		Assert.That<AudioSource[]>(root.GetComponentsInChildren<AudioSource>(includeInactive: true), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)sourceCount));
		UnityEngine.Object.Destroy(root);
		UnityEngine.Object.Destroy(definition);
		UnityEngine.Object.Destroy(catalog);
		UnityEngine.Object.Destroy(clip);
		yield return null;
	}

	[UnityTest]
	public IEnumerator MusicCrossfade_LeavesExactlyOneLoop()
	{
		GameObject root = new GameObject("AudioMusic_Test");
		AudioEventCatalogDefinition catalog = Resources.Load<AudioEventCatalogDefinition>("Data/Audio/AudioEventCatalog_V01");
		SettingsService settings = root.AddComponent<SettingsService>();
		settings.Initialize(null, null, "audio-music-tests");
		AudioService audio = root.AddComponent<AudioService>();
		audio.Initialize(catalog, settings, 4);
		Assert.That<bool>(audio.PlayMusic("music.main_menu", 0f), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(audio.PlayMusic("music.world_map", 0.05f), (IResolveConstraint)(object)Is.True);
		yield return WaitRealtime(0.12f);
		Assert.That<string>(audio.CurrentMusicEventId, (IResolveConstraint)(object)Is.EqualTo((object)"music.world_map"));
		Assert.That<int>(audio.ActiveMusicLoopCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		UnityEngine.Object.Destroy(root);
		yield return null;
	}

	[UnityTest]
	public IEnumerator GaronReturn_RestoresAreaMusic()
	{
		yield return ClearPersistentServices();
		EidrenServiceRoot eidrenServiceRoot = EidrenServiceRoot.FindOrCreate();
		eidrenServiceRoot.GameSession.StartNewGame();
		eidrenServiceRoot.PlayerProgression.RecordEnemyDefeated(10525);
		yield return SceneManager.LoadSceneAsync("Zone_EmberRuins", LoadSceneMode.Single);
		yield return WaitUntil(() => EidrenServiceRoot.Instance != null && UnityEngine.Object.FindFirstObjectByType<BossController>() != null, "Ember Ruins composition did not initialize.");
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		BossController boss = UnityEngine.Object.FindFirstObjectByType<BossController>();
		services.AudioRuntimeBinder.BindCurrentScene(SceneManager.GetActiveScene());
		yield return WaitUntil(() => services.AudioService.CurrentMusicEventId == "music.outdoor", "Ember Ruins audio profile did not initialize.");
		boss.BeginBattle();
		Assert.That<string>(services.AudioService.CurrentMusicEventId, (IResolveConstraint)(object)Is.EqualTo((object)"music.garon"));
		yield return WaitUntil(() => !boss.BattleActive, "Garon did not return after exceeding his leash.");
		yield return WaitRealtime(0.75f);
		Assert.That<string>(services.AudioService.CurrentMusicEventId, (IResolveConstraint)(object)Is.EqualTo((object)"music.outdoor"));
		Assert.That<int>(services.AudioService.ActiveMusicLoopCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		yield return CleanupCurrentScene();
	}

	[UnityTest]
	public IEnumerator SceneChange_SwitchesMainMenuAndWorldMapMusic()
	{
		yield return ClearPersistentServices();
		yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
		yield return WaitUntil(() => EidrenServiceRoot.Instance != null && EidrenServiceRoot.Instance.AudioService.CurrentMusicEventId == "music.main_menu", "MainMenu music did not initialize.");
		EidrenServiceRoot services = EidrenServiceRoot.Instance;
		Assert.That<bool>(services.SceneFlowService.TryLoadScene("WorldMap"), (IResolveConstraint)(object)Is.True);
		yield return WaitUntil(() => SceneManager.GetActiveScene().name == "WorldMap" && services.AudioService.CurrentMusicEventId == "music.world_map" && !services.SceneFlowService.IsTransitioning, "WorldMap music did not replace MainMenu music.");
		yield return WaitRealtime(0.75f);
		Assert.That<int>(services.AudioService.ActiveMusicLoopCount, (IResolveConstraint)(object)Is.EqualTo((object)1));
		yield return CleanupCurrentScene();
	}

	[UnityTest]
	public IEnumerator MixerVolumes_AreAppliedInLogarithmicDecibels()
	{
		GameObject root = new GameObject("AudioMixerVolume_Test");
		AudioMixer mixer = Resources.Load<AudioMixer>("Audio/EidrenAudioMixer");
		SettingsService settings = root.AddComponent<SettingsService>();
		settings.Initialize(mixer, null, "audio-mixer-tests");
		settings.SetMasterVolume(0.5f);
		settings.SetMusicVolume(0.25f);
		settings.SetSfxVolume(0.75f);
		yield return null;
		Assert.That<bool>(mixer.GetFloat("MasterVolume", out var master), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(mixer.GetFloat("MusicVolume", out var music), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(mixer.GetFloat("SFXVolume", out var sfx), (IResolveConstraint)(object)Is.True);
		Assert.That<float>(master, (IResolveConstraint)(object)Is.EqualTo((object)(-6.0206f)).Within((object)0.01f));
		Assert.That<float>(music, (IResolveConstraint)(object)Is.EqualTo((object)(-12.0412f)).Within((object)0.01f));
		Assert.That<float>(sfx, (IResolveConstraint)(object)Is.EqualTo((object)(-2.4988f)).Within((object)0.01f));
		settings.SetMasterVolume(1f);
		settings.SetMusicVolume(0.8f);
		settings.SetSfxVolume(0.9f);
		UnityEngine.Object.Destroy(root);
		yield return null;
	}

	private static IEnumerator WaitRealtime(float duration)
	{
		float end = Time.realtimeSinceStartup + duration;
		while (Time.realtimeSinceStartup < end)
		{
			yield return null;
		}
	}

	private static IEnumerator WaitUntil(Func<bool> condition, string failureMessage)
	{
		float deadline = Time.realtimeSinceStartup + 12f;
		while (Time.realtimeSinceStartup < deadline)
		{
			if (condition())
			{
				yield break;
			}
			yield return null;
		}
		Assert.Fail(failureMessage);
	}

	private static IEnumerator ClearPersistentServices()
	{
		Time.timeScale = 1f;
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
			yield return null;
		}
	}

	private static IEnumerator CleanupCurrentScene()
	{
		Time.timeScale = 1f;
		Scene loaded = SceneManager.GetActiveScene();
		SceneManager.SetActiveScene(SceneManager.CreateScene("AudioServiceTestCleanup"));
		if (EidrenServiceRoot.Instance != null)
		{
			UnityEngine.Object.Destroy(EidrenServiceRoot.Instance.gameObject);
		}
		yield return null;
		if (loaded.IsValid() && loaded.isLoaded)
		{
			yield return SceneManager.UnloadSceneAsync(loaded);
		}
	}
}
}
