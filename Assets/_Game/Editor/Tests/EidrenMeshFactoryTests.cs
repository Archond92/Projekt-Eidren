using Eidren.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class EidrenMeshFactoryTests
	{
		[Test]
		public void TaperedBox_HatVertexfarbenUndZwoelfDreiecke()
		{
			Mesh mesh = EidrenMeshFactory.TaperedBox(new Vector3(1f, 0.5f, 0.6f), 0.8f, new Color(0.4f, 0.3f, 0.2f));
			Assert.That(mesh.triangles.Length, Is.EqualTo(36), "12 Dreiecke erwartet");
			Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount), "jede Ecke braucht eine Vertexfarbe");
			Assert.That(mesh.colors.Length, Is.GreaterThan(0));
		}

		[Test]
		public void Wedge_HatVertexfarbenUndAchtDreiecke()
		{
			Mesh mesh = EidrenMeshFactory.Wedge(new Vector3(1f, 0.4f, 0.6f), new Color(0.4f, 0.3f, 0.2f));
			Assert.That(mesh.triangles.Length, Is.EqualTo(24), "8 Dreiecke erwartet");
			Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount));
		}

		[Test]
		public void Fabrik_IstDeterministisch()
		{
			Mesh erster = EidrenMeshFactory.TaperedBox(new Vector3(1f, 0.5f, 0.6f), 0.8f, new Color(0.4f, 0.3f, 0.2f));
			Mesh zweiter = EidrenMeshFactory.TaperedBox(new Vector3(1f, 0.5f, 0.6f), 0.8f, new Color(0.4f, 0.3f, 0.2f));
			CollectionAssert.AreEqual(erster.vertices, zweiter.vertices);
			CollectionAssert.AreEqual(erster.colors, zweiter.colors);
		}

		[Test]
		public void TonVariation_BleibtImKorridor()
		{
			Color basis = new Color(0.5f, 0.4f, 0.3f);
			Mesh mesh = EidrenMeshFactory.TaperedBox(new Vector3(1f, 1f, 1f), 1f, basis);
			foreach (Color farbe in mesh.colors)
			{
				// Obergrenze: Basis plus Variation; Untergrenze: Basis minus Variation mal AO-Boden.
				Assert.That(farbe.r, Is.LessThanOrEqualTo(basis.r * (1f + EidrenMeshFactory.ToneVariation) + 0.001f));
				Assert.That(farbe.r, Is.GreaterThanOrEqualTo(basis.r * (1f - EidrenMeshFactory.ToneVariation) * EidrenMeshFactory.ContactShadeFloor - 0.001f));
			}
		}

		[Test]
		public void KontaktAo_DunkeltUntereFlaechenAb()
		{
			Mesh mesh = EidrenMeshFactory.TaperedBox(new Vector3(1f, 1f, 1f), 1f, new Color(0.5f, 0.5f, 0.5f));
			float unten = 0f, oben = 0f;
			int untenAnzahl = 0, obenAnzahl = 0;
			Vector3[] punkte = mesh.vertices;
			Color[] farben = mesh.colors;
			for (int index = 0; index < punkte.Length; index++)
			{
				if (punkte[index].y < 0.01f) { unten += farben[index].r; untenAnzahl++; }
				if (punkte[index].y > 0.99f) { oben += farben[index].r; obenAnzahl++; }
			}
			Assert.That(untenAnzahl, Is.GreaterThan(0));
			Assert.That(obenAnzahl, Is.GreaterThan(0));
			Assert.That(unten / untenAnzahl, Is.LessThan(oben / obenAnzahl), "Bodennahe Ecken muessen dunkler sein");
		}

		[Test]
		public void TaperedBox_OhneKontaktAo_HatKeineBodenAbdunklung()
		{
			// G-005: erhoehte Teile (z.B. Baumkronen) duerfen keine Bodenkontakt-Abdunklung zeigen.
			// Ohne AO liegt der Unterschied zwischen unterer und oberer mittlerer Helligkeit nur noch
			// innerhalb der Ton-Variation (±ToneVariation), nicht mehr im AO-Band (ContactShadeFloor).
			Mesh mesh = EidrenMeshFactory.TaperedBox(new Vector3(1f, 1f, 1f), 1f, new Color(0.5f, 0.5f, 0.5f), kontaktAo: false);
			float unten = 0f, oben = 0f;
			int untenAnzahl = 0, obenAnzahl = 0;
			Vector3[] punkte = mesh.vertices;
			Color[] farben = mesh.colors;
			for (int index = 0; index < punkte.Length; index++)
			{
				if (punkte[index].y < 0.01f) { unten += farben[index].r; untenAnzahl++; }
				if (punkte[index].y > 0.99f) { oben += farben[index].r; obenAnzahl++; }
			}
			Assert.That(untenAnzahl, Is.GreaterThan(0));
			Assert.That(obenAnzahl, Is.GreaterThan(0));
			float untenMittel = unten / untenAnzahl;
			float obenMittel = oben / obenAnzahl;
			Assert.That(untenMittel, Is.EqualTo(obenMittel).Within(2f * EidrenMeshFactory.ToneVariation + 0.001f), "ohne kontaktAo darf der Boden nicht dunkler sein als die Ton-Variation erlaubt");
		}

		[Test]
		public void Loft_Rechteck_DreiecksZahlStimmt()
		{
			EidrenMeshFactory.LoftProfile[] profil =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1f, 0.6f),
				new EidrenMeshFactory.LoftProfile(0.2f, 0.9f, 0.55f),
				new EidrenMeshFactory.LoftProfile(0.35f, 0.6f, 0.4f)
			};
			Mesh mesh = EidrenMeshFactory.Loft(profil, EidrenMeshFactory.LoftShape.Rect, new Color(0.4f, 0.3f, 0.2f), capBottom: true, capTop: true);
			// Seiten: 4 Flaechen x 2 Ringe x 2 Dreiecke = 16; Deckel: je 2 => 20 Dreiecke.
			Assert.That(mesh.triangles.Length, Is.EqualTo(60));
			Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount));
		}

		[Test]
		public void Loft_Oktogon_DreiecksZahlStimmt()
		{
			EidrenMeshFactory.LoftProfile[] profil =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1f, 1f),
				new EidrenMeshFactory.LoftProfile(0.3f, 0.8f, 0.8f)
			};
			Mesh mesh = EidrenMeshFactory.Loft(profil, EidrenMeshFactory.LoftShape.Oct, new Color(0.4f, 0.3f, 0.2f), capBottom: true, capTop: true);
			// Seiten: 8 x 1 x 2 = 16; Deckel: 8-Eck-Faecher = je 6 => 28 Dreiecke.
			Assert.That(mesh.triangles.Length, Is.EqualTo(84));
			Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount));
		}

		[Test]
		public void TaperedBox_FlaechenZeigenNachAussen()
		{
			AssertAuswaertsGewickelt(EidrenMeshFactory.TaperedBox(new Vector3(1f, 1f, 0.8f), 0.7f, Color.gray), "TaperedBox");
		}

		[Test]
		public void Wedge_FlaechenZeigenNachAussen()
		{
			AssertAuswaertsGewickelt(EidrenMeshFactory.Wedge(new Vector3(1f, 0.5f, 0.8f), Color.gray), "Wedge");
		}

		[Test]
		public void Loft_SeitenflaechenZeigenNachAussen()
		{
			/* Befund 14.08.2026: Loft-Seitenflaechen waren einwaerts gewickelt - Unity cullt
			   sie dann von aussen, Baeume/Steine/Bueschel wirkten durchsichtig (der Quell-
			   Viewer zeichnete Backfaces mit und verbarg das). Deckel waren korrekt. */
			EidrenMeshFactory.LoftProfile[] profil =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1f, 0.8f),
				new EidrenMeshFactory.LoftProfile(0.6f, 0.9f, 0.7f),
				new EidrenMeshFactory.LoftProfile(1f, 0.5f, 0.4f)
			};
			AssertAuswaertsGewickelt(EidrenMeshFactory.Loft(profil, EidrenMeshFactory.LoftShape.Oct, Color.gray, capBottom: true, capTop: true), "Okt-Loft");
			AssertAuswaertsGewickelt(EidrenMeshFactory.Loft(profil, EidrenMeshFactory.LoftShape.Rect, Color.gray, capBottom: true, capTop: true), "Rect-Loft");
		}

		/* Konvexe Fabrikkoerper: jede Dreiecksnormale (Unity-Wicklung, Cross(b-a, c-a))
		   muss vom Schwerpunkt weg zeigen, sonst zeigt die Rueckseite nach aussen. */
		private static void AssertAuswaertsGewickelt(Mesh mesh, string name)
		{
			Vector3[] punkte = mesh.vertices;
			int[] dreiecke = mesh.triangles;
			Vector3 mitte = Vector3.zero;
			foreach (Vector3 punkt in punkte)
			{
				mitte += punkt;
			}
			mitte /= punkte.Length;
			for (int index = 0; index < dreiecke.Length; index += 3)
			{
				Vector3 a = punkte[dreiecke[index]];
				Vector3 b = punkte[dreiecke[index + 1]];
				Vector3 c = punkte[dreiecke[index + 2]];
				Vector3 normale = Vector3.Cross(b - a, c - a);
				Vector3 flaechenMitte = (a + b + c) / 3f;
				Assert.That(Vector3.Dot(normale, flaechenMitte - mitte), Is.GreaterThan(0f),
					name + ": Dreieck ab Index " + index + " ist einwaerts gewickelt");
			}
		}

		[Test]
		public void Loft_MitVersatz_VerschiebtDenRing()
		{
			EidrenMeshFactory.LoftProfile[] profil =
			{
				new EidrenMeshFactory.LoftProfile(0f, 1f, 1f),
				new EidrenMeshFactory.LoftProfile(0.5f, 1f, 1f, 0.3f, 0.1f)
			};
			Mesh mesh = EidrenMeshFactory.Loft(profil, EidrenMeshFactory.LoftShape.Rect, Color.gray, capBottom: false, capTop: false);
			float maxX = float.MinValue;
			foreach (Vector3 punkt in mesh.vertices)
			{
				maxX = Mathf.Max(maxX, punkt.x);
			}
			Assert.That(maxX, Is.EqualTo(0.8f).Within(0.001f), "0.5 halbe Breite + 0.3 Versatz");
		}
	}
}
