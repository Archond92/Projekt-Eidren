using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/World Map/Definition")]
	public sealed class WorldMapDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[SerializeField]
		private Sprite mapVisual;

		[SerializeField]
		private Sprite fallbackNodeIcon;

		[SerializeField]
		private WorldMapThemeData theme;

		[SerializeField]
		private WorldMapNodeDefinition[] nodes = Array.Empty<WorldMapNodeDefinition>();

		public string Id => id;

		public string DisplayName => displayName;

		public Sprite MapVisual => mapVisual;

		public Sprite FallbackNodeIcon => fallbackNodeIcon;

		public WorldMapThemeData Theme => theme;

		public IReadOnlyList<WorldMapNodeDefinition> Nodes => nodes;

		public bool TryGetNode(string nodeId, out WorldMapNodeDefinition node)
		{
			WorldMapNodeDefinition[] array = nodes;
			foreach (WorldMapNodeDefinition worldMapNodeDefinition in array)
			{
				if (worldMapNodeDefinition != null && string.Equals(worldMapNodeDefinition.Id, nodeId, StringComparison.Ordinal))
				{
					node = worldMapNodeDefinition;
					return true;
				}
			}
			node = null;
			return false;
		}

		public void ValidateOrThrow()
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new InvalidOperationException("WorldMapDefinition requires a stable map ID.");
			}
			if (mapVisual == null)
			{
				throw new InvalidOperationException("World map '" + id + "' has no map visual.");
			}
			if (theme == null)
			{
				throw new InvalidOperationException("World map '" + id + "' has no theme.");
			}
			if (nodes == null || nodes.Length != 8)
			{
				throw new InvalidOperationException("World map '" + id + "' must contain exactly eight nodes.");
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			WorldMapNodeDefinition[] array = nodes;
			foreach (WorldMapNodeDefinition worldMapNodeDefinition in array)
			{
				if (worldMapNodeDefinition == null)
				{
					throw new InvalidOperationException("World map '" + id + "' contains a null node.");
				}
				if (string.IsNullOrWhiteSpace(worldMapNodeDefinition.Id))
				{
					throw new InvalidOperationException("World map '" + id + "' contains a node without ID.");
				}
				if (!hashSet.Add(worldMapNodeDefinition.Id))
				{
					throw new InvalidOperationException("Duplicate world-map node ID '" + worldMapNodeDefinition.Id + "'.");
				}
				if (string.IsNullOrWhiteSpace(worldMapNodeDefinition.SceneKey))
				{
					throw new InvalidOperationException("World-map node '" + worldMapNodeDefinition.Id + "' has no scene key.");
				}
			}
		}
	}
}
