using Eidren.Data;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.Audio;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Eidren.Core.Services
{
	public sealed class AudioService : MonoBehaviour
	{
		private sealed class PooledVoice
		{
			public AudioSource Source;

			public string EventId = string.Empty;

			public float ReleaseAt;
		}

		private const int DefaultPoolSize = 12;

		private const float DefaultCrossfadeDuration = 0.65f;

		private readonly List<PooledVoice> _voices = new List<PooledVoice>();

		private readonly Dictionary<string, float> _lastPlayedAt = new Dictionary<string, float>(StringComparer.Ordinal);

		private readonly AudioSource[] _musicSources = new AudioSource[2];

		private readonly bool[] _musicPlaying = new bool[2];

		private AudioEventCatalogDefinition _catalog;

		private SettingsService _settings;

		private AudioSource _ambienceSource;

		private Coroutine _musicFade;

		private Coroutine _ambienceFade;

		private int _activeMusicIndex = -1;

		private string _currentSceneKey = string.Empty;

		private string _currentAmbienceEventId = string.Empty;

		private bool _bossMusicActive;

		private bool _initialized;

		public string CurrentMusicEventId { get; private set; } = string.Empty;

		public string CurrentAmbienceEventId => _currentAmbienceEventId;

		public int PoolSize => _voices.Count;

		public int ActiveOneShotCount
		{
			get
			{
				int num = 0;
				float unscaledTime = Time.unscaledTime;
				foreach (PooledVoice voice in _voices)
				{
					if (!string.IsNullOrEmpty(voice.EventId) && voice.ReleaseAt > unscaledTime)
					{
						num++;
					}
				}
				return num;
			}
		}

		public int ActiveMusicLoopCount
		{
			get
			{
				int num = 0;
				for (int i = 0; i < _musicPlaying.Length; i++)
				{
					if (_musicPlaying[i])
					{
						num++;
					}
				}
				return num;
			}
		}

		public void Initialize(AudioEventCatalogDefinition catalog, SettingsService settings, int poolSize = 12)
		{
			StopAllImmediate();
			_catalog = catalog;
			_settings = settings;
			EnsureSources(Mathf.Max(1, poolSize));
			_lastPlayedAt.Clear();
			_initialized = true;
		}

		public bool PlayOneShot(string eventId)
		{
			return PlayOneShot(eventId, base.transform.position, usePosition: false);
		}

		public bool PlayOneShot(string eventId, Vector3 position)
		{
			return PlayOneShot(eventId, position, usePosition: true);
		}

		public bool PlayMusic(string eventId, float crossfadeDuration = 0.65f)
		{
			if (!TryGetDefinition(eventId, AudioEventPlaybackType.Music, out var definition) || !TryChooseClip(definition, out var clip))
			{
				return false;
			}
			if (string.Equals(CurrentMusicEventId, eventId, StringComparison.Ordinal) && _activeMusicIndex >= 0 && _musicPlaying[_activeMusicIndex])
			{
				return true;
			}
			if (_musicFade != null)
			{
				StopCoroutine(_musicFade);
			}
			_musicFade = null;
			int activeMusicIndex = _activeMusicIndex;
			int num = ((activeMusicIndex == 0) ? 1 : 0);
			if (activeMusicIndex < 0)
			{
				num = 0;
			}
			StopMusicSource(num);
			AudioSource audioSource = _musicSources[num];
			float num2 = RandomVolume(definition);
			ConfigureLoopSource(audioSource, definition, clip);
			audioSource.volume = ((activeMusicIndex < 0 || crossfadeDuration <= 0f) ? num2 : 0f);
			audioSource.Play();
			_musicPlaying[num] = true;
			_activeMusicIndex = num;
			CurrentMusicEventId = eventId;
			if (activeMusicIndex < 0 || crossfadeDuration <= 0f)
			{
				if (activeMusicIndex >= 0)
				{
					StopMusicSource(activeMusicIndex);
				}
				return true;
			}
			_musicFade = StartCoroutine(CrossfadeMusic(activeMusicIndex, num, num2, crossfadeDuration));
			return true;
		}

		public void StopMusic(float fadeDuration = 0.65f)
		{
			if (_musicFade != null)
			{
				StopCoroutine(_musicFade);
			}
			_musicFade = null;
			CurrentMusicEventId = string.Empty;
			if (_activeMusicIndex >= 0)
			{
				if (fadeDuration <= 0f)
				{
					StopMusicSource(_activeMusicIndex);
					_activeMusicIndex = -1;
				}
				else
				{
					_musicFade = StartCoroutine(FadeOutMusic(_activeMusicIndex, fadeDuration));
				}
			}
		}

		public bool PlayAmbience(string eventId, float fadeDuration = 0.65f)
		{
			if (!TryGetDefinition(eventId, AudioEventPlaybackType.Ambience, out var definition) || !TryChooseClip(definition, out var clip))
			{
				return false;
			}
			if (string.Equals(_currentAmbienceEventId, eventId, StringComparison.Ordinal) && _ambienceSource.isPlaying)
			{
				return true;
			}
			if (_ambienceFade != null)
			{
				StopCoroutine(_ambienceFade);
			}
			_ambienceFade = StartCoroutine(SwitchAmbience(definition, clip, Mathf.Max(0f, fadeDuration)));
			return true;
		}

		public void StopAmbience(float fadeDuration = 0.65f)
		{
			if (_ambienceFade != null)
			{
				StopCoroutine(_ambienceFade);
			}
			_ambienceFade = null;
			_currentAmbienceEventId = string.Empty;
			if (!(_ambienceSource == null) && _ambienceSource.isPlaying)
			{
				if (fadeDuration <= 0f)
				{
					_ambienceSource.Stop();
				}
				else
				{
					_ambienceFade = StartCoroutine(FadeOutAmbience(fadeDuration));
				}
			}
		}

		public void PlaySceneAudio(string sceneKey, float crossfadeDuration = 0.65f)
		{
			_currentSceneKey = sceneKey ?? string.Empty;
			_bossMusicActive = false;
			if (_catalog == null || !_catalog.TryGetSceneProfile(_currentSceneKey, out var profile))
			{
				StopMusic(crossfadeDuration);
				StopAmbience(crossfadeDuration);
				return;
			}
			if (string.IsNullOrWhiteSpace(profile.MusicEventId))
			{
				StopMusic(crossfadeDuration);
			}
			else
			{
				PlayMusic(profile.MusicEventId, crossfadeDuration);
			}
			if (string.IsNullOrWhiteSpace(profile.AmbienceEventId))
			{
				StopAmbience(crossfadeDuration);
			}
			else
			{
				PlayAmbience(profile.AmbienceEventId, crossfadeDuration);
			}
		}

		public void SetBossMusicActive(bool active, float crossfadeDuration = 0.65f)
		{
			if (_bossMusicActive != active)
			{
				_bossMusicActive = active;
				SceneAudioProfile profile;
				if (active)
				{
					PlayMusic("music.garon", crossfadeDuration);
				}
				else if (_catalog != null && _catalog.TryGetSceneProfile(_currentSceneKey, out profile) && !string.IsNullOrWhiteSpace(profile.MusicEventId))
				{
					PlayMusic(profile.MusicEventId, crossfadeDuration);
				}
				else
				{
					StopMusic(crossfadeDuration);
				}
			}
		}

		public int CountActiveInstances(string eventId)
		{
			int num = 0;
			float unscaledTime = Time.unscaledTime;
			foreach (PooledVoice voice in _voices)
			{
				if (voice.ReleaseAt > unscaledTime && string.Equals(voice.EventId, eventId, StringComparison.Ordinal))
				{
					num++;
				}
			}
			return num;
		}

		private bool PlayOneShot(string eventId, Vector3 position, bool usePosition)
		{
			if (!TryGetDefinition(eventId, AudioEventPlaybackType.OneShot, out var definition) || !TryChooseClip(definition, out var clip))
			{
				return false;
			}
			float unscaledTime = Time.unscaledTime;
			if (_lastPlayedAt.TryGetValue(eventId, out var value) && unscaledTime - value < definition.Cooldown)
			{
				return false;
			}
			if (CountActiveInstances(eventId) >= definition.MaximumInstances)
			{
				return false;
			}
			PooledVoice pooledVoice = FindAvailableVoice(unscaledTime);
			if (pooledVoice == null)
			{
				return false;
			}
			AudioSource source = pooledVoice.Source;
			source.Stop();
			source.clip = clip;
			source.loop = false;
			source.volume = RandomVolume(definition);
			source.pitch = UnityEngine.Random.Range(definition.MinimumPitch, definition.MaximumPitch);
			source.spatialBlend = (definition.Spatial ? 1f : 0f);
			source.outputAudioMixerGroup = ResolveMixerGroup(definition.MixerBus);
			source.transform.position = (usePosition ? position : base.transform.position);
			source.Play();
			pooledVoice.EventId = eventId;
			pooledVoice.ReleaseAt = unscaledTime + clip.length / Mathf.Max(0.1f, Mathf.Abs(source.pitch));
			_lastPlayedAt[eventId] = unscaledTime;
			return true;
		}

		private IEnumerator CrossfadeMusic(int previousIndex, int nextIndex, float targetVolume, float duration)
		{
			AudioSource previous = _musicSources[previousIndex];
			AudioSource next = _musicSources[nextIndex];
			float previousStart = previous.volume;
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float progress = Mathf.Clamp01(elapsed / duration);
				previous.volume = Mathf.Lerp(previousStart, 0f, progress);
				next.volume = Mathf.Lerp(0f, targetVolume, progress);
				yield return null;
			}
			StopMusicSource(previousIndex);
			next.volume = targetVolume;
			_musicFade = null;
		}

		private IEnumerator FadeOutMusic(int index, float duration)
		{
			AudioSource source = _musicSources[index];
			float start = source.volume;
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				source.volume = Mathf.Lerp(start, 0f, elapsed / duration);
				yield return null;
			}
			StopMusicSource(index);
			if (_activeMusicIndex == index)
			{
				_activeMusicIndex = -1;
			}
			_musicFade = null;
		}

		private IEnumerator SwitchAmbience(AudioEventDefinition definition, AudioClip clip, float duration)
		{
			if (_ambienceSource.isPlaying && duration > 0f)
			{
				float start = _ambienceSource.volume;
				float elapsed = 0f;
				while (elapsed < duration * 0.5f)
				{
					elapsed += Time.unscaledDeltaTime;
					_ambienceSource.volume = Mathf.Lerp(start, 0f, elapsed / (duration * 0.5f));
					yield return null;
				}
			}
			_ambienceSource.Stop();
			ConfigureLoopSource(_ambienceSource, definition, clip);
			float target = RandomVolume(definition);
			_ambienceSource.volume = ((duration > 0f) ? 0f : target);
			_ambienceSource.Play();
			_currentAmbienceEventId = definition.Id;
			if (duration > 0f)
			{
				float elapsed2 = 0f;
				while (elapsed2 < duration * 0.5f)
				{
					elapsed2 += Time.unscaledDeltaTime;
					_ambienceSource.volume = Mathf.Lerp(0f, target, elapsed2 / (duration * 0.5f));
					yield return null;
				}
			}
			_ambienceSource.volume = target;
			_ambienceFade = null;
		}

		private IEnumerator FadeOutAmbience(float duration)
		{
			float start = _ambienceSource.volume;
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				_ambienceSource.volume = Mathf.Lerp(start, 0f, elapsed / duration);
				yield return null;
			}
			_ambienceSource.Stop();
			_ambienceFade = null;
		}

		private void EnsureSources(int poolSize)
		{
			while (_voices.Count < poolSize)
			{
				int count = _voices.Count;
				AudioSource source = CreateSource($"AudioVoice_{count:00}");
				_voices.Add(new PooledVoice
				{
					Source = source
				});
			}
			for (int i = 0; i < _musicSources.Length; i++)
			{
				if (_musicSources[i] == null)
				{
					_musicSources[i] = CreateSource($"Music_{i + 1}");
				}
			}
			if (_ambienceSource == null)
			{
				_ambienceSource = CreateSource("Ambience");
			}
		}

		private AudioSource CreateSource(string sourceName)
		{
			GameObject gameObject = new GameObject(sourceName);
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			AudioSource audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSource.loop = false;
			audioSource.spatialBlend = 0f;
			audioSource.dopplerLevel = 0f;
			return audioSource;
		}

		private PooledVoice FindAvailableVoice(float now)
		{
			foreach (PooledVoice voice in _voices)
			{
				if (string.IsNullOrEmpty(voice.EventId) || voice.ReleaseAt <= now)
				{
					voice.EventId = string.Empty;
					return voice;
				}
			}
			return null;
		}

		private bool TryGetDefinition(string eventId, AudioEventPlaybackType expectedType, out AudioEventDefinition definition)
		{
			definition = null;
			return _initialized && _catalog != null && _catalog.TryGetEvent(eventId, out definition) && definition != null && definition.PlaybackType == expectedType;
		}

		private static bool TryChooseClip(AudioEventDefinition definition, out AudioClip clip)
		{
			clip = null;
			int num = 0;
			foreach (AudioClip clip2 in definition.Clips)
			{
				if (clip2 != null)
				{
					num++;
				}
			}
			if (num == 0)
			{
				return false;
			}
			int num2 = UnityEngine.Random.Range(0, num);
			foreach (AudioClip clip3 in definition.Clips)
			{
				if (clip3 == null || num2-- != 0)
				{
					continue;
				}
				clip = clip3;
				return true;
			}
			return false;
		}

		private void ConfigureLoopSource(AudioSource source, AudioEventDefinition definition, AudioClip clip)
		{
			source.clip = clip;
			source.loop = true;
			source.pitch = UnityEngine.Random.Range(definition.MinimumPitch, definition.MaximumPitch);
			source.spatialBlend = 0f;
			source.outputAudioMixerGroup = ResolveMixerGroup(definition.MixerBus);
		}

		private AudioMixerGroup ResolveMixerGroup(AudioMixerBus bus)
		{
			if (_settings == null)
			{
				return null;
			}
			if (1 == 0)
			{
			}
			AudioMixerGroup result = bus switch
			{
				AudioMixerBus.Master => _settings.MasterGroup, 
				AudioMixerBus.Music => _settings.MusicGroup, 
				AudioMixerBus.Ui => _settings.UiGroup, 
				_ => _settings.SfxGroup, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private static float RandomVolume(AudioEventDefinition definition)
		{
			return UnityEngine.Random.Range(definition.MinimumVolume, definition.MaximumVolume);
		}

		private void StopMusicSource(int index)
		{
			if (index >= 0 && index < _musicSources.Length)
			{
				AudioSource audioSource = _musicSources[index];
				if (audioSource != null)
				{
					audioSource.Stop();
					audioSource.clip = null;
					audioSource.volume = 0f;
				}
				_musicPlaying[index] = false;
			}
		}

		private void Update()
		{
			float unscaledTime = Time.unscaledTime;
			foreach (PooledVoice voice in _voices)
			{
				if (!string.IsNullOrEmpty(voice.EventId) && voice.ReleaseAt <= unscaledTime)
				{
					voice.Source.Stop();
					voice.Source.clip = null;
					voice.EventId = string.Empty;
				}
			}
		}

		private void StopAllImmediate()
		{
			if (_musicFade != null)
			{
				StopCoroutine(_musicFade);
			}
			if (_ambienceFade != null)
			{
				StopCoroutine(_ambienceFade);
			}
			_musicFade = null;
			_ambienceFade = null;
			foreach (PooledVoice voice in _voices)
			{
				voice.Source?.Stop();
				voice.EventId = string.Empty;
				voice.ReleaseAt = 0f;
			}
			for (int i = 0; i < _musicSources.Length; i++)
			{
				StopMusicSource(i);
			}
			_ambienceSource?.Stop();
			_activeMusicIndex = -1;
			CurrentMusicEventId = string.Empty;
			_currentAmbienceEventId = string.Empty;
			_bossMusicActive = false;
		}

		private void OnDisable()
		{
			StopAllImmediate();
		}
	}
}
