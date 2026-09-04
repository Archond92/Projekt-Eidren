using Eidren.Core.BuildGrid;
using System;
using UnityEngine;

namespace Eidren.Interaction
{
	[DisallowMultipleComponent]
	public sealed class WallConnectionView : MonoBehaviour
	{
		[SerializeField]
		private GameObject postMinusX;

		[SerializeField]
		private GameObject postPlusX;

		[SerializeField]
		private GameObject capMinusX;

		[SerializeField]
		private GameObject capPlusX;

		public void ConfigureReferences(GameObject postAtMinusX, GameObject postAtPlusX, GameObject capAtMinusX, GameObject capAtPlusX)
		{
			postMinusX = postAtMinusX;
			postPlusX = postAtPlusX;
			capMinusX = capAtMinusX;
			capPlusX = capAtPlusX;
		}

		public static bool LocalXPointsToUpperNode(int quarterTurns)
		{
			int num = (quarterTurns % 4 + 4) % 4;
			return num == 0 || num == 3;
		}

		public void Apply(GridWallConnection connection, int quarterTurns)
		{
			if (postMinusX == null || postPlusX == null || capMinusX == null || capPlusX == null)
			{
				throw new InvalidOperationException("Edge prefab '" + base.name + "' is missing its wall connection pieces.");
			}
			bool flag = LocalXPointsToUpperNode(quarterTurns);
			Show(postPlusX, capPlusX, flag ? connection.Upper : connection.Lower);
			Show(postMinusX, capMinusX, flag ? connection.Lower : connection.Upper);
		}

		private static void Show(GameObject post, GameObject cap, GridWallShape shape)
		{
			cap.SetActive(shape == GridWallShape.EndCap);
			post.SetActive(shape != GridWallShape.EndCap && shape != GridWallShape.Straight);
		}
	}
}
