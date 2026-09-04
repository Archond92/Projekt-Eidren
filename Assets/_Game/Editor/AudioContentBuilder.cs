using Eidren.Data;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
public static class AudioContentBuilder
{
	private readonly struct EventSpec
	{
		public string Id { get; }

		public float Frequency { get; }

		public float Duration { get; }

		public Vector2 Volume { get; }

		public Vector2 Pitch { get; }

		public AudioMixerBus Bus { get; }

		public AudioEventPlaybackType Type { get; }

		public bool Spatial { get; }

		public int MaximumInstances { get; }

		public float Cooldown { get; }

		public EventSpec(string id, float frequency, float duration, float volumeMin, float volumeMax, float pitchMin, float pitchMax, AudioMixerBus bus, AudioEventPlaybackType type, bool spatial, int maximumInstances, float cooldown)
		{
			Id = id;
			Frequency = frequency;
			Duration = duration;
			Volume = new Vector2(volumeMin, volumeMax);
			Pitch = new Vector2(pitchMin, pitchMax);
			Bus = bus;
			Type = type;
			Spatial = spatial;
			MaximumInstances = maximumInstances;
			Cooldown = cooldown;
		}
	}

	private const int SampleRate = 22050;

	private const string ClipDirectory = "Assets/_Game/Audio/TemporaryGenerated";

	private const string EventDirectory = "Assets/_Game/Data/Audio/Events";

	private const string CatalogDirectory = "Assets/_Game/Resources/Data/Audio";

	private const string CatalogPath = "Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset";

	[MenuItem("Eidren/Audio/Build V0.1 Audio Pass")]
	public static void Build()
	{
		EnsureDirectory("Assets/_Game/Audio/TemporaryGenerated");
		EnsureDirectory("Assets/_Game/Data/Audio/Events");
		EnsureDirectory("Assets/_Game/Resources/Data/Audio");
		EventSpec[] specs = CreateSpecs();
		EventSpec[] array = specs;
		for (int i = 0; i < array.Length; i++)
		{
			WriteWave(array[i]);
		}
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		List<AudioEventDefinition> definitions = new List<AudioEventDefinition>(specs.Length);
		array = specs;
		foreach (EventSpec spec in array)
		{
			ConfigureImporter(spec);
			definitions.Add(CreateOrUpdateDefinition(spec));
		}
		AudioEventCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<AudioEventCatalogDefinition>("Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset");
		if (catalog == null)
		{
			catalog = ScriptableObject.CreateInstance<AudioEventCatalogDefinition>();
			AssetDatabase.CreateAsset(catalog, "Assets/_Game/Resources/Data/Audio/AudioEventCatalog_V01.asset");
		}
		catalog.Configure(definitions.ToArray(), CreateSceneProfiles());
		EditorUtility.SetDirty(catalog);
		string[] errors = catalog.GetValidationErrors();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException("Generated audio catalog is invalid:\n- " + string.Join("\n- ", errors));
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static AudioEventDefinition CreateOrUpdateDefinition(EventSpec spec)
	{
		string path = "Assets/_Game/Data/Audio/Events/" + FileName(spec.Id) + ".asset";
		AudioEventDefinition definition = AssetDatabase.LoadAssetAtPath<AudioEventDefinition>(path);
		if (definition == null)
		{
			definition = ScriptableObject.CreateInstance<AudioEventDefinition>();
			AssetDatabase.CreateAsset(definition, path);
		}
		AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(spec));
		if (clip == null)
		{
			throw new InvalidOperationException("Generated clip for '" + spec.Id + "' was not imported.");
		}
		definition.Configure(spec.Id, new AudioClip[1] { clip }, spec.Volume, spec.Pitch, spec.Bus, spec.Type, spec.Spatial, spec.MaximumInstances, spec.Cooldown);
		EditorUtility.SetDirty(definition);
		return definition;
	}

	private static void ConfigureImporter(EventSpec spec)
	{
		AudioImporter importer = AssetImporter.GetAtPath(ClipPath(spec)) as AudioImporter;
		if (!(importer == null))
		{
			importer.forceToMono = true;
			AudioImporterSampleSettings settings = importer.defaultSampleSettings;
			settings.preloadAudioData = spec.Type == AudioEventPlaybackType.OneShot;
			settings.loadType = ((spec.Type != AudioEventPlaybackType.OneShot) ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad);
			settings.compressionFormat = AudioCompressionFormat.Vorbis;
			settings.quality = ((spec.Type == AudioEventPlaybackType.OneShot) ? 0.55f : 0.72f);
			importer.defaultSampleSettings = settings;
			importer.SaveAndReimport();
		}
	}

	private static void WriteWave(EventSpec spec)
	{
		float[] samples = GenerateSamples(spec);
		using FileStream stream = new FileStream(ClipPath(spec), FileMode.Create, FileAccess.Write);
		using BinaryWriter writer = new BinaryWriter(stream);
		int dataLength = samples.Length * 2;
		writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
		writer.Write(36 + dataLength);
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
		writer.Write(dataLength);
		float[] array = samples;
		foreach (float sample in array)
		{
			writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * 32767f));
		}
	}

	private static float[] GenerateSamples(EventSpec spec)
	{
		int count = Mathf.Max(1, Mathf.RoundToInt(spec.Duration * 22050f));
		float[] samples = new float[count];
		uint noise = (uint)StableHash(spec.Id);
		for (int index = 0; index < count; index++)
		{
			float time = (float)index / 22050f;
			float normalized = (float)index / (float)Mathf.Max(1, count - 1);
			float value;
			if (spec.Type == AudioEventPlaybackType.Music)
			{
				value = GenerateMusic(spec.Frequency, time, normalized);
			}
			else if (spec.Type == AudioEventPlaybackType.Ambience)
			{
				noise = noise * 1664525 + 1013904223;
				float random = (float)((noise >> 8) & 0xFFFF) / 32767.5f - 1f;
				value = GenerateAmbience(spec.Frequency, time, normalized, random);
			}
			else
			{
				noise = noise * 1664525 + 1013904223;
				float random2 = (float)((noise >> 8) & 0xFFFF) / 32767.5f - 1f;
				value = GenerateOneShot(spec, time, normalized, random2);
			}
			samples[index] = Mathf.Clamp(value, -0.92f, 0.92f);
		}
		return samples;
	}

	private static float GenerateMusic(float root, float time, float normalized)
	{
		float slowPulse = 0.72f + 0.28f * Mathf.Sin((float)Math.PI / 4f * time);
		float num = Mathf.Sin((float)Math.PI * 2f * root * time) * 0.42f + Mathf.Sin((float)Math.PI * 2f * root * 1.25f * time) * 0.26f + Mathf.Sin((float)Math.PI * 2f * root * 1.5f * time) * 0.2f;
		float motif = Mathf.Sin((float)Math.PI * 2f * (root * 2f + Mathf.Sin(time * 0.7f) * 1.5f) * time) * 0.12f;
		float edge = Mathf.Clamp01(Mathf.Min(normalized, 1f - normalized) * 18f);
		return (num * slowPulse + motif) * 0.2f * edge;
	}

	private static float GenerateAmbience(float root, float time, float normalized, float noise)
	{
		float num = Mathf.Sin((float)Math.PI * 2f * root * time) * 0.05f + Mathf.Sin((float)Math.PI * 2f * root * 0.37f * time) * 0.04f;
		float movement = Mathf.Sin((float)Math.PI * 17f / 50f * time) * 0.04f;
		float edge = Mathf.Clamp01(Mathf.Min(normalized, 1f - normalized) * 12f);
		return (num + movement + noise * 0.025f) * edge;
	}

	private static float GenerateOneShot(EventSpec spec, float time, float normalized, float noise)
	{
		float envelope = Mathf.Pow(1f - normalized, 2.2f) * Mathf.Clamp01(normalized * 45f);
		float sweep = spec.Frequency * Mathf.Lerp(1.35f, 0.78f, normalized);
		float num = Mathf.Sin((float)Math.PI * 2f * sweep * time) * 0.62f + Mathf.Sin((float)Math.PI * 2f * sweep * 1.93f * time) * 0.2f;
		float transient = noise * Mathf.Pow(1f - normalized, 5f) * 0.38f;
		return (num + transient) * envelope * 0.56f;
	}

	private static EventSpec[] CreateSpecs()
	{
		List<EventSpec> list = new List<EventSpec>();
		AddPlayer(list);
		AddResources(list);
		AddUi(list);
		AddWildling(list);
		AddGaron(list);
		AddState(list);
		AddMusic(list);
		AddAmbience(list);
		return list.ToArray();
	}

	private static void AddPlayer(List<EventSpec> result)
	{
		result.Add(Sfx("player.footstep", 128f, 0.11f, 0.28f, 0.38f, spatial: true, 2, 0.16f, 0.93f, 1.07f));
		result.Add(Sfx("player.dodge", 235f, 0.22f, 0.42f, 0.56f, spatial: true, 1, 0.18f, 0.94f, 1.05f));
		result.Add(Sfx("player.attack.hammer", 82f, 0.28f, 0.56f, 0.72f, spatial: true, 2, 0.12f, 0.94f, 1.03f));
		result.Add(Sfx("player.attack.daggers", 410f, 0.13f, 0.42f, 0.58f, spatial: true, 3, 0.07f, 0.96f, 1.08f));
		result.Add(Sfx("player.attack.spear", 265f, 0.2f, 0.48f, 0.62f, spatial: true, 2, 0.09f, 0.94f, 1.07f));
		result.Add(Sfx("player.hit", 170f, 0.1f, 0.38f, 0.52f, spatial: true, 4, 0.035f, 0.96f, 1.06f));
		result.Add(Sfx("player.damage", 96f, 0.24f, 0.5f, 0.64f, spatial: true, 2, 0.12f, 0.96f, 1.03f));
		result.Add(Sfx("player.healing_potion", 620f, 0.32f, 0.44f, 0.54f, spatial: false, 1, 0.2f, 0.98f, 1.02f));
		result.Add(Sfx("player.buff_food", 330f, 0.38f, 0.42f, 0.52f, spatial: false, 1, 0.2f, 0.98f, 1.02f));
	}

	private static void AddResources(List<EventSpec> result)
	{
		result.Add(Sfx("resource.wood", 155f, 0.27f, 0.5f, 0.64f, spatial: true, 2, 0.12f, 0.92f, 1.05f));
		result.Add(Sfx("resource.stone", 92f, 0.3f, 0.56f, 0.7f, spatial: true, 2, 0.12f, 0.94f, 1.04f));
		result.Add(Sfx("resource.plant_fiber", 520f, 0.22f, 0.34f, 0.46f, spatial: true, 2, 0.1f, 0.94f, 1.08f));
		result.Add(Sfx("resource.copper_ore", 245f, 0.34f, 0.56f, 0.7f, spatial: true, 2, 0.12f, 0.96f, 1.04f));
	}

	private static void AddUi(List<EventSpec> result)
	{
		result.Add(Ui("ui.item_pickup", 740f, 0.18f, 0.4f, 0.5f, 2, 0.08f));
		result.Add(Ui("ui.button", 540f, 0.07f, 0.24f, 0.32f, 2, 0.045f));
		result.Add(Ui("ui.menu_open", 430f, 0.16f, 0.3f, 0.4f, 1, 0.08f));
		result.Add(Ui("ui.menu_close", 300f, 0.14f, 0.28f, 0.38f, 1, 0.08f));
		result.Add(Ui("ui.crafting_success", 680f, 0.3f, 0.42f, 0.52f, 1, 0.15f));
		result.Add(Ui("ui.crafting_failure", 145f, 0.24f, 0.4f, 0.5f, 1, 0.15f));
	}

	private static void AddWildling(List<EventSpec> result)
	{
		result.Add(Sfx("wildling.discover", 205f, 0.27f, 0.46f, 0.6f, spatial: true, 2, 0.2f, 0.93f, 1.08f));
		result.Add(Sfx("wildling.attack", 165f, 0.24f, 0.5f, 0.64f, spatial: true, 3, 0.12f, 0.92f, 1.06f));
		result.Add(Sfx("wildling.hit", 125f, 0.13f, 0.42f, 0.56f, spatial: true, 4, 0.05f, 0.94f, 1.08f));
		result.Add(Sfx("wildling.stagger", 78f, 0.38f, 0.54f, 0.68f, spatial: true, 2, 0.2f, 0.96f, 1.03f));
		result.Add(Sfx("wildling.death", 62f, 0.52f, 0.58f, 0.72f, spatial: true, 2, 0.3f, 0.94f, 1.02f));
	}

	private static void AddGaron(List<EventSpec> result)
	{
		result.Add(Sfx("garon.discover", 58f, 0.65f, 0.65f, 0.78f, spatial: true, 1, 1f, 0.98f, 1.01f));
		result.Add(Sfx("garon.attack.front", 84f, 0.42f, 0.62f, 0.76f, spatial: true, 1, 0.3f, 0.97f, 1.02f));
		result.Add(Sfx("garon.attack.charge", 52f, 0.62f, 0.7f, 0.82f, spatial: true, 1, 0.4f, 0.98f, 1.01f));
		result.Add(Sfx("garon.attack.spin", 118f, 0.5f, 0.64f, 0.78f, spatial: true, 1, 0.3f, 0.96f, 1.03f));
		result.Add(Sfx("garon.hit", 72f, 0.16f, 0.46f, 0.58f, spatial: true, 3, 0.055f, 0.96f, 1.04f));
		result.Add(Sfx("garon.stagger", 44f, 0.72f, 0.72f, 0.84f, spatial: true, 1, 0.5f, 0.98f, 1.01f));
		result.Add(Sfx("garon.death", 36f, 1.25f, 0.75f, 0.88f, spatial: true, 1, 1f, 0.99f, 1.01f));
	}

	private static void AddState(List<EventSpec> result)
	{
		result.Add(Sfx("state.victory", 820f, 0.85f, 0.58f, 0.7f, spatial: false, 1, 1f, 0.99f, 1.01f));
		result.Add(Sfx("state.defeat", 48f, 0.95f, 0.64f, 0.76f, spatial: false, 1, 1f, 0.99f, 1.01f));
		result.Add(Ui("state.scene_transition", 260f, 0.38f, 0.34f, 0.44f, 1, 0.25f));
	}

	private static void AddMusic(List<EventSpec> result)
	{
		result.Add(Loop("music.main_menu", 55f, 8f, 0.42f, AudioEventPlaybackType.Music));
		result.Add(Loop("music.world_map", 62f, 8f, 0.4f, AudioEventPlaybackType.Music));
		result.Add(Loop("music.home_base", 73f, 8f, 0.38f, AudioEventPlaybackType.Music));
		result.Add(Loop("music.outdoor", 49f, 8f, 0.4f, AudioEventPlaybackType.Music));
		result.Add(Loop("music.garon", 41f, 8f, 0.52f, AudioEventPlaybackType.Music));
	}

	private static void AddAmbience(List<EventSpec> result)
	{
		result.Add(Loop("ambience.greenwood", 17f, 6f, 0.32f, AudioEventPlaybackType.Ambience));
		result.Add(Loop("ambience.quarry", 11f, 6f, 0.3f, AudioEventPlaybackType.Ambience));
		result.Add(Loop("ambience.marsh", 14f, 6f, 0.34f, AudioEventPlaybackType.Ambience));
		result.Add(Loop("ambience.ember_ruins", 9f, 6f, 0.36f, AudioEventPlaybackType.Ambience));
	}

	private static EventSpec Sfx(string id, float frequency, float duration, float volumeMin, float volumeMax, bool spatial, int instances, float cooldown, float pitchMin, float pitchMax)
	{
		return new EventSpec(id, frequency, duration, volumeMin, volumeMax, pitchMin, pitchMax, AudioMixerBus.Sfx, AudioEventPlaybackType.OneShot, spatial, instances, cooldown);
	}

	private static EventSpec Ui(string id, float frequency, float duration, float volumeMin, float volumeMax, int instances, float cooldown)
	{
		return new EventSpec(id, frequency, duration, volumeMin, volumeMax, 0.98f, 1.02f, AudioMixerBus.Ui, AudioEventPlaybackType.OneShot, spatial: false, instances, cooldown);
	}

	private static EventSpec Loop(string id, float frequency, float duration, float volume, AudioEventPlaybackType type)
	{
		return new EventSpec(id, frequency, duration, volume, volume, 1f, 1f, (type == AudioEventPlaybackType.Music) ? AudioMixerBus.Music : AudioMixerBus.Sfx, type, spatial: false, 1, 0f);
	}

	private static SceneAudioProfile[] CreateSceneProfiles()
	{
		return new SceneAudioProfile[8]
		{
			new SceneAudioProfile("Bootstrap", "music.main_menu", string.Empty),
			new SceneAudioProfile("MainMenu", "music.main_menu", string.Empty),
			new SceneAudioProfile("WorldMap", "music.world_map", string.Empty),
			new SceneAudioProfile("HomeBase", "music.home_base", string.Empty),
			new SceneAudioProfile("Zone_Greenwood", "music.outdoor", "ambience.greenwood"),
			new SceneAudioProfile("Zone_Quarry", "music.outdoor", "ambience.quarry"),
			new SceneAudioProfile("Zone_Marsh", "music.outdoor", "ambience.marsh"),
			new SceneAudioProfile("Zone_EmberRuins", "music.outdoor", "ambience.ember_ruins")
		};
	}

	private static string ClipPath(EventSpec spec)
	{
		return "Assets/_Game/Audio/TemporaryGenerated/" + FileName(spec.Id) + ".wav";
	}

	private static string FileName(string eventId)
	{
		return (eventId ?? "audio_event").Replace('.', '_').Replace('/', '_');
	}

	private static int StableHash(string value)
	{
		uint hash = 2166136261u;
		string text = value ?? string.Empty;
		foreach (char character in text)
		{
			hash ^= character;
			hash *= 16777619;
		}
		return (int)hash;
	}

	private static void EnsureDirectory(string path)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}
}
}
