using System;

namespace Eidren.Data
{
	[Flags]
	public enum WorldChestFamilyMask
	{
		None = 0,
		Common = 1,
		Guarded = 2,
		Hidden = 4,
		All = 7
	}
}
