using Eidren.UI;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/// <summary>
	/// Verlies-Minimap: Die Wandlinien entstehen aus den Aussenkanten der
	/// NavMesh-Triangulation — Kanten, die nur zu genau einem Dreieck
	/// gehoeren, trennen begehbar von unbegehbar und sind damit die Kontur
	/// von Waenden, Saeulen und Abgruenden. Die Extraktion ist reine
	/// Geometrie und wird hier ohne NavMesh geprueft.
	/// </summary>
	public sealed class MinimapWallOutlineTests
	{
		[Test]
		public void Quadrat_LiefertVierAussenkanten_OhneDiagonale()
		{
			Vector3[] ecken =
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(4f, 0f, 0f),
				new Vector3(4f, 0f, 4f),
				new Vector3(0f, 0f, 4f)
			};
			int[] dreiecke = { 0, 1, 2, 0, 2, 3 };
			List<MinimapWallSegment> kanten = MinimapWallOutline.ExtractBoundaryEdges(ecken, dreiecke);
			Assert.That(kanten.Count, Is.EqualTo(4), "Ein Quadrat hat vier Aussenkanten; die Diagonale ist innen.");
			foreach (MinimapWallSegment kante in kanten)
			{
				float laenge = Vector3.Distance(kante.From, kante.To);
				Assert.That(laenge, Is.EqualTo(4f).Within(0.001f),
					"Jede Aussenkante des Quadrats ist 4 lang — die Diagonale (5,66) darf nicht auftauchen.");
			}
		}

		[Test]
		public void DuplikatEcken_WerdenVerschweisst_KanteGiltAlsInnen()
		{
			// NavMesh-Triangulationen liefern die gemeinsame Kante zweier
			// Dreiecke oft ueber positionsgleiche, aber doppelte Vertices.
			Vector3[] ecken =
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(4f, 0f, 0f),
				new Vector3(4f, 0f, 4f),
				// Dreieck 2 mit eigenen, positionsgleichen Ecken:
				new Vector3(0f, 0f, 0.0004f),
				new Vector3(4f, 0f, 4.0004f),
				new Vector3(0f, 0f, 4f)
			};
			int[] dreiecke = { 0, 1, 2, 3, 4, 5 };
			List<MinimapWallSegment> kanten = MinimapWallOutline.ExtractBoundaryEdges(ecken, dreiecke);
			Assert.That(kanten.Count, Is.EqualTo(4),
				"Die ueber Duplikate geteilte Diagonale ist innen — ohne Verschweissen ergaeben sich 6 Kanten.");
		}

		[Test]
		public void WinzKanten_FliegenHeraus()
		{
			// Ein degeneriertes Splitterdreieck neben einem echten: Die
			// Splitterkanten unterhalb der Mindestlaenge verrauschen die
			// Karte nur und werden verworfen.
			Vector3[] ecken =
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(4f, 0f, 0f),
				new Vector3(0f, 0f, 4f),
				new Vector3(10f, 0f, 10f),
				new Vector3(10.02f, 0f, 10f),
				new Vector3(10f, 0f, 10.02f)
			};
			int[] dreiecke = { 0, 1, 2, 3, 4, 5 };
			List<MinimapWallSegment> kanten = MinimapWallOutline.ExtractBoundaryEdges(ecken, dreiecke);
			Assert.That(kanten.Count, Is.EqualTo(3), "Nur das echte Dreieck liefert Konturkanten.");
		}
	}
}
