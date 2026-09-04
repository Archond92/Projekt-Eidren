# Schmiede Etappe 2C — Einzelstücke: Umsetzungsplan

> **Status: umgesetzt und abgenommen (16.08.2026).** Die Checkboxen unten
> stehen im Planungszustand. Nachweis: Blasebalg, Düsenstein, Erzkarren,
> Schwanzhammer, Bindungsreife, Gichtkübel, Trümmer, geborstener Formwall und
> Bauplatz in `EidraForgePropBuilder.cs`; Captures unter
> `TempReview/SchmiedeE2C/`.
>
> Offen geblieben und an Etappe 3 übergeben: der Schwanzhammer war nicht
> beurteilbar, weil dunkles Eisen auf dunklem Boden stand. Vier Objekte
> (Geländer, Deckenbalken, Bindungsreife, Bauplatz) sind ohne Bauvorlage
> entstanden und kommen nur in den Stimmungsbildern vor.

> **Für agentische Bearbeiter:** ERFORDERLICHE UNTER-SKILL: `superpowers:subagent-driven-development` oder `superpowers:executing-plans`. Schritte nutzen Checkbox-Syntax (`- [ ]`).

**Ziel:** Jeder Raum bekommt sein Wahrzeichen — das Objekt, an dem man ihn wiedererkennt. Danach ist die Ausstattung der Schmiede vollständig und Etappe 3 kann Licht setzen.

**Architektur:** Wie 2A und 2B. Daten in `EidraForgeProps`, Bau in `EidraForgePropBuilder`, Invarianten in `EidraForgePropTests`.

## Globale Vorgaben

- Koordinaten wie bisher. Böden Y=0, Grubenboden Y=−6, Wandhöhe 5,5.
- `TaperedBox` und `Wedge` wachsen von der Basisfläche bei y=0 nach oben.
- **Anbindungen an verjüngte Körper auf ihrer tatsächlichen Höhe rechnen**, nicht am Grundriss ablesen.
- **Sichtprüfung nur im Spielzoom** (Kameragröße 7,4). Für Formprüfung die neutrale Ansicht (`Run`), für Stimmung die dunkle (`RunDunkel`).
- Nur die Esse darf die Wandhöhe überragen — der Höhenbudget-Test hält das fest.
- Farben: Stein 0,22/0,19/0,20 · Eisen 0,15/0,13/0,14 · Rost 0,42/0,12/0,025 · Holz 0,29/0,21/0,14 · Leder 0,20/0,13/0,10 · Schlacke 0,10/0,09/0,11.
- **Vor jedem Unity-Lauf prüfen, ob ein fremder Unity-Prozess läuft.** Ein zweiter Agent arbeitet zeitweise im selben Projekt.
- Der Exit-Code lügt bei Compile-Fehlern — Log auf `error CS` prüfen.

---

## Aufgabe 1: Platzierungsdaten und die Blasebalg-Anbindung

Der Blasebalg ist 8 lang, die Balgkammer nur 14 breit und 8 tief. Er passt nur **längs zur Z-Achse**. Damit verschiebt sich sein Stutzen an die Nordkante der Kammer — und der Windrohrverlauf muss dort beginnen, statt wie bisher bei (−6 | −24).

**Dateien:** `EidraForgeProps.cs` ändern, `EidraForgePropTests.cs` ändern.

**Schnittstellen:** Liefert `EidraForgeProps.Einzelstuecke` (`IReadOnlyList<ForgeAufbau>`) und einen geänderten `Windrohrverlauf`.

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
	[Test]
	public void Windrohr_BeginntAmBlasebalgStutzen()
	{
		ForgeAufbau balg = EidraForgeProps.Einzelstuecke.First(e => e.Id == "forge.prop.blasebalg");
		Vector2 anfang = EidraForgeProps.Windrohrverlauf.First();
		Assert.That(Mathf.Abs(anfang.x - balg.X), Is.LessThan(1f),
			"Das Rohr muss auf der Achse des Blasebalgs beginnen.");
		Assert.That(anfang.y, Is.GreaterThan(balg.Z),
			"Der Stutzen zeigt nach Norden, das Rohr muss dort weiterlaufen.");
	}

	[Test]
	public void Einzelstuecke_StehenAufBodenUndBlockierenNiemanden()
	{
		foreach (ForgeAufbau stueck in EidraForgeProps.Einzelstuecke)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(stueck.X, stueck.Z, out _), Is.True,
				$"{stueck.Id} steht nicht auf Boden.");
			foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
			{
				Assert.That(Punktabstand(stueck.X, stueck.Z, truhe.X, truhe.Z), Is.GreaterThan(1.5f),
					$"{stueck.Id} steckt in {truhe.Id}.");
			}
		}
	}
```

- [ ] **Schritt 2: Fehlschlag bestätigen** — erwartet `error CS0117` für `Einzelstuecke`.

- [ ] **Schritt 3: Daten ergänzen**

```csharp
		/* Einzelstuecke: je Raum das Objekt, an dem man ihn wiedererkennt.
		   Hohe Stuecke stehen an Nord- und Ostwaenden, damit sie bei 52 Grad
		   Kulisse sind und nicht die Spielfigur verdecken. */
		private static readonly ForgeAufbau[] EinzelstueckeIntern =
		{
			new ForgeAufbau("forge.prop.blasebalg", -4.5f, -24f, 3.5f, 8f, 3f),
			new ForgeAufbau("forge.prop.duesenstein", 0f, -15f, 6f, 2.5f, 4f),
			new ForgeAufbau("forge.prop.erzkarren", -8f, -32f, 2.2f, 1.4f, 1.4f, 22f),
			new ForgeAufbau("forge.prop.schwanzhammer", 25f, 29f, 3f, 5f, 4f),
			new ForgeAufbau("forge.prop.reif.01", -33f, 29f, 2.5f, 0.4f, 2.5f),
			new ForgeAufbau("forge.prop.reif.02", -33f, 32f, 2.5f, 0.4f, 2.5f),
			new ForgeAufbau("forge.prop.reif.03", -33f, 35f, 2.5f, 0.4f, 2.5f),
			new ForgeAufbau("forge.prop.gichtkuebel.west", -12f, 30f, 1.4f, 1.4f, 1.6f),
			new ForgeAufbau("forge.prop.gichtkuebel.ost", 12f, 38f, 1.4f, 1.4f, 1.6f),
			new ForgeAufbau("forge.prop.formwall", 20f, -5f, 8f, 1.2f, 2.2f, 12f),
			new ForgeAufbau("forge.prop.bauplatz", 16f, 6f, 4f, 3f, 0.3f)
		};

		public static IReadOnlyList<ForgeAufbau> Einzelstuecke => EinzelstueckeIntern;
```

`AlleAufbauten` um `.Concat(EinzelstueckeIntern)` erweitern, damit Höhenbudget und Kollisionsprüfung auch hier greifen.

Der Windrohrverlauf beginnt neu:

```csharp
			/* Am Stutzen des Blasebalgs an der Nordkante der Balgkammer. */
			new Vector2(-4.5f, -20f),
			new Vector2(-2.5f, -20f),
```

statt der bisherigen ersten beiden Punkte.

- [ ] **Schritt 4: Grün bestätigen.** Meldet der Höhenbudget-Test den Schwanzhammer oder den Blasebalg, die Höhe senken — nicht die Schwelle.

---

## Aufgabe 2: Akt 1 — Blasebalg, Düsenstein, Erzkarren

**Dateien:** `EidraForgePropBuilder.cs` ändern.

**Schnittstellen:** Liefert `BaueEinzelstuecke(Transform wurzel)`.

- [ ] **Schritt 1: Blasebalg bauen**

Kein Kasten, sondern ein flacher Keil: hinten hoch und breit, nach vorn auf den Stutzen zulaufend, eine Hälfte eingesackt.

```csharp
		private static readonly Color Leder = new Color(0.2f, 0.13f, 0.1f);

		private static void BaueBlasebalg(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			Vector3 fuss = new Vector3(daten.X, boden, daten.Z - daten.Tiefe * 0.5f);
			/* Balgkoerper: Loft von breit-hoch nach schmal-flach entlang +Z. Die
			   Profilringe laufen in Z, deshalb bleibt der Loft aufrecht und wird
			   um -90 Grad um X gekippt. */
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

			/* Deckplatte, auf einer Seite abgesackt: die andere Haelfte steht noch. */
			GameObject platte = Teil(wurzel, daten.Id + "_Deckplatte",
				EidrenMeshFactory.TaperedBox(new Vector3(daten.Breite * 0.95f, 0.35f, daten.Tiefe * 0.6f), 0.8f, Holz),
				fuss + new Vector3(0f, daten.Hoehe * 0.85f, daten.Tiefe * 0.3f), welt);
			platte.transform.rotation = Quaternion.Euler(0f, 0f, 9f);

			/* Stutzen am Nordende, auf Rohrhoehe. */
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
```

- [ ] **Schritt 2: Düsenstein bauen**

Der Gang führt hindurch, also vier Blöcke um eine Öffnung statt eines Quaders mit Loch — die Mesh-Fabrik kennt keine Boolean-Operationen.

```csharp
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
```

**Wichtig:** die Öffnung von 2,5 muss begehbar bleiben. Nach dem Bau das NavMesh prüfen — schlägt `ZoneStructureTests` fehl oder ist der Gang zu, die Öffnung auf 3 erhöhen.

- [ ] **Schritt 3: Erzkarren bauen** — Rechteckkasten auf zwei achteckigen Rädern, ein drittes Rad daneben liegend.

```csharp
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
				EidrenMeshFactory.LoftProfile[] rad =
				{
					new EidrenMeshFactory.LoftProfile(0f, 0.9f, 0.9f),
					new EidrenMeshFactory.LoftProfile(0.18f, 0.9f, 0.9f)
				};
				GameObject teil = Teil(wurzel, $"{daten.Id}_Rad{seite}", EidrenMeshFactory.Loft(rad,
					EidrenMeshFactory.LoftShape.Oct, Eisen, capBottom: true, capTop: true), 
					fuss + new Vector3(seite * daten.Breite * 0.5f, -0.45f, 0f), welt);
				teil.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
			}
			EidrenMeshFactory.LoftProfile[] loses =
			{
				new EidrenMeshFactory.LoftProfile(0f, 0.9f, 0.9f),
				new EidrenMeshFactory.LoftProfile(0.18f, 0.9f, 0.9f)
			};
			Teil(wurzel, daten.Id + "_RadAb", EidrenMeshFactory.Loft(loses,
				EidrenMeshFactory.LoftShape.Oct, Eisen, capBottom: true, capTop: true),
				new Vector3(daten.X + 1.8f, boden, daten.Z - 1.2f), welt);
		}
```

- [ ] **Schritt 4: Bauen, Strukturtests, Sichtprüfung neutral im Spielzoom** auf Balgkammer und Düse.

---

## Aufgabe 3: Akt 3 — Schwanzhammer, Bindungsreife, Gichtkübel

- [ ] **Schritt 1: Schwanzhammer** — ein zusammenhängendes Gerät: zwei Ständer, Querriegel, gelagerter Balken, Hammerkopf unten auf dem Amboss. Alle Teile berühren einander; das war im Referenzblatt der Hauptfehler.

```csharp
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
				EidrenMeshFactory.TaperedBox(new Vector3(3.1f, 0.4f, 0.5f), 1f, Holz),
				fuss + new Vector3(0f, daten.Hoehe - 0.4f, 1.4f), welt);
			/* Balken im Schlag: vorn abgesenkt, hinten schraeg hoch. */
			GameObject balken = Teil(wurzel, daten.Id + "_Balken",
				EidrenMeshFactory.TaperedBox(new Vector3(0.55f, 0.55f, 5.4f), 1f, Holz, kontaktAo: false),
				fuss + new Vector3(0f, daten.Hoehe - 0.9f, 1.4f), welt);
			balken.transform.rotation = Quaternion.Euler(-16f, 0f, 0f);
			Teil(wurzel, daten.Id + "_Lager",
				EidrenMeshFactory.TaperedBox(new Vector3(0.7f, 0.7f, 0.7f), 1f, Eisen),
				fuss + new Vector3(0f, daten.Hoehe - 1.1f, 1.4f), welt);
			Teil(wurzel, daten.Id + "_Hammerkopf",
				EidrenMeshFactory.TaperedBox(new Vector3(0.9f, 1.1f, 0.9f), 1.1f, Eisen),
				fuss + new Vector3(0f, 0.9f, -1.1f), welt);
			Teil(wurzel, daten.Id + "_Amboss",
				EidrenMeshFactory.TaperedBox(new Vector3(1.1f, 0.55f, 1.1f), 0.8f, Eisen),
				fuss + new Vector3(0f, 0.35f, -1.1f), welt);
			Teil(wurzel, daten.Id + "_Klotz",
				EidrenMeshFactory.TaperedBox(new Vector3(1.3f, 0.35f, 1.3f), 0.95f, Holz),
				fuss + new Vector3(0f, 0f, -1.1f), welt);
		}
```

- [ ] **Schritt 2: Bindungsreife** — achteckige Eisenreife, hochkant im Boden. Acht Segmente je Reif, weil die Fabrik keinen Torus kennt.

```csharp
		private static void BaueReif(Transform wurzel, ForgeAufbau daten, Material welt)
		{
			EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
			float radius = daten.Breite * 0.5f;
			for (int ecke = 0; ecke < 8; ecke++)
			{
				float winkel = ecke * 45f;
				float bogen = winkel * Mathf.Deg2Rad;
				Vector3 versatz = new Vector3(Mathf.Sin(bogen) * radius, radius + Mathf.Cos(bogen) * radius, 0f);
				GameObject teil = Teil(wurzel, $"{daten.Id}_S{ecke}",
					EidrenMeshFactory.TaperedBox(new Vector3(0.22f, radius * 0.78f, 0.3f), 1f, Eisen,
						kontaktAo: false),
					new Vector3(daten.X + versatz.x, boden + versatz.y, daten.Z), welt);
				teil.transform.rotation = Quaternion.Euler(0f, 0f, -winkel);
			}
		}
```

- [ ] **Schritt 3: Gichtkübel** — Ausleger von der Galerie, Kübel frei über der Grube.

```csharp
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
```

- [ ] **Schritt 4: Bauen und im Spielzoom prüfen.** Der Gichtkübel ist der riskanteste: ragt er wirklich frei über die Grube, oder schwebt er neben der Kante? Kante liegt bei x = ±10.

---

## Aufgabe 4: Der Einbruch — Trümmer, Formwall, Bauplatz

- [ ] **Schritt 1: Deckentrümmer** — sechs kantige Blöcke, unterschiedlich groß und gedreht, als Oct-Lofts mit Versatz. Sie brechen die Sichtlinien; das ist ihr eigentlicher Zweck.

```csharp
		private static readonly (float x, float z, float groesse, float drehung)[] TruemmerIntern =
		{
			(9f, -6.5f, 2.6f, 12f), (16f, -4f, 3.2f, 48f), (21f, -1f, 2.2f, 71f),
			(11f, 2f, 2.8f, 26f), (24f, 3f, 2.4f, 55f), (16f, 8f, 3f, 8f)
		};
```

Bau als drei Profilringe mit `offsetX`/`offsetZ`, damit die Blöcke schief liegen statt gestapelt zu wirken.

- [ ] **Schritt 2: Geborstener Formwall** — eine Reihe Formkästen, in der Mitte auseinandergerissen: linke Hälfte steht, rechte gekippt. Position aus `Einzelstuecke`.

- [ ] **Schritt 3: Bauplatz** — abgesteckter Grund 4 × 3 mit vier Gerüststangen an den Ecken und einem leeren Materialgestell. **Noch ohne Funktion** — die Mechanik kommt in Etappe 4. Hier entsteht nur das Objekt, damit der Raum vollständig ist.

- [ ] **Schritt 4: Bauen und im Spielzoom prüfen**, ob die Trümmer tatsächlich Sichtlinien brechen: aus dem Zoom auf (14 | 0) heraus dürfen nicht alle vier Ecken des Einbruchs gleichzeitig einsehbar sein.

---

## Aufgabe 5: Abnahme

- [ ] **Schritt 1: Volle EditMode-Suite** — Baseline 1088 plus neue Tests, keine Fehlschläge.
- [ ] **Schritt 2: Volle PlayMode-Suite** — 120 bestanden, 3 `Explicit` übersprungen.
- [ ] **Schritt 3: `ZoneStructureTests` gesondert prüfen** — der Düsenstein steht im Gang, das NavMesh muss weiter durchgehen.
- [ ] **Schritt 4: Renderer-Zahl.** Nach 2B lag die Szene bei 802. Über 900 im Bericht vermerken und für Etappe 3 eine Zusammenfassung statischer Geometrie vorschlagen.
- [ ] **Schritt 5: Captures nach `TempReview/SchmiedeE2C/`**, neutral **und** dunkel, jeweils im Spielzoom.

---

## Nicht Teil dieser Etappe

- Punktlichter, Nebel, Umgebungslicht — Etappe 3
- Der Formwall als Mechanik mit Kosten, Ertrag und Speicherzustand — Etappe 4
