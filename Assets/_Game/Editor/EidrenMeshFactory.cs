using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Gemeinsame Mesh-Fabrik fuer prozedurale Weltobjekte im Vertexfarben-Stil.
	/// Jede Funktion wendet automatisch die drei Stilregeln an: Ton-Variation
	/// pro Flaeche (deterministisch aus der Flaechenmitte gehasht), Kontakt-
	/// Abdunklung zum Boden und Flat Shading ueber getrennte Ecken pro Flaeche.
	/// </summary>
	public static class EidrenMeshFactory
	{
		public const float ToneVariation = 0.06f;

		public const float ContactShadeBand = 0.15f;

		public const float ContactShadeFloor = 0.78f;

		/// <summary>Kasten mit skaliertem Kopf; Basisflaeche liegt auf y=0, zentriert um x=z=0.
		/// kontaktAo: false ueberspringt die Bodenabdunklung (fuer erhoehte Teile wie Baumkronen,
		/// G-005 - Kronen duerfen nicht wie Bodenkontakt behandelt werden).</summary>
		public static Mesh TaperedBox(Vector3 size, float topScale, Color color, bool kontaktAo = true)
		{
			Builder builder = new Builder(kontaktAo);
			float bx = size.x * 0.5f;
			float bz = size.z * 0.5f;
			float tx = Mathf.Max(0.01f, bx * topScale);
			float tz = Mathf.Max(0.01f, bz * topScale);
			Vector3[] p =
			{
				new Vector3(-bx, 0f, -bz), new Vector3(bx, 0f, -bz), new Vector3(bx, 0f, bz), new Vector3(-bx, 0f, bz),
				new Vector3(-tx, size.y, -tz), new Vector3(tx, size.y, -tz), new Vector3(tx, size.y, tz), new Vector3(-tx, size.y, tz)
			};
			builder.Quad(p[3], p[2], p[6], p[7], color); // vorn (+z)
			builder.Quad(p[1], p[0], p[4], p[5], color); // hinten
			builder.Quad(p[0], p[3], p[7], p[4], color); // links
			builder.Quad(p[2], p[1], p[5], p[6], color); // rechts
			builder.Quad(p[7], p[6], p[5], p[4], color); // oben
			builder.Quad(p[0], p[1], p[2], p[3], color); // unten
			return builder.Finish("TaperedBox");
		}

		/// <summary>Keil: rechteckige Basis auf y=0, Firstkante hinten (−z) auf Hoehe size.y.
		/// kontaktAo: false ueberspringt die Bodenabdunklung (siehe TaperedBox).</summary>
		public static Mesh Wedge(Vector3 size, Color color, bool kontaktAo = true)
		{
			Builder builder = new Builder(kontaktAo);
			float bx = size.x * 0.5f;
			float bz = size.z * 0.5f;
			Vector3[] p =
			{
				new Vector3(-bx, 0f, -bz), new Vector3(bx, 0f, -bz), new Vector3(bx, 0f, bz), new Vector3(-bx, 0f, bz),
				new Vector3(-bx, size.y, -bz), new Vector3(bx, size.y, -bz)
			};
			builder.Quad(p[3], p[2], p[5], p[4], color);          // Schraege (+z nach oben-hinten)
			builder.Quad(p[1], p[0], p[4], p[5], color);          // Rueckwand
			builder.Quad(p[0], p[1], p[2], p[3], color);          // unten
			builder.Tri(p[0], p[3], p[4], color);                  // Seite links
			builder.Tri(p[2], p[1], p[5], color);                  // Seite rechts
			return builder.Finish("Wedge");
		}

		/// <summary>Ringquerschnitt eines Lofts.</summary>
		public enum LoftShape
		{
			Rect,
			Oct
		}

		/// <summary>Ein Profilring des Lofts: Lage, Ausdehnung und seitlicher Versatz.</summary>
		public readonly struct LoftProfile
		{
			public LoftProfile(float height, float width, float depth, float offsetX = 0f, float offsetZ = 0f)
			{
				Height = height;
				Width = width;
				Depth = depth;
				OffsetX = offsetX;
				OffsetZ = offsetZ;
			}

			public float Height { get; }

			public float Width { get; }

			public float Depth { get; }

			public float OffsetX { get; }

			public float OffsetZ { get; }
		}

		/// <summary>Profilstapel: verbindet die Ringe mit Flat-Shading-Flaechen; optional Deckel.
		/// kontaktAo: false ueberspringt die Bodenabdunklung (siehe TaperedBox).</summary>
		public static Mesh Loft(LoftProfile[] profiles, LoftShape shape, Color color, bool capBottom, bool capTop, bool kontaktAo = true)
		{
			Builder builder = new Builder(kontaktAo);
			int seiten = ((shape == LoftShape.Oct) ? 8 : 4);
			Vector3[][] ringe = new Vector3[profiles.Length][];
			for (int ring = 0; ring < profiles.Length; ring++)
			{
				ringe[ring] = RingPoints(profiles[ring], shape, seiten);
			}
			for (int ring = 0; ring < profiles.Length - 1; ring++)
			{
				for (int seite = 0; seite < seiten; seite++)
				{
					int naechste = (seite + 1) % seiten;
					// Eckreihenfolge im Uhrzeigersinn von aussen gesehen (Unity-Wicklung):
					// naechste->seite unten, dann seite->naechste oben - sonst zeigen die
					// Seitenflaechen einwaerts und werden von aussen gecullt.
					builder.Quad(ringe[ring][naechste], ringe[ring][seite], ringe[ring + 1][seite], ringe[ring + 1][naechste], color);
				}
			}
			if (capBottom)
			{
				for (int seite = 1; seite < seiten - 1; seite++)
				{
					builder.Tri(ringe[0][0], ringe[0][seite], ringe[0][seite + 1], color);
				}
			}
			if (capTop)
			{
				Vector3[] deckel = ringe[profiles.Length - 1];
				for (int seite = 1; seite < seiten - 1; seite++)
				{
					builder.Tri(deckel[0], deckel[seite + 1], deckel[seite], color);
				}
			}
			return builder.Finish("Loft");
		}

		private static Vector3[] RingPoints(LoftProfile profil, LoftShape shape, int seiten)
		{
			Vector3[] punkte = new Vector3[seiten];
			float halbBreite = profil.Width * 0.5f;
			float halbTiefe = profil.Depth * 0.5f;
			if (shape == LoftShape.Rect)
			{
				punkte[0] = new Vector3(-halbBreite, profil.Height, -halbTiefe);
				punkte[1] = new Vector3(halbBreite, profil.Height, -halbTiefe);
				punkte[2] = new Vector3(halbBreite, profil.Height, halbTiefe);
				punkte[3] = new Vector3(-halbBreite, profil.Height, halbTiefe);
			}
			else
			{
				// Oktogon: Eckpunkte auf der Ellipse, beginnend bei 22,5 Grad fuer stehende Kanten.
				for (int index = 0; index < seiten; index++)
				{
					float winkel = ((float)index + 0.5f) / seiten * Mathf.PI * 2f;
					punkte[index] = new Vector3(Mathf.Cos(winkel) * halbBreite, profil.Height, Mathf.Sin(winkel) * halbTiefe);
				}
			}
			for (int index = 0; index < seiten; index++)
			{
				punkte[index] += new Vector3(profil.OffsetX, 0f, profil.OffsetZ);
			}
			return punkte;
		}

		/// <summary>Sammelt Flaechen mit getrennten Ecken (Flat Shading) und Vertexfarben.</summary>
		private sealed class Builder
		{
			private readonly List<Vector3> _punkte = new List<Vector3>();

			private readonly List<Color> _farben = new List<Color>();

			private readonly List<int> _dreiecke = new List<int>();

			private readonly bool _kontaktAo;

			public Builder(bool kontaktAo = true)
			{
				_kontaktAo = kontaktAo;
			}

			public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color farbe)
			{
				Color getoent = farbe * FaceTint((a + b + c + d) * 0.25f);
				getoent.a = 1f;
				int start = _punkte.Count;
				_punkte.AddRange(new[] { a, b, c, d });
				for (int index = 0; index < 4; index++)
				{
					_farben.Add(getoent);
				}
				_dreiecke.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
			}

			public void Tri(Vector3 a, Vector3 b, Vector3 c, Color farbe)
			{
				Color getoent = farbe * FaceTint((a + b + c) / 3f);
				getoent.a = 1f;
				int start = _punkte.Count;
				_punkte.AddRange(new[] { a, b, c });
				for (int index = 0; index < 3; index++)
				{
					_farben.Add(getoent);
				}
				_dreiecke.AddRange(new[] { start, start + 1, start + 2 });
			}

			public Mesh Finish(string name)
			{
				if (_kontaktAo)
				{
					ApplyContactShade();
				}
				Mesh mesh = new Mesh { name = name };
				mesh.SetVertices(_punkte);
				mesh.SetColors(_farben);
				mesh.SetTriangles(_dreiecke, 0);
				mesh.RecalculateNormals();
				mesh.RecalculateBounds();
				return mesh;
			}

			/// <summary>Dunkelt Ecken im unteren Hoehenband zum Boden hin ab (Kontakt-AO aus G-003).</summary>
			private void ApplyContactShade()
			{
				float min = float.MaxValue;
				float max = float.MinValue;
				foreach (Vector3 punkt in _punkte)
				{
					min = Mathf.Min(min, punkt.y);
					max = Mathf.Max(max, punkt.y);
				}
				float band = Mathf.Max(0.0001f, (max - min) * ContactShadeBand);
				for (int index = 0; index < _punkte.Count; index++)
				{
					float anteil = Mathf.Clamp01((_punkte[index].y - min) / band);
					float faktor = Mathf.Lerp(ContactShadeFloor, 1f, anteil);
					Color farbe = _farben[index] * faktor;
					farbe.a = 1f;
					_farben[index] = farbe;
				}
			}

			/// <summary>Deterministische Ton-Variation aus der Flaechenmitte (±ToneVariation).</summary>
			private static float FaceTint(Vector3 mitte)
			{
				float hash = Mathf.Sin(Vector3.Dot(mitte, new Vector3(127.1f, 311.7f, 74.7f))) * 43758.5453f;
				hash -= Mathf.Floor(hash);
				return 1f - ToneVariation + 2f * ToneVariation * hash;
			}
		}
	}
}
