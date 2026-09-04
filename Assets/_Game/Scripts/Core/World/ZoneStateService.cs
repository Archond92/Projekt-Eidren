using System.Collections.Generic;
using System;

namespace Eidren.Core.Services
{
	public sealed class ZoneStateService
	{
		public const string HomeZoneId = "home_base";

		private readonly Dictionary<string, ZoneState> _states = new Dictionary<string, ZoneState>(StringComparer.Ordinal);

		/// <summary>
		/// Ist dieser Wert gesetzt, wuerfelt jede NEU angelegte Sitzung dieselbe
		/// Welt. Produktiv bleibt er <c>null</c> — jeder Durchgang bekommt seine
		/// eigene Welt. Die Testsuite nagelt ihn fest, weil die Zonen sonst pro
		/// Lauf neu gewuerfelt werden und ein gruener Wiederholungslauf nichts
		/// beweist. Denselben Zweck erfuellt <c>G001VisualAbnahmeRunner.WorldSeed</c>
		/// bereits fuer die Bildabnahme — nur eben nicht fuer die Suite.
		/// </summary>
		public static int? DefaultMasterSeed { get; set; }

		private Random _seedSource = CreateSource(DefaultMasterSeed);

		public int Count => _states.Count;

		/// <summary>
		/// Setzt die Zonen zurueck. Ohne Argument gilt <see cref="DefaultMasterSeed"/> —
		/// produktiv ist der null und die Welt damit wie bisher zufaellig.
		/// Der Vorgabewert MUSS hier greifen: <c>GameSession.StartNewGame</c>
		/// ruft parameterlos auf, und ein Wegwerfen der Saat an dieser Stelle
		/// haette das Festnageln still unwirksam gemacht.
		/// </summary>
		public void Reset(int? masterSeed = null)
		{
			_states.Clear();
			_seedSource = CreateSource(masterSeed ?? DefaultMasterSeed);
		}

		public ZoneState GetOrCreate(string zoneId, int generatorVersion)
		{
			if (string.IsNullOrWhiteSpace(zoneId))
			{
				throw new ArgumentException("A stable zone ID is required.", "zoneId");
			}
			if (_states.TryGetValue(zoneId, out var value))
			{
				if (value.GeneratorVersion != generatorVersion)
				{
					value.Regenerate(NextSeed(), generatorVersion);
				}
				return value;
			}
			ZoneState zoneState = new ZoneState(zoneId, NextSeed(), generatorVersion);
			_states.Add(zoneId, zoneState);
			return zoneState;
		}

		public bool TryGet(string zoneId, out ZoneState state)
		{
			if (string.IsNullOrWhiteSpace(zoneId))
			{
				state = null;
				return false;
			}
			return _states.TryGetValue(zoneId, out state);
		}

		public bool MarkHarvested(string zoneId, string nodeInstanceId)
		{
			if (TryGet(zoneId, out var state))
			{
				return state.MarkHarvested(nodeInstanceId);
			}
			return false;
		}

		public bool InvalidateZone(string zoneId, int generatorVersion)
		{
			if (!AllowsRegeneration(zoneId) || !TryGet(zoneId, out var state))
			{
				return false;
			}
			state.Regenerate(NextSeed(), generatorVersion);
			return true;
		}

		public int InvalidateAllForHomecoming(int generatorVersion)
		{
			int num = 0;
			string[] array = new string[_states.Count];
			_states.Keys.CopyTo(array, 0);
			string[] array2 = array;
			foreach (string zoneId in array2)
			{
				if (InvalidateZone(zoneId, generatorVersion))
				{
					num++;
				}
			}
			return num;
		}

		public ZoneState[] Export()
		{
			ZoneState[] array = new ZoneState[_states.Count];
			_states.Values.CopyTo(array, 0);
			Array.Sort(array, (ZoneState first, ZoneState second) => string.CompareOrdinal(first.ZoneId, second.ZoneId));
			return array;
		}

		public void Restore(IEnumerable<ZoneState> states)
		{
			_states.Clear();
			if (states == null)
			{
				return;
			}
			foreach (ZoneState state in states)
			{
				if (state != null)
				{
					_states[state.ZoneId] = state;
				}
			}
		}

		private static bool AllowsRegeneration(string zoneId)
		{
			if (!string.IsNullOrWhiteSpace(zoneId))
			{
				return !string.Equals(zoneId, "home_base", StringComparison.Ordinal);
			}
			return false;
		}

		private static Random CreateSource(int? masterSeed)
		{
			return masterSeed.HasValue ? new Random(masterSeed.Value) : new Random();
		}

		private int NextSeed()
		{
			return _seedSource.Next(int.MinValue, int.MaxValue);
		}
	}
}
