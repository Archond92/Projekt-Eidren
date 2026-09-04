# Schmiede Etappe 2A — Leitmotiv und Feuer: Umsetzungsplan

> **Status: umgesetzt und abgenommen (16.08.2026).** Die Checkboxen unten
> stehen im Planungszustand. Nachweis: `EidraForgeProps.cs`,
> `EidraForgePropBuilder.cs`, `EidraForgePropTests.cs`, Glut-Material und
> Shader `Eidren/World/VertexGlow`, Captures unter `TempReview/SchmiedeE2A/`.
> Windrohr, Esse, Glutadern und Kohlenpfannen stehen in der Szene.
>
> Eingearbeitet in `Documentation/PROGRAMMIERBIBEL.md` **M10.11** und
> **M14.1**. Offen geblieben: die Glutadern lesen sich als gleichmäßig breite
> Leuchtstreifen statt als Glut in Rissen.

> **Für agentische Bearbeiter:** ERFORDERLICHE UNTER-SKILL: `superpowers:subagent-driven-development` oder `superpowers:executing-plans`, um diesen Plan Aufgabe für Aufgabe umzusetzen. Die Schritte nutzen Checkbox-Syntax (`- [ ]`).

**Ziel:** Die Schmiede bekommt ihr verbindendes Objekt und ihre Lichtquellen: ein Windrohr, das von der Balgkammer bis in die Esse durchläuft, die Esse selbst, und die selbstleuchtenden Glutflächen. Danach ist der Raum als Schmiede erkennbar.

**Architektur:** Wie in Etappe 1 werden Daten und Bau getrennt. `EidraForgeProps` hält Platzierungen und den Windrohrverlauf als reine Daten ohne Szenenbezug; `EidraForgePropBuilder` übersetzt sie in Profilstapel. Dadurch sind Kollisionen, Bodenhaftung und die Durchgängigkeit des Rohrs ohne Szenenöffnung prüfbar.

**Technik:** Unity EditMode, NUnit, `EidrenMeshFactory` (Loft mit Oct/Rect, TaperedBox), die Materialien aus `EidrenWorldStyleAssets` — beleuchtet für Baukörper, unbeleuchtet für Glut.

## Globale Vorgaben

- Koordinaten wie in Etappe 1: X quer, Z Süd→Nord, Y Höhe. Böden bei Y=0, Grubenboden bei Y=−6.
- `TaperedBox` und `Wedge` wachsen von der Basisfläche bei y=0 nach oben, sie sind **nicht** zentriert.
- Windrohr auf Höhe **3,5**, Durchmesser **1,2**.
- Esse: achteckig, Fuß 8 breit, Gesamthöhe **10**, Profilringe und Bänder wie in `SCHMIEDE_ENTWURF.md` Abschnitt 8 — die Werte sind durch `ForgeGlowProbe` belegt.
- Selbstleuchtende Flächen bekommen `EidrenWorldStyleAssets.EnsureGlowMaterial()` und werden mit **`kontaktAo: false`** gebaut, sonst bleicht die Kontaktabdunklung die Glut aus.
- Glutfarbe **1,0 / 0,42 / 0,06**. Steinfarbe **0,22 / 0,19 / 0,20**, Rost **0,42 / 0,12 / 0,025**.
- Hohe Objekte gehören an Nord- und Ostwände (Kamera blickt aus Südwesten), niedrige nach Südwesten.
- `Assets/` ist nicht versioniert. Vor dem Ändern bestehender Dateien eine Kopie ins Scratchpad legen.
- Testlauf über den mitgelieferten Editor; **der Exit-Code lügt bei Compile-Fehlern** — immer das Log auf `error CS` prüfen.

---

## Dateistruktur

| Datei | Verantwortung |
|---|---|
| `Assets/_Game/Editor/EidraForgeProps.cs` (neu) | Platzierungsdaten und Windrohrverlauf. Kein Szenenbezug. |
| `Assets/_Game/Editor/Tests/EidraForgePropTests.cs` (neu) | Invarianten der Platzierungen. |
| `Assets/_Game/Editor/EidraForgePropBuilder.cs` (neu) | Baut Windrohr, Esse, Kohlenpfannen und Glutadern. |
| `Assets/_Game/Editor/EidraForgeSceneBuilder.cs` (ändern) | Ruft den Prop-Builder in `BuildLayout` auf. |

---

## Aufgabe 1: Platzierungsdaten und Invarianten

**Dateien:**
- Anlegen: `Assets/_Game/Editor/EidraForgeProps.cs`
- Test: `Assets/_Game/Editor/Tests/EidraForgePropTests.cs`

**Schnittstellen:**
- Verbraucht: `EidraForgeLayout.IstAufBoden`, `EidraForgeLayout.Truhen`, `EidraForgeLayout.Gegneranker`.
- Liefert: `EidraForgeProps.Windrohrverlauf` (`IReadOnlyList<Vector2>`, Punkte in X/Z), `EidraForgeProps.Kohlenpfannen` (`IReadOnlyList<ForgePlatz>`), `EidraForgeProps.Glutadern` (`IReadOnlyList<ForgeGlutader>`), `EidraForgeProps.Rohrhoehe = 3.5f`, `EidraForgeProps.EssenFuss` (Vector2, Mittelpunkt der Esse). `ForgeGlutader` hat `X`, `Z`, `Laenge`, `EntlangZ`.

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
using Eidren.Editor;
using NUnit.Framework;
using System.Linq;
using UnityEngine;

public sealed class EidraForgePropTests
{
	[Test]
	public void Windrohr_LaeuftDurchgehendUndAchsenparallel()
	{
		var verlauf = EidraForgeProps.Windrohrverlauf;
		Assert.That(verlauf.Count, Is.GreaterThanOrEqualTo(2));
		for (int index = 1; index < verlauf.Count; index++)
		{
			Vector2 a = verlauf[index - 1];
			Vector2 b = verlauf[index];
			bool nurX = Mathf.Abs(a.y - b.y) < 0.01f && Mathf.Abs(a.x - b.x) > 0.01f;
			bool nurZ = Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) > 0.01f;
			Assert.That(nurX || nurZ, Is.True,
				$"Abschnitt {index} von ({a.x}|{a.y}) nach ({b.x}|{b.y}) ist nicht achsenparallel.");
		}
	}

	[Test]
	public void Windrohr_BeginntInDerBalgkammerUndEndetAnDerEsse()
	{
		Vector2 anfang = EidraForgeProps.Windrohrverlauf.First();
		Vector2 ende = EidraForgeProps.Windrohrverlauf.Last();
		ForgeFlaeche balgkammer = EidraForgeLayout.Flaechen.First(f => f.Name == "Balgkammer");
		Assert.That(anfang.x, Is.InRange(balgkammer.MinX, balgkammer.MaxX));
		Assert.That(anfang.y, Is.InRange(balgkammer.MinZ, balgkammer.MaxZ));
		Assert.That(Vector2.Distance(ende, EidraForgeProps.EssenFuss), Is.LessThan(5f),
			"Das Rohr muss an der Esse enden.");
	}

	[Test]
	public void Kohlenpfannen_StehenAufBodenUndNichtInWegenAnderer()
	{
		foreach (ForgePlatz pfanne in EidraForgeProps.Kohlenpfannen)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(pfanne.X, pfanne.Z, out _), Is.True,
				$"Kohlenpfanne {pfanne.Id} steht nicht auf Boden.");
			foreach (ForgePlatz anker in EidraForgeLayout.Gegneranker)
			{
				Assert.That(Abstand(pfanne, anker), Is.GreaterThan(1.5f), $"{pfanne.Id} steckt in {anker.Id}.");
			}
			foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
			{
				Assert.That(Abstand(pfanne, truhe), Is.GreaterThan(1.5f), $"{pfanne.Id} steckt in {truhe.Id}.");
			}
		}
	}

	[Test]
	public void Glutadern_LiegenAufBoden()
	{
		foreach (ForgeGlutader ader in EidraForgeProps.Glutadern)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(ader.X, ader.Z, out _), Is.True,
				$"Glutader bei ({ader.X}|{ader.Z}) liegt nicht auf Boden.");
			float halb = ader.Laenge * 0.5f;
			float endeX = ader.EntlangZ ? ader.X : ader.X + halb;
			float endeZ = ader.EntlangZ ? ader.Z + halb : ader.Z;
			Assert.That(EidraForgeLayout.IstAufBoden(endeX, endeZ, out _), Is.True,
				$"Glutader bei ({ader.X}|{ader.Z}) ragt ueber die Bodenkante hinaus.");
		}
	}

	private static float Abstand(ForgePlatz a, ForgePlatz b)
	{
		return Mathf.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
	}
}
```

- [ ] **Schritt 2: Test laufen lassen und Fehlschlag bestätigen**

```bash
.unity-editor/Editor/Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -testFilter ".*EidraForgePropTests.*" -testResults "%TEMP%\props.xml" -logFile "%TEMP%\props.log"
```

Erwartet: `error CS0246: The type or namespace name 'EidraForgeProps' could not be found`.

- [ ] **Schritt 3: Platzierungsdaten anlegen**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>Eine glühende Ader im Bodenriss.</summary>
	public readonly struct ForgeGlutader
	{
		public ForgeGlutader(float x, float z, float laenge, bool entlangZ)
		{
			X = x;
			Z = z;
			Laenge = laenge;
			EntlangZ = entlangZ;
		}

		public float X { get; }

		public float Z { get; }

		public float Laenge { get; }

		public bool EntlangZ { get; }
	}

	/// <summary>
	/// Platzierung der Requisiten der Schmiede als reine Daten
	/// (SCHMIEDE_ENTWURF.md Abschnitt 8).
	/// </summary>
	public static class EidraForgeProps
	{
		public const float Rohrhoehe = 3.5f;

		public const float Rohrdurchmesser = 1.2f;

		/// <summary>Mittelpunkt der Esse auf dem Grubenboden.</summary>
		public static readonly Vector2 EssenFuss = new Vector2(0f, 32f);

		/* Das Rohr beginnt am Blasebalg in der Balgkammer, zieht auf halber
		   Westseite durch Duese und Rinne nach Norden, schwenkt auf der
		   Galerie zur Mitte und endet an der Suedflanke der Esse. */
		private static readonly Vector2[] VerlaufIntern =
		{
			new Vector2(-6f, -24f),
			new Vector2(-2.5f, -24f),
			new Vector2(-2.5f, 20f),
			new Vector2(0f, 20f),
			new Vector2(0f, 28f)
		};

		private static readonly ForgePlatz[] PfannenIntern =
		{
			new ForgePlatz("forge.prop.pfanne.01", 5f, -26f),
			new ForgePlatz("forge.prop.pfanne.02", 3f, 6f),
			new ForgePlatz("forge.prop.pfanne.03", -16f, 18f),
			new ForgePlatz("forge.prop.pfanne.04", 17f, 36f),
			new ForgePlatz("forge.prop.pfanne.05", 24f, 0f),
			new ForgePlatz("forge.prop.pfanne.06", 24f, 28f)
		};

		/* Der erstarrte Auslauf im Einbruch verzweigt; in der Rinne liegt das
		   Rinnenmetall, in der Grube die Reste der Abstiche. */
		private static readonly ForgeGlutader[] AdernIntern =
		{
			new ForgeGlutader(10f, -4f, 6f, entlangZ: true),
			new ForgeGlutader(14f, -1f, 5f, entlangZ: false),
			new ForgeGlutader(18f, 2f, 4f, entlangZ: true),
			new ForgeGlutader(12f, 5f, 3f, entlangZ: false),
			new ForgeGlutader(0f, -6f, 6f, entlangZ: true),
			new ForgeGlutader(0f, 8f, 5f, entlangZ: true),
			new ForgeGlutader(-6f, 26f, 4f, entlangZ: true),
			new ForgeGlutader(6f, 36f, 4f, entlangZ: true)
		};

		public static IReadOnlyList<Vector2> Windrohrverlauf => VerlaufIntern;

		public static IReadOnlyList<ForgePlatz> Kohlenpfannen => PfannenIntern;

		public static IReadOnlyList<ForgeGlutader> Glutadern => AdernIntern;
	}
}
```

- [ ] **Schritt 4: Test laufen lassen und grün bestätigen**

Erwartet: `total="4" passed="4" failed="0"`. Meldet ein Test eine Kollision oder eine Ader über der Bodenkante, die betroffene Koordinate verschieben — **nicht** den Test lockern.

- [ ] **Schritt 5: Sicherungskopie und Festhalten**

```bash
cp Assets/_Game/Editor/EidraForgeProps.cs "$SCRATCH/backup/"
```

---

## Aufgabe 2: Das Windrohr

**Dateien:**
- Anlegen: `Assets/_Game/Editor/EidraForgePropBuilder.cs`

**Schnittstellen:**
- Verbraucht: `EidraForgeProps.Windrohrverlauf`, `EidraForgeProps.Rohrhoehe`, `EidraForgeProps.Rohrdurchmesser`.
- Liefert: `EidraForgePropBuilder.BaueWindrohr(Transform wurzel)`.

Jeder Abschnitt entsteht als achteckiger Loft zwischen zwei Verlaufspunkten, dazu alle acht Einheiten eine Wandkonsole als `TaperedBox`.

- [ ] **Schritt 1: Builder anlegen**

```csharp
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>Baut die Requisiten der Schmiede aus den Daten in EidraForgeProps.</summary>
	public static class EidraForgePropBuilder
	{
		private static readonly Color Stein = new Color(0.22f, 0.19f, 0.2f);

		private static readonly Color Rost = new Color(0.42f, 0.12f, 0.025f);

		private static readonly Color Glut = new Color(1f, 0.42f, 0.06f);

		private static readonly Color GlutMitte = new Color(0.75f, 0.3f, 0.04f);

		private static readonly Color GlutTief = new Color(0.55f, 0.21f, 0.03f);

		/// <summary>
		/// Das Windrohr ist das Leitmotiv: ein Objekt, das in jedem Raum
		/// wiederkehrt und immer zum Feuer zeigt. Jeder Abschnitt ist ein
		/// achteckiger Loft zwischen zwei Verlaufspunkten.
		/// </summary>
		public static void BaueWindrohr(Transform wurzel)
		{
			Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
			var verlauf = EidraForgeProps.Windrohrverlauf;
			float radius = EidraForgeProps.Rohrdurchmesser;
			for (int index = 1; index < verlauf.Count; index++)
			{
				Vector2 a = verlauf[index - 1];
				Vector2 b = verlauf[index];
				float laenge = Vector2.Distance(a, b);
				bool entlangZ = Mathf.Abs(b.y - a.y) > 0.01f;
				Vector2 mitte = (a + b) * 0.5f;
				EidrenMeshFactory.LoftProfile[] rohr =
				{
					new EidrenMeshFactory.LoftProfile(0f, radius, radius),
					new EidrenMeshFactory.LoftProfile(laenge, radius, radius)
				};
				GameObject teil = new GameObject($"Windrohr_{index}");
				teil.transform.SetParent(wurzel, worldPositionStays: false);
				teil.transform.position = new Vector3(a.x, EidraForgeProps.Rohrhoehe, a.y);
				/* Der Loft waechst entlang +Y; die Drehung legt ihn waagerecht in
				   die Verlaufsrichtung. */
				teil.transform.rotation = entlangZ
					? Quaternion.Euler(b.y > a.y ? -90f : 90f, 0f, 0f)
					: Quaternion.Euler(0f, 0f, b.x > a.x ? -90f : 90f);
				teil.AddComponent<MeshFilter>().sharedMesh = EidrenMeshFactory.Loft(rohr,
					EidrenMeshFactory.LoftShape.Oct, Stein, capBottom: false, capTop: false, kontaktAo: false);
				teil.AddComponent<MeshRenderer>().sharedMaterial = welt;

				int konsolen = Mathf.Max(1, Mathf.RoundToInt(laenge / 8f));
				for (int nummer = 0; nummer <= konsolen; nummer++)
				{
					Vector2 punkt = Vector2.Lerp(a, b, (float)nummer / konsolen);
					Konsole(wurzel, $"Rohrbock_{index}_{nummer}", punkt, welt);
				}
			}
		}

		private static void Konsole(Transform wurzel, string name, Vector2 punkt, Material material)
		{
			GameObject teil = new GameObject(name);
			teil.transform.SetParent(wurzel, worldPositionStays: false);
			teil.transform.position = new Vector3(punkt.x, EidraForgeProps.Rohrhoehe - 1.2f, punkt.y);
			teil.AddComponent<MeshFilter>().sharedMesh =
				EidrenMeshFactory.TaperedBox(new Vector3(0.4f, 1.2f, 0.4f), 0.6f, Rost, kontaktAo: false);
			teil.AddComponent<MeshRenderer>().sharedMaterial = material;
		}
	}
}
```

- [ ] **Schritt 2: In den Szenenbau einhängen**

In `EidraForgeSceneBuilder.BuildLayout`, direkt nach den Geometrieaufrufen:

```csharp
EidraForgePropBuilder.BaueWindrohr(environment.transform);
```

- [ ] **Schritt 3: Szene bauen und Log prüfen**

```bash
.unity-editor/Editor/Unity.exe -batchmode -quit -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.EidraForgeSceneBuilder.Build -logFile "%TEMP%\schmiede-2a.log"
```

Erwartet: `Eidren: built the hand-authored Eidra Forge scene.` und keine `error CS`.

- [ ] **Schritt 4: Sichtprüfung**

`Eidren/V0.2/Schmiede/Grundriss-Captures` laufen lassen und prüfen: Läuft das Rohr ohne Unterbrechung von der Balgkammer bis zur Esse? Hängt es überall auf Höhe 3,5? Sitzen die Konsolen an der Wand und nicht in der Luft über dem Raum?

Der letzte Punkt ist der wahrscheinlichste Fehler: der Verlauf führt bei x=−2,5 mitten durch Düse und Rinne, dort gibt es keine Wand zum Anlehnen. Falls die Konsolen frei stehen, den Verlauf näher an die Westwand ziehen — Rinne ist x −4…4, also x=−3,4 statt −2,5 — und die Düse bleibt bei x=−2,5, weil sie nur 6 breit ist.

---

## Aufgabe 3: Die Esse

**Dateien:**
- Ändern: `Assets/_Game/Editor/EidraForgePropBuilder.cs`

**Schnittstellen:**
- Verbraucht: `EidraForgeProps.EssenFuss`, `EidraForgeLayout.Grubentiefe`.
- Liefert: `EidraForgePropBuilder.BaueEsse(Transform wurzel)`.

Die Profilwerte sind aus `ForgeGlowProbe` übernommen und dort im Bild belegt.

- [ ] **Schritt 1: Esse bauen**

```csharp
/// <summary>
/// Die Esse: sieben Profilringe, drei Eisenbaender mit 0,3 Ueberstand, eine
/// Deckplatte. Hoehe 10 statt der urspruenglich geplanten 6 - bei 52 Grad
/// Kamerawinkel verdeckt der nahe Grubenrand eine buendig mit der Galerie
/// abschliessende Oeffnung.
/// </summary>
public static void BaueEsse(Transform wurzel)
{
	Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
	Material leucht = EidrenWorldStyleAssets.EnsureGlowMaterial();
	Vector3 fuss = new Vector3(EidraForgeProps.EssenFuss.x, EidraForgeLayout.Grubentiefe, EidraForgeProps.EssenFuss.y);
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

	/* Feueroeffnung auf der kamerazugewandten Suedflanke. Rahmen beleuchtet,
	   Glutflaeche selbstleuchtend mit kontaktAo:false. */
	Teil(wurzel, "Esse_OeffnungRahmen",
		EidrenMeshFactory.TaperedBox(new Vector3(3.4f, 3.2f, 0.6f), 1f, Rost),
		fuss + new Vector3(0f, 6f, -2.75f), welt);
	Teil(wurzel, "Esse_OeffnungGlut",
		EidrenMeshFactory.TaperedBox(new Vector3(2.6f, 2.4f, 0.2f), 1f, Glut, kontaktAo: false),
		fuss + new Vector3(0f, 6f, -3f), leucht);
}

private static void Band(Transform wurzel, string name, Vector3 fuss, float hoehe, float breite, Material material)
{
	EidrenMeshFactory.LoftProfile[] ring =
	{
		new EidrenMeshFactory.LoftProfile(0f, breite, breite),
		new EidrenMeshFactory.LoftProfile(0.35f, breite, breite)
	};
	Teil(wurzel, name, EidrenMeshFactory.Loft(ring, EidrenMeshFactory.LoftShape.Oct, Rost,
		capBottom: false, capTop: false), fuss + new Vector3(0f, hoehe, 0f), material);
}

private static void Teil(Transform wurzel, string name, Mesh mesh, Vector3 position, Material material)
{
	GameObject teil = new GameObject(name);
	teil.transform.SetParent(wurzel, worldPositionStays: false);
	teil.transform.position = position;
	teil.AddComponent<MeshFilter>().sharedMesh = mesh;
	teil.AddComponent<MeshRenderer>().sharedMaterial = material;
}
```

- [ ] **Schritt 2: Aufruf ergänzen**

In `BuildLayout`, nach `BaueWindrohr`:

```csharp
EidraForgePropBuilder.BaueEsse(environment.transform);
```

- [ ] **Schritt 3: Bauen, Log prüfen, Sichtprüfung**

Erwartet: Die Esse steht in der Grube, ihre Oberkante ragt vier Einheiten über die Galerie, und die Feueröffnung ist von der Galerie aus sichtbar. Falls die Öffnung vom Grubenrand verdeckt wird, ist die Essenhöhe zu niedrig — nicht die Kamera anpassen.

---

## Aufgabe 4: Glutadern und Kohlenpfannen

**Dateien:**
- Ändern: `Assets/_Game/Editor/EidraForgePropBuilder.cs`

**Schnittstellen:**
- Verbraucht: `EidraForgeProps.Glutadern`, `EidraForgeProps.Kohlenpfannen`.
- Liefert: `EidraForgePropBuilder.BaueGlut(Transform wurzel)`.

Die Glut-Probe hat gezeigt, dass selbstleuchtende Flächen **ohne eigenes Punktlicht** tragen. Licht kommt erst in Etappe 3 und nur dort, wo die Umgebung mitgefärbt werden soll.

Sie hat aber auch gezeigt, dass gleichmäßig breite Vollton-Streifen wie Leuchtröhren aussehen. Deshalb wird jede Ader aus drei Segmenten mit gestaffelter Helligkeit gebaut und in eine dunklere Vertiefung gesetzt.

- [ ] **Schritt 1: Glut bauen**

```csharp
/// <summary>
/// Glutadern und Kohlenpfannen. Jede Ader entsteht aus drei Segmenten mit
/// gestaffelter Helligkeit in einer dunkleren Vertiefung - ein einzelner
/// Vollton-Streifen liest sich als Leuchtroehre, nicht als Glut (Befund aus
/// ForgeGlowProbe).
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
		/* Vertiefung: dunkler Rahmen, damit die Ader im Boden liegt statt darauf. */
		Vector3 rahmenGroesse = ader.EntlangZ
			? new Vector3(0.9f, 0.1f, ader.Laenge + 0.4f)
			: new Vector3(ader.Laenge + 0.4f, 0.1f, 0.9f);
		Teil(wurzel, $"Glutriss_{nummer}", EidrenMeshFactory.TaperedBox(rahmenGroesse, 1f,
			new Color(0.08f, 0.06f, 0.06f)), new Vector3(ader.X, hoehe, ader.Z), welt);

		for (int stufe = 0; stufe < 3; stufe++)
		{
			float versatz = (stufe - 1) * teilLaenge;
			Vector3 position = ader.EntlangZ
				? new Vector3(ader.X, hoehe + 0.02f, ader.Z + versatz)
				: new Vector3(ader.X + versatz, hoehe + 0.02f, ader.Z);
			Vector3 groesse = ader.EntlangZ
				? new Vector3(0.35f + stufe * 0.05f, 0.12f, teilLaenge)
				: new Vector3(teilLaenge, 0.12f, 0.35f + stufe * 0.05f);
			Teil(wurzel, $"Glutader_{nummer}_{stufe}",
				EidrenMeshFactory.TaperedBox(groesse, 1f, stufen[stufe], kontaktAo: false),
				position, leucht);
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
			fuss + new Vector3(0f, 0.98f, 0f), leucht);
	}
}
```

- [ ] **Schritt 2: Aufruf ergänzen**

```csharp
EidraForgePropBuilder.BaueGlut(environment.transform);
```

- [ ] **Schritt 3: Bauen und Sichtprüfung mit Verliesbeleuchtung**

Die normale Grundriss-Sichtprüfung ist neutral beleuchtet und zeigt nicht, ob die Glut trägt. Für diese Aufgabe stattdessen eine dunkle Ansicht abziehen: in `ForgeLayoutCapture` vorübergehend `ambientLight` auf `0,055 / 0,06 / 0,08` und die Lichtintensität auf `0,8` setzen — die Werte aus `ForgeGlowProbe` — und prüfen, ob die Adern als Glut lesbar sind und nicht als Leuchtstreifen.

---

## Aufgabe 5: Abnahme

- [ ] **Schritt 1: Volle EditMode-Suite**

Erwartet: keine Fehlschläge. Baseline dieser Etappe: **1078 / 1078**.

- [ ] **Schritt 2: Volle PlayMode-Suite**

Erwartet: **120 / 120**, 3 `Explicit`-Tests übersprungen. Schlägt `CopperVein_Hold225SecondsCollectsExactlyOnce` fehl, ist das der bekannte Aussetzer aus Etappe 1 — Lauf wiederholen, bevor er als Regression gewertet wird.

- [ ] **Schritt 3: Objektzahl prüfen**

Die Szene hatte nach Etappe 1 rund 580 Renderer. Kommen mehr als etwa 900 zusammen, im Bericht vermerken — die Bodenkacheln und Wandsegmente sind statisch und lassen sich zusammenfassen, aber das gehört dann in eine eigene Aufgabe.

- [ ] **Schritt 4: Captures ablegen**

Nach `TempReview/SchmiedeE2A/`, nicht nach `Temp/`.

---

## Nicht Teil dieser Etappe

- Blasebalg, Düsenstein, Erzkarren, Schwanzhammer, Bindungsringe, Geländer, Gichtkübel, Masselbetten, Rinnenstege, Schlackenhaufen, Formkästen, Deckentrümmer, Formwall-Bauplatz — Etappe 2B
- Punktlichter, Nebel, Umgebungslicht — Etappe 3
- Der Formwall als Mechanik — Etappe 4
