using Eidren.Data;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class V02AudioContentBuilder
{
	private readonly struct Spec
	{
		public string Id { get; }

		public float Frequency { get; }

		public float Duration { get; }

		public AudioEventPlaybackType Type { get; }

		public bool Spatial { get; }

		public Spec(string id, float frequency, float duration, AudioEventPlaybackType type = AudioEventPlaybackType.OneShot, bool spatial = true)
		{
			Id = id;
			Frequency = frequency;
			Duration = duration;
			Type = type;
			Spatial = spatial;
		}
	}

	private const int SampleRate = 22050;

	private const string ClipRoot = "Assets/_Game/Audio/V02";

	private const string EventRoot = "Assets/_Game/Data/Audio/Events/V02";

	private const string CatalogPath = "Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset";

	[MenuItem("Eidren/Audio/Build V0.2 Audio Families")]
	public static void Build()
	{
		EnsureFolders();
		List<Spec> specs = Specs();
		foreach (Spec item in specs)
		{
			WriteWave(item);
		}
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		List<AudioEventDefinition> definitions = new List<AudioEventDefinition>();
		foreach (Spec spec in specs)
		{
			definitions.Add(BuildDefinition(spec));
		}
		AudioEventCatalogDefinition audioEventCatalogDefinition = AssetDatabase.LoadAssetAtPath<AudioEventCatalogDefinition>("Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset");
		if (audioEventCatalogDefinition == null)
		{
			throw new FileNotFoundException("Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset");
		}
		List<AudioEventDefinition> events = new List<AudioEventDefinition>(audioEventCatalogDefinition.Events);
		events.RemoveAll((AudioEventDefinition value) => value != null && value.Id.StartsWith("v02.", StringComparison.Ordinal));
		events.AddRange(definitions);
		List<SceneAudioProfile> profiles = new List<SceneAudioProfile>(audioEventCatalogDefinition.SceneProfiles);
		AddOrReplaceProfile(profiles, "Zone_TwilightGrove", "music.outdoor", "v02.ambience.twilight_grove");
		AddOrReplaceProfile(profiles, "Zone_VeilMarsh", "music.outdoor", "v02.ambience.veil_marsh");
		AddOrReplaceProfile(profiles, "Zone_GreyRifts", "music.outdoor", "v02.ambience.grey_rifts");
		AddOrReplaceProfile(profiles, "EidraForge", "v02.music.eidra_forge", "v02.ambience.eidra_forge");
		audioEventCatalogDefinition.Configure(events.ToArray(), profiles.ToArray());
		EditorUtility.SetDirty(audioEventCatalogDefinition);
		string[] errors = audioEventCatalogDefinition.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException(string.Join("\n", errors));
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"Eidren: built {definitions.Count} v0.2 audio events.");
	}

	private static List<Spec> Specs()
	{
		List<Spec> result = new List<Spec>
		{
			new Spec("v02.music.eidra_forge", 38f, 8f, AudioEventPlaybackType.Music, spatial: false),
			new Spec("v02.ambience.twilight_grove", 13f, 6f, AudioEventPlaybackType.Ambience, spatial: false),
			new Spec("v02.ambience.veil_marsh", 9f, 6f, AudioEventPlaybackType.Ambience, spatial: false),
			new Spec("v02.ambience.grey_rifts", 7f, 6f, AudioEventPlaybackType.Ambience, spatial: false),
			new Spec("v02.ambience.eidra_forge", 11f, 6f, AudioEventPlaybackType.Ambience, spatial: false)
		};
		string[] enemies = new string[10] { "enemy.riftling", "enemy.root_charger", "enemy.moor_thrower", "enemy.granite_shell", "enemy.rift_guardian", "enemy.ember_eater", "enemy.ash_runner", "enemy.forge_guardian", "enemy.seal_guardian", "enemy.core_guardian" };
		string[] enemyCues = new string[5] { "discover", "attack", "hit", "stagger", "death" };
		for (int actor = 0; actor < enemies.Length; actor++)
		{
			for (int cue = 0; cue < enemyCues.Length; cue++)
			{
				result.Add(new Spec(AudioEventIds.V02Enemy(enemies[actor], enemyCues[cue]), 62f + (float)actor * 19f + (float)cue * 13f, 0.18f + (float)cue * 0.085f));
			}
		}
		string[] resources = new string[4] { "hardwood", "swamp_hemp", "granite", "iron_ore" };
		string[] resourceCues = new string[3] { "hit", "complete", "exhausted" };
		for (int resource = 0; resource < resources.Length; resource++)
		{
			for (int i = 0; i < resourceCues.Length; i++)
			{
				result.Add(new Spec(AudioEventIds.V02Resource(resources[resource], resourceCues[i]), 96f + (float)resource * 71f + (float)i * 24f, 0.24f + (float)i * 0.08f));
			}
		}
		string[] containers = new string[11]
		{
			"world_common", "world_guarded", "world_hidden", "forge_supply", "forge_optional", "forge_elite", "forge_completion", "reward_small", "reward_medium", "reward_large",
			"recovery"
		};
		string[] containerCues = new string[3] { "open", "empty", "payment" };
		for (int family = 0; family < containers.Length; family++)
		{
			for (int j = 0; j < containerCues.Length; j++)
			{
				result.Add(new Spec(AudioEventIds.V02Container(containers[family], containerCues[j]), 118f + (float)family * 23f + (float)j * 67f, 0.2f + (float)j * 0.07f));
			}
		}
		string[] array = new string[6] { "move", "react", "capture", "flee", "ember_circle", "molten_brand" };
		foreach (string cue2 in array)
		{
			result.Add(new Spec(AudioEventIds.V02Ignivar(cue2), 260f + (float)(result.Count % 7) * 46f, 0.32f));
		}
		array = new string[5] { "durability_low", "break", "t2_unlock", "mark_pickup", "forge_reset" };
		foreach (string cue3 in array)
		{
			result.Add(new Spec(AudioEventIds.V02Ui(cue3), 330f + (float)(result.Count % 6) * 75f, 0.24f, AudioEventPlaybackType.OneShot, spatial: false));
		}
		array = new string[3] { "hammer", "daggers", "spear" };
		foreach (string family2 in array)
		{
			string[] array2 = new string[3] { "body", "protection", "environment" };
			foreach (string surface in array2)
			{
				result.Add(new Spec(AudioEventIds.V02WeaponImpact(family2, surface), 88f + (float)(result.Count % 9) * 39f, 0.19f));
			}
		}
		return result;
	}

	private static AudioEventDefinition BuildDefinition(Spec spec)
	{
		string path = "Assets/_Game/Data/Audio/Events/V02/" + FileName(spec.Id) + ".asset";
		AudioEventDefinition value = AssetDatabase.LoadAssetAtPath<AudioEventDefinition>(path);
		if (value == null)
		{
			value = ScriptableObject.CreateInstance<AudioEventDefinition>();
			AssetDatabase.CreateAsset(value, path);
		}
		AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(spec));
		if (clip == null)
		{
			throw new FileNotFoundException(ClipPath(spec));
		}
		value.Configure(spec.Id, new AudioClip[1] { clip }, new Vector2(0.68f, 0.88f), new Vector2(0.95f, 1.05f), (spec.Type == AudioEventPlaybackType.Music) ? AudioMixerBus.Music : AudioMixerBus.Sfx, spec.Type, spec.Spatial, (spec.Type != AudioEventPlaybackType.OneShot) ? 1 : 3, (spec.Type == AudioEventPlaybackType.OneShot) ? 0.06f : 0f);
		EditorUtility.SetDirty(value);
		return value;
	}

	private static void WriteWave(Spec spec)
	{
		int count = Mathf.RoundToInt(spec.Duration * 22050f);
		using FileStream stream = new FileStream(ClipPath(spec), FileMode.Create);
		using BinaryWriter writer = new BinaryWriter(stream);
		writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
		writer.Write(36 + count * 2);
		writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
		writer.Write(new char[4] { 'f', 'm', 't', ' ' });
		writer.Write(16);
		writer.Write((short)1);
		writer.Write((short)1);
		writer.Write(22050);
		writer.Write(44100);
		writer.Write((short)2);
		writer.Write((short)16);
		writer.Write(new char[4] { 'd', 'a', 't', 'a' });
		writer.Write(count * 2);
		uint noise = (uint)spec.Id.GetHashCode();
		for (int index = 0; index < count; index++)
		{
			float t = (float)index / 22050f;
			float n = (float)index / (float)Mathf.Max(1, count - 1);
			noise = noise * 1664525 + 1013904223;
			float random = (float)((noise >> 8) & 0xFFFF) / 32767.5f - 1f;
			float envelope = ((spec.Type == AudioEventPlaybackType.OneShot) ? (Mathf.Pow(1f - n, 2f) * Mathf.Clamp01(n * 36f)) : Mathf.Clamp01(Mathf.Min(n, 1f - n) * 14f));
			float signal = Mathf.Sin((float)Math.PI * 2f * spec.Frequency * t) * 0.36f + Mathf.Sin((float)Math.PI * 2f * spec.Frequency * 1.51f * t) * 0.16f + random * 0.055f;
			writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(signal * envelope, -0.9f, 0.9f) * 32767f));
		}
	}

	private static void AddOrReplaceProfile(List<SceneAudioProfile> values, string scene, string music, string ambience)
	{
		values.RemoveAll((SceneAudioProfile value) => value.SceneKey == scene);
		values.Add(new SceneAudioProfile(scene, music, ambience));
	}

	private static string ClipPath(Spec spec)
	{
		return "Assets/_Game/Audio/V02/" + FileName(spec.Id) + ".wav";
	}

	private static string FileName(string id)
	{
		return id.Replace('.', '_').Replace('/', '_');
	}

	private static void EnsureFolders()
	{
		Folder("Assets/_Game/Audio", "V02");
		Folder("Assets/_Game/Data/Audio/Events", "V02");
	}

	private static void Folder(string parent, string child)
	{
		if (!AssetDatabase.IsValidFolder(parent + "/" + child))
		{
			AssetDatabase.CreateFolder(parent, child);
		}
	}
}
}
