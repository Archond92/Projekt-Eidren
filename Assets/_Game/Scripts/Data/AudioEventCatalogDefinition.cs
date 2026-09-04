using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(fileName = "AudioEventCatalog", menuName = "Eidren/Audio/Audio Event Catalog")]
	public sealed class AudioEventCatalogDefinition : ScriptableObject
	{
		[SerializeField]
		private AudioEventDefinition[] events = Array.Empty<AudioEventDefinition>();

		[SerializeField]
		private SceneAudioProfile[] sceneProfiles = Array.Empty<SceneAudioProfile>();

		private readonly Dictionary<string, AudioEventDefinition> _byId = new Dictionary<string, AudioEventDefinition>(StringComparer.Ordinal);

		private readonly Dictionary<string, SceneAudioProfile> _byScene = new Dictionary<string, SceneAudioProfile>(StringComparer.Ordinal);

		private bool _indexBuilt;

		public IReadOnlyList<AudioEventDefinition> Events => events ?? Array.Empty<AudioEventDefinition>();

		public IReadOnlyList<SceneAudioProfile> SceneProfiles => sceneProfiles ?? Array.Empty<SceneAudioProfile>();

		public void Configure(AudioEventDefinition[] definitions, SceneAudioProfile[] profiles)
		{
			events = definitions ?? Array.Empty<AudioEventDefinition>();
			sceneProfiles = profiles ?? Array.Empty<SceneAudioProfile>();
			RebuildIndex();
		}

		public bool TryGetEvent(string eventId, out AudioEventDefinition definition)
		{
			EnsureIndex();
			definition = null;
			return !string.IsNullOrWhiteSpace(eventId) && _byId.TryGetValue(eventId, out definition);
		}

		public bool TryGetSceneProfile(string sceneKey, out SceneAudioProfile profile)
		{
			EnsureIndex();
			profile = default(SceneAudioProfile);
			return !string.IsNullOrWhiteSpace(sceneKey) && _byScene.TryGetValue(sceneKey, out profile);
		}

		public string[] GetValidationErrors()
		{
			List<string> list = new List<string>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (AudioEventDefinition @event in Events)
			{
				if (@event == null)
				{
					list.Add("Audio catalog contains a null event.");
					continue;
				}
				if (!hashSet.Add(@event.Id))
				{
					list.Add("Duplicate audio event ID '" + @event.Id + "'.");
				}
				string[] validationErrors = @event.GetValidationErrors();
				foreach (string item in validationErrors)
				{
					list.Add(item);
				}
			}
			HashSet<string> hashSet2 = new HashSet<string>(StringComparer.Ordinal);
			foreach (SceneAudioProfile sceneProfile in SceneProfiles)
			{
				if (string.IsNullOrWhiteSpace(sceneProfile.SceneKey))
				{
					list.Add("Scene audio profile needs a scene key.");
					continue;
				}
				if (!hashSet2.Add(sceneProfile.SceneKey))
				{
					list.Add("Duplicate scene audio profile '" + sceneProfile.SceneKey + "'.");
				}
				ValidateReference(sceneProfile.SceneKey, sceneProfile.MusicEventId, AudioEventPlaybackType.Music, hashSet, list);
				ValidateReference(sceneProfile.SceneKey, sceneProfile.AmbienceEventId, AudioEventPlaybackType.Ambience, hashSet, list);
			}
			return list.ToArray();
		}

		private static void ValidateReference(string sceneKey, string eventId, AudioEventPlaybackType expected, HashSet<string> ids, List<string> errors)
		{
			if (!string.IsNullOrWhiteSpace(eventId) && !ids.Contains(eventId))
			{
				errors.Add("Scene '" + sceneKey + "' references unknown audio event '" + eventId + "'.");
			}
		}

		private void EnsureIndex()
		{
			if (!_indexBuilt)
			{
				RebuildIndex();
			}
		}

		private void RebuildIndex()
		{
			_byId.Clear();
			_byScene.Clear();
			foreach (AudioEventDefinition @event in Events)
			{
				if (@event != null && !string.IsNullOrWhiteSpace(@event.Id) && !_byId.ContainsKey(@event.Id))
				{
					_byId.Add(@event.Id, @event);
				}
			}
			foreach (SceneAudioProfile sceneProfile in SceneProfiles)
			{
				if (!string.IsNullOrWhiteSpace(sceneProfile.SceneKey) && !_byScene.ContainsKey(sceneProfile.SceneKey))
				{
					_byScene.Add(sceneProfile.SceneKey, sceneProfile);
				}
			}
			_indexBuilt = true;
		}

		private void OnValidate()
		{
			_indexBuilt = false;
		}
	}
}
