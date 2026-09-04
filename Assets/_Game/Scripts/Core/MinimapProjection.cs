using UnityEngine;

namespace Eidren.Core.Services
{
	/// <summary>
	/// Rechnet Weltpositionen in Kartenpunkte der Minimap um (N04-001).
	/// Reine Rechnung ohne Szenenzugriff: kein Raycast, keine Kamera, keine
	/// Allokation — die Karte aktualisiert getaktet und darf §8 nicht verletzen.
	/// </summary>
	public static class MinimapProjection
	{
		/// <summary>
		/// Projiziert <paramref name="worldPosition" /> auf die Kartenfläche um
		/// <paramref name="centerPosition" />. Liefert false, wenn das Ziel
		/// ausserhalb von <paramref name="worldRadius" /> liegt und deshalb nicht
		/// gezeichnet wird.
		/// </summary>
		/// <param name="yawDegrees">
		/// Drehung der Karte. Mit der Kameradrehung 45° (IsometricCamera) zeigt
		/// "oben auf der Karte" in dieselbe Richtung wie "oben auf dem Bildschirm".
		/// </param>
		public static bool TryProject(Vector3 worldPosition, Vector3 centerPosition, float worldRadius, float mapRadius, float yawDegrees, out Vector2 mapPosition)
		{
			mapPosition = Vector2.zero;
			if (worldRadius <= 0f || mapRadius <= 0f)
			{
				return false;
			}
			// Draufsicht: die Höhe zählt nicht, sonst wandert ein Ziel auf einem
			// Felsen seitlich aus.
			float east = worldPosition.x - centerPosition.x;
			float north = worldPosition.z - centerPosition.z;
			if (east * east + north * north > worldRadius * worldRadius)
			{
				return false;
			}
			mapPosition = Project(worldPosition, centerPosition, worldRadius, mapRadius, yawDegrees);
			return true;
		}

		/// <summary>
		/// Wie <see cref="TryProject" />, aber ohne Radiusprüfung. Gebraucht für
		/// den Kartenrand: Dessen Eckpunkte liegen fast immer ausserhalb des
		/// Ausschnitts, die Linie dazwischen kreuzt ihn trotzdem.
		/// </summary>
		public static Vector2 Project(Vector3 worldPosition, Vector3 centerPosition, float worldRadius, float mapRadius, float yawDegrees)
		{
			if (worldRadius <= 0f || mapRadius <= 0f)
			{
				return Vector2.zero;
			}
			float east = worldPosition.x - centerPosition.x;
			float north = worldPosition.z - centerPosition.z;
			float radians = yawDegrees * Mathf.Deg2Rad;
			float cos = Mathf.Cos(radians);
			float sin = Mathf.Sin(radians);
			float scale = mapRadius / worldRadius;
			return new Vector2((east * cos - north * sin) * scale, (east * sin + north * cos) * scale);
		}

		/// <summary>
		/// Kürzester Abstand eines Punktes zu einer Strecke, in der Draufsicht.
		/// Damit entscheidet die Karte, ob ein Randstück überhaupt im Ausschnitt
		/// liegt — die Entfernung zum Eckpunkt allein genügt dafür nicht.
		/// </summary>
		public static float DistanceToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
		{
			Vector2 p = new Vector2(point.x, point.z);
			Vector2 a = new Vector2(segmentStart.x, segmentStart.z);
			Vector2 b = new Vector2(segmentEnd.x, segmentEnd.z);
			Vector2 direction = b - a;
			float lengthSquared = direction.sqrMagnitude;
			if (lengthSquared < 0.0001f)
			{
				return Vector2.Distance(p, a);
			}
			float t = Mathf.Clamp01(Vector2.Dot(p - a, direction) / lengthSquared);
			return Vector2.Distance(p, a + direction * t);
		}
	}
}
