using System.Globalization;

namespace Eidren.Core.Services
{
	public readonly struct EidraInstanceState
	{
		public string InstanceId { get; }

		public string EidraId { get; }

		public bool IsValid => !string.IsNullOrWhiteSpace(InstanceId) && !string.IsNullOrWhiteSpace(EidraId);

		public EidraInstanceState(string instanceId, string eidraId)
		{
			InstanceId = instanceId ?? string.Empty;
			EidraId = eidraId ?? string.Empty;
		}

		public static string BuildInstanceId(string eidraId, int ordinal)
		{
			return (eidraId ?? string.Empty) + "." + ordinal.ToString("000", CultureInfo.InvariantCulture);
		}

		public static bool TryParseOrdinal(string instanceId, out int ordinal)
		{
			ordinal = 0;
			if (string.IsNullOrWhiteSpace(instanceId))
			{
				return false;
			}
			int num = instanceId.LastIndexOf('.');
			if (num < 0 || num == instanceId.Length - 1)
			{
				return false;
			}
			int num2 = num + 1;
			return int.TryParse(instanceId.Substring(num2, instanceId.Length - num2), NumberStyles.None, CultureInfo.InvariantCulture, out ordinal);
		}

		public override string ToString()
		{
			return EidraId + " (" + InstanceId + ")";
		}
	}
}
