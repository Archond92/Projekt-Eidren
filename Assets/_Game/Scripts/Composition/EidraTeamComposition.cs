using Eidren.Core.Services;
using Eidren.Data;
using System;

namespace Eidren.Composition
{
	public static class EidraTeamComposition
	{
		public static void Resolve(EidrenServiceRoot services, out EidraData first, out EidraData second, params EidraData[] preferredAssets)
		{
			first = null;
			second = null;
			if (services == null || services.EidraRoster == null || services.ContentDatabase == null)
			{
				return;
			}
			string[] activeInstanceIds = services.EidraRoster.Roster.GetActiveInstanceIds();
			for (int i = 0; i < activeInstanceIds.Length && i < 2; i++)
			{
				if (services.EidraRoster.Roster.TryGet(activeInstanceIds[i], out var state))
				{
					EidraData eidraData = Resolve(services.ContentDatabase, state.EidraId, preferredAssets);
					if (i == 0)
					{
						first = eidraData;
					}
					else
					{
						second = eidraData;
					}
				}
			}
		}

		private static EidraData Resolve(ContentDatabase content, string eidraId, EidraData[] preferredAssets)
		{
			if (preferredAssets != null)
			{
				foreach (EidraData eidraData in preferredAssets)
				{
					if (eidraData != null && string.Equals(eidraData.Id, eidraId, StringComparison.Ordinal))
					{
						return eidraData;
					}
				}
			}
			EidraData value;
			return content.TryGetEidra(eidraId, out value) ? value : null;
		}
	}
}
