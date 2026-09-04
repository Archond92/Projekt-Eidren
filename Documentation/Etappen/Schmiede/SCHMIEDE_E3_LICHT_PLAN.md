# Schmiede Etappe 3 — Licht und Nebel: Umsetzungsplan

> **Status: umgesetzt, aber NICHT abgenommen (16.08.2026).** Gebaut sind
> `EidraForgeLicht.cs`, `EidraForgeLichtBuilder.cs` und
> `EidraForgeLichtTests.cs`; die Schmiede hat eine eigene Beleuchtung statt
> der aus `Zone_EmberRuins` geerbten, das Vier-Lichter-Limit je Objekt wird
> eingehalten, die Farbregel ebenfalls. Captures unter
> `TempReview/SchmiedeE3/`.
>
> **Aufgabe 3 (Sichtprüfung und Nachjustierung) ist offen.** Für flüssiges
> Spielen ist das Verlies weiterhin zu dunkel. Ursache und Wirkungskette sind
> geklärt und in `Documentation/PROGRAMMIERBIBEL.md` **§27** festgehalten: die
> Albedowerte waren zweimal falsch, und der zweite Fehler entstand daraus,
> dass eine Messung unter Richtungslicht auf einen beschatteten Raum
> übertragen wurde. Der Feinabgleich der Intensitäten gehört in Version 0.3.

> **Für agentische Bearbeiter:** ERFORDERLICHE UNTER-SKILL: `superpowers:subagent-driven-development` oder `superpowers:executing-plans`. Schritte nutzen Checkbox-Syntax (`- [ ]`).

**Ziel:** Das Verlies bekommt seine Beleuchtung. Danach ist die Schmiede spielbar dunkel statt unspielbar dunkel, und die beiden offenen Sichtfragen aus 2B und 2C lassen sich beantworten.

**Architektur:** Wie bisher — Lichtdaten getrennt vom Bau. `EidraForgeLicht` hält die Punktlichter als Tabelle, `EidraForgeLichtBuilder` setzt sie und die Szenenwerte. Der Renderer erlaubt **vier Zusatzlichter pro Objekt**; das wird zur Testinvariante, nicht zur Hoffnung.

## Globale Vorgaben

- Alle Grundwerte sind aus `ForgeGlowProbe` **gemessen**, nicht geschätzt.
- Umgebungslicht flach **0,055 / 0,06 / 0,08**. Richtungslicht Intensität **0,8**, Farbe **0,72 / 0,78 / 1,0**, rund 30° über dem Horizont aus Westsüdwest.
- Warmlicht der Esse: Punkt, Reichweite **18**, Intensität **3,2**, Farbe **1,0 / 0,5 / 0,16**.
- **Nur das Richtungslicht wirft Schatten** (`m_AdditionalLightShadowsSupported: 0`). Punktlichter geben Farbe, das Richtungslicht gibt Form.
- **Höchstens vier Punktlichter erreichen eine Bodenkachel** (`m_AdditionalLightsPerObjectLimit: 4`). Darüber wechselt die Auswahl pro Objekt und das Bild flackert.
- **Glutflächen brauchen kein eigenes Licht** — in der Probe belegt. Licht wird nur gesetzt, wo die Umgebung mitgefärbt werden soll.
- Höchstens drei Lichtfarben im Bild. **Blau kommt genau einmal vor**: in der Bindungskammer.
- Sichtprüfung im Spielzoom (Kameragröße 7,4), dunkel **und** neutral.
- Vor jedem Unity-Lauf prüfen, ob ein fremder Unity-Prozess läuft.

---

## Aufgabe 1: Lichtdaten und das Vier-Lichter-Limit

**Dateien:** `EidraForgeLicht.cs` anlegen, `EidraForgeLichtTests.cs` anlegen.

**Schnittstellen:** Liefert `EidraForgeLicht.Punktlichter` (`IReadOnlyList<ForgeLicht>`) mit `Id`, `X`, `Z`, `Hoehe`, `Reichweite`, `Intensitaet`, `Farbe`; dazu die Konstanten `Umgebung`, `Richtungsfarbe`, `Richtungsintensitaet`, `Nebeldichte`.

Die entscheidende Invariante:

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
	/// <summary>
	/// Die harte Rendergrenze: m_AdditionalLightsPerObjectLimit steht auf 4.
	/// Erreichen mehr Punktlichter eine Bodenkachel, waehlt Unity pro Objekt neu
	/// aus und das Bild flackert beim Gehen.
	/// </summary>
	[Test]
	public void KeineBodenkachel_WirdVonMehrAlsVierLichternErreicht()
	{
		foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
		{
			foreach (var kachel in EidraForgeGeometryBuilder.Kacheln(flaeche))
			{
				int treffer = 0;
				string namen = string.Empty;
				foreach (ForgeLicht licht in EidraForgeLicht.Punktlichter)
				{
					float abstand = Mathf.Sqrt(
						(licht.X - kachel.x) * (licht.X - kachel.x) +
						(licht.Z - kachel.z) * (licht.Z - kachel.z));
					if (abstand <= licht.Reichweite)
					{
						treffer++;
						namen += licht.Id + " ";
					}
				}
				Assert.That(treffer, Is.LessThanOrEqualTo(4),
					$"Kachel ({kachel.x}|{kachel.z}) wird von {treffer} Lichtern erreicht: {namen}");
			}
		}
	}

	[Test]
	public void JederKampfraum_HatMindestensEineLichtquelle()
	{
		string[] pflicht = { "Windfang", "Balgkammer", "MasselI", "MasselII", "EinbruchB", "RingSued", "Hammerwerk" };
		foreach (string name in pflicht)
		{
			ForgeFlaeche raum = EidraForgeLayout.Flaechen.First(f => f.Name == name);
			bool beleuchtet = EidraForgeLicht.Punktlichter.Any(l =>
				l.X >= raum.MinX - l.Reichweite && l.X <= raum.MaxX + l.Reichweite &&
				l.Z >= raum.MinZ - l.Reichweite && l.Z <= raum.MaxZ + l.Reichweite);
			Assert.That(beleuchtet, Is.True, $"{name} hat keine Lichtquelle - der Raum waere unbespielbar.");
		}
	}
```

- [ ] **Schritt 2: Fehlschlag bestätigen** — erwartet `error CS0246` für `EidraForgeLicht`.

- [ ] **Schritt 3: Lichtdaten anlegen**

Jede Kohlenpfanne aus `EidraForgeProps` bekommt ein Licht. Dazu Lichter für die Räume, die bisher keine Glutquelle haben — in 2B ist aufgefallen, dass die Masselkammer im Verlieslicht praktisch schwarz ist.

```csharp
	public readonly struct ForgeLicht
	{
		public ForgeLicht(string id, float x, float z, float hoehe, float reichweite, float intensitaet, Color farbe)
		{ Id = id; X = x; Z = z; Hoehe = hoehe; Reichweite = reichweite; Intensitaet = intensitaet; Farbe = farbe; }

		public string Id { get; }
		public float X { get; }
		public float Z { get; }
		public float Hoehe { get; }
		public float Reichweite { get; }
		public float Intensitaet { get; }
		public Color Farbe { get; }
	}
```

Farbwerte: Glut `1,0 / 0,5 / 0,16` — Kaltlicht der Bindungskammer `0,55 / 0,68 / 1,0`.

Reichweiten bewusst klein halten: das Vier-Lichter-Limit gilt pro Kachel, und großzügige Reichweiten summieren sich schnell. Die Esse mit Reichweite 18 belegt in der ganzen Essenkammer bereits einen Platz.

- [ ] **Schritt 4: Grün bestätigen.** Meldet der Test eine Kachel mit fünf Lichtern, **Reichweiten senken**, nicht die Schwelle — die 4 ist eine Rendergrenze, keine Stilfrage.

---

## Aufgabe 2: Licht und Nebel setzen

**Dateien:** `EidraForgeLichtBuilder.cs` anlegen, `EidraForgeSceneBuilder.cs` ändern.

- [ ] **Schritt 1: Bauen**

```csharp
		public static void BaueLicht(Transform wurzel)
		{
			foreach (ForgeLicht daten in EidraForgeLicht.Punktlichter)
			{
				GameObject objekt = new GameObject(daten.Id);
				objekt.transform.SetParent(wurzel, worldPositionStays: false);
				EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
				objekt.transform.position = new Vector3(daten.X, boden + daten.Hoehe, daten.Z);
				Light licht = objekt.AddComponent<Light>();
				licht.type = LightType.Point;
				licht.range = daten.Reichweite;
				licht.intensity = daten.Intensitaet;
				licht.color = daten.Farbe;
				licht.shadows = LightShadows.None;
			}
		}
```

`shadows = None` ist ausdrücklich gesetzt, nicht vergessen: Zusatzlichter können im Projekt ohnehin keine Schatten werfen, und ein aktivierter Schalter würde nur suggerieren, dass sie es täten.

- [ ] **Schritt 2: Szenenwerte setzen**

Umgebungslicht, Richtungslicht und Nebel in `ConfigureZone` oder einem eigenen Schritt. Das geerbte `Sun`-Objekt unter `ZoneRoot/Systems` wird dabei auf die Verlieswerte gesetzt, **nicht** gelöscht — es ist die einzige schattenfähige Quelle und trägt die Wandschatten, die seit dem Verzicht auf Decken die Enge erzeugen.

```csharp
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = EidraForgeLicht.Umgebung;
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Linear;
			RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.05f);
			RenderSettings.fogStartDistance = 18f;
			RenderSettings.fogEndDistance = 60f;
```

- [ ] **Schritt 3: Bauen, Strukturtests, Wegtest** — die Beleuchtung darf weder das NavMesh noch die Zonenvalidierung berühren.

---

## Aufgabe 3: Sichtprüfung und Nachjustierung

Hier werden die beiden offenen Punkte aus 2B und 2C beantwortet.

- [ ] **Schritt 1: Masselkammer.** In 2B war sie im Verlieslicht praktisch schwarz — die 5,5 hohe Wand wirft ihren Schatten hinein. Prüfen, ob die neue Lichtquelle das löst. Falls die Masselbetten weiter mit dem Boden verschmelzen: Betten heller absetzen oder Rostkanten ergänzen. Ihr Albedo liegt mit 0,22 nur knapp über dem Boden mit 0,17.

- [ ] **Schritt 2: Schwanzhammer.** In 2C nicht beurteilbar, weil dunkles Eisen auf dunklem Boden. Mit Licht prüfen: liest sich das Gerät als **eine** Maschine, berühren Balken, Hammerkopf und Amboss einander sichtbar? Das war beim Referenzblatt der Hauptfehler.

- [ ] **Schritt 3: Farbregel.** Ein Bild pro Akt ziehen und zählen: höchstens drei Lichtfarben, Blau genau einmal.

- [ ] **Schritt 4: Nebel.** Prüfen, ob er die Kante des orthografischen Ausschnitts verdeckt, ohne den Spielbereich einzutrüben. Start bei 18 ist ein Startwert, kein Messergebnis.

---

## Aufgabe 4: Abnahme

- [ ] Volle EditMode-Suite — Baseline 1091 plus neue Tests.
- [ ] Volle PlayMode-Suite — 120 bestanden, 3 `Explicit` übersprungen.
- [ ] Wegtest gesondert bestätigen.
- [ ] Renderer-Zahl vermerken; nach 2C lag sie bei 864.
- [ ] Captures nach `TempReview/SchmiedeE3/`, dunkel und neutral.

---

## Nicht Teil dieser Etappe

- Der Formwall als Mechanik mit Kosten, Ertrag und Speicherzustand — Etappe 4
- Zusammenfassung statischer Geometrie, falls die Renderer-Zahl über 900 steigt
