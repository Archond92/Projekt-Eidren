using System;
using UnityEngine;

namespace Eidren.Data
{
	[Serializable]
	public struct WorldMapResourcePreview
	{
		public string StableId;

		public string DisplayName;

		public Sprite Icon;

		public bool IsFuturePlaceholder;
	}
}
