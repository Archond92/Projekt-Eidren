using System;
using UnityEngine;

namespace Eidren.Data
{
	[Serializable]
	public struct WorldChestSpawnPointDefinition
	{
		[SerializeField]
		private string stableId;

		[SerializeField]
		private Vector3 position;

		[SerializeField]
		private float rotationY;

		[SerializeField]
		private WorldChestFamilyMask allowedFamilies;

		public string StableId => stableId ?? string.Empty;

		public Vector3 Position => position;

		public float RotationY => rotationY;

		public WorldChestFamilyMask AllowedFamilies => allowedFamilies;

		public bool Allows(WorldChestFamily family)
		{
			return ((uint)allowedFamilies & (uint)(family switch
			{
				WorldChestFamily.Common => 1, 
				WorldChestFamily.Guarded => 2, 
				WorldChestFamily.Hidden => 4, 
				_ => 0, 
			})) != 0;
		}
	}
}
