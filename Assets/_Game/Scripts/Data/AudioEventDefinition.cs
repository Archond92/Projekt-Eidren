using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(fileName = "AudioEvent", menuName = "Eidren/Audio/Audio Event")]
	public sealed class AudioEventDefinition : ScriptableObject
	{
		[SerializeField]
		private string eventId = string.Empty;

		[SerializeField]
		private AudioClip[] clips = Array.Empty<AudioClip>();

		[SerializeField]
		private Vector2 volumeRange = new Vector2(0.8f, 1f);

		[SerializeField]
		private Vector2 pitchRange = new Vector2(0.96f, 1.04f);

		[SerializeField]
		private AudioMixerBus mixerBus = AudioMixerBus.Sfx;

		[SerializeField]
		private AudioEventPlaybackType playbackType = AudioEventPlaybackType.OneShot;

		[SerializeField]
		private bool spatial;

		[SerializeField]
		[Min(1f)]
		private int maximumInstances = 3;

		[SerializeField]
		[Min(0f)]
		private float cooldown = 0.04f;

		public string Id => eventId ?? string.Empty;

		public IReadOnlyList<AudioClip> Clips => clips ?? Array.Empty<AudioClip>();

		public float MinimumVolume => Mathf.Clamp01(Mathf.Min(volumeRange.x, volumeRange.y));

		public float MaximumVolume => Mathf.Clamp01(Mathf.Max(volumeRange.x, volumeRange.y));

		public float MinimumPitch => Mathf.Clamp(Mathf.Min(pitchRange.x, pitchRange.y), 0.1f, 3f);

		public float MaximumPitch => Mathf.Clamp(Mathf.Max(pitchRange.x, pitchRange.y), 0.1f, 3f);

		public AudioMixerBus MixerBus => mixerBus;

		public AudioEventPlaybackType PlaybackType => playbackType;

		public bool Spatial => spatial;

		public int MaximumInstances => Mathf.Max(1, maximumInstances);

		public float Cooldown => Mathf.Max(0f, cooldown);

		public void Configure(string stableEventId, AudioClip[] eventClips, Vector2 eventVolumeRange, Vector2 eventPitchRange, AudioMixerBus bus, AudioEventPlaybackType type, bool isSpatial, int maxInstances, float repeatCooldown)
		{
			eventId = stableEventId ?? string.Empty;
			clips = eventClips ?? Array.Empty<AudioClip>();
			volumeRange = eventVolumeRange;
			pitchRange = eventPitchRange;
			mixerBus = bus;
			playbackType = type;
			spatial = isSpatial;
			maximumInstances = Mathf.Max(1, maxInstances);
			cooldown = Mathf.Max(0f, repeatCooldown);
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			if (string.IsNullOrWhiteSpace(Id))
			{
				list.Add("Audio event ID is required.");
			}
			if (MinimumVolume > MaximumVolume)
			{
				list.Add("Audio event '" + Id + "' has an invalid volume range.");
			}
			if (MinimumPitch > MaximumPitch)
			{
				list.Add("Audio event '" + Id + "' has an invalid pitch range.");
			}
			if (MaximumInstances < 1)
			{
				list.Add("Audio event '" + Id + "' needs at least one instance.");
			}
			return list.ToArray();
		}
	}

	[Serializable]
	public struct SceneAudioProfile
	{
		[SerializeField]
		private string sceneKey;

		[SerializeField]
		private string musicEventId;

		[SerializeField]
		private string ambienceEventId;

		public string SceneKey => sceneKey ?? string.Empty;

		public string MusicEventId => musicEventId ?? string.Empty;

		public string AmbienceEventId => ambienceEventId ?? string.Empty;

		public SceneAudioProfile(string scene, string music, string ambience)
		{
			sceneKey = scene ?? string.Empty;
			musicEventId = music ?? string.Empty;
			ambienceEventId = ambience ?? string.Empty;
		}
	}
}
