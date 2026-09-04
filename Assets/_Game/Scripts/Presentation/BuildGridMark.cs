using System;
using UnityEngine;

namespace Eidren.Presentation
{
	[DisallowMultipleComponent]
	public sealed class BuildGridMark : MonoBehaviour
	{
		[SerializeField]
		private MeshRenderer[] pieces;

		public MeshRenderer[] Pieces => pieces ?? Array.Empty<MeshRenderer>();

		public void ConfigureReferences(MeshRenderer[] configuredPieces)
		{
			pieces = configuredPieces;
		}

		public void SetMaterial(Material material)
		{
			if (material == null)
			{
				throw new ArgumentNullException("material", "Rastermarke '" + base.name + "' braucht ein Material.");
			}
			if (pieces == null || pieces.Length == 0)
			{
				throw new InvalidOperationException("Rastermarke '" + base.name + "' hat keine autorierten Teile.");
			}
			MeshRenderer[] array = pieces;
			foreach (MeshRenderer meshRenderer in array)
			{
				if (meshRenderer != null)
				{
					meshRenderer.sharedMaterial = material;
				}
			}
		}
	}
}
