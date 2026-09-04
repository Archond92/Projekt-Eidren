using System;

namespace Eidren.Core.Services
{
	public static class ItemInstanceIds
	{
		public static string Create()
		{
			return $"item.{Guid.NewGuid():N}";
		}
	}
}
