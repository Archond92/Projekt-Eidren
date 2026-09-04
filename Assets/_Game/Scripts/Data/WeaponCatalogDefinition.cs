using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(fileName = "WeaponCatalog", menuName = "Eidren/Combat/Weapon Catalog")]
	public sealed class WeaponCatalogDefinition : ScriptableObject
	{
		[SerializeField]
		private WeaponData[] weapons = Array.Empty<WeaponData>();

		public IReadOnlyList<WeaponData> Weapons => weapons ?? Array.Empty<WeaponData>();

		public WeaponData[] WeaponArray => (weapons != null) ? ((WeaponData[])weapons.Clone()) : Array.Empty<WeaponData>();
	}
}
