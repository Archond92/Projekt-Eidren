using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Gameplay.Presentation;
using NUnit.Framework.Constraints;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Tests.EditMode
{
public sealed class AudioContentTests
{
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

	private const string CatalogPath = "Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset";

	[Test]
	public void AudioEventIds_AreUniqueAndAllHaveDefinitions()
	{
		AudioEventCatalogDefinition catalog = LoadCatalog();
		string[] ids = (from field in typeof(AudioEventIds).GetFields(BindingFlags.Public | BindingFlags.Static)
			where field.IsLiteral
			select field.GetRawConstantValue() as string into value2
			where !string.IsNullOrWhiteSpace(value2)
			select value2).ToArray();
		Assert.That<string[]>(ids, (IResolveConstraint)(object)((ConstraintExpression)Has.Length).GreaterThanOrEqualTo((object)48));
		Assert.That<int>(ids.Distinct(StringComparer.Ordinal).Count(), (IResolveConstraint)(object)Is.EqualTo((object)ids.Length));
		Assert.That<int>(catalog.Events.Count, (IResolveConstraint)(object)Is.GreaterThanOrEqualTo((object)ids.Length));
		string[] array = ids;
		foreach (string id in array)
		{
			Assert.That<bool>(catalog.TryGetEvent(id, out var value), (IResolveConstraint)(object)Is.True, id, Array.Empty<object>());
			Assert.That<string>(value.Id, (IResolveConstraint)(object)Is.EqualTo((object)id));
		}
		Assert.That<string[]>(catalog.GetValidationErrors(), (IResolveConstraint)(object)Is.Empty);
	}

	[Test]
	public void EveryConfiguredEvent_HasAnImportedApprovedClip()
	{
		foreach (AudioEventDefinition definition in LoadCatalog().Events)
		{
			Assert.That<AudioEventDefinition>(definition, (IResolveConstraint)(object)Is.Not.Null);
			Assert.That<IReadOnlyList<AudioClip>>(definition.Clips, (IResolveConstraint)(object)Is.Not.Empty, definition.Id, Array.Empty<object>());
			Assert.That<bool>(definition.Clips.All((AudioClip clip) => clip != null), (IResolveConstraint)(object)Is.True, definition.Id, Array.Empty<object>());
			foreach (AudioClip clip in definition.Clips)
			{
				string path = AssetDatabase.GetAssetPath(clip);
				Assert.That<bool>(path.StartsWith("Assets/_Game/Audio/TemporaryGenerated/") || path.StartsWith("Assets/_Game/Audio/V02/"), (IResolveConstraint)(object)Is.True, path, Array.Empty<object>());
			}
		}
	}

	[Test]
	public void V02Catalog_CoversEveryRequiredProductionFamily()
	{
		AudioEventCatalogDefinition catalog = LoadCatalog();
		string[] source = catalog.Events.Select((AudioEventDefinition value) => value.Id).ToArray();
		Assert.That<int>(source.Count((string value) => value.StartsWith("v02.enemy.")), (IResolveConstraint)(object)Is.EqualTo((object)50));
		Assert.That<int>(source.Count((string value) => value.StartsWith("v02.container.")), (IResolveConstraint)(object)Is.EqualTo((object)33));
		Assert.That<int>(source.Count((string value) => value.StartsWith("v02.resource.")), (IResolveConstraint)(object)Is.EqualTo((object)12));
		Assert.That<int>(source.Count((string value) => value.StartsWith("v02.ignivar.")), (IResolveConstraint)(object)Is.EqualTo((object)6));
		Assert.That<int>(source.Count((string value) => value.StartsWith("v02.weapon.")), (IResolveConstraint)(object)Is.EqualTo((object)9));
		string[] array = new string[4] { "Zone_TwilightGrove", "Zone_VeilMarsh", "Zone_GreyRifts", "EidraForge" };
		foreach (string scene in array)
		{
			Assert.That<bool>(catalog.TryGetSceneProfile(scene, out var _), (IResolveConstraint)(object)Is.True, scene, Array.Empty<object>());
		}
	}

	[Test]
	public void SceneProfiles_CoverEveryPlayableAudioContext()
	{
		AudioEventCatalogDefinition catalog = LoadCatalog();
		string[] array = new string[8] { "Bootstrap", "MainMenu", "WorldMap", "HomeBase", "Zone_Greenwood", "Zone_Quarry", "Zone_Marsh", "Zone_EmberRuins" };
		foreach (string scene in array)
		{
			Assert.That<bool>(catalog.TryGetSceneProfile(scene, out var _), (IResolveConstraint)(object)Is.True, scene, Array.Empty<object>());
		}
	}

	[Test]
	public void MissingClip_IsControlledAndDoesNotCreateSources()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Expected O, but got Unknown
		GameObject root = new GameObject("MissingAudioClip_Test");
		AudioEventDefinition definition = null;
		AudioEventCatalogDefinition catalog = null;
		try
		{
			definition = ScriptableObject.CreateInstance<AudioEventDefinition>();
			definition.Configure("test.missing_clip", Array.Empty<AudioClip>(), Vector2.one, Vector2.one, AudioMixerBus.Sfx, AudioEventPlaybackType.OneShot, isSpatial: false, 1, 0f);
			catalog = ScriptableObject.CreateInstance<AudioEventCatalogDefinition>();
			catalog.Configure(new AudioEventDefinition[1] { definition }, Array.Empty<SceneAudioProfile>());
			Eidren.Core.Services.SettingsService settings = root.AddComponent<Eidren.Core.Services.SettingsService>();
			settings.Initialize(null, new MemorySaveFileSystem(), "audio-missing-tests");
			AudioService audio = root.AddComponent<AudioService>();
			audio.Initialize(catalog, settings, 2);
			int sourcesBefore = root.GetComponentsInChildren<AudioSource>(includeInactive: true).Length;
			Assert.DoesNotThrow((TestDelegate)delegate
			{
				Assert.That<bool>(audio.PlayOneShot("test.missing_clip"), (IResolveConstraint)(object)Is.False);
			});
			Assert.That<AudioSource[]>(root.GetComponentsInChildren<AudioSource>(includeInactive: true), (IResolveConstraint)(object)((ConstraintExpression)Has.Length).EqualTo((object)sourcesBefore));
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
			UnityEngine.Object.DestroyImmediate(definition);
			UnityEngine.Object.DestroyImmediate(catalog);
		}
	}

	[Test]
	public void PrototypeAudio_IsOnlyAServiceAdapter()
	{
		GameObject root = new GameObject("PrototypeAudioAdapter_Test");
		try
		{
			root.AddComponent<PrototypeAudio>();
			Assert.That<AudioSource>(root.GetComponent<AudioSource>(), (IResolveConstraint)(object)Is.Null);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(root);
		}
	}

	private static AudioEventCatalogDefinition LoadCatalog()
	{
		AudioEventCatalogDefinition audioEventCatalogDefinition = AssetDatabase.LoadAssetAtPath<AudioEventCatalogDefinition>("Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset");
		Assert.That<AudioEventCatalogDefinition>(audioEventCatalogDefinition, (IResolveConstraint)(object)Is.Not.Null, "Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset", Array.Empty<object>());
		return audioEventCatalogDefinition;
	}
}
}
