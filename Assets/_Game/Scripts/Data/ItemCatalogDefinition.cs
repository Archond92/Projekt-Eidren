using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Items/Item Catalog")]
	public sealed class ItemCatalogDefinition : ScriptableObject
	{
		[SerializeField]
		private ItemDefinition[] items = Array.Empty<ItemDefinition>();

		public IReadOnlyList<ItemDefinition> Items => items;

		public ItemDefinition[] ItemArray => items ?? Array.Empty<ItemDefinition>();
	}
}
