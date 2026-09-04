using System;
using UnityEngine;

namespace Eidren.Data
{
	public static class VisualScaleLoader
	{
		private static VisualScaleTable _cached;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetCache()
		{
			_cached = null;
		}

		public static VisualScaleTable Load()
		{
			if (_cached != null)
			{
				return _cached;
			}
			_cached = Resources.Load<VisualScaleTable>("Data/VisualScaleTable");
			if (_cached == null)
			{
				throw new InvalidOperationException("Die Groessentabelle fehlt unter Resources/Data/VisualScaleTable. Ohne sie wird keine Groesse geraten (M10.2).");
			}
			return _cached;
		}
	}
}
