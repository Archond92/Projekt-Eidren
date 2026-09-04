using System;

namespace Eidren.Core.Services
{
	[Flags]
	public enum MapSideFlags
	{
		None = 0,
		North = 1,
		East = 2,
		South = 4,
		West = 8,
		All = 0xF
	}
}
