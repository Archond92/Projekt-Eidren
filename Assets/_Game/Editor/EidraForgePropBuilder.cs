using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Baut die Requisiten der Schmiede aus den Daten in EidraForgeProps.
	/// Farben laut SCHMIEDE_ENTWURF.md Abschnitt 9; selbstleuchtende Teile
	/// bekommen das Glut-Material und werden mit kontaktAo:false gebaut.
	/// </summary>
	public static class EidraForgePropBuilder
	{
		/* Mit dem Boden angehoben: siehe EidraForgeGeometryBuilder. Bei 0,22 warf
		   der Stein zu wenig Licht zurueck, um von den Punktlichtern erreicht zu
		   werden. */
		private static readonly Color Stein = new Color(0.32f, 0.29f, 0.3f);

		/* Eisen deutlich dunkler als Mauerwerk. Mit dem Steinwert und ohne
		   Kontakt-AO war das Rohr das hellste grosse Objekt im Bild und las sich
		   wie ein anderes Material. */
		private static readonly Color Eisen = new Color(0.15f, 0.13f, 0.14f);

		private static readonly Color Rost = new Color(0.42f, 0.12f, 0.025f);

		private static readonly Color Riss = new Color(0.08f, 0.06f, 0.06f);

		private static readonly Color Glut = new Color(1f, 0.42f, 0.06f);

		private static readonly Color GlutMitte = new Color(0.75f, 0.3f, 0.04f);

		private static readonly Color GlutTief = new Color(0.55f, 0.21f, 0.03f);

		private static readonly Color Holz = new Color(0.29f, 0.21f, 0.14f);

		private static readonly Color Schlacke = new Color(0.1f, 0.09f, 0.11f);

		private static readonly Color Leder = new Color(0.2f, 0.13f, 0.1f);

		/// <summary>
		/// Das Windrohr ist das Leitmotiv: ein Objekt, das in jedem Raum
		/// wiederkehrt und immer zum Feuer zeigt. Jeder Abschnitt ist ein
		/// achteckiger Loft zwischen zwei Verlaufspunkten.
		/// </summary>
		public static void BaueWindrohr(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			var verlauf = EidraForgeProps.Windrohrverlauf;
			float weite = EidraForgeProps.Rohrdurchmesser;
			for (int index = 1; index < verlauf.Count; index++)
			{
				Vector2 a = verlauf[index - 1];
				Vector2 b = verlauf[index];
				float laenge = Vector2.Distance(a, b);
				bool entlangZ = Mathf.Abs(b.y - a.y) > 0.01f;
				EidrenMeshFactory.LoftProfile[] rohr =
				{
					new EidrenMeshFactory.LoftProfile(0f, weite, weite),
					new EidrenMeshFactory.LoftProfile(laenge, weite, weite)
				};
				/* Der Loft waechst entlang +Y. Die Drehung legt ihn waagerecht in
				   die Verlaufsrichtung: +Z braucht +90 Grad um X, +X braucht
				   -90 Grad um Z. */
				Quaternion drehung = entlangZ
					? Quaternion.Euler(b.y > a.y ? 90f : -90f, 0f, 0f)
					: Quaternion.Euler(0f, 0f, b.x > a.x ? -90f : 90f);
				GameObject teil = Teil(wurzel, $"Windrohr_{index}",
					EidrenMeshFactory.Loft(rohr, EidrenMeshFactory.LoftShape.Oct, Eisen,
						capBottom: false, capTop: false, kontaktAo: false),
					new Vector3(a.x, EidraForgeProps.Rohrhoehe, a.y), welt);
				teil.transform.rotation = drehung;

				/* Schellen statt Wandkonsolen: der Verlauf fuehrt streckenweise
				   frei durch den Raum, wo keine Wand zum Anlehnen da waere.
				   Ein Ring um das Rohr sitzt ueberall richtig. */
				int schellen = Mathf.Max(1, Mathf.RoundToInt(laenge / 8f));
				for (int nummer = 0; nummer <= schellen; nummer++)
				{
					Vector2 punkt = Vector2.Lerp(a, b, (float)nummer / schellen);
					EidrenMeshFactory.LoftProfile[] ring =
					{
						new EidrenMeshFactory.LoftProfile(0f, weite + 0.25f, weite + 0.25f),
						new EidrenMeshFactory.LoftProfile(0.3f, weite + 0.25f, weite + 0.25f)
					};
					GameObject schelle = Teil(wurzel, $"Rohrschelle_{index}_{nummer}",
						EidrenMeshFactory.Loft(ring, EidrenMeshFactory.LoftShape.Oct, Rost,
							capBottom: false, capTop: false, kontaktAo: false),
						new Vector3(punkt.x, EidraForgeProps.Rohrhoehe, punkt.y), welt);
					schelle.transform.rotation = drehung;
				}
			}
		}

		/// <summary>
		/// Die Esse: sieben Profilringe, drei Eisenbaender mit Ueberstand, eine
		/// Deckplatte. Hoehe 10 statt der urspruenglich geplanten 6 - bei 52 Grad
		/// Kamerawinkel verdeckt der nahe Grubenrand eine buendig mit der Galerie
		/// abschliessende Oeffnung. Werte durch ForgeGlowProbe belegt.
		/// </summary>
		public static void BaueEsse(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			Material leucht = EidrenWorldStyleAssets.EnsureGlowMaterial();
			Vector3 fuss = new Vector3(EidraForgeProps.EssenFuss.x, EidraForgeLayout.Grubentiefe,
				EidraForgeProps.EssenFuss.y);
			EidrenMeshFactory.LoftProfile[] koerper =
			{
				new EidrenMeshFactory.LoftProfile(0f, 8f, 8f),
				new EidrenMeshFactory.LoftProfile(1.8f, 7.6f, 7.6f),
				new EidrenMeshFactory.LoftProfile(3.6f, 6.8f, 6.8f),
				new EidrenMeshFactory.LoftProfile(5.4f, 5.6f, 5.6f),
				new EidrenMeshFactory.LoftProfile(7.2f, 4.4f, 4.4f),
				new EidrenMeshFactory.LoftProfile(8.6f, 4f, 4f),
				new EidrenMeshFactory.LoftProfile(9.5f, 3.6f, 3.6f)
			};
			Teil(wurzel, "Esse_Koerper", EidrenMeshFactory.Loft(koerper, EidrenMeshFactory.LoftShape.Oct,
				Stein, capBottom: true, capTop: false), fuss, welt);

			Band(wurzel, "Esse_BandTief", fuss, 1.6f, 7.94f, welt);
			Band(wurzel, "Esse_BandMitte", fuss, 3.8f, 6.97f, welt);
			Band(wurzel, "Esse_BandHoch", fuss, 8.3f, 4.39f, welt);

			EidrenMeshFactory.LoftProfile[] deckel =
			{
				new EidrenMeshFactory.LoftProfile(0f, 4.4f, 4.4f),
				new EidrenMeshFactory.LoftProfile(0.5f, 4.2f, 4.2f)
			};
			Teil(wurzel, "Esse_Deckplatte", EidrenMeshFactory.Loft(deckel, EidrenMeshFactory.LoftShape.Oct,
				Stein, capBottom: false, capTop: true), fuss + new Vector3(0f, 9.5f, 0f), welt);

			Teil(wurzel, "Esse_OeffnungRahmen",
				EidrenMeshFactory.TaperedBox(new Vector3(3.4f, 3.2f, 0.6f), 1f, Rost),
				fuss + new Vector3(0f, 6f, -2.75f), welt);
			Teil(wurzel, "Esse_OeffnungGlut",
				EidrenMeshFactory.TaperedBox(new Vector3(2.6f, 2.4f, 0.2f), 1f, Glut, kontaktAo: false),
				fuss + new Vector3(0f, 6f, -3f), leucht, fest: false);
		}

		/// <summary>
		/// Glutadern und Kohlenpfannen. Jede Ader entsteht aus drei Segmenten mit
		/// gestaffelter Helligkeit in einer dunkleren Vertiefung - ein einzelner
		/// Vollton-Streifen liest sich als Leuchtroehre statt als Glut (Befund aus
		/// ForgeGlowProbe). Punktlichter kommen erst in Etappe 3: die Probe hat
		/// gezeigt, dass selbstleuchtende Flaechen ohne eigenes Licht tragen.
		/// </summary>
		public static void BaueGlut(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			Material leucht = EidrenWorldStyleAssets.EnsureGlowMaterial();
			Color[] stufen = { Glut, GlutMitte, GlutTief };

			int nummer = 0;
			foreach (ForgeGlutader ader in EidraForgeProps.Glutadern)
			{
				EidraForgeLayout.IstAufBoden(ader.X, ader.Z, out float hoehe);
				float teilLaenge = ader.Laenge / 3f;
				Vector3 rissGroesse = ader.EntlangZ
					? new Vector3(0.9f, 0.1f, ader.Laenge + 0.4f)
					: new Vector3(ader.Laenge + 0.4f, 0.1f, 0.9f);
				Teil(wurzel, $"Glutriss_{nummer}",
					EidrenMeshFactory.TaperedBox(rissGroesse, 1f, Riss),
					new Vector3(ader.X, hoehe, ader.Z), welt, fest: false);

				for (int stufe = 0; stufe < 3; stufe++)
				{
					float versatz = (stufe - 1) * teilLaenge;
					Vector3 position = ader.EntlangZ
						? new Vector3(ader.X, hoehe + 0.02f, ader.Z + versatz)
						: new Vector3(ader.X + versatz, hoehe + 0.02f, ader.Z);
					/* Hellstes Segment am breitesten: dort ist der Riss am tiefsten
					   aufgerissen. Umgekehrt sah es aus wie ein sich verjuengender
					   Balken statt wie Glut in einem Spalt. */
					float breite = 0.45f - stufe * 0.05f;
					Vector3 groesse = ader.EntlangZ
						? new Vector3(breite, 0.12f, teilLaenge)
						: new Vector3(teilLaenge, 0.12f, breite);
					Teil(wurzel, $"Glutader_{nummer}_{stufe}",
						EidrenMeshFactory.TaperedBox(groesse, 1f, stufen[stufe], kontaktAo: false),
						position, leucht, fest: false);
				}
				nummer++;
			}

			foreach (ForgePlatz pfanne in EidraForgeProps.Kohlenpfannen)
			{
				EidraForgeLayout.IstAufBoden(pfanne.X, pfanne.Z, out float hoehe);
				Vector3 fuss = new Vector3(pfanne.X, hoehe, pfanne.Z);
				for (int bein = 0; bein < 3; bein++)
				{
					float winkel = bein * Mathf.PI * 2f / 3f;
					Vector3 versatz = new Vector3(Mathf.Cos(winkel) * 0.32f, 0f, Mathf.Sin(winkel) * 0.32f);
					Teil(wurzel, $"{pfanne.Id}_Bein{bein}",
						EidrenMeshFactory.TaperedBox(new Vector3(0.14f, 0.75f, 0.14f), 0.7f, Rost),
						fuss + versatz, welt);
				}
				EidrenMeshFactory.LoftProfile[] schale =
				{
					new EidrenMeshFactory.LoftProfile(0f, 0.5f, 0.5f),
					new EidrenMeshFactory.LoftProfile(0.3f, 0.9f, 0.9f)
				};
				Teil(wurzel, $"{pfanne.Id}_Schale", EidrenMeshFactory.Loft(schale,
					EidrenMeshFactory.LoftShape.Oct, Rost, capBottom: true, capTop: false),
					fuss + new Vector3(0f, 0.75f, 0f), welt);
				Teil(wurzel, $"{pfanne.Id}_Glut",
					EidrenMeshFactory.TaperedBox(new Vector3(0.7f, 0.1f, 0.7f), 1f, Glut, kontaktAo: false),
					fuss + new Vector3(0f, 0.98f, 0f), leucht, fest: false);
			}
		}

		/// <summary>
		/// Die wiederkehrenden Bauteile: Masselbetten und Kaesten als Rechteckkoerper,
		/// Schlackenhaufen als achteckige Lofts mit seitlichem Versatz, Stege quer
		/// ueber der Rinne auf Hoehe 4,5.
		/// </summary>
		public static void BaueAufbauten(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();

			foreach (ForgeAufbau bett in EidraForgeProps.Masselbetten)
			{
				EidraForgeLayout.IstAufBoden(bett.X, bett.Z, out float hoehe);
				GameObject teil = Teil(wurzel, bett.Id,
					EidrenMeshFactory.TaperedBox(new Vector3(bett.Breite, bett.Hoehe, bett.Tiefe), 0.85f, Stein),
					new Vector3(bett.X, hoehe, bett.Z), welt);
				teil.transform.rotation = Quaternion.Euler(0f, bett.Drehung, 0f);
			}

			foreach (ForgeAufbau stapel in EidraForgeProps.Formkastenstapel)
			{
				EidraForgeLayout.IstAufBoden(stapel.X, stapel.Z, out float hoehe);
				int kaesten = Mathf.Max(1, Mathf.RoundToInt(stapel.Hoehe / 0.6f));
				for (int lage = 0; lage < kaesten; lage++)
				{
					float schrumpf = 1f - lage * 0.08f;
					GameObject teil = Teil(wurzel, $"{stapel.Id}_{lage}",
						EidrenMeshFactory.TaperedBox(
							new Vector3(stapel.Breite * schrumpf, 0.6f, stapel.Tiefe * schrumpf), 1f, Holz),
						new Vector3(stapel.X, hoehe + lage * 0.6f, stapel.Z), welt);
					teil.transform.rotation = Quaternion.Euler(0f, stapel.Drehung + lage * 4f, 0f);
				}
			}

			foreach (ForgeAufbau haufen in EidraForgeProps.Schlackenhaufen)
			{
				EidraForgeLayout.IstAufBoden(haufen.X, haufen.Z, out float hoehe);
				EidrenMeshFactory.LoftProfile[] form =
				{
					new EidrenMeshFactory.LoftProfile(0f, haufen.Breite, haufen.Tiefe),
					new EidrenMeshFactory.LoftProfile(haufen.Hoehe * 0.55f, haufen.Breite * 0.7f,
						haufen.Tiefe * 0.75f, 0.15f),
					new EidrenMeshFactory.LoftProfile(haufen.Hoehe, haufen.Breite * 0.25f,
						haufen.Tiefe * 0.3f, 0.25f)
				};
				Teil(wurzel, haufen.Id, EidrenMeshFactory.Loft(form, EidrenMeshFactory.LoftShape.Oct,
					Schlacke, capBottom: false, capTop: true), new Vector3(haufen.X, hoehe, haufen.Z), welt);
			}

			foreach (ForgeAufbau steg in EidraForgeProps.Rinnenstege)
			{
				Teil(wurzel, steg.Id,
					EidrenMeshFactory.TaperedBox(new Vector3(steg.Breite, steg.Hoehe, steg.Tiefe), 1f, Holz,
						kontaktAo: false),
					new Vector3(steg.X, 4.5f, steg.Z), welt);
			}
		}

		/// <summary>
		/// Leitet das Gelaender aus der Grubenkante ab, statt es zu platzieren -
		/// wie schon die Waende in Etappe 1. Entlang des Randes wird im Einer-Raster
		/// geprueft, ob aussen die Galerie auf Ebene 0 liegt. An Treppen und
		/// Abstichrinne entfaellt es, weil dort eine Rampe die Kante ueberbrueckt;
		/// bei einer Layoutaenderung kann es die Zugaenge also nicht zuwachsen.
		/// </summary>
		public static IEnumerable<(float x, float z, float breite, float tiefe)> Gelaenderkanten()
		{
			ForgeFlaeche grube = default;
			foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
			{
				if (flaeche.Name == "Essenkern")
				{
					grube = flaeche;
				}
			}
			List<(float x, float z, float breite, float tiefe)> ergebnis =
				new List<(float x, float z, float breite, float tiefe)>();
			for (float x = grube.MinX; x < grube.MaxX - 0.001f; x += 1f)
			{
				Gelaenderstueck(x + 0.5f, grube.MinZ - 0.5f, 1f, 0.25f, ergebnis);
				Gelaenderstueck(x + 0.5f, grube.MaxZ + 0.5f, 1f, 0.25f, ergebnis);
			}
			for (float z = grube.MinZ; z < grube.MaxZ - 0.001f; z += 1f)
			{
				Gelaenderstueck(grube.MinX - 0.5f, z + 0.5f, 0.25f, 1f, ergebnis);
				Gelaenderstueck(grube.MaxX + 0.5f, z + 0.5f, 0.25f, 1f, ergebnis);
			}
			return ergebnis;
		}

		private static void Gelaenderstueck(float x, float z, float breite, float tiefe,
			List<(float x, float z, float breite, float tiefe)> ergebnis)
		{
			if (!EidraForgeLayout.TryFinde(x, z, out ForgeFlaeche nachbar) || nachbar.IstRampe)
			{
				return;
			}
			if (Mathf.Abs(nachbar.Hoehe) > 0.01f)
			{
				return;
			}
			ergebnis.Add((x, z, breite, tiefe));
		}

		/// <summary>Gelaender: Handlauf auf 1,1 mit Pfosten in jedem zweiten Stueck.</summary>
		public static void BaueGelaender(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			int nummer = 0;
			foreach (var kante in Gelaenderkanten())
			{
				Teil(wurzel, $"Gelaender_{nummer}",
					EidrenMeshFactory.TaperedBox(new Vector3(kante.breite, 0.12f, kante.tiefe), 1f, Eisen,
						kontaktAo: false),
					new Vector3(kante.x, 1.1f, kante.z), welt);
				if (nummer % 2 == 0)
				{
					Teil(wurzel, $"Gelaenderpfosten_{nummer}",
						EidrenMeshFactory.TaperedBox(new Vector3(0.16f, 1.1f, 0.16f), 0.8f, Eisen),
						new Vector3(kante.x, 0f, kante.z), welt);
				}
				nummer++;
			}
		}

		/// <summary>
		/// Die Einzelstuecke, an denen man jeden Raum wiedererkennt. Verteilt nach
		/// Id, weil jedes Stueck eine eigene Form hat - anders als die Bauteile,
		/// die sich aus Maszen ableiten lassen.
		/// </summary>
		public static void BaueEinzelstuecke(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			foreach (ForgeAufbau stueck in EidraForgeProps.Einzelstuecke)
			{
				switch (stueck.Id)
				{
				case "forge.prop.blasebalg":
					BaueBlasebalg(wurzel, stueck, welt);
					break;
				case "forge.prop.duesenstein":
					BaueDuesenstein(wurzel, stueck, welt);
					break;
				case "forge.prop.erzkarren":
					BaueErzkarren(wurzel, stueck, welt);
					break;
				case "forge.prop.schwanzhammer":
					BaueSchwanzhammer(wurzel, stueck, welt);
					break;
				case "forge.prop.reif.01":
				case "forge.prop.reif.02":
				case "forge.prop.reif.03":
					BaueReif(wurzel, stueck, welt);
					break;
				case "forge.prop.gichtkuebel.west":
				case "forge.prop.gichtkuebel.ost":
					BaueGichtkuebel(wurzel, stueck, welt);
					break;
				case "forge.prop.formwall":
					BaueFormwall(wurzel, stueck, welt);
					break;
				case "forge.prop.bauplatz":
					BaueBauplatz(wurzel, stueck, welt);
					break;
				}
			}
			BaueTruemmer(wurzel, welt);
		}

		/* Deckentruemmer: drei Profilringe mit seitlichem Versatz, damit die Bloecke
		   schief liegen statt gestapelt zu wirken. Ihr Zweck ist das Brechen der
		   Sichtlinien - der Einbruch ist der einzige Raum, in dem man beim Betreten
		   nicht sofort sieht, was drin ist. */
		private static void BaueTruemmer(Transform wurzel, Material welt)
		{
			foreach (ForgeAufbau brocken in EidraForgeProps.Deckentruemmer)
			{
				EidraForgeLayout.IstAufBoden(brocken.X, brocken.Z, out float boden);
				EidrenMeshFactory.LoftProfile[] form =
				{
					new EidrenMeshFactory.LoftProfile(0f, brocken.Breite, brocken.Tiefe),
					new EidrenMeshFactory.LoftProfile(brocken.Hoehe * 0.6f, brocken.Breite * 0.85f,
						brocken.Tiefe * 0.9f, 0.25f, -0.15f),
					new EidrenMeshFactory.LoftProfile(brocken.Hoehe, brocken.Breite * 0.5f,
						brocken.Tiefe * 0.55f, 0.45f, -0.3f)
				};
				GameObject teil = Teil(wurzel, brocken.Id, EidrenMeshFactory.Loft(form,
					EidrenMeshFactory.LoftShape.Oct, Stein, capBottom: false, capTop: true),
					new Vector3(brocken.X, boden, brocken.Z), welt);
				teil.transform.rotation = Quaternion.Euler(0f, brocken.Drehung, 0f);
				teil.AddComponent<BoxCollider>();
			}
		}

		/* Die Formwand war eine gerade Reihe Formkaesten. Jetzt steht die linke
		   Haelfte noch, die rechte ist gekippt und auseinandergerissen - daran
		   erkennt man, dass hier dasselbe stand wie in den Masselkammern. */
		private static void BaueFormwall(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			int kaesten = 5;
			for (int nummer = 0; nummer < kaesten; nummer++)
			{
				float anteil = nummer / (float)(kaesten - 1) - 0.5f;
				bool gekippt = nummer >= 3;
				Vector3 versatz = new Vector3(anteil * daten.Breite, 0f, gekippt ? 0.5f * (nummer - 2) : 0f);
				GameObject teil = Teil(wurzel, $"{daten.Id}_{nummer}",
					EidrenMeshFactory.TaperedBox(new Vector3(1.5f, daten.Hoehe, daten.Tiefe), 1f, Holz),
					new Vector3(daten.X + versatz.x, boden, daten.Z + versatz.z), welt);
				teil.transform.rotation = gekippt
					? Quaternion.Euler(0f, daten.Drehung + nummer * 9f, 24f + nummer * 7f)
					: Quaternion.Euler(0f, daten.Drehung, 0f);
			}
		}

		/* Der abgesteckte Grund fuer den Formwall-Ausbau. Noch ohne Funktion - die
		   Mechanik kommt in Etappe 4. Hier entsteht nur das Objekt, damit der Raum
		   vollstaendig ist und man sieht, dass hier etwas hingehoert. */
		private static void BaueBauplatz(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			Teil(wurzel, daten.Id + "_Grund",
				EidrenMeshFactory.TaperedBox(new Vector3(daten.Breite, daten.Hoehe, daten.Tiefe), 1f, Riss),
				new Vector3(daten.X, boden, daten.Z), welt, fest: false);
			foreach (int ex in new[] { -1, 1 })
			{
				foreach (int ez in new[] { -1, 1 })
				{
					Teil(wurzel, $"{daten.Id}_Stange{ex}{ez}",
						EidrenMeshFactory.TaperedBox(new Vector3(0.18f, 2.2f, 0.18f), 0.75f, Holz),
						new Vector3(daten.X + ex * daten.Breite * 0.45f, boden,
							daten.Z + ez * daten.Tiefe * 0.45f), welt);
				}
			}
			Teil(wurzel, daten.Id + "_Gestell",
				EidrenMeshFactory.TaperedBox(new Vector3(daten.Breite * 0.7f, 0.15f, 0.5f), 1f, Holz,
					kontaktAo: false),
				new Vector3(daten.X, boden + 0.9f, daten.Z + daten.Tiefe * 0.4f), welt);

			/* Der Ausloeser fuer Bau und Ernte. Ohne ihn waere der Bauplatz blosse
			   Geometrie und der Formwall unerreichbar. */
			GameObject ausloeser = new GameObject(daten.Id + "_Interaktion");
			ausloeser.transform.SetParent(wurzel, worldPositionStays: false);
			ausloeser.transform.position = new Vector3(daten.X, boden, daten.Z);
			SphereCollider bereich = ausloeser.AddComponent<SphereCollider>();
			bereich.isTrigger = true;
			bereich.radius = 2.2f;
			ausloeser.AddComponent<Eidren.Interaction.EidraForgeFormwallSite>();
		}

		/* Kein Kasten, sondern ein flacher Keil: hinten hoch und breit, nach vorn
		   auf den Stutzen zulaufend, eine Haelfte eingesackt. Das Referenzblatt
		   hatte hier zuerst eine rote Kiste geliefert. */
		private static void BaueBlasebalg(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			Vector3 fuss = new Vector3(daten.X, boden, daten.Z - daten.Tiefe * 0.5f);
			EidrenMeshFactory.LoftProfile[] balg =
			{
				new EidrenMeshFactory.LoftProfile(0f, daten.Breite, daten.Hoehe),
				new EidrenMeshFactory.LoftProfile(daten.Tiefe * 0.55f, daten.Breite * 0.7f, daten.Hoehe * 0.7f),
				new EidrenMeshFactory.LoftProfile(daten.Tiefe * 0.9f, 1f, 0.9f),
				new EidrenMeshFactory.LoftProfile(daten.Tiefe, 0.6f, 0.6f)
			};
			GameObject koerper = Teil(wurzel, daten.Id, EidrenMeshFactory.Loft(balg,
				EidrenMeshFactory.LoftShape.Rect, Leder, capBottom: true, capTop: false), fuss, welt);
			koerper.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

			GameObject platte = Teil(wurzel, daten.Id + "_Deckplatte",
				EidrenMeshFactory.TaperedBox(new Vector3(daten.Breite * 0.95f, 0.35f, daten.Tiefe * 0.6f), 0.8f, Holz),
				fuss + new Vector3(0f, daten.Hoehe * 0.85f, daten.Tiefe * 0.3f), welt);
			platte.transform.rotation = Quaternion.Euler(0f, 0f, 9f);

			EidrenMeshFactory.LoftProfile[] stutzen =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.7f, 0.7f),
				new EidrenMeshFactory.LoftProfile(1.2f, 0.7f, 0.7f)
			};
			GameObject rohr = Teil(wurzel, daten.Id + "_Stutzen", EidrenMeshFactory.Loft(stutzen,
				EidrenMeshFactory.LoftShape.Oct, Eisen, capBottom: false, capTop: false, kontaktAo: false),
				new Vector3(daten.X, EidraForgeProps.Rohrhoehe, daten.Z + daten.Tiefe * 0.5f - 0.6f), welt);
			rohr.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		}

		/* Der Gang fuehrt hindurch, also vier Bloecke um eine Oeffnung statt eines
		   Quaders mit Loch - die Mesh-Fabrik kennt keine Boolean-Operationen. */
		private static void BaueDuesenstein(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			float oeffnung = 2.5f;
			float wange = (daten.Breite - oeffnung) * 0.5f;
			foreach (int seite in new[] { -1, 1 })
			{
				Teil(wurzel, $"{daten.Id}_Wange{seite}",
					EidrenMeshFactory.TaperedBox(new Vector3(wange, daten.Hoehe, daten.Tiefe), 0.9f, Stein),
					new Vector3(daten.X + seite * (oeffnung + wange) * 0.5f, boden, daten.Z), welt);
			}
			Teil(wurzel, daten.Id + "_Sturz",
				EidrenMeshFactory.TaperedBox(new Vector3(daten.Breite, daten.Hoehe - 2.6f, daten.Tiefe), 0.95f, Stein),
				new Vector3(daten.X, boden + 2.6f, daten.Z), welt);
			Teil(wurzel, daten.Id + "_Rost",
				EidrenMeshFactory.TaperedBox(new Vector3(oeffnung + 0.3f, 0.25f, daten.Tiefe + 0.2f), 1f, Rost),
				new Vector3(daten.X, boden + 2.45f, daten.Z), welt);
		}

		private static void BaueErzkarren(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			Vector3 fuss = new Vector3(daten.X, boden + 0.45f, daten.Z);
			GameObject kasten = Teil(wurzel, daten.Id,
				EidrenMeshFactory.TaperedBox(new Vector3(daten.Breite, daten.Hoehe * 0.6f, daten.Tiefe), 1.15f, Holz),
				fuss, welt);
			kasten.transform.rotation = Quaternion.Euler(0f, daten.Drehung, 6f);
			foreach (int seite in new[] { -1, 1 })
			{
				GameObject teil = Teil(wurzel, $"{daten.Id}_Rad{seite}", Rad(),
					fuss + new Vector3(seite * daten.Breite * 0.5f, -0.45f, 0f), welt);
				teil.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
			}
			Teil(wurzel, daten.Id + "_RadAb", Rad(),
				new Vector3(daten.X + 1.8f, boden, daten.Z - 1.2f), welt);
		}

		/* Ein zusammenhaengendes Geraet: alle Teile beruehren einander. Im
		   Referenzblatt lagen Gestell, Balken und Amboss als lose Einzelteile
		   nebeneinander - genau das soll hier nicht passieren. */
		private static void BaueSchwanzhammer(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			Vector3 fuss = new Vector3(daten.X, boden, daten.Z);
			foreach (int seite in new[] { -1, 1 })
			{
				Teil(wurzel, $"{daten.Id}_Staender{seite}",
					EidrenMeshFactory.TaperedBox(new Vector3(0.45f, daten.Hoehe, 0.45f), 0.85f, Holz),
					fuss + new Vector3(seite * 1.1f, 0f, 1.4f), welt);
			}
			Teil(wurzel, daten.Id + "_Querriegel",
				EidrenMeshFactory.TaperedBox(new Vector3(3.1f, 0.4f, 0.5f), 1f, Holz, kontaktAo: false),
				fuss + new Vector3(0f, daten.Hoehe - 0.4f, 1.4f), welt);
			GameObject balken = Teil(wurzel, daten.Id + "_Balken",
				EidrenMeshFactory.TaperedBox(new Vector3(0.55f, 0.55f, 5.4f), 1f, Holz, kontaktAo: false),
				fuss + new Vector3(0f, daten.Hoehe - 0.9f, 1.4f), welt);
			balken.transform.rotation = Quaternion.Euler(-16f, 0f, 0f);
			Teil(wurzel, daten.Id + "_Lager",
				EidrenMeshFactory.TaperedBox(new Vector3(0.7f, 0.7f, 0.7f), 1f, Eisen, kontaktAo: false),
				fuss + new Vector3(0f, daten.Hoehe - 1.1f, 1.4f), welt);
			/* Hammerkopf in Rost statt Eisen: in dunklem Eisen auf dunklem Boden war
			   im Bild nicht zu erkennen, ob er den Balken beruehrt - und genau das
			   ist die Frage, an der das Geraet als Maschine liest oder nicht. */
			Teil(wurzel, daten.Id + "_Hammerkopf",
				EidrenMeshFactory.TaperedBox(new Vector3(0.9f, 1.1f, 0.9f), 1.1f, Rost, kontaktAo: false),
				fuss + new Vector3(0f, 0.9f, -1.1f), welt);
			Teil(wurzel, daten.Id + "_Amboss",
				EidrenMeshFactory.TaperedBox(new Vector3(1.1f, 0.55f, 1.1f), 0.8f, Eisen),
				fuss + new Vector3(0f, 0.35f, -1.1f), welt);
			Teil(wurzel, daten.Id + "_Klotz",
				EidrenMeshFactory.TaperedBox(new Vector3(1.3f, 0.35f, 1.3f), 0.95f, Holz),
				fuss + new Vector3(0f, 0f, -1.1f), welt);
		}

		/* Achteckige Eisenreife aus acht Segmenten - die Mesh-Fabrik kennt keinen
		   Torus, und Achtecke passen ohnehin zur Formsprache. */
		private static void BaueReif(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			float radius = daten.Breite * 0.5f;
			for (int ecke = 0; ecke < 8; ecke++)
			{
				float winkel = ecke * 45f;
				float bogen = winkel * Mathf.Deg2Rad;
				Vector3 versatz = new Vector3(Mathf.Sin(bogen) * radius, radius - Mathf.Cos(bogen) * radius, 0f);
				GameObject teil = Teil(wurzel, $"{daten.Id}_S{ecke}",
					EidrenMeshFactory.TaperedBox(new Vector3(0.22f, radius * 0.8f, 0.3f), 1f, Eisen,
						kontaktAo: false),
					new Vector3(daten.X + versatz.x, boden + versatz.y, daten.Z), welt);
				teil.transform.rotation = Quaternion.Euler(0f, 0f, -winkel);
			}
		}

		/* Ausleger von der Galerie, Kuebel frei ueber der Grube. */
		private static void BaueGichtkuebel(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			float richtung = daten.X < 0f ? 1f : -1f;
			Teil(wurzel, daten.Id + "_Mast",
				EidrenMeshFactory.TaperedBox(new Vector3(0.3f, 2.6f, 0.3f), 0.85f, Eisen),
				new Vector3(daten.X, boden, daten.Z), welt);
			Teil(wurzel, daten.Id + "_Ausleger",
				EidrenMeshFactory.TaperedBox(new Vector3(3.2f, 0.25f, 0.3f), 1f, Eisen, kontaktAo: false),
				new Vector3(daten.X + richtung * 1.6f, boden + 2.6f, daten.Z), welt);
			EidrenMeshFactory.LoftProfile[] kuebel =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.8f, 0.8f),
				new EidrenMeshFactory.LoftProfile(1.2f, 1.4f, 1.4f)
			};
			GameObject topf = Teil(wurzel, daten.Id + "_Kuebel", EidrenMeshFactory.Loft(kuebel,
				EidrenMeshFactory.LoftShape.Oct, Eisen, capBottom: true, capTop: false, kontaktAo: false),
				new Vector3(daten.X + richtung * 3f, boden + 1.1f, daten.Z), welt);
			topf.transform.rotation = Quaternion.Euler(0f, 0f, richtung * 14f);
		}

		private static Mesh Rad()
		{
			EidrenMeshFactory.LoftProfile[] rad =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.9f, 0.9f),
				new EidrenMeshFactory.LoftProfile(0.18f, 0.9f, 0.9f)
			};
			return EidrenMeshFactory.Loft(rad, EidrenMeshFactory.LoftShape.Oct, Eisen,
				capBottom: true, capTop: true, kontaktAo: false);
		}

		private static void Band(Transform wurzel, string name, Vector3 fuss, float hoehe, float breite,
			Material material)
		{
			EidrenMeshFactory.LoftProfile[] ring =
			{
				new EidrenMeshFactory.LoftProfile(0f, breite, breite),
				new EidrenMeshFactory.LoftProfile(0.35f, breite, breite)
			};
			Teil(wurzel, name, EidrenMeshFactory.Loft(ring, EidrenMeshFactory.LoftShape.Oct, Rost,
				capBottom: false, capTop: false), fuss + new Vector3(0f, hoehe, 0f), material);
		}

		/// <summary>
		/// F32-008: Requisiten sind standardmaessig FEST. Vorher setzte
		/// dieser Helfer nie einen Kollider — man lief durch die Esse, durch
		/// die Windrohre und durch die Gelaender. Die Wandpruefungen waren
		/// dabei gruen, weil die Waende aus einem anderen Builder stammen.
		///
		/// Ausnahmen bekommen <c>fest: false</c> und sind damit im Code
		/// sichtbar: flache Bodenzeichnungen (Glutrisse, Glutadern,
		/// Bauplatzgrund) und reine Leuchtflaechen. Ein Kollider auf einer
		/// Bodenzeichnung waere eine Stolperkante — ein Fehler gegen einen
		/// anderen getauscht.
		/// </summary>
		private static GameObject Teil(Transform wurzel, string name, Mesh mesh, Vector3 position, Material material, bool fest = true)
		{
			GameObject teil = new GameObject(name);
			teil.transform.SetParent(wurzel, worldPositionStays: false);
			teil.transform.position = position;
			teil.AddComponent<MeshFilter>().sharedMesh = mesh;
			teil.AddComponent<MeshRenderer>().sharedMaterial = material;
			if (fest)
			{
				teil.AddComponent<BoxCollider>();
			}
			return teil;
		}
	}
}
