using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class WeaponProgressionState
	{
		private readonly struct WeaponUpgradeRuntime
		{
			public int Level { get; }

			public float HealthDamageMultiplier { get; }

			public float StaggerDamageMultiplier { get; }

			public WeaponUpgradeRuntime(int level, float healthDamageMultiplier, float staggerDamageMultiplier)
			{
				Level = level;
				HealthDamageMultiplier = healthDamageMultiplier;
				StaggerDamageMultiplier = staggerDamageMultiplier;
			}
		}

		private readonly Dictionary<string, WeaponUpgradeRuntime> _upgrades = new Dictionary<string, WeaponUpgradeRuntime>(StringComparer.Ordinal);

		public event Action<string, int> UpgradeApplied;

		public int GetLevel(string weaponId)
		{
			if (string.IsNullOrWhiteSpace(weaponId) || !_upgrades.TryGetValue(weaponId, out var value))
			{
				return 1;
			}
			return value.Level;
		}

		public float GetHealthDamageMultiplier(string weaponId)
		{
			if (string.IsNullOrWhiteSpace(weaponId) || !_upgrades.TryGetValue(weaponId, out var value))
			{
				return 1f;
			}
			return value.HealthDamageMultiplier;
		}

		public float GetStaggerDamageMultiplier(string weaponId)
		{
			if (string.IsNullOrWhiteSpace(weaponId) || !_upgrades.TryGetValue(weaponId, out var value))
			{
				return 1f;
			}
			return value.StaggerDamageMultiplier;
		}

		public bool CanApply(string weaponId, int targetLevel)
		{
			if (!string.IsNullOrWhiteSpace(weaponId))
			{
				return targetLevel > GetLevel(weaponId);
			}
			return false;
		}

		public bool TryApply(string weaponId, int targetLevel, float healthDamageMultiplier, float staggerDamageMultiplier)
		{
			if (!CanApply(weaponId, targetLevel) || healthDamageMultiplier < 1f || staggerDamageMultiplier < 1f)
			{
				return false;
			}
			_upgrades[weaponId] = new WeaponUpgradeRuntime(targetLevel, healthDamageMultiplier, staggerDamageMultiplier);
			this.UpgradeApplied?.Invoke(weaponId, targetLevel);
			return true;
		}

		public void Reset()
		{
			_upgrades.Clear();
		}

		public WeaponUpgradeRuntimeState[] Export()
		{
			WeaponUpgradeRuntimeState[] array = new WeaponUpgradeRuntimeState[_upgrades.Count];
			int num = 0;
			foreach (KeyValuePair<string, WeaponUpgradeRuntime> upgrade in _upgrades)
			{
				array[num++] = new WeaponUpgradeRuntimeState(upgrade.Key, upgrade.Value.Level, upgrade.Value.HealthDamageMultiplier, upgrade.Value.StaggerDamageMultiplier);
			}
			return array;
		}

		public void Restore(IReadOnlyList<WeaponUpgradeRuntimeState> upgrades)
		{
			_upgrades.Clear();
			if (upgrades == null)
			{
				return;
			}
			foreach (WeaponUpgradeRuntimeState upgrade in upgrades)
			{
				if (upgrade.Level > 1)
				{
					TryApply(upgrade.WeaponId, upgrade.Level, upgrade.HealthDamageMultiplier, upgrade.StaggerDamageMultiplier);
				}
			}
		}
	}
}
