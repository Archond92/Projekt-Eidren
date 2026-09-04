using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/Resources/Resource Node Catalog")]
	public sealed class ResourceNodeCatalogDefinition : ScriptableObject
	{
		[SerializeField]
		private ResourceNodeDefinition[] definitions = Array.Empty<ResourceNodeDefinition>();

		public ResourceNodeDefinition[] Definitions => definitions ?? Array.Empty<ResourceNodeDefinition>();
	}
}
