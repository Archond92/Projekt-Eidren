using Eidren.Core.Services;
using UnityEngine;

namespace Eidren.Presentation
{
	[DisallowMultipleComponent]
	public sealed class PlayerExperienceFeedback : MonoBehaviour
	{
		private PlayerProgressionService _progression;

		private int _burstIndex;

		public void Bind(PlayerProgressionService progression)
		{
			if (_progression != null)
			{
				_progression.ExperienceGained -= HandleExperienceGained;
			}
			_progression = progression;
			if (_progression != null)
			{
				_progression.ExperienceGained += HandleExperienceGained;
			}
		}

		private void HandleExperienceGained(int amount)
		{
			if (amount > 0)
			{
				float num = (float)(_burstIndex++ % 3) * 0.22f;
				CombatFeedback.SpawnStatusText(base.transform.position + Vector3.up * (2.15f + num), $"+{amount} XP", new Color(0.96f, 0.76f, 0.28f), 1.15f);
			}
		}

		private void OnDestroy()
		{
			if (_progression != null)
			{
				_progression.ExperienceGained -= HandleExperienceGained;
			}
		}
	}
}
