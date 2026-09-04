using Eidren.AI;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class ZoneThreatWatcher
	{
		private const float RescanSeconds = 0.5f;

		private readonly ZoneController _zone;

		private readonly List<EnemyControllerBase> _enemies = new List<EnemyControllerBase>();

		private float _nextScanAt = -1f / 0f;

		public ZoneThreatWatcher(ZoneController zone)
		{
			_zone = zone ?? throw new ArgumentNullException("zone");
		}

		public bool IsThreatened()
		{
			if (Time.unscaledTime >= _nextScanAt)
			{
				_nextScanAt = Time.unscaledTime + 0.5f;
				Collect();
			}
			foreach (EnemyControllerBase enemy in _enemies)
			{
				if (enemy != null && enemy.IsAlive && EnemyThreat.IsEngaged(enemy.EnemyState))
				{
					return true;
				}
			}
			return false;
		}

		private void Collect()
		{
			_enemies.Clear();
			AddFrom(_zone.PopulationRoot);
			AddFrom(_zone.BossArea);
		}

		private void AddFrom(Transform root)
		{
			if (!(root == null))
			{
				_enemies.AddRange(root.GetComponentsInChildren<EnemyControllerBase>(includeInactive: true));
			}
		}
	}
}
