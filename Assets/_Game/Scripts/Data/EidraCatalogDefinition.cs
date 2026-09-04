using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Eidra Catalog")]
	public sealed class EidraCatalogDefinition : ScriptableObject
	{
		[SerializeField]
		private EidraData[] eidren = Array.Empty<EidraData>();

		public IReadOnlyList<EidraData> Eidren => eidren;

		public EidraData[] EidraArray => eidren ?? Array.Empty<EidraData>();
	}

	public static class EidraIds
	{
		public const string Terrock = "terrock";

		public const string Noctarion = "noctarion";

		public const string Ignivar = "ignivar";
	}
}
