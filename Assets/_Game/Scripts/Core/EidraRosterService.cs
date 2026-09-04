using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class EidraRosterService
	{
		public const int MaximumActiveSlots = 2;

		private readonly EidraRoster _roster;

		private readonly TechnologyUnlockService _unlocks;

		public int ActiveSlotCapacity => (!_unlocks.IsFeatureUnlocked("feature.eidra_slot_2")) ? 1 : 2;

		public EidraRoster Roster => _roster;

		// F31-006: Meldet jeden erfolgreichen Fang an den Questdienst.
		public event Action Captured;

		public EidraRosterService(EidraRoster roster, TechnologyUnlockService unlocks)
		{
			_roster = roster ?? throw new ArgumentNullException("roster");
			_unlocks = unlocks ?? throw new ArgumentNullException("unlocks");
		}

		public bool TryCapture(string eidraId, out EidraInstanceState instance, out string error)
		{
			instance = default(EidraInstanceState);
			if (string.IsNullOrWhiteSpace(eidraId))
			{
				error = "An eidra id is required to capture.";
				return false;
			}
			EidraInstanceState eidraInstanceState = new EidraInstanceState(EidraInstanceState.BuildInstanceId(eidraId, _roster.NextOrdinalFor(eidraId)), eidraId);
			if (!_roster.TryAdd(eidraInstanceState))
			{
				error = "Eidra instance '" + eidraInstanceState.InstanceId + "' already exists in the roster.";
				return false;
			}
			string[] activeInstanceIds = _roster.GetActiveInstanceIds();
			if (activeInstanceIds.Length < ActiveSlotCapacity)
			{
				string[] array = new string[activeInstanceIds.Length + 1];
				Array.Copy(activeInstanceIds, array, activeInstanceIds.Length);
				array[activeInstanceIds.Length] = eidraInstanceState.InstanceId;
				if (!_roster.TrySetActiveTeam(array, ActiveSlotCapacity, out error))
				{
					return false;
				}
			}
			instance = eidraInstanceState;
			error = string.Empty;
			this.Captured?.Invoke();
			return true;
		}

		public bool TrySetActiveTeam(IReadOnlyList<string> instanceIds, out string error)
		{
			return _roster.TrySetActiveTeam(instanceIds, ActiveSlotCapacity, out error);
		}
	}
}
