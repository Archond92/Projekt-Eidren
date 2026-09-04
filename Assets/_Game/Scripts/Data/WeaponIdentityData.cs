using System;
using UnityEngine;

namespace Eidren.Data
{
	[Serializable]
	public struct WeaponIdentityData
	{
		public WeaponFamily Family;

		public WeaponSignature Signature;

		[Min(0f)]
		public int Tier;

		public bool IsNamedVariant;

		[TextArea(1, 3)]
		public string SignatureHint;
	}
}
