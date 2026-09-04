# Schmiede Etappe 2B — Bauteile: Umsetzungsplan

> **Status: umgesetzt und abgenommen (16.08.2026).** Die Checkboxen unten
> stehen im Planungszustand. Nachweis: erweiterte `EidraForgeProps.cs` und
> `EidraForgePropBuilder.cs` (Masselbetten, Formkästen, Schlacke, Stege,
> Geländer aus der Grubenkante), `EidraForgePropTests.cs`, Captures unter
> `TempReview/SchmiedeE2B/`.
>
> Offen geblieben und an Etappe 3 übergeben: die Masselkammer war im
> Verlieslicht praktisch schwarz — die 5,5 hohe Wand wirft ihren Schatten
> hinein. Siehe `Documentation/PROGRAMMIERBIBEL.md` **§27**.

> **Für agentische Bearbeiter:** ERFORDERLICHE UNTER-SKILL: `superpowers:subagent-driven-development` oder `superpowers:executing-plans`. Schritte nutzen Checkbox-Syntax (`- [ ]`).

**Ziel:** Die Räume bekommen die wiederkehrenden Bauteile, die aus leeren Kästen benutzte Werkstätten machen: Masselbetten, Formkästen, Schlackenhaufen, Rinnenstege und das Geländer an der Grubenkante.

**Architektur:** Wie 2A. Neue Platzierungen kommen in `EidraForgeProps`, gebaut wird in `EidraForgePropBuilder`, geprüft ohne Szenenöffnung in `EidraForgePropTests`. Das Geländer wird nicht von Hand platziert, sondern wie die Wände aus der Grubenkante abgeleitet — dann kann es an Treppen und Abstichrinne nicht versehentlich zuwachsen.

**Technik:** Unity EditMode, NUnit, `EidrenMeshFactory`, geteiltes Weltmaterial.

## Globale Vorgaben

- Koordinaten wie bisher. Böden Y=0, Grubenboden Y=−6, Wandhöhe 5,5.
- `TaperedBox` und `Wedge` wachsen von der Basisfläche bei y=0 nach oben.
- **Anbindungen an verjüngte Körper werden auf ihrer tatsächlichen Höhe gerechnet, nicht am Grundriss abgelesen.** In 2A hat derselbe Denkfehler drei Anläufe gekostet: die Esse misst am Fuß 8, auf Rohrhöhe aber nur 3,6.
- **Sichtprüfung nur im Spielzoom** (Kameragröße 7,4). In den Übersichten mit Größe 52 ist alles siebenfach zu klein für ein Urteil.
- Hohe Objekte an Nord- und Ostwände, niedrige nach Südwesten.
- Farben: Stein 0,22 / 0,19 / 0,20 · Eisen 0,15 / 0,13 / 0,14 · Rost 0,42 / 0,12 / 0,025 · Holz 0,29 / 0,21 / 0,14 · Schlacke 0,10 / 0,09 / 0,11.
- `Assets/` ist nicht versioniert — vor dem Ändern bestehender Dateien Kopie ins Scratchpad.
- **Vor jedem Unity-Lauf prüfen, ob ein fremder Unity-Prozess läuft** (`Get-Process Unity`). Ein zweiter Agent arbeitet zeitweise im selben Projekt; zwei Instanzen auf demselben `Library` beschädigen beide. Lockfile prüfen, nicht löschen.
- Der Exit-Code lügt bei Compile-Fehlern — immer das Log auf `error CS` prüfen.

---

## Dateistruktur

| Datei | Verantwortung |
|---|---|
| `Assets/_Game/Editor/EidraForgeProps.cs` (ändern) | Neue Platzierungstabellen: Masselbetten, Formkastenstapel, Schlackenhaufen, Stege. |
| `Assets/_Game/Editor/EidraForgePropBuilder.cs` (ändern) | Bau der fünf Bauteiltypen, Geländer aus der Grubenkante abgeleitet. |
| `Assets/_Game/Editor/Tests/EidraForgePropTests.cs` (ändern) | Invarianten der neuen Platzierungen plus Höhenbudget. |

---

## Aufgabe 1: Platzierungsdaten erweitern

**Dateien:**
- Ändern: `Assets/_Game/Editor/EidraForgeProps.cs`
- Test: `Assets/_Game/Editor/Tests/EidraForgePropTests.cs`

**Schnittstellen:**
- Verbraucht: `EidraForgeLayout.IstAufBoden`, `.Truhen`, `.Gegneranker`, `EidraForgeProps.Kohlenpfannen`.
- Liefert: `EidraForgeProps.Masselbetten`, `.Formkastenstapel`, `.Schlackenhaufen`, `.Rinnenstege` — alle `IReadOnlyList<ForgeAufbau>`. `ForgeAufbau` hat `Id`, `X`, `Z`, `Breite`, `Tiefe`, `Hoehe`, `Drehung` (Grad um Y).

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
	[Test]
	public void AlleAufbauten_StehenAufBodenUndBlockierenNiemanden()
	{
		foreach (ForgeAufbau aufbau in EidraForgeProps.AlleAufbauten)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(aufbau.X, aufbau.Z, out _), Is.True,
				$"{aufbau.Id} steht nicht auf Boden.");
			foreach (ForgePlatz anker in EidraForgeLayout.Gegneranker)
			{
				Assert.That(Abstand(aufbau.X, aufbau.Z, anker.X, anker.Z), Is.GreaterThan(1.2f),
					$"{aufbau.Id} steckt in {anker.Id}.");
			}
			foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
			{
				Assert.That(Abstand(aufbau.X, aufbau.Z, truhe.X, truhe.Z), Is.GreaterThan(1.2f),
					$"{aufbau.Id} steckt in {truhe.Id}.");
			}
			foreach (ForgePlatz pfanne in EidraForgeProps.Kohlenpfannen)
			{
				Assert.That(Abstand(aufbau.X, aufbau.Z, pfanne.X, pfanne.Z), Is.GreaterThan(1.2f),
					$"{aufbau.Id} steckt in {pfanne.Id}.");
			}
		}
	}

	/// <summary>
	/// Kamerabudget: nur die Esse darf die Wandhoehe ueberragen. Alles andere
	/// wuerde bei 52 Grad Blickwinkel die Spielfigur verdecken.
	/// </summary>
	[Test]
	public void KeinAufbau_UeberragtDieWandhoehe()
	{
		foreach (ForgeAufbau aufbau in EidraForgeProps.AlleAufbauten)
		{
			Assert.That(aufbau.Hoehe, Is.LessThanOrEqualTo(EidraForgeLayout.Wandhoehe),
				$"{aufbau.Id} ist hoeher als die Wand.");
		}
	}

	private static float Abstand(float ax, float az, float bx, float bz)
	{
		return Mathf.Sqrt((ax - bx) * (ax - bx) + (az - bz) * (az - bz));
	}
```

- [ ] **Schritt 2: Test laufen lassen und Fehlschlag bestätigen**

Erwartet: `error CS0117` — `AlleAufbauten` existiert nicht.

- [ ] **Schritt 3: Datentyp und Tabellen ergänzen**

```csharp
	/// <summary>Ein stehender Aufbau: Masselbett, Kastenstapel, Schlackenhaufen, Steg.</summary>
	public readonly struct ForgeAufbau
	{
		public ForgeAufbau(string id, float x, float z, float breite, float tiefe, float hoehe, float drehung = 0f)
		{
			Id = id;
			X = x;
			Z = z;
			Breite = breite;
			Tiefe = tiefe;
			Hoehe = hoehe;
			Drehung = drehung;
		}

		public string Id { get; }

		public float X { get; }

		public float Z { get; }

		public float Breite { get; }

		public float Tiefe { get; }

		public float Hoehe { get; }

		public float Drehung { get; }
	}
```

In `EidraForgeProps`:

```csharp
		/* Zwei Reihen je Masselkammer, dazwischen ein Gang. Die Gegneranker
		   stehen auf z=-6 bzw. z=2 - genau in diesem Gang. */
		private static readonly ForgeAufbau[] MasselbettenIntern =
		{
			new ForgeAufbau("forge.prop.massel.1a", -15f, -6.9f, 14f, 1f, 0.4f),
			new ForgeAufbau("forge.prop.massel.1b", -15f, -5.1f, 14f, 1f, 0.4f),
			new ForgeAufbau("forge.prop.massel.2a", -15f, 1.1f, 14f, 1f, 0.4f),
			new ForgeAufbau("forge.prop.massel.2b", -15f, 2.9f, 14f, 1f, 0.4f)
		};

		/* Kastenstapel an den Waenden: in den Masselkammern ordentlich, im
		   Einbruch gekippt (Drehung), weil dort die Formwand geborsten ist. */
		private static readonly ForgeAufbau[] KastenIntern =
		{
			new ForgeAufbau("forge.prop.kasten.01", -23f, -7f, 1.8f, 1.2f, 1.2f),
			new ForgeAufbau("forge.prop.kasten.02", -23f, 3f, 1.8f, 1.2f, 1.8f),
			new ForgeAufbau("forge.prop.kasten.03", -8f, 3f, 1.8f, 1.2f, 0.6f),
			new ForgeAufbau("forge.prop.kasten.04", 7f, -7f, 1.8f, 1.2f, 1.2f, 18f),
			new ForgeAufbau("forge.prop.kasten.05", 26f, 3f, 1.8f, 1.2f, 0.6f, 34f),
			new ForgeAufbau("forge.prop.kasten.06", 7f, 9f, 1.8f, 1.2f, 1.8f, 9f)
		};

		private static readonly ForgeAufbau[] SchlackeIntern =
		{
			new ForgeAufbau("forge.prop.schlacke.01", 6f, -27f, 2.2f, 1.8f, 0.8f),
			new ForgeAufbau("forge.prop.schlacke.02", -22f, -4.5f, 1.6f, 1.4f, 0.6f),
			new ForgeAufbau("forge.prop.schlacke.03", 22f, -7f, 2.4f, 2f, 0.9f),
			new ForgeAufbau("forge.prop.schlacke.04", 19f, 8f, 1.8f, 1.6f, 0.7f),
			new ForgeAufbau("forge.prop.schlacke.05", -18f, 20f, 2f, 1.8f, 0.8f),
			new ForgeAufbau("forge.prop.schlacke.06", 18f, 20f, 1.6f, 1.4f, 0.6f),
			new ForgeAufbau("forge.prop.schlacke.07", -7f, 24f, 2.4f, 2f, 0.9f),
			new ForgeAufbau("forge.prop.schlacke.08", 7f, 40f, 2f, 1.8f, 0.8f),
			new ForgeAufbau("forge.prop.schlacke.09", 34f, 36f, 1.8f, 1.6f, 0.7f),
			new ForgeAufbau("forge.prop.schlacke.10", -34f, 36f, 1.6f, 1.4f, 0.6f)
		};

		/* Stege quer ueber die Rinne auf Hoehe 4,5: das einzige, was ueber dem
		   Kopf liegt, seit die Decken entfallen sind. Sie werfen Streifenschatten,
		   weil das Richtungslicht als einzige Quelle schattenfaehig ist. */
		private static readonly ForgeAufbau[] StegeIntern =
		{
			new ForgeAufbau("forge.prop.steg.01", 0f, -4f, 11f, 0.7f, 0.6f),
			new ForgeAufbau("forge.prop.steg.02", 0f, 4f, 11f, 0.7f, 0.6f),
			new ForgeAufbau("forge.prop.steg.03", 0f, 12f, 11f, 0.7f, 0.6f)
		};

		public static IReadOnlyList<ForgeAufbau> Masselbetten => MasselbettenIntern;

		public static IReadOnlyList<ForgeAufbau> Formkastenstapel => KastenIntern;

		public static IReadOnlyList<ForgeAufbau> Schlackenhaufen => SchlackeIntern;

		public static IReadOnlyList<ForgeAufbau> Rinnenstege => StegeIntern;

		/// <summary>Alle stehenden Aufbauten, fuer die gemeinsamen Invarianten.</summary>
		public static IEnumerable<ForgeAufbau> AlleAufbauten =>
			MasselbettenIntern.Concat(KastenIntern).Concat(SchlackeIntern).Concat(StegeIntern);
```

Dazu `using System.Linq;` im Dateikopf.

**Achtung bei den Stegen:** sie liegen auf Höhe 4,5 und ihre `Hoehe` von 0,6 meint die Balkenstärke, nicht die Höhe über dem Boden. Der Höhenbudget-Test prüft nur die Stärke — das ist beabsichtigt, denn die Stege liegen ohnehin unter der Wandkrone.

- [ ] **Schritt 4: Test laufen lassen und grün bestätigen**

Erwartet: alle bisherigen plus zwei neue Tests grün. Meldet ein Test eine Kollision, die Koordinate verschieben, nicht den Schwellwert senken.

---

## Aufgabe 2: Masselbetten, Kästen, Schlacke und Stege bauen

**Dateien:**
- Ändern: `Assets/_Game/Editor/EidraForgePropBuilder.cs`

**Schnittstellen:**
- Liefert: `EidraForgePropBuilder.BaueAufbauten(Transform wurzel)`.

- [ ] **Schritt 1: Bauen**

```csharp
		private static readonly Color Holz = new Color(0.29f, 0.21f, 0.14f);

		private static readonly Color Schlacke = new Color(0.1f, 0.09f, 0.11f);

		/// <summary>
		/// Baut die wiederkehrenden Bauteile. Masselbetten und Kaesten sind
		/// Rechteck-Lofts, Schlackenhaufen achteckige Lofts mit drei Ringen,
		/// Stege liegen auf Hoehe 4,5 quer ueber der Rinne.
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
					new EidrenMeshFactory.LoftProfile(haufen.Hoehe * 0.55f, haufen.Breite * 0.7f, haufen.Tiefe * 0.75f, 0.15f),
					new EidrenMeshFactory.LoftProfile(haufen.Hoehe, haufen.Breite * 0.25f, haufen.Tiefe * 0.3f, 0.25f)
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
```

- [ ] **Schritt 2: Aufruf in `BuildLayout` ergänzen**

```csharp
EidraForgePropBuilder.BaueAufbauten(environment.transform);
```

- [ ] **Schritt 3: Bauen und im Spielzoom prüfen**

Auf die Masselkammer und den Einbruch zoomen. Prüfen: Liegen die Betten parallel und lassen den Gang frei? Sind die gekippten Kästen im Einbruch als *gekippt* erkennbar, oder wirkt die Drehung nur wie ein Modellierfehler? Werfen die Stege sichtbare Streifenschatten auf den Rinnenboden?

Der letzte Punkt ist der unsicherste: das Richtungslicht steht in der Verliesbeleuchtung nur auf 0,8 Intensität. Bleiben die Schatten unsichtbar, ist das ein Befund für Etappe 3 — dort wird das Licht gesetzt — und **nicht** hier durch stärkeres Licht zu übertünchen.

---

## Aufgabe 3: Geländer aus der Grubenkante ableiten

Das Geländer wird nicht platziert, sondern berechnet — wie die Wände in Etappe 1. Sonst wächst es bei einer Layoutänderung über Treppen und Abstichrinne zu.

**Dateien:**
- Ändern: `Assets/_Game/Editor/EidraForgePropBuilder.cs`
- Test: `Assets/_Game/Editor/Tests/EidraForgePropTests.cs`

**Schnittstellen:**
- Liefert: `EidraForgePropBuilder.Gelaenderkanten()` → `IEnumerable<(float x, float z, float breite, float tiefe)>` und `BaueGelaender(Transform wurzel)`.

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
	[Test]
	public void Gelaender_UmschliesstDieGrubeUndLaesstTreppenFrei()
	{
		var kanten = EidraForgePropBuilder.Gelaenderkanten().ToList();
		Assert.That(kanten, Is.Not.Empty, "Die Grube hat kein Gelaender.");
		foreach (var kante in kanten)
		{
			bool aufTreppeWest = kante.x > -16f && kante.x < -10f && kante.z > 26f && kante.z < 30f;
			bool aufTreppeOst = kante.x > 10f && kante.x < 16f && kante.z > 34f && kante.z < 38f;
			bool aufAbstich = kante.x > -3f && kante.x < 3f && kante.z > 42f;
			Assert.That(aufTreppeWest || aufTreppeOst || aufAbstich, Is.False,
				$"Gelaender versperrt einen Zugang bei ({kante.x}|{kante.z}).");
		}
	}
```

- [ ] **Schritt 2: Fehlschlag bestätigen**

Erwartet: `error CS0117` — `Gelaenderkanten` existiert nicht.

- [ ] **Schritt 3: Ableiten und bauen**

```csharp
		/// <summary>
		/// Leitet das Gelaender aus der Grubenkante ab: entlang des Randes wird in
		/// Schritten von einer Einheit geprueft, ob innen die Grube und aussen die
		/// Galerie liegt. An Treppen und Abstichrinne entfaellt es, weil dort eine
		/// Rampe die Kante ueberbrueckt.
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
				Pruefe(x + 0.5f, grube.MinZ - 0.5f, 1f, 0.25f, ergebnis);
				Pruefe(x + 0.5f, grube.MaxZ + 0.5f, 1f, 0.25f, ergebnis);
			}
			for (float z = grube.MinZ; z < grube.MaxZ - 0.001f; z += 1f)
			{
				Pruefe(grube.MinX - 0.5f, z + 0.5f, 0.25f, 1f, ergebnis);
				Pruefe(grube.MaxX + 0.5f, z + 0.5f, 0.25f, 1f, ergebnis);
			}
			return ergebnis;
		}

		private static void Pruefe(float x, float z, float breite, float tiefe,
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

		/// <summary>Gelaender: Handlauf auf 1,1 mit Pfosten alle zwei Einheiten.</summary>
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
```

- [ ] **Schritt 4: Aufruf ergänzen und grün bestätigen**

```csharp
EidraForgePropBuilder.BaueGelaender(environment.transform);
```

- [ ] **Schritt 5: Spielzoom-Sichtprüfung an der Grubenkante**

Prüfen: Läuft das Geländer ringsum und bricht an beiden Treppen und der Abstichrinne sauber ab? Verdeckt es aus 52 Grad Blickwinkel die Esse? Falls ja, Handlaufhöhe von 1,1 auf 0,8 senken — **nicht** die Kamera ändern.

---

## Aufgabe 4: Abnahme

- [ ] **Schritt 1: Volle EditMode-Suite** — Baseline 1078 plus die neuen Tests, keine Fehlschläge.
- [ ] **Schritt 2: Volle PlayMode-Suite** — 120 bestanden, 3 `Explicit` übersprungen. Bei `CopperVein_Hold225SecondsCollectsExactlyOnce` den bekannten Aussetzer prüfen: Lauf wiederholen, bevor er als Regression gilt.
- [ ] **Schritt 3: Objektzahl vermerken.** Nach 2A lag die Szene bei rund 700 Renderern. 2B fügt grob 40 Aufbauten und etwa 120 Geländerstücke hinzu. Über 900 im Bericht vermerken und für Etappe 3 eine Zusammenfassung statischer Geometrie vorschlagen.
- [ ] **Schritt 4: Captures nach `TempReview/SchmiedeE2B/`**, jeweils Übersicht **und** Spielzoom.

---

## Nicht Teil dieser Etappe

- Blasebalg, Düsenstein, Erzkarren, Schwanzhammer, Bindungsreife, Gichtkübel, Einbruch-Gruppe mit Deckentrümmern und Formwall-Bauplatz — Etappe 2C
- Punktlichter, Nebel, Umgebungslicht — Etappe 3
- Der Formwall als Mechanik — Etappe 4
