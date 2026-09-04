using Eidren.Data;
using Eidren.Eidra;
using System;

namespace Eidren.Composition
{
	public sealed class EidraTeamRosterBinding : IDisposable
	{
		private readonly EidrenServiceRoot _services;

		private readonly EidraTeamController _team;

		private readonly EidraData[] _preferredAssets;

		private bool _disposed;

		public EidraTeamRosterBinding(EidrenServiceRoot services, EidraTeamController team, params EidraData[] preferredAssets)
		{
			_services = services ?? throw new ArgumentNullException("services");
			_team = team ?? throw new ArgumentNullException("team");
			_preferredAssets = preferredAssets ?? Array.Empty<EidraData>();
			_services.EidraRoster.Roster.Changed -= HandleRosterChanged;
			_services.EidraRoster.Roster.Changed += HandleRosterChanged;
			Refresh();
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_services.EidraRoster.Roster.Changed -= HandleRosterChanged;
				_disposed = true;
			}
		}

		private void HandleRosterChanged()
		{
			Refresh();
		}

		private void Refresh()
		{
			if (!_disposed && !(_team == null))
			{
				EidraTeamComposition.Resolve(_services, out var first, out var second, _preferredAssets);
				_team.SetTeam(first, second);
			}
		}
	}
}
