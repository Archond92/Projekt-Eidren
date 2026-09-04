using System;

namespace Eidren.Core.Services
{
	public static class WorldMapNodeIds
	{
		public const string HomeBase = "home_base";

		public const string Greenwood = "zone_greenwood";

		public const string Quarry = "zone_quarry";

		public const string Marsh = "zone_marsh";

		public const string EmberRuins = "zone_ember_ruins";

		public const string TwilightGrove = "zone_twilight_grove";

		public const string VeilMarsh = "zone_veil_marsh";

		public const string GreyRifts = "zone_grey_rifts";

		public static bool IsKnown(string id)
		{
			if (!string.Equals(id, "home_base", StringComparison.Ordinal) && !string.Equals(id, "zone_greenwood", StringComparison.Ordinal) && !string.Equals(id, "zone_quarry", StringComparison.Ordinal) && !string.Equals(id, "zone_marsh", StringComparison.Ordinal) && !string.Equals(id, "zone_ember_ruins", StringComparison.Ordinal) && !string.Equals(id, "zone_twilight_grove", StringComparison.Ordinal) && !string.Equals(id, "zone_veil_marsh", StringComparison.Ordinal))
			{
				return string.Equals(id, "zone_grey_rifts", StringComparison.Ordinal);
			}
			return true;
		}

		public static bool IsTierTwo(string id)
		{
			if (!string.Equals(id, "zone_twilight_grove", StringComparison.Ordinal) && !string.Equals(id, "zone_veil_marsh", StringComparison.Ordinal))
			{
				return string.Equals(id, "zone_grey_rifts", StringComparison.Ordinal);
			}
			return true;
		}
	}
}
