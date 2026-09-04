using System;

namespace Eidren.Core.Services
{
	[Serializable]
	public sealed class SettingsData
	{
		public int settingsVersion = 1;

		public float masterVolume = 1f;

		public float musicVolume = 0.8f;

		public float sfxVolume = 0.9f;

		public EidrenQualityPreset quality = EidrenQualityPreset.Medium;

		public int targetFrameRate = 60;

		public SettingsData Copy()
		{
			return new SettingsData
			{
				settingsVersion = settingsVersion,
				masterVolume = masterVolume,
				musicVolume = musicVolume,
				sfxVolume = sfxVolume,
				quality = quality,
				targetFrameRate = targetFrameRate
			};
		}
	}
}
