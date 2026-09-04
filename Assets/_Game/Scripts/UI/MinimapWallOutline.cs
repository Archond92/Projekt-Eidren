using System.Collections.Generic;
using UnityEngine;

namespace Eidren.UI
{
	/// <summary>Ein Wandliniensegment in Weltkoordinaten.</summary>
	public readonly struct MinimapWallSegment
	{
		public readonly Vector3 From;

		public readonly Vector3 To;

		public MinimapWallSegment(Vector3 from, Vector3 to)
		{
			From = from;
			To = to;
		}
	}

	/// <summary>
	/// Gewinnt die Wandkontur eines Verlieses aus seiner NavMesh-
	/// Triangulation: Kanten, die nur zu genau einem Dreieck gehoeren,
	/// trennen begehbar von unbegehbar — das sind Waende, Saeulen und
	/// Abgruende. Duplikat-Vertices (typisch fuer NavMesh-Daten) werden
	/// ueber ein Toleranzgitter verschweisst, Splitterkanten verworfen.
	/// </summary>
	public static class MinimapWallOutline
	{
		private const float WeldTolerance = 0.1f;

		private const float MinEdgeLength = 0.25f;

		public static List<MinimapWallSegment> ExtractBoundaryEdges(Vector3[] vertices, int[] indices)
		{
			var segments = new List<MinimapWallSegment>();
			if (vertices == null || indices == null || indices.Length < 3)
			{
				return segments;
			}
			// Vertex-Verschweissen: positionsgleiche Ecken bekommen dieselbe Id.
			var cellToId = new Dictionary<(int, int, int), int>();
			var weldedIds = new int[vertices.Length];
			var weldedPositions = new List<Vector3>();
			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 position = vertices[i];
				(int, int, int) cell = (
					Mathf.RoundToInt(position.x / WeldTolerance),
					Mathf.RoundToInt(position.y / WeldTolerance),
					Mathf.RoundToInt(position.z / WeldTolerance));
				if (!cellToId.TryGetValue(cell, out int id))
				{
					id = weldedPositions.Count;
					weldedPositions.Add(position);
					cellToId.Add(cell, id);
				}
				weldedIds[i] = id;
			}
			// Kanten zaehlen: eine Aussenkante gehoert zu genau einem Dreieck.
			var edgeCounts = new Dictionary<(int, int), int>();
			for (int i = 0; i + 2 < indices.Length; i += 3)
			{
				CountEdge(edgeCounts, weldedIds[indices[i]], weldedIds[indices[i + 1]]);
				CountEdge(edgeCounts, weldedIds[indices[i + 1]], weldedIds[indices[i + 2]]);
				CountEdge(edgeCounts, weldedIds[indices[i + 2]], weldedIds[indices[i]]);
			}
			foreach (KeyValuePair<(int, int), int> edge in edgeCounts)
			{
				if (edge.Value != 1)
				{
					continue;
				}
				Vector3 from = weldedPositions[edge.Key.Item1];
				Vector3 to = weldedPositions[edge.Key.Item2];
				if (Vector3.Distance(from, to) >= MinEdgeLength)
				{
					segments.Add(new MinimapWallSegment(from, to));
				}
			}
			return segments;
		}

		private static void CountEdge(Dictionary<(int, int), int> edgeCounts, int first, int second)
		{
			// Degenerierte Kanten (verschweisste Splitter) zaehlen nicht.
			if (first == second)
			{
				return;
			}
			(int, int) key = (first < second) ? (first, second) : (second, first);
			edgeCounts.TryGetValue(key, out int count);
			edgeCounts[key] = count + 1;
		}
	}
}
