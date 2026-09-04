using Eidren.Composition;
using Eidren.Core.Services;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using UnityEditor;
using UnityEngine.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class PauseSettingsTests
{
	private readonly struct Runtime
	{
		public PauseService Pause { get; }

		public Runtime(PauseService pause)
		{
			Pause = pause;
		}
	}

	private sealed class RejectingSceneLoader : ISceneLoader
	{
		public string ActiveSceneName => "PauseSettingsTests";

		public bool CanLoad(string sceneName)
		{
			return false;
		}

		public IEnumerator LoadAsync(string sceneName)
		{
			yield break;
		}
	}

	private sealed class MemorySaveFileSystem : ISaveFileSystem
	{
		private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.Ordinal);

		public bool FileExists(string path)
		{
			return _files.ContainsKey(path);
		}

		public string ReadAllText(string path)
		{
			return _files[path];
		}

		public void WriteAllText(string path, string contents)
		{
			_files[path] = contents;
		}

		public void Copy(string source, string destination, bool overwrite)
		{
			if (!overwrite && _files.ContainsKey(destination))
			{
				throw new InvalidOperationException();
			}
			_files[destination] = _files[source];
		}

		public void Move(string source, string destination)
		{
			_files[destination] = _files[source];
			_files.Remove(source);
		}

		public void Replace(string source, string destination)
		{
			_files[destination] = _files[source];
			_files.Remove(source);
		}

		public void Delete(string path)
		{
			_files.Remove(path);
		}

		public void CreateDirectory(string path)
		{
		}
	}

	private readonly List<GameObject> _objects = new List<GameObject>();

	private int _qualityLevel;

	private int _targetFrameRate;

	private int _vSyncCount;

	private float _timeScale;

	[SetUp]
	public void SetUp()
	{
		_qualityLevel = QualitySettings.GetQualityLevel();
		_targetFrameRate = Application.targetFrameRate;
		_vSyncCount = QualitySettings.vSyncCount;
		_timeScale = Time.timeScale;
	}

	[TearDown]
	public void TearDown()
	{
		for (int index = _objects.Count - 1; index >= 0; index--)
		{
			if (_objects[index] != null)
			{
				UnityEngine.Object.DestroyImmediate(_objects[index]);
			}
		}
		_objects.Clear();
		Time.timeScale = _timeScale;
		QualitySettings.SetQualityLevel(_qualityLevel, applyExpensiveChanges: true);
		QualitySettings.vSyncCount = _vSyncCount;
		Application.targetFrameRate = _targetFrameRate;
	}

	[Test]
	public void Pause_BlocksOnce_AndResumeRestoresTimeScale()
	{
		Runtime runtime = CreateRuntime();
		int pauseEvents = 0;
		runtime.Pause.PauseChanged += delegate
		{
			pauseEvents++;
		};
		Assert.That<bool>(runtime.Pause.TryPause(), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(runtime.Pause.IsPaused, (IResolveConstraint)(object)Is.True);
		Assert.That<float>(Time.timeScale, (IResolveConstraint)(object)Is.Zero);
		Assert.That<bool>(runtime.Pause.TryPause(), (IResolveConstraint)(object)Is.False);
		Assert.That<int>(pauseEvents, (IResolveConstraint)(object)Is.EqualTo((object)1));
		Assert.That<bool>(runtime.Pause.Resume(), (IResolveConstraint)(object)Is.True);
		Assert.That<bool>(runtime.Pause.IsPaused, (IResolveConstraint)(object)Is.False);
		Assert.That<float>(Time.timeScale, (IResolveConstraint)(object)Is.EqualTo((object)_timeScale));
		Assert.That<int>(pauseEvents, (IResolveConstraint)(object)Is.EqualTo((object)2));
	}

	[Test]
	public void Settings_RoundTrip_PreservesVolumesQualityAndFrameRate()
	{
		MemorySaveFileSystem files = new MemorySaveFileSystem();
		Eidren.Core.Services.SettingsService settingsService = CreateSettings(files);
		settingsService.SetMasterVolume(0.42f);
		settingsService.SetMusicVolume(0.31f);
		settingsService.SetSfxVolume(0.73f);
		settingsService.SetQuality(EidrenQualityPreset.Low);
		settingsService.SetTargetFrameRate(30);
		Assert.That<bool>(settingsService.Save(out var saveError), (IResolveConstraint)(object)Is.True, saveError, Array.Empty<object>());
		SettingsData current = CreateSettings(files).Current;
		Assert.That<float>(current.masterVolume, (IResolveConstraint)(object)Is.EqualTo((object)0.42f).Within((object)0.001f));
		Assert.That<float>(current.musicVolume, (IResolveConstraint)(object)Is.EqualTo((object)0.31f).Within((object)0.001f));
		Assert.That<float>(current.sfxVolume, (IResolveConstraint)(object)Is.EqualTo((object)0.73f).Within((object)0.001f));
		Assert.That<EidrenQualityPreset>(current.quality, (IResolveConstraint)(object)Is.EqualTo((object)EidrenQualityPreset.Low));
		Assert.That<int>(current.targetFrameRate, (IResolveConstraint)(object)Is.EqualTo((object)30));
		Assert.That<int>(Application.targetFrameRate, (IResolveConstraint)(object)Is.EqualTo((object)30));
	}

	[TestCase(1f, 0f)]
	[TestCase(0.5f, -6.0206f)]
	[TestCase(0f, -80f)]
	public void Volume_UsesLogarithmicDecibelConversion(float linear, float expected)
	{
		Assert.That<float>(Eidren.Core.Services.SettingsService.LinearToDecibels(linear), (IResolveConstraint)(object)Is.EqualTo((object)expected).Within((object)0.001f));
	}

	[Test]
	public void FrameRate_AppliesOnlySupportedThirtyOrSixtyValues()
	{
		Eidren.Core.Services.SettingsService settingsService = CreateSettings(new MemorySaveFileSystem());
		settingsService.SetTargetFrameRate(30);
		Assert.That<int>(Application.targetFrameRate, (IResolveConstraint)(object)Is.EqualTo((object)30));
		settingsService.SetTargetFrameRate(60);
		Assert.That<int>(Application.targetFrameRate, (IResolveConstraint)(object)Is.EqualTo((object)60));
		settingsService.SetTargetFrameRate(144);
		Assert.That<int>(Application.targetFrameRate, (IResolveConstraint)(object)Is.EqualTo((object)60));
	}

	[Test]
	public void AudioMixer_ContainsRequiredGroupsAndParameters()
	{
		AudioMixer audioMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Game/Resources/Audio/EidrenAudioMixer.mixer");
		Assert.That<AudioMixer>(audioMixer, (IResolveConstraint)(object)Is.Not.Null);
		Assert.That<AudioMixerGroup[]>(audioMixer.FindMatchingGroups("Master"), (IResolveConstraint)(object)Is.Not.Empty);
		Assert.That<AudioMixerGroup[]>(audioMixer.FindMatchingGroups("Music"), (IResolveConstraint)(object)Is.Not.Empty);
		Assert.That<AudioMixerGroup[]>(audioMixer.FindMatchingGroups("SFX"), (IResolveConstraint)(object)Is.Not.Empty);
		Assert.That<AudioMixerGroup[]>(audioMixer.FindMatchingGroups("UI"), (IResolveConstraint)(object)Is.Not.Empty);
		string serializedMixer = File.ReadAllText(AssetDatabase.GetAssetPath(audioMixer));
		StringAssert.Contains("name: MasterVolume", serializedMixer);
		StringAssert.Contains("name: MusicVolume", serializedMixer);
		StringAssert.Contains("name: SFXVolume", serializedMixer);
	}

	[Test]
	public void ServiceRoot_HasOnlyOnePauseMenuAndPauseService()
	{
		EidrenServiceRoot root = EidrenServiceRoot.FindOrCreate();
		_objects.Add(root.gameObject);
		Assert.That<EidrenServiceRoot>(EidrenServiceRoot.FindOrCreate(), (IResolveConstraint)(object)Is.SameAs((object)root));
		Assert.That<PauseService[]>(root.GetComponents<PauseService>(), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
		Assert.That<PauseMenuController[]>(root.GetComponentsInChildren<PauseMenuController>(includeInactive: true), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)1));
	}

	private Eidren.Core.Services.SettingsService CreateSettings(MemorySaveFileSystem files)
	{
		Eidren.Core.Services.SettingsService settingsService = Track("Settings_Test").AddComponent<Eidren.Core.Services.SettingsService>();
		settingsService.Initialize(null, files, "settings-tests");
		return settingsService;
	}

	private Runtime CreateRuntime()
	{
		GameObject gameObject = Track("PauseRuntime_Test");
		ContentDatabase content = gameObject.AddComponent<ContentDatabase>();
		GameSession session = gameObject.AddComponent<GameSession>();
		session.Initialize();
		session.ConfigureContentDatabase(content);
		session.StartNewGame();
		SceneFlowService sceneFlow = gameObject.AddComponent<SceneFlowService>();
		sceneFlow.Configure(null, new RejectingSceneLoader());
		SaveGameService save = gameObject.AddComponent<SaveGameService>();
		save.Initialize(session, content, sceneFlow, new MemorySaveFileSystem(), "pause-tests");
		PauseService pauseService = gameObject.AddComponent<PauseService>();
		pauseService.Initialize(session, sceneFlow, save);
		return new Runtime(pauseService);
	}

	private GameObject Track(string name)
	{
		GameObject value = new GameObject(name);
		_objects.Add(value);
		return value;
	}
}
}
