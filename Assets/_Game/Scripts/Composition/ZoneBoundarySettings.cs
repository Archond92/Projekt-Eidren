using Eidren.Core.Services;
using Eidren.Player;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class ZoneBoundarySettings : MonoBehaviour
	{
		[Header("Player hard boundary")]
		[SerializeField]
		private MovementBoundaryShape boundaryShape = MovementBoundaryShape.None;

		[SerializeField]
		private Vector3 boundaryCenter;

		[SerializeField]
		private float circleRadius;

		[SerializeField]
		private Vector2 rectangleSize = new Vector2(40f, 40f);

		[SerializeField]
		private MapSideFlags openExitSides = MapSideFlags.All;

		[Header("Safe map area")]
		[SerializeField]
		private Vector3 safeAreaCenter;

		[SerializeField]
		private Vector2 safeAreaSize = new Vector2(40f, 40f);

		public MovementBoundaryShape BoundaryShape => boundaryShape;

		public MapSideFlags OpenExitSides => openExitSides;

		public Vector3 SafeAreaCenter => base.transform.TransformPoint(safeAreaCenter);

		public Vector2 SafeAreaSize => safeAreaSize;

		public void Configure(MovementBoundaryShape shape, Vector3 localBoundaryCenter, float radius, Vector2 boundaryRectangleSize, MapSideFlags openSides, Vector3 localSafeAreaCenter, Vector2 configuredSafeAreaSize)
		{
			boundaryShape = shape;
			boundaryCenter = localBoundaryCenter;
			circleRadius = Mathf.Max(0f, radius);
			rectangleSize = boundaryRectangleSize;
			openExitSides = openSides;
			safeAreaCenter = localSafeAreaCenter;
			safeAreaSize = configuredSafeAreaSize;
		}

		public PlayerMovementBoundary CreatePlayerBoundary()
		{
			Vector3 vector = base.transform.TransformPoint(boundaryCenter);
			MovementBoundaryShape movementBoundaryShape = boundaryShape;
			if (1 == 0)
			{
			}
			PlayerMovementBoundary result = movementBoundaryShape switch
			{
				MovementBoundaryShape.Circle => PlayerMovementBoundary.Circle(vector, circleRadius, openExitSides), 
				MovementBoundaryShape.Rectangle => PlayerMovementBoundary.Rectangle(vector, rectangleSize, openExitSides), 
				_ => PlayerMovementBoundary.Unbounded(openExitSides), 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		/// <summary>
		/// Das "Ende der Karte" fuer die Minimap (N04-001). Alle Zonenszenen sind
		/// mit Shape None gebaut — die Bewegungsgrenze ist dort Unbounded, das
		/// Zonenrechteck steckt aber in rectangleSize. Eine echte harte Grenze
		/// ist selbst der Kartenrand; ohne sie faellt die Karte auf das Rechteck
		/// zurueck. Reine Zeichengeometrie: offene Seiten spielen keine Rolle.
		/// </summary>
		public PlayerMovementBoundary CreateMapEdgeBoundary()
		{
			if (boundaryShape != MovementBoundaryShape.None)
			{
				return CreatePlayerBoundary();
			}
			if (rectangleSize.x <= 0f || rectangleSize.y <= 0f)
			{
				return PlayerMovementBoundary.Unbounded(openExitSides);
			}
			return PlayerMovementBoundary.Rectangle(base.transform.TransformPoint(boundaryCenter), rectangleSize);
		}

		public bool IsFullyInsideSafeArea(Vector3 worldPosition, float participantRadius)
		{
			Vector3 vector = SafeAreaCenter;
			Vector2 vector2 = safeAreaSize * 0.5f;
			float num = Mathf.Max(0f, participantRadius);
			return Mathf.Abs(worldPosition.x - vector.x) + num < vector2.x && Mathf.Abs(worldPosition.z - vector.z) + num < vector2.y;
		}
	}
}
