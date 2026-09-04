using System.Collections.Generic;
using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/World Map/Node")]
	public sealed class WorldMapNodeDefinition : ScriptableObject
	{
		[SerializeField]
		private string id;

		[SerializeField]
		private string displayName;

		[TextArea(2, 5)]
		[SerializeField]
		private string description;

		[SerializeField]
		private string sceneKey;

		[SerializeField]
		private Vector2 mapPosition;

		[SerializeField]
		private Sprite icon;

		[SerializeField]
		private WorldRegionType regionType;

		[Range(1f, 5f)]
		[SerializeField]
		private int dangerLevel = 1;

		[SerializeField]
		private string recommendedProgress;

		[SerializeField]
		private bool initiallyAvailable = true;

		[SerializeField]
		private bool hasBoss;

		[SerializeField]
		private WorldMapResourcePreview[] resources = Array.Empty<WorldMapResourcePreview>();

		[SerializeField]
		private string[] possibleEnemies = Array.Empty<string>();

		[SerializeField]
		private string[] specialPlaces = Array.Empty<string>();

		[SerializeField]
		private string[] directNeighborIds = Array.Empty<string>();

		public string Id => id;

		public string DisplayName => displayName;

		public string Description => description;

		public string SceneKey => sceneKey;

		public Vector2 MapPosition => mapPosition;

		public Sprite Icon => icon;

		public WorldRegionType RegionType => regionType;

		public int DangerLevel => dangerLevel;

		public string RecommendedProgress => recommendedProgress;

		public bool InitiallyAvailable => initiallyAvailable;

		public bool HasBoss => hasBoss;

		public IReadOnlyList<WorldMapResourcePreview> Resources => resources;

		public IReadOnlyList<string> PossibleEnemies => possibleEnemies;

		public IReadOnlyList<string> SpecialPlaces => specialPlaces;

		public IReadOnlyList<string> DirectNeighborIds => directNeighborIds;

		public Sprite GetIconOrFallback(Sprite fallback)
		{
			return (icon != null) ? icon : fallback;
		}
	}
}
