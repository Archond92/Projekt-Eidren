# Schmiede Etappe 1 — Geometrie: Umsetzungsplan

> **Status: umgesetzt und abgenommen (16.08.2026).** Die Checkboxen unten
> stehen im Planungszustand; sie wurden während der Umsetzung nicht
> mitgeführt. Der Nachweis liegt im Code und in den Läufen, nicht in dieser
> Liste: `EidraForgeLayout.cs`, `EidraForgeGeometryBuilder.cs`, umgebauter
> `EidraForgeSceneBuilder.cs`, Tests in `EidraForgeLayoutTests.cs` und
> `EidraForgeSceneTests.cs` (einschließlich Wegtest Windfang → Grube),
> Captures unter `TempReview/SchmiedeE1/`.
>
> Ergebnisse sind in `Documentation/PROGRAMMIERBIBEL.md` **M14.1** und **§26**
> eingearbeitet, sichtbare Änderungen im `CHANGELOG.md`. Sicherung des
> Vorzustands: `Backups/2026-08-16-schmiede-neubau/`.

> **Für agentische Bearbeiter:** ERFORDERLICHE UNTER-SKILL: `superpowers:subagent-driven-development` (empfohlen) oder `superpowers:executing-plans`, um diesen Plan Aufgabe für Aufgabe umzusetzen. Die Schritte nutzen Checkbox-Syntax (`- [ ]`) zur Verfolgung.

**Ziel:** Der Grundriss der Verlassenen Eidra-Schmiede aus `SCHMIEDE_ENTWURF.md` steht als begehbare Szene — Flächen, Wände, Spawns, Truhen- und Gegnerplätze — und die vier Fehler des alten Builders sind durch Tests dauerhaft ausgeschlossen.

**Architektur:** Das Layout wird von der Szenenerzeugung getrennt. `EidraForgeLayout` ist eine reine Datenklasse ohne Unity-Szenenbezug: eine Tabelle von Rechteckflächen plus Truhen- und Gegnerplätzen, dazu Abfragen wie `IstAufBoden`. Dadurch lassen sich die Layout-Invarianten in schnellen EditMode-Tests prüfen, ohne eine Szene zu öffnen. `EidraForgeGeometryBuilder` übersetzt diese Daten in Bodenkacheln und Wände, `EidraForgeSceneBuilder` orchestriert nur noch.

**Technik:** Unity EditMode, NUnit, `EidrenMeshFactory` (Loft/TaperedBox), URP mit dem geteilten Vertexfarben-Material. Tests laufen über den mitgelieferten Editor unter `.unity-editor/`.

## Globale Vorgaben

- Koordinatensystem: X quer, Z von Süd (Eingang) nach Nord (Boss), Y die Höhe. Gesamtausdehnung X −36…36, Z −38…50.
- Wandhöhe **5,5**; Grubenboden bei **Y = −6**; alle übrigen Böden bei **Y = 0**.
- Bodenkachelgröße **4 × 4** — Pflicht, weil `m_AdditionalLightsPerObjectLimit: 4` pro Objekt gilt.
- `TaperedBox` und `Wedge` wachsen von der Basisfläche bei y=0 nach oben, sie sind **nicht** zentriert.
- Gegnerzahl bleibt **24** (12 EmberEater, 6 AshRunner, 4 ForgeGuardian, 1 Siegelwächter, 1 Kernwächter). `EidraForgePopulationRules`, `EidraForgeBalance` und `EidraForgeContainerRules` werden **nicht** angefasst.
- Truhenverteilung bleibt 3 Supply, 2 Optional, 1 Elite, 1 Completion.
- Kein `git`-Schutz auf `Assets/` — vor dem Ändern bestehender Dateien eine Kopie ins Scratchpad legen.
- Testlauf: `.unity-editor/Editor/Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath "<Projekt>" -testFilter "<Filter>" -testResults "<xml>" -logFile "<log>"`. **Der Exit-Code lügt bei Compile-Fehlern** — immer das Log auf `error CS` prüfen.

---

## Dateistruktur

| Datei | Verantwortung |
|---|---|
| `Assets/_Game/Editor/EidraForgeLayout.cs` (neu) | Reine Layoutdaten: Flächen, Truhenplätze, Gegnerplätze, Abfragen. Kein Szenenbezug. |
| `Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs` (neu) | Invarianten des Layouts. Schnell, ohne Szene. |
| `Assets/_Game/Editor/EidraForgeGeometryBuilder.cs` (neu) | Böden als 4×4-Kacheln, Wände inklusive Stirnwänden. |
| `Assets/_Game/Editor/EidraForgeSceneBuilder.cs` (ändern) | Nur noch Orchestrierung: Szene öffnen, Zone konfigurieren, Geometrie und Plätze anstoßen, NavMesh backen, speichern. |

---

## Aufgabe 1: Layoutdaten und ihre Invarianten

Das Herzstück. Alle vier Fehler des alten Builders werden hier als Test festgeschrieben, bevor eine einzige Fläche gebaut wird.

**Dateien:**
- Anlegen: `Assets/_Game/Editor/EidraForgeLayout.cs`
- Test: `Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs`

**Schnittstellen:**
- Verbraucht: nichts.
- Liefert: `EidraForgeLayout.Flaechen` (`IReadOnlyList<ForgeFlaeche>`), `EidraForgeLayout.Truhen` und `EidraForgeLayout.Gegneranker` (beide `IReadOnlyList<ForgePlatz>`), `EidraForgeLayout.IstAufBoden(float x, float z, out float hoehe)`, die Konstanten `Wandhoehe = 5.5f`, `Grubentiefe = -6f`, `Kachelgroesse = 4f`. `ForgeFlaeche` hat die Felder `Name`, `MinX`, `MaxX`, `MinZ`, `MaxZ`, `Hoehe`. `ForgePlatz` hat `Id`, `X`, `Z`.

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
using Eidren.Editor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class EidraForgeLayoutTests
{
	[Test]
	public void AlleTruhen_LiegenAufBoden()
	{
		foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(truhe.X, truhe.Z, out _), Is.True,
				$"Truhe {truhe.Id} liegt bei ({truhe.X}|{truhe.Z}) nicht auf Boden.");
		}
	}

	[Test]
	public void AlleGegneranker_LiegenAufBoden()
	{
		foreach (ForgePlatz anker in EidraForgeLayout.Gegneranker)
		{
			Assert.That(EidraForgeLayout.IstAufBoden(anker.X, anker.Z, out _), Is.True,
				$"Anker {anker.Id} liegt bei ({anker.X}|{anker.Z}) nicht auf Boden.");
		}
	}

	[Test]
	public void KeineZweiGegneranker_TeilenDieselbePosition()
	{
		List<ForgePlatz> anker = EidraForgeLayout.Gegneranker.ToList();
		for (int i = 0; i < anker.Count; i++)
		{
			for (int j = i + 1; j < anker.Count; j++)
			{
				float abstand = Mathf.Sqrt(
					(anker[i].X - anker[j].X) * (anker[i].X - anker[j].X) +
					(anker[i].Z - anker[j].Z) * (anker[i].Z - anker[j].Z));
				Assert.That(abstand, Is.GreaterThan(1.5f),
					$"{anker[i].Id} und {anker[j].Id} stehen ineinander.");
			}
		}
	}

	[Test]
	public void Gegneranker_HabenDieKanonischeZusammensetzung()
	{
		Assert.That(EidraForgeLayout.Gegneranker.Count, Is.EqualTo(24));
		Assert.That(Zaehle("ember_eater"), Is.EqualTo(12));
		Assert.That(Zaehle("ash_runner"), Is.EqualTo(6));
		Assert.That(Zaehle("forge_guardian"), Is.EqualTo(4));
		Assert.That(Zaehle("seal_guardian"), Is.EqualTo(1));
		Assert.That(Zaehle("core_guardian"), Is.EqualTo(1));
	}

	private static int Zaehle(string stamm)
	{
		return EidraForgeLayout.Gegneranker.Count(platz => platz.Id.Contains(stamm));
	}

	[Test]
	public void AlleFlaechen_SindVomWindfangErreichbar()
	{
		HashSet<string> erreicht = new HashSet<string> { "Windfang" };
		bool gewachsen = true;
		while (gewachsen)
		{
			gewachsen = false;
			foreach (ForgeFlaeche kandidat in EidraForgeLayout.Flaechen)
			{
				if (erreicht.Contains(kandidat.Name))
				{
					continue;
				}
				foreach (ForgeFlaeche offen in EidraForgeLayout.Flaechen.Where(f => erreicht.Contains(f.Name)))
				{
					if (Beruehren(kandidat, offen))
					{
						erreicht.Add(kandidat.Name);
						gewachsen = true;
						break;
					}
				}
			}
		}
		string[] fehlend = EidraForgeLayout.Flaechen
			.Where(f => !erreicht.Contains(f.Name))
			.Select(f => f.Name)
			.ToArray();
		Assert.That(fehlend, Is.Empty, "Nicht erreichbar: " + string.Join(", ", fehlend));
	}

	private static bool Beruehren(ForgeFlaeche a, ForgeFlaeche b)
	{
		bool xUeberlappt = a.MinX <= b.MaxX + 0.01f && b.MinX <= a.MaxX + 0.01f;
		bool zUeberlappt = a.MinZ <= b.MaxZ + 0.01f && b.MinZ <= a.MaxZ + 0.01f;
		return xUeberlappt && zUeberlappt;
	}
}
```

- [ ] **Schritt 2: Test laufen lassen und Fehlschlag bestätigen**

```bash
.unity-editor/Editor/Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -testFilter ".*EidraForgeLayoutTests.*" -testResults "%TEMP%\layout.xml" -logFile "%TEMP%\layout.log"
```

Erwartet: Compile-Fehler `error CS0246: The type or namespace name 'EidraForgeLayout' could not be found` im Log. Das ist der gewünschte Fehlschlag.

- [ ] **Schritt 3: Layoutdaten anlegen**

```csharp
using System.Collections.Generic;

namespace Eidren.Editor
{
	/// <summary>Eine rechteckige Bodenflaeche des Dungeons.</summary>
	public readonly struct ForgeFlaeche
	{
		public ForgeFlaeche(string name, float minX, float maxX, float minZ, float maxZ, float hoehe = 0f)
		{
			Name = name;
			MinX = minX;
			MaxX = maxX;
			MinZ = minZ;
			MaxZ = maxZ;
			Hoehe = hoehe;
		}

		public string Name { get; }
		public float MinX { get; }
		public float MaxX { get; }
		public float MinZ { get; }
		public float MaxZ { get; }
		public float Hoehe { get; }
	}

	/// <summary>Ein benannter Platz auf dem Boden: Truhe oder Gegneranker.</summary>
	public readonly struct ForgePlatz
	{
		public ForgePlatz(string id, float x, float z)
		{
			Id = id;
			X = x;
			Z = z;
		}

		public string Id { get; }
		public float X { get; }
		public float Z { get; }
	}

	/// <summary>
	/// Grundriss der Verlassenen Eidra-Schmiede als reine Daten, ohne Szenenbezug
	/// (SCHMIEDE_ENTWURF.md Abschnitt 3). Getrennt vom Bau, damit die Invarianten
	/// ohne Szenenoeffnung pruefbar sind.
	/// </summary>
	public static class EidraForgeLayout
	{
		public const float Wandhoehe = 5.5f;

		public const float Grubentiefe = -6f;

		public const float Kachelgroesse = 4f;

		private static readonly ForgeFlaeche[] FlaechenIntern =
		{
			// Akt 1 - Blasebalg
			new ForgeFlaeche("Windfang", -10f, 10f, -38f, -30f),
			new ForgeFlaeche("Schlund", -5f, 5f, -30f, -28f),
			new ForgeFlaeche("Balgkammer", -7f, 7f, -28f, -20f),
			new ForgeFlaeche("Duese", -3f, 3f, -20f, -10f),
			// Akt 2 - Giesshalle
			new ForgeFlaeche("Rinne", -4f, 4f, -10f, 14f),
			new ForgeFlaeche("MasselI", -24f, -6f, -8f, -4f),
			new ForgeFlaeche("MasselII", -24f, -6f, 0f, 4f),
			new ForgeFlaeche("Steg", -24f, -20f, -4f, 0f),
			new ForgeFlaeche("VerbinderMasselI", -6f, -4f, -7f, -5f),
			new ForgeFlaeche("VerbinderMasselII", -6f, -4f, 1f, 3f),
			new ForgeFlaeche("VerbinderMasselRing", -20f, -16f, 4f, 14f),
			new ForgeFlaeche("EinbruchA", 5f, 24f, -8f, -2f),
			new ForgeFlaeche("EinbruchB", 8f, 28f, -2f, 4f),
			new ForgeFlaeche("EinbruchC", 5f, 20f, 4f, 10f),
			new ForgeFlaeche("VerbinderEinbruch", 4f, 6f, -6f, 0f),
			new ForgeFlaeche("VerbinderEinbruchRing", 12f, 18f, 10f, 14f),
			// Akt 3 - Esse
			new ForgeFlaeche("RingSued", -20f, 20f, 14f, 22f),
			new ForgeFlaeche("RingWest", -20f, -10f, 22f, 42f),
			new ForgeFlaeche("RingOst", 10f, 20f, 22f, 42f),
			new ForgeFlaeche("RingNord", -20f, 20f, 42f, 50f),
			new ForgeFlaeche("Essenkern", -10f, 10f, 22f, 42f, Grubentiefe),
			new ForgeFlaeche("TreppeWest", -16f, -10f, 26f, 30f),
			new ForgeFlaeche("TreppeOst", 10f, 16f, 34f, 38f),
			new ForgeFlaeche("Abstichrinne", -3f, 3f, 42f, 48f),
			new ForgeFlaeche("Hammerwerk", 22f, 36f, 26f, 38f),
			new ForgeFlaeche("VerbinderHammerwerk", 20f, 22f, 30f, 34f),
			new ForgeFlaeche("Bindungskammer", -36f, -22f, 26f, 38f),
			new ForgeFlaeche("VerbinderBindungskammer", -22f, -20f, 30f, 34f)
		};

		private static readonly ForgePlatz[] TruhenIntern =
		{
			new ForgePlatz("forge.reward.small", -5f, -34f),
			new ForgePlatz("forge.reward.medium", 0f, -34f),
			new ForgePlatz("forge.reward.large", 5f, -34f),
			new ForgePlatz("forge.recovery", 8f, -32f),
			new ForgePlatz("forge.supply.01", -22f, -6f),
			new ForgePlatz("forge.supply.02", -22f, 2f),
			new ForgePlatz("forge.supply.03", -15f, 38f),
			new ForgePlatz("forge.optional.01", -26f, 30f),
			new ForgePlatz("forge.optional.02", 15f, 26f),
			new ForgePlatz("forge.elite.01", 30f, 34f),
			new ForgePlatz("forge.completion.01", 8f, 47f)
		};

		private static readonly ForgePlatz[] GegnerankerIntern =
		{
			new ForgePlatz("forge.enemy.ember_eater.01", -3f, -25f),
			new ForgePlatz("forge.enemy.ember_eater.02", 3f, -23f),
			new ForgePlatz("forge.enemy.ember_eater.03", -17f, -6f),
			new ForgePlatz("forge.enemy.ember_eater.04", -13f, -6f),
			new ForgePlatz("forge.enemy.ember_eater.05", -9f, -6f),
			new ForgePlatz("forge.enemy.ember_eater.06", -17f, 2f),
			new ForgePlatz("forge.enemy.ember_eater.07", -13f, 2f),
			new ForgePlatz("forge.enemy.ember_eater.08", 12f, -6f),
			new ForgePlatz("forge.enemy.ember_eater.09", 20f, 2f),
			new ForgePlatz("forge.enemy.ember_eater.10", 12f, 7f),
			new ForgePlatz("forge.enemy.ember_eater.11", -14f, 18f),
			new ForgePlatz("forge.enemy.ember_eater.12", 14f, 18f),
			new ForgePlatz("forge.enemy.ash_runner.01", 0f, -17f),
			new ForgePlatz("forge.enemy.ash_runner.02", 0f, -13f),
			new ForgePlatz("forge.enemy.ash_runner.03", 10f, -5f),
			new ForgePlatz("forge.enemy.ash_runner.04", 18f, -5f),
			new ForgePlatz("forge.enemy.ash_runner.05", 14f, 1f),
			new ForgePlatz("forge.enemy.ash_runner.06", 22f, 1f),
			new ForgePlatz("forge.enemy.forge_guardian.01", 0f, -2f),
			new ForgePlatz("forge.enemy.forge_guardian.02", 0f, 9f),
			new ForgePlatz("forge.enemy.forge_guardian.03", -9f, 2f),
			new ForgePlatz("forge.enemy.forge_guardian.04", 15f, 32f),
			new ForgePlatz("forge.enemy.seal_guardian.01", 29f, 32f),
			new ForgePlatz("forge.enemy.core_guardian.01", 0f, 32f)
		};

		public static IReadOnlyList<ForgeFlaeche> Flaechen => FlaechenIntern;

		public static IReadOnlyList<ForgePlatz> Truhen => TruhenIntern;

		public static IReadOnlyList<ForgePlatz> Gegneranker => GegnerankerIntern;

		/// <summary>Liegt der Punkt auf einer Bodenflaeche? Liefert deren Hoehe mit.</summary>
		public static bool IstAufBoden(float x, float z, out float hoehe)
		{
			foreach (ForgeFlaeche flaeche in FlaechenIntern)
			{
				if (x >= flaeche.MinX && x <= flaeche.MaxX && z >= flaeche.MinZ && z <= flaeche.MaxZ)
				{
					hoehe = flaeche.Hoehe;
					return true;
				}
			}
			hoehe = 0f;
			return false;
		}
	}
}
```

- [ ] **Schritt 4: Test laufen lassen und grün bestätigen**

Denselben Befehl wie in Schritt 2. Erwartet: `total="5" passed="5" failed="0"`. Log auf `error CS` prüfen — ohne Ausgabe ist der Lauf sauber.

Falls `AlleGegneranker_LiegenAufBoden` fehlschlägt: die gemeldete Koordinate gegen die Flächentabelle halten und den Anker verschieben, **nicht** den Test lockern. Genau dieser Fall war der alte Fehler `forge.optional.02`.

- [ ] **Schritt 5: Festschreiben**

```bash
git add Assets/_Game/Editor/EidraForgeLayout.cs Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs
git commit -m "feat(schmiede): Layoutdaten der Schmiede mit Invariantentests"
```

---

## Aufgabe 2: Bodenkacheln aus dem Layout

**Dateien:**
- Anlegen: `Assets/_Game/Editor/EidraForgeGeometryBuilder.cs`
- Test: `Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs` (erweitern)

**Schnittstellen:**
- Verbraucht: `EidraForgeLayout.Flaechen`, `EidraForgeLayout.Kachelgroesse`.
- Liefert: `EidraForgeGeometryBuilder.Kacheln(ForgeFlaeche flaeche)` → `IEnumerable<(float x, float z, float breite, float tiefe, float hoehe)>` und `EidraForgeGeometryBuilder.BaueBoeden(Transform wurzel)`.

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
	[Test]
	public void Kacheln_DeckenDieFlaecheVollstaendigUndUeberschreitenSieNicht()
	{
		ForgeFlaeche windfang = EidraForgeLayout.Flaechen.First(f => f.Name == "Windfang");
		var kacheln = EidraForgeGeometryBuilder.Kacheln(windfang).ToList();
		float summe = kacheln.Sum(k => k.breite * k.tiefe);
		float soll = (windfang.MaxX - windfang.MinX) * (windfang.MaxZ - windfang.MinZ);
		Assert.That(summe, Is.EqualTo(soll).Within(0.01f), "Kachelflaeche weicht von der Sollflaeche ab.");
		foreach (var kachel in kacheln)
		{
			Assert.That(kachel.breite, Is.LessThanOrEqualTo(EidraForgeLayout.Kachelgroesse + 0.01f));
			Assert.That(kachel.tiefe, Is.LessThanOrEqualTo(EidraForgeLayout.Kachelgroesse + 0.01f));
		}
	}

	[Test]
	public void JedeFlaeche_ErzeugtMindestensEineKachel()
	{
		foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
		{
			Assert.That(EidraForgeGeometryBuilder.Kacheln(flaeche).Any(), Is.True, flaeche.Name);
		}
	}
```

- [ ] **Schritt 2: Test laufen lassen und Fehlschlag bestätigen**

Erwartet: `error CS0246` für `EidraForgeGeometryBuilder`.

- [ ] **Schritt 3: Kachelaufteilung umsetzen**

Der Dateikopf lautet:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>Baut Boeden und Waende der Schmiede aus den Daten in EidraForgeLayout.</summary>
	public static class EidraForgeGeometryBuilder
	{
```

Darin dann:

```csharp
/// <summary>
/// Zerlegt eine Flaeche in Kacheln von hoechstens Kachelgroesse Kantenlaenge.
/// Randkacheln werden gestutzt, damit die Summe exakt der Flaeche entspricht.
/// Noetig, weil das Zusatzlicht-Limit von vier pro Objekt gilt - ein Raumboden
/// als einzelner Quader bekaeme vier Lichter fuer den ganzen Raum.
/// </summary>
public static IEnumerable<(float x, float z, float breite, float tiefe, float hoehe)> Kacheln(ForgeFlaeche flaeche)
{
	for (float x = flaeche.MinX; x < flaeche.MaxX - 0.001f; x += EidraForgeLayout.Kachelgroesse)
	{
		float breite = Mathf.Min(EidraForgeLayout.Kachelgroesse, flaeche.MaxX - x);
		for (float z = flaeche.MinZ; z < flaeche.MaxZ - 0.001f; z += EidraForgeLayout.Kachelgroesse)
		{
			float tiefe = Mathf.Min(EidraForgeLayout.Kachelgroesse, flaeche.MaxZ - z);
			yield return (x + breite * 0.5f, z + tiefe * 0.5f, breite, tiefe, flaeche.Hoehe);
		}
	}
}
```

- [ ] **Schritt 4: Test laufen lassen und grün bestätigen**

Erwartet: `total="7" passed="7" failed="0"`.

- [ ] **Schritt 5: Bodenbau ergänzen**

```csharp
/// <summary>Legt alle Bodenkacheln unter der Wurzel an. Steinalbedo 0,22 laut
/// SCHMIEDE_ENTWURF.md Abschnitt 9 - der alte Wert 0,12 war nicht lesbar.</summary>
public static void BaueBoeden(Transform wurzel)
{
	Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
	Color bodenfarbe = new Color(0.17f, 0.15f, 0.16f);
	foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
	{
		foreach (var kachel in Kacheln(flaeche))
		{
			GameObject teil = new GameObject($"Boden_{flaeche.Name}_{kachel.x:0.#}_{kachel.z:0.#}");
			teil.transform.SetParent(wurzel, worldPositionStays: false);
			teil.transform.position = new Vector3(kachel.x, kachel.hoehe - 0.4f, kachel.z);
			teil.AddComponent<MeshFilter>().sharedMesh =
				EidrenMeshFactory.TaperedBox(new Vector3(kachel.breite, 0.4f, kachel.tiefe), 1f, bodenfarbe);
			teil.AddComponent<MeshRenderer>().sharedMaterial = welt;
			teil.AddComponent<BoxCollider>();
		}
	}
}
```

- [ ] **Schritt 6: Festschreiben**

```bash
git add Assets/_Game/Editor/EidraForgeGeometryBuilder.cs Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs
git commit -m "feat(schmiede): Boeden als 4x4-Kacheln aus den Layoutdaten"
```

---

## Aufgabe 3: Wände mit Stirnwänden

Der alte Builder erzeugt nur linke und rechte Wände; Nord- und Südseiten sind offen. Bei 5,5 Metern Höhe fällt das sofort auf.

**Dateien:**
- Ändern: `Assets/_Game/Editor/EidraForgeGeometryBuilder.cs`
- Test: `Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs` (erweitern)

**Schnittstellen:**
- Verbraucht: `EidraForgeLayout.Flaechen`, `EidraForgeLayout.Wandhoehe`, `EidraForgeLayout.IstAufBoden`.
- Liefert: `EidraForgeGeometryBuilder.Wandkanten()` → `IEnumerable<(float x, float z, float breite, float tiefe)>` und `EidraForgeGeometryBuilder.BaueWaende(Transform wurzel)`.

Eine Wand entsteht überall dort, wo eine Flächenkante **nicht** an eine andere Fläche grenzt. Damit gibt es automatisch Stirnwände, und an Durchgängen entsteht keine Wand — der alte Fehler der abgeriegelten Seitenwege ist konstruktiv ausgeschlossen.

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
	[Test]
	public void AnDurchgaengen_EntstehtKeineWand()
	{
		// Der Verbinder zur Massel I stoesst beidseitig an Boden - dort darf nichts stehen.
		var kanten = EidraForgeGeometryBuilder.Wandkanten().ToList();
		foreach (var kante in kanten)
		{
			bool innenliegend = EidraForgeLayout.IstAufBoden(kante.x, kante.z, out _);
			Assert.That(innenliegend, Is.False,
				$"Wandstueck bei ({kante.x}|{kante.z}) steht mitten auf begehbarem Boden.");
		}
	}

	[Test]
	public void JederRaum_HatWaendeAufAllenVierSeiten()
	{
		var kanten = EidraForgeGeometryBuilder.Wandkanten().ToList();
		ForgeFlaeche windfang = EidraForgeLayout.Flaechen.First(f => f.Name == "Windfang");
		Assert.That(kanten.Any(k => k.z < windfang.MinZ), Is.True, "Suedliche Stirnwand fehlt.");
		Assert.That(kanten.Any(k => k.x < windfang.MinX && k.z > windfang.MinZ && k.z < windfang.MaxZ), Is.True, "Westwand fehlt.");
		Assert.That(kanten.Any(k => k.x > windfang.MaxX && k.z > windfang.MinZ && k.z < windfang.MaxZ), Is.True, "Ostwand fehlt.");
	}
```

- [ ] **Schritt 2: Test laufen lassen und Fehlschlag bestätigen**

Erwartet: `error CS1061` — `Wandkanten` existiert nicht.

- [ ] **Schritt 3: Wandkanten ableiten**

```csharp
/// <summary>
/// Liefert alle Wandstuecke: je Flaeche wird der Rand in Abschnitte von einer
/// Kachelgroesse zerlegt, und ein Abschnitt wird nur dann zur Wand, wenn direkt
/// dahinter kein Boden liegt. Dadurch entstehen Stirnwaende automatisch und
/// Durchgaenge bleiben offen - der alte Builder hatte hier seine abgeriegelten
/// Seitenwege.
/// </summary>
public static IEnumerable<(float x, float z, float breite, float tiefe)> Wandkanten()
{
	const float dicke = 0.5f;
	float schritt = EidraForgeLayout.Kachelgroesse;
	foreach (ForgeFlaeche flaeche in EidraForgeLayout.Flaechen)
	{
		for (float x = flaeche.MinX; x < flaeche.MaxX - 0.001f; x += schritt)
		{
			float breite = Mathf.Min(schritt, flaeche.MaxX - x);
			float mitte = x + breite * 0.5f;
			if (!EidraForgeLayout.IstAufBoden(mitte, flaeche.MinZ - dicke, out _))
			{
				yield return (mitte, flaeche.MinZ - dicke * 0.5f, breite, dicke);
			}
			if (!EidraForgeLayout.IstAufBoden(mitte, flaeche.MaxZ + dicke, out _))
			{
				yield return (mitte, flaeche.MaxZ + dicke * 0.5f, breite, dicke);
			}
		}
		for (float z = flaeche.MinZ; z < flaeche.MaxZ - 0.001f; z += schritt)
		{
			float tiefe = Mathf.Min(schritt, flaeche.MaxZ - z);
			float mitte = z + tiefe * 0.5f;
			if (!EidraForgeLayout.IstAufBoden(flaeche.MinX - dicke, mitte, out _))
			{
				yield return (flaeche.MinX - dicke * 0.5f, mitte, dicke, tiefe);
			}
			if (!EidraForgeLayout.IstAufBoden(flaeche.MaxX + dicke, mitte, out _))
			{
				yield return (flaeche.MaxX + dicke * 0.5f, mitte, dicke, tiefe);
			}
		}
	}
}
```

- [ ] **Schritt 4: Test laufen lassen und grün bestätigen**

Erwartet: `total="9" passed="9" failed="0"`.

- [ ] **Schritt 5: Wandbau ergänzen**

```csharp
/// <summary>Baut die Waende auf Wandhoehe. Jede Wand entsteht aus zwei Segmenten
/// mit leicht gestaffelter Vertexfarbe - flat shading erzeugt pro Flaeche genau
/// einen Ton, gestaffelte Baender ersetzen den fehlenden Verlauf.</summary>
public static void BaueWaende(Transform wurzel)
{
	Material welt = EidrenWorldStyleAssets.EnsureWorldMaterial();
	Color unten = new Color(0.22f, 0.19f, 0.2f);
	Color oben = new Color(0.17f, 0.15f, 0.16f);
	int laufnummer = 0;
	foreach (var kante in Wandkanten())
	{
		Segment(wurzel, $"Wand_{laufnummer}_u", kante, 0f, EidraForgeLayout.Wandhoehe * 0.6f, unten, welt);
		Segment(wurzel, $"Wand_{laufnummer}_o", kante, EidraForgeLayout.Wandhoehe * 0.6f,
			EidraForgeLayout.Wandhoehe * 0.4f, oben, welt);
		laufnummer++;
	}
}

private static void Segment(Transform wurzel, string name,
	(float x, float z, float breite, float tiefe) kante, float fuss, float hoehe, Color farbe, Material material)
{
	GameObject teil = new GameObject(name);
	teil.transform.SetParent(wurzel, worldPositionStays: false);
	teil.transform.position = new Vector3(kante.x, fuss, kante.z);
	teil.AddComponent<MeshFilter>().sharedMesh =
		EidrenMeshFactory.TaperedBox(new Vector3(kante.breite, hoehe, kante.tiefe), 1f, farbe);
	teil.AddComponent<MeshRenderer>().sharedMaterial = material;
	teil.AddComponent<BoxCollider>();
}
```

- [ ] **Schritt 6: Festschreiben**

```bash
git add Assets/_Game/Editor/EidraForgeGeometryBuilder.cs Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs
git commit -m "feat(schmiede): Waende inklusive Stirnwaenden aus den Flaechenkanten"
```

---

## Aufgabe 4: Truhen und Gegneranker aus dem Layout setzen

**Dateien:**
- Ändern: `Assets/_Game/Editor/EidraForgeSceneBuilder.cs` (Methoden `BuildChests` und `BuildEnemyAnchors` ersetzen)
- Test: `Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs` (erweitern)

**Schnittstellen:**
- Verbraucht: `EidraForgeLayout.Truhen`, `EidraForgeLayout.Gegneranker`, `EidraForgeLayout.IstAufBoden`.
- Liefert: unveränderte Signaturen `Chest(Transform, string, Vector3, string, ForgePhysicalChestKind, ForgeRewardChestSize)` und `Anchor(Transform, string, string, Vector3, GameObject, EnemyDefinition, CoreGuardianData)`.

**Vorher sichern:**

```bash
cp "Assets/_Game/Editor/EidraForgeSceneBuilder.cs" "$SCRATCH/backup/"
```

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
	[Test]
	public void Truhen_HabenDieKanonischeFamilienverteilung()
	{
		Assert.That(EidraForgeLayout.Truhen.Count(t => t.Id.StartsWith("forge.supply.")), Is.EqualTo(3));
		Assert.That(EidraForgeLayout.Truhen.Count(t => t.Id.StartsWith("forge.optional.")), Is.EqualTo(2));
		Assert.That(EidraForgeLayout.Truhen.Count(t => t.Id.StartsWith("forge.elite.")), Is.EqualTo(1));
		Assert.That(EidraForgeLayout.Truhen.Count(t => t.Id.StartsWith("forge.completion.")), Is.EqualTo(1));
	}

	[Test]
	public void Truhen_KollidierenNichtMitGegnerankern()
	{
		foreach (ForgePlatz truhe in EidraForgeLayout.Truhen)
		{
			foreach (ForgePlatz anker in EidraForgeLayout.Gegneranker)
			{
				float abstand = Mathf.Sqrt(
					(truhe.X - anker.X) * (truhe.X - anker.X) + (truhe.Z - anker.Z) * (truhe.Z - anker.Z));
				Assert.That(abstand, Is.GreaterThan(1.5f), $"{truhe.Id} steckt in {anker.Id}.");
			}
		}
	}
```

- [ ] **Schritt 2: Test laufen lassen und Fehlschlag bestätigen**

Erwartet: beide Tests laufen bereits gegen die Daten aus Aufgabe 1. Schlägt `Truhen_KollidierenNichtMitGegnerankern` fehl, die gemeldete Truhe verschieben und die Koordinate in `EidraForgeLayout` korrigieren.

- [ ] **Schritt 3: Platzierung aus dem Layout speisen**

Ersetze in `EidraForgeSceneBuilder` den Rumpf von `BuildChests` und `BuildEnemyAnchors`:

```csharp
private static void BuildChests(Transform root)
{
	foreach (ForgePlatz platz in EidraForgeLayout.Truhen)
	{
		EidraForgeLayout.IstAufBoden(platz.X, platz.Z, out float hoehe);
		Vector3 position = new Vector3(platz.X, hoehe, platz.Z);
		switch (platz.Id)
		{
			case "forge.reward.small":
				Chest(root, platz.Id, position, "SmallRewardChest", ForgePhysicalChestKind.Reward);
				break;
			case "forge.reward.medium":
				Chest(root, platz.Id, position, "MediumRewardChest", ForgePhysicalChestKind.Reward, ForgeRewardChestSize.Medium);
				break;
			case "forge.reward.large":
				Chest(root, platz.Id, position, "LargeRewardChest", ForgePhysicalChestKind.Reward, ForgeRewardChestSize.Large);
				break;
			case "forge.recovery":
				Chest(root, platz.Id, position, "RecoveryContainer", ForgePhysicalChestKind.Recovery);
				break;
			case "forge.elite.01":
				Chest(root, platz.Id, position, "EliteChest");
				break;
			case "forge.completion.01":
				Chest(root, platz.Id, position, "CompletionChest");
				break;
			default:
				Chest(root, platz.Id, position, platz.Id.StartsWith("forge.optional.") ? "OptionalChest" : "SupplyChest");
				break;
		}
	}
}

private static void BuildEnemyAnchors(Transform root)
{
	EnemyDefinition ignivar = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Enemies/Enemy_Ignivar.asset");
	foreach (ForgePlatz platz in EidraForgeLayout.Gegneranker)
	{
		EidraForgeLayout.IstAufBoden(platz.X, platz.Z, out float hoehe);
		Vector3 position = new Vector3(platz.X, hoehe, platz.Z);
		if (platz.Id.Contains("core_guardian"))
		{
			Anchor(root, platz.Id, null, position,
				AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/Forge/CoreGuardian.prefab"),
				null, AssetDatabase.LoadAssetAtPath<CoreGuardianData>("Assets/_Game/Data/Bosses/CoreGuardian.asset"));
			continue;
		}
		string stamm = platz.Id.Contains("ember_eater") ? "EmberEater"
			: platz.Id.Contains("ash_runner") ? "AshRunner"
			: platz.Id.Contains("forge_guardian") ? "ForgeGuardian"
			: "SealGuardian";
		Anchor(root, platz.Id, stamm, position);
	}
	EidraForgeLayout.IstAufBoden(-30f, 34f, out float ignivarHoehe);
	Anchor(root, "forge.ignivar.01", null, new Vector3(-30f, ignivarHoehe, 34f),
		AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/WildEidra.prefab"), ignivar);
}
```

- [ ] **Schritt 4: Test laufen lassen und grün bestätigen**

Erwartet: `total="11" passed="11" failed="0"`.

- [ ] **Schritt 5: Festschreiben**

```bash
git add Assets/_Game/Editor/EidraForgeSceneBuilder.cs Assets/_Game/Editor/Tests/EidraForgeLayoutTests.cs
git commit -m "feat(schmiede): Truhen und Gegneranker aus den Layoutdaten"
```

---

## Aufgabe 5: Zone konfigurieren — Spawn, Kamerabegrenzung, NavMesh

`ZoneStructureTests` verlangt: `ZoneController` ohne Validierungsfehler, `Systems`, `PlayerSpawnPoints/Spawn_Default`, Spawns für alle vier Richtungen, gesetzte `cameraBounds` und ein **gespeichertes** NavMesh mit `collectObjects = All`.

**Dateien:**
- Ändern: `Assets/_Game/Editor/EidraForgeSceneBuilder.cs` (`ConfigureZone`, `BuildLayout`)

**Schnittstellen:**
- Verbraucht: `EidraForgeGeometryBuilder.BaueBoeden`, `EidraForgeGeometryBuilder.BaueWaende`.
- Liefert: nichts Neues nach außen.

- [ ] **Schritt 1: Spawn und Aufbau umstellen**

Der Spawn liegt im Windfang. Alle vier Richtungsspawns zeigen auf denselben Punkt, weil der Dungeon nur einen Eingang hat.

```csharp
string[] array = new string[5] { "spawnDefault", "spawnFromNorth", "spawnFromEast", "spawnFromSouth", "spawnFromWest" };
foreach (string name in array)
{
	Transform spawn = zoneSo.FindProperty(name).objectReferenceValue as Transform;
	if (spawn != null)
	{
		spawn.position = new Vector3(0f, 0.2f, -35f);
	}
}
```

In `BuildLayout` die alten `BuildRooms`-Aufrufe ersetzen:

```csharp
EidraForgeGeometryBuilder.BaueBoeden(environment.transform);
EidraForgeGeometryBuilder.BaueWaende(environment.transform);
```

Die Methode `BuildRooms` samt der Aufrufe für `OptionalPath_Left` und `SealGuardianPath` ersatzlos löschen — beide Flächen sind im neuen Layout durch `Hammerwerk` und `Bindungskammer` ersetzt.

- [ ] **Schritt 2: Kamerabegrenzung auf die neue Ausdehnung setzen**

Die Begrenzung muss X −36…36 und Z −38…50 umfassen, sonst meldet `GetValidationErrors()` `cameraBounds`.

`ZoneController.CameraBounds` ist eine schreibgeschützte `Bounds`-Eigenschaft; der Collider liegt im privaten Feld `cameraBounds` und wird wie die Spawns über `SerializedObject` geholt.

```csharp
/* Kamerabegrenzung auf die Gesamtausdehnung des neuen Grundrisses
   (SCHMIEDE_ENTWURF.md Abschnitt 3): X -36..36, Z -38..50. */
BoxCollider bounds = zoneSo.FindProperty("cameraBounds").objectReferenceValue as BoxCollider;
if (bounds != null)
{
	bounds.center = new Vector3(0f, 0f, 6f);
	bounds.size = new Vector3(72f, 12f, 88f);
}
```

- [ ] **Schritt 3: Szene bauen lassen**

```bash
.unity-editor/Editor/Unity.exe -batchmode -quit -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.EidraForgeSceneBuilder.Build -logFile "%TEMP%\schmiede-build.log"
```

Anschließend das Log auf `error CS`, `Exception` und `NullReference` prüfen. Der Exit-Code allein sagt nichts.

- [ ] **Schritt 4: Strukturtests laufen lassen**

```bash
.unity-editor/Editor/Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -testFilter ".*ZoneStructureTests.*" -testResults "%TEMP%\struktur.xml" -logFile "%TEMP%\struktur.log"
```

Erwartet: alle grün. Schlägt `ZoneScene_HasRequiredReferencesAndStaticNavigation` mit `requires a saved NavMesh bake` fehl, wurde das NavMesh gebacken, aber nicht gespeichert — `zone.NavigationSurface.BuildNavMesh()` muss **vor** `EditorSceneManager.SaveScene` laufen und `collectObjects` auf `CollectObjects.All` stehen.

- [ ] **Schritt 5: Festschreiben**

```bash
git add Assets/_Game/Editor/EidraForgeSceneBuilder.cs
git commit -m "feat(schmiede): Zone auf den neuen Grundriss konfiguriert"
```

---

## Aufgabe 6: Gesamtabnahme

**Dateien:** keine Änderung, nur Nachweis.

- [ ] **Schritt 1: Volle EditMode-Suite laufen lassen**

```bash
.unity-editor/Editor/Unity.exe -batchmode -runTests -testPlatform EditMode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -testResults "%TEMP%\schmiede-e1-editmode.xml" -logFile "%TEMP%\schmiede-e1-editmode.log"
```

Erwartet: keine Fehlschläge. Die Baseline vor dieser Etappe lag bei 1009/1009. Neue Tests kommen hinzu, bestehende dürfen nicht kippen.

- [ ] **Schritt 2: PlayMode-Suite laufen lassen**

```bash
.unity-editor/Editor/Unity.exe -batchmode -runTests -testPlatform PlayMode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -testResults "%TEMP%\schmiede-e1-playmode.xml" -logFile "%TEMP%\schmiede-e1-playmode.log"
```

Erwartet: 120/120 wie in der Baseline.

- [ ] **Schritt 3: Sichtprüfung erzeugen**

Die Glut-Probe zeigt nur ein Einzelobjekt. Für den Grundriss eine Draufsicht aus der Zonenkamera abziehen und mit dem Plan in `SCHMIEDE_ENTWURF.md` Abschnitt 3 vergleichen: Sind alle drei Wege durch die Gießhalle offen? Ist die Grube sechs tief? Stehen Hammerwerk und Bindungskammer frei erreichbar an der Galerie?

Ablage unter `TempReview/SchmiedeE1/`, **nicht** unter `Temp/`.

- [ ] **Schritt 4: Festschreiben**

```bash
git add -A
git commit -m "feat(schmiede): Etappe 1 Geometrie abgenommen"
```

---

## Nicht Teil dieser Etappe

- Requisiten und das Windrohr — Etappe 2
- Beleuchtung, Glutflächen, Nebel — Etappe 3
- Der Formwall mit Kosten, Ertrag und Speicherzustand — Etappe 4

Die Szene sieht nach dieser Etappe nackt aus: graue Kacheln und graue Wände. Das ist beabsichtigt — der Grundriss muss zuerst stimmen und begehbar sein.
