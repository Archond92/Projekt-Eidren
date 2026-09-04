using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Baut Boeden und Waende der Schmiede aus den Daten in EidraForgeLayout.
	/// Kennt selbst keine Koordinaten - alles kommt aus der Flaechentabelle.
	/// </summary>
	public static class EidraForgeGeometryBuilder
	{
		private const float Bodendicke = 0.4f;

		private const float Wanddicke = 0.5f;

		/* Steinalbedo 0,22 laut SCHMIEDE_ENTWURF.md Abschnitt 9. Der alte Wert 0,12
		   der Forge-Materialien war bei schwachem Licht nicht mehr lesbar - die
		   Glut-Probe zeigte eine vollstaendig schwarze Silhouette. */
		/* Zweite Korrektur, diesmal gemessen statt geschaetzt. ForgeLichtProbe zeigt,
		   dass ein Punktlicht die Helligkeit einer Flaeche mit Albedo 0,5 um das
		   Vierzehnfache hebt - der Zusatzlichtpfad im Weltshader arbeitet also.
		   Dunkel blieb das Verlies, weil Boden und Waende mit 0,17 bzw. 0,22 fast
		   nichts zurueckwerfen. Licht kann nicht sichtbar machen, was kein Licht
		   reflektiert. */
		private static readonly Color Bodenfarbe = new Color(0.3f, 0.27f, 0.28f);

		private static readonly Color WandfarbeUnten = new Color(0.34f, 0.3f, 0.31f);

		private static readonly Color WandfarbeOben = new Color(0.27f, 0.24f, 0.25f);

		/// <summary>
		/// Zerlegt eine Flaeche in Kacheln von hoechstens Kachelgroesse Kantenlaenge.
		/// Randkacheln werden gestutzt, damit die Summe exakt der Flaeche entspricht.
		/// Noetig, weil das Zusatzlicht-Limit von vier pro Objekt gilt - ein Raumboden
		/// als einzelner Quader bekaeme vier Lichter fuer den ganzen Raum, und die
		/// Auswahl faellt pro Objekt neu.
		/// </summary>
		public static IEnumerable<(float x, float z, float breite, float tiefe, float hoehe)> Kacheln(ForgeFlaeche flaeche)
		{
			/* Rampen werden in Einer-Stufen zerlegt statt in 4er-Kacheln: bei
			   10 Einheiten Lauf und 6 Einheiten Gefaelle betraegt der Absatz dann
			   0,6 und bleibt unter der Schritthoehe des NavMesh-Agenten. Mit
			   4er-Kacheln waeren es 2,4 Einheiten pro Stufe - unbegehbar. */
			float schritt = flaeche.IstRampe ? 1f : EidraForgeLayout.Kachelgroesse;
			for (float x = flaeche.MinX; x < flaeche.MaxX - 0.001f; x += schritt)
			{
				float breite = Mathf.Min(schritt, flaeche.MaxX - x);
				for (float z = flaeche.MinZ; z < flaeche.MaxZ - 0.001f; z += schritt)
				{
					float tiefe = Mathf.Min(schritt, flaeche.MaxZ - z);
					yield return (x + breite * 0.5f, z + tiefe * 0.5f, breite, tiefe,
						Stufenhoehe(flaeche, x + breite * 0.5f, z + tiefe * 0.5f));
				}
			}
		}

		/// <summary>Hoehe einer Rampenstufe: linear vom oberen Ende zur Grubentiefe.</summary>
		private static float Stufenhoehe(ForgeFlaeche flaeche, float x, float z)
		{
			if (!flaeche.IstRampe)
			{
				return flaeche.Hoehe;
			}
			float anteil;
			if (Mathf.Abs(flaeche.GefaelleX) > 0.01f)
			{
				anteil = Mathf.InverseLerp(flaeche.MinX, flaeche.MaxX, x);
				if (flaeche.GefaelleX < 0f)
				{
					anteil = 1f - anteil;
				}
			}
			else if (Mathf.Abs(flaeche.GefaelleZ) > 0.01f)
			{
				anteil = Mathf.InverseLerp(flaeche.MinZ, flaeche.MaxZ, z);
				if (flaeche.GefaelleZ < 0f)
				{
					anteil = 1f - anteil;
				}
			}
			else
			{
				return flaeche.Hoehe;
			}
			return Mathf.Lerp(0f, EidraForgeLayout.Grubentiefe, anteil);
		}

		/// <summary>Legt alle Bodenkacheln unter der Wurzel an.</summary>
		public static void BaueBoeden(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
			{
				if (flaeche.IstRampe)
				{
					BaueRampe(wurzel, flaeche, welt);
					continue;
				}
				foreach (var kachel in Kacheln(flaeche))
				{
					/* Rampen liegen innerhalb der Ringschenkel. Deren flache Kacheln
					   wuerden die Stufen zudecken, deshalb sparen sie die
					   Rampenflaeche aus - TryFinde bevorzugt Rampen. */
					if (!flaeche.IstRampe
						&& EidraForgeLayout.TryFinde(kachel.x, kachel.z, out ForgeFlaeche darunter)
						&& darunter.IstRampe)
					{
						continue;
					}
					/* TaperedBox waechst von der Basisflaeche nach oben, ist also nicht
					   zentriert: der Fuss muss um die Dicke tiefer sitzen, damit die
					   Oberkante auf der Sollhoehe liegt. */
					Teil(wurzel, $"Boden_{flaeche.Name}_{kachel.x:0.#}_{kachel.z:0.#}",
						new Vector3(kachel.breite, Bodendicke, kachel.tiefe),
						new Vector3(kachel.x, kachel.hoehe - Bodendicke, kachel.z), Bodenfarbe, welt);
				}
			}
		}

		/// <summary>
		/// Liefert alle Wandstuecke: der Rand jeder Flaeche wird abgeschritten, und
		/// ein Abschnitt wird nur dann zur Wand, wenn direkt dahinter kein Boden
		/// liegt. Dadurch entstehen Stirnwaende automatisch und Durchgaenge bleiben
		/// offen - der alte Builder hatte hier seine abgeriegelten Seitenwege.
		/// </summary>
		public static IEnumerable<(float x, float z, float breite, float tiefe, float fuss, float hoehe)> Wandkanten()
		{
			List<(float x, float z, float breite, float tiefe, float fuss, float hoehe)> ergebnis =
				new List<(float x, float z, float breite, float tiefe, float fuss, float hoehe)>();
			foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
			{
				Sammle(flaeche, flaeche.MinX, flaeche.MaxX, entlangX: true,
					flaeche.MinZ - Wanddicke * 0.5f, flaeche.MinZ - Wanddicke, flaeche.MinZ + 0.5f, ergebnis);
				Sammle(flaeche, flaeche.MinX, flaeche.MaxX, entlangX: true,
					flaeche.MaxZ + Wanddicke * 0.5f, flaeche.MaxZ + Wanddicke, flaeche.MaxZ - 0.5f, ergebnis);
				Sammle(flaeche, flaeche.MinZ, flaeche.MaxZ, entlangX: false,
					flaeche.MinX - Wanddicke * 0.5f, flaeche.MinX - Wanddicke, flaeche.MinX + 0.5f, ergebnis);
				Sammle(flaeche, flaeche.MinZ, flaeche.MaxZ, entlangX: false,
					flaeche.MaxX + Wanddicke * 0.5f, flaeche.MaxX + Wanddicke, flaeche.MaxX - 0.5f, ergebnis);
			}
			return ergebnis;
		}

		/// <summary>Baut die Waende. Jede Wand entsteht aus zwei uebereinanderliegenden
		/// Segmenten mit leicht gestaffelter Vertexfarbe: flat shading erzeugt pro
		/// Flaeche genau einen Ton, gestaffelte Baender ersetzen den fehlenden
		/// Verlauf (SCHMIEDE_ENTWURF.md Abschnitt 9).</summary>
		public static void BaueWaende(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			int laufnummer = 0;
			foreach (var kante in Wandkanten())
			{
				float untenHoehe = kante.hoehe * 0.6f;
				Teil(wurzel, $"Wand_{laufnummer}_u", new Vector3(kante.breite, untenHoehe, kante.tiefe),
					new Vector3(kante.x, kante.fuss, kante.z), WandfarbeUnten, welt);
				Teil(wurzel, $"Wand_{laufnummer}_o", new Vector3(kante.breite, kante.hoehe - untenHoehe, kante.tiefe),
					new Vector3(kante.x, kante.fuss + untenHoehe, kante.z), WandfarbeOben, welt);
				laufnummer++;
			}
		}

		/* Die Kante wird in Schritten von einer Einheit abgeschritten und
		   zusammenhaengende Wandzellen werden zu einem Stueck verschmolzen. Die feine
		   Abtastung ist noetig, weil Durchgaenge nicht auf dem Kachelraster liegen:
		   mit 4er-Abschnitten entstand eine Luecke bei (-6,5|-28,5), wo der Abschnitt
		   die Oeffnungskante des Schlunds ueberlappte. Das Verschmelzen haelt die
		   Objektzahl trotz feiner Abtastung niedrig. */
		private static void Sammle(ForgeFlaeche flaeche, float von, float bis, bool entlangX,
			float wandLage, float pruefLage, float innenLage,
			List<(float x, float z, float breite, float tiefe, float fuss, float hoehe)> ergebnis)
		{
			float laufAnfang = float.NaN;
			float laufFuss = 0f;
			float laufHoehe = 0f;
			for (float t = von; t < bis - 0.001f; t += 1f)
			{
				float mitte = t + 0.5f;
				float px = entlangX ? mitte : pruefLage;
				float pz = entlangX ? pruefLage : mitte;
				float ix = entlangX ? mitte : innenLage;
				float iz = entlangX ? innenLage : mitte;
				bool wand = BrauchtWand(flaeche, px, pz, ix, iz, out float fuss, out float hoehe);
				bool gleich = !float.IsNaN(laufAnfang)
					&& Mathf.Abs(fuss - laufFuss) < 0.01f && Mathf.Abs(hoehe - laufHoehe) < 0.01f;
				if (wand && float.IsNaN(laufAnfang))
				{
					laufAnfang = t;
					laufFuss = fuss;
					laufHoehe = hoehe;
				}
				else if (wand && !gleich)
				{
					Schliesse(laufAnfang, t, entlangX, wandLage, laufFuss, laufHoehe, ergebnis);
					laufAnfang = t;
					laufFuss = fuss;
					laufHoehe = hoehe;
				}
				else if (!wand && !float.IsNaN(laufAnfang))
				{
					Schliesse(laufAnfang, t, entlangX, wandLage, laufFuss, laufHoehe, ergebnis);
					laufAnfang = float.NaN;
				}
			}
			if (!float.IsNaN(laufAnfang))
			{
				Schliesse(laufAnfang, bis, entlangX, wandLage, laufFuss, laufHoehe, ergebnis);
			}
		}

		/* Eine Wand steht dort, wo hinter der Kante kein Boden liegt - und zusaetzlich
		   dort, wo der Nachbar auf anderer Hoehe liegt: sonst haette die Grube des
		   Essenkerns keine Wand und man fiele von der Galerie ueberall hinein. An
		   Rampen entfaellt die Wand, denn sie ueberbruecken den Hoehensprung. */
		private static bool BrauchtWand(ForgeFlaeche flaeche, float px, float pz, float ix, float iz,
			out float fuss, out float hoehe)
		{
			if (!EidraForgeLayout.TryFinde(px, pz, out ForgeFlaeche nachbar))
			{
				fuss = flaeche.Hoehe;
				hoehe = EidraForgeLayout.Wandhoehe;
				return true;
			}
			/* Auch die eigene Seite pruefen: liegt dort eine Rampe, gehoert an
			   diese Stelle keine Wand. Sonst mauert die Stuetzwand des
			   Ringschenkels den Rampenausgang zu - genau das hat den Weg in die
			   Grube blockiert. */
			bool innenRampe = EidraForgeLayout.TryFinde(ix, iz, out ForgeFlaeche innen) && innen.IstRampe;
			if (Mathf.Abs(nachbar.Hoehe - flaeche.Hoehe) < 0.01f || flaeche.IstRampe || nachbar.IstRampe
				|| innenRampe)
			{
				fuss = 0f;
				hoehe = 0f;
				return false;
			}
			fuss = Mathf.Min(flaeche.Hoehe, nachbar.Hoehe);
			hoehe = Mathf.Abs(nachbar.Hoehe - flaeche.Hoehe);
			return true;
		}

		private static void Schliesse(float von, float bis, bool entlangX, float wandLage, float fuss, float hoehe,
			List<(float x, float z, float breite, float tiefe, float fuss, float hoehe)> ergebnis)
		{
			float laenge = bis - von;
			float mitte = von + laenge * 0.5f;
			(float x, float z, float breite, float tiefe, float fuss, float hoehe) stueck = entlangX
				? (mitte, wandLage, laenge, Wanddicke, fuss, hoehe)
				: (wandLage, mitte, Wanddicke, laenge, fuss, hoehe);
			foreach (var vorhanden in ergebnis)
			{
				if (Mathf.Abs(vorhanden.x - stueck.x) < 0.01f && Mathf.Abs(vorhanden.z - stueck.z) < 0.01f
					&& Mathf.Abs(vorhanden.breite - stueck.breite) < 0.01f
					&& Mathf.Abs(vorhanden.tiefe - stueck.tiefe) < 0.01f
					&& Mathf.Abs(vorhanden.fuss - stueck.fuss) < 0.01f)
				{
					return;
				}
			}
			ergebnis.Add(stueck);
		}

		/// <summary>
		/// Rampen entstehen als eine durchgehend geneigte Platte, nicht als Stufen.
		/// Gemessener Grund: der NavMesh-Agent hat Radius 0,5 und erodiert von jeder
		/// Kante einen halben Meter. Eine Stufe mit einem Meter Auftritt behaelt
		/// danach null Breite, die Stufen bleiben unverbunden. Laengere Auftritte
		/// wuerden bei 6 Metern Gefaelle die Kletterhoehe von 0,75 sprengen -
		/// eine echte Schraege ist der einzige Weg, der beide Grenzen einhaelt.
		/// Bei 10 Metern Lauf sind das rund 31 Grad, unter der Grenze von 45.
		/// </summary>
		private static void BaueRampe(Transform wurzel, ForgeFlaeche flaeche, Material material)
		{
			bool entlangX = Mathf.Abs(flaeche.GefaelleX) > 0.01f;
			float lauf = entlangX ? flaeche.MaxX - flaeche.MinX : flaeche.MaxZ - flaeche.MinZ;
			float breite = entlangX ? flaeche.MaxZ - flaeche.MinZ : flaeche.MaxX - flaeche.MinX;
			float fall = -EidraForgeLayout.Grubentiefe;
			float winkel = Mathf.Atan2(fall, lauf) * Mathf.Rad2Deg;
			float schraeg = Mathf.Sqrt(lauf * lauf + fall * fall);
			float mitteX = (flaeche.MinX + flaeche.MaxX) * 0.5f;
			float mitteZ = (flaeche.MinZ + flaeche.MaxZ) * 0.5f;
			float vorzeichen = entlangX ? flaeche.GefaelleX : flaeche.GefaelleZ;

			GameObject teil = new GameObject("Rampe_" + flaeche.Name);
			teil.transform.SetParent(wurzel, worldPositionStays: false);
			teil.transform.position = new Vector3(mitteX,
				EidraForgeLayout.Grubentiefe * 0.5f - Bodendicke * Mathf.Cos(winkel * Mathf.Deg2Rad), mitteZ);
			/* Drehsinn: eine positive Drehung um Z hebt die +X-Seite, eine positive
			   Drehung um X senkt die +Z-Seite. Fuer Gefaelle nach +X braucht es
			   also eine negative Z-Drehung, fuer Gefaelle nach +Z eine positive
			   X-Drehung. Gemessen, nachdem die erste Fassung die Rampe verkehrt
			   herum ansteigen liess. */
			teil.transform.rotation = entlangX
				? Quaternion.Euler(0f, 0f, -winkel * vorzeichen)
				: Quaternion.Euler(winkel * vorzeichen, 0f, 0f);
			teil.AddComponent<MeshFilter>().sharedMesh = EidrenMeshFactory.TaperedBox(
				entlangX ? new Vector3(schraeg, Bodendicke, breite) : new Vector3(breite, Bodendicke, schraeg),
				1f, Bodenfarbe);
			teil.AddComponent<MeshRenderer>().sharedMaterial = material;
			teil.AddComponent<BoxCollider>();
		}

		private static void Teil(Transform wurzel, string name, Vector3 groesse, Vector3 position, Color farbe, Material material)
		{
			GameObject teil = new GameObject(name);
			teil.transform.SetParent(wurzel, worldPositionStays: false);
			teil.transform.position = position;
			teil.AddComponent<MeshFilter>().sharedMesh = EidrenMeshFactory.TaperedBox(groesse, 1f, farbe);
			teil.AddComponent<MeshRenderer>().sharedMaterial = material;
			teil.AddComponent<BoxCollider>();
		}
	}
}
