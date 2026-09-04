# Stilumbau Etappe 1 — Fundament und Weltkisten: Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** `EidrenMeshFactory` (Vertexfarben-Primitive mit Stilregeln), Shader `Eidren/World/VertexLit`, Material `M_EidrenWorld_VertexLit` und die Umstellung der drei Weltkisten-Familien auf den neuen Stil inklusive Loot-Füllstand.

**Architektur:** Eine Editor-Meshfabrik erzeugt Vertexfarben-Meshes mit Ton-Variation (±6 % pro Fläche, deterministisch), Kontakt-Abdunklung und Flat Shading. Ein statischer Ableger des `WandererVertexLit`-Shaders rendert sie über ein einziges gemeinsames Material. `WorldChestContentBuilder` baut die Kisten neu (Sockel, Planken, Bogendeckel, Familien-Details, LootFill); `WorldChestVisual` schaltet den Füllstand über die vorhandenen Zustände.

**Tech Stack:** Unity 6000.3 (lokal unter `.unity-editor/`), URP 17.3 (Gamma + LDR), NUnit EditMode-Tests in `Assets/_Game/Editor/Tests/`.

**Spezifikation:** `Documentation/Etappen/Stilumbau/STILUMBAU_WELTINVENTAR_ENTWURF.md`

## Globale Vorgaben

- Projektpfad: `C:\Users\phine\Documents\Projekt Eidren` (Leerzeichen! Immer quoten, Aufrufoperator `&`, nie `Start-Process`).
- Unity headless: `& ".\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" …` — **Exit-Code lügt:** Erfolg nur, wenn das `-logFile` frei von `error CS` ist bzw. die Ergebnis-XML existiert. Nach `&` immer `Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue` plus Puffer.
- Vor JEDEM Unity-Aufruf: `Test-Path "Library\UnityLockfile"` — bei `True` warten (paralleler codex-Agent), **niemals löschen**.
- Regressionsmaßstab EditMode ist der **Namensdiff gegen die Baseline aus Task 0** (bekannte Fehlschläge, darunter `V02ContainerVisualTests`), nicht `failed="0"`.
- **Kein `git commit`** — das Repo trackt nur Release-Dateien. Beleg je Task: Logdatei/Ergebnis-XML mit Präfix `stil-e1-…` bzw. erzeugte PNGs.
- Code-Sprache: deutsche Kommentare/Doku-Kommentare, Tabs als Einrückung, `sealed`-Klassen — wie im Bestand.
- EditMode-Falle: `Object.Instantiate` ruft kein `Awake` — Komponenten im Test immer per `Configure(...)` verdrahten oder nur `SetActive`-Logik prüfen.
- Farbwerte, Collidermaße, Teil-Namen und `Configure`-Verdrahtung der Kisten sind Bestandsverträge — exakt übernehmen (Werte stehen in den Tasks).

---

### Task 0: Sicherung, Vorflug und EditMode-Baseline

**Files:** keine Quellcode-Änderung.

**Interfaces:**
- Produces: Sicherungsordner; `TestResults-stil-e1-baseline.xml` — Task 7 vergleicht dagegen per Namensdiff.

- [ ] **Step 1: Quellstand sichern**

```powershell
$stamp = Get-Date -Format "yyyyMMdd-HHmm"
$ziel = "C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e1-$stamp"
New-Item -ItemType Directory -Force $ziel | Out-Null
Copy-Item "C:\Users\phine\Documents\Projekt Eidren\Assets\_Game" "$ziel\_Game" -Recurse
Copy-Item "C:\Users\phine\Documents\Projekt Eidren\Packages\manifest.json" "$ziel\manifest.json"
Get-ChildItem $ziel | Select-Object Name
```

Erwartet: `_Game` und `manifest.json` im Sicherungsordner.

- [ ] **Step 2: Lockfile prüfen**

```powershell
Test-Path "C:\Users\phine\Documents\Projekt Eidren\Library\UnityLockfile"
```

Erwartet: `False`. Bei `True`: warten, nicht löschen.

- [ ] **Step 3: EditMode-Baseline erzeugen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform EditMode -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-stil-e1-baseline.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e1-baseline.log"
Wait-Process -Name Unity -Timeout 1800 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\stil-e1-baseline.log" -Pattern "error CS"
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\TestResults-stil-e1-baseline.xml" -Pattern '<test-run' | Select-Object -First 1
```

Erwartet: keine `error CS`; XML existiert. Die Zahl der Fehlschläge ist egal — sie ist die Baseline.

- [ ] **Step 4: Prüfnachweis**

Sicherungsordner-Listing, Baseline-XML vorhanden, `failed`-Zahl aus der `<test-run>`-Zeile notiert.

---

### Task 1: EidrenMeshFactory — TaperedBox und Wedge mit Stilregeln

**Files:**
- Create: `Assets/_Game/Editor/EidrenMeshFactory.cs`
- Test: `Assets/_Game/Editor/Tests/EidrenMeshFactoryTests.cs`

**Interfaces:**
- Produces (Task 2 erweitert die Klasse, Task 5/6 rufen sie):

```csharp
namespace Eidren.Editor
{
	public static class EidrenMeshFactory
	{
		public const float ToneVariation = 0.06f;     // ±6 % pro Flaeche
		public const float ContactShadeBand = 0.15f;  // unterer Hoehenanteil mit Abdunklung
		public const float ContactShadeFloor = 0.78f; // dunkelster Faktor am Boden
		public static Mesh TaperedBox(Vector3 size, float topScale, Color color); // Basis auf y=0, zentriert
		public static Mesh Wedge(Vector3 size, Color color);                      // Keil, Firstkante hinten (−z)
	}
}
```

- [ ] **Step 1: Failing Tests schreiben** — `Assets/_Game/Editor/Tests/EidrenMeshFactoryTests.cs`:

```csharp
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
	}
}
```

- [ ] **Step 2: Testlauf — muss an fehlender Klasse scheitern**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.EidrenMeshFactoryTests" -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-stil-e1-t1a.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t1a.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t1a.log" -Pattern "error CS"
```

Erwartet: `error CS0103`/`CS0246` (EidrenMeshFactory unbekannt).

- [ ] **Step 3: Implementierung** — `Assets/_Game/Editor/EidrenMeshFactory.cs`:

```csharp
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

		/// <summary>Kasten mit skaliertem Kopf; Basisflaeche liegt auf y=0, zentriert um x=z=0.</summary>
		public static Mesh TaperedBox(Vector3 size, float topScale, Color color)
		{
			Builder builder = new Builder();
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

		/// <summary>Keil: rechteckige Basis auf y=0, Firstkante hinten (−z) auf Hoehe size.y.</summary>
		public static Mesh Wedge(Vector3 size, Color color)
		{
			Builder builder = new Builder();
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

		/// <summary>Sammelt Flaechen mit getrennten Ecken (Flat Shading) und Vertexfarben.</summary>
		private sealed class Builder
		{
			private readonly List<Vector3> _punkte = new List<Vector3>();

			private readonly List<Color> _farben = new List<Color>();

			private readonly List<int> _dreiecke = new List<int>();

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
				ApplyContactShade();
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
```

- [ ] **Step 4: Tests laufen lassen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.EidrenMeshFactoryTests" -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-stil-e1-t1.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t1.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t1.log" -Pattern "error CS"
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\TestResults-stil-e1-t1.xml" -Pattern 'result="Passed"' | Select-Object -First 1
```

Erwartet: keine Compile-Fehler, alle fünf Tests `Passed`.

- [ ] **Step 5: Prüfnachweis**

Log und XML abgelegt; XML meldet `Passed`.

---

### Task 2: EidrenMeshFactory — Loft-Primitive

**Files:**
- Modify: `Assets/_Game/Editor/EidrenMeshFactory.cs`
- Test: `Assets/_Game/Editor/Tests/EidrenMeshFactoryTests.cs` (Ergänzung)

**Interfaces:**
- Consumes: `Builder`-Innenklasse aus Task 1.
- Produces (Task 6 baut Bogendeckel und Wurzeln damit; beide Typen sind **in `EidrenMeshFactory` geschachtelt**, Verwendung also `EidrenMeshFactory.LoftProfile` usw.):

```csharp
public enum LoftShape { Rect, Oct }

public readonly struct LoftProfile
{
	public LoftProfile(float height, float width, float depth, float offsetX = 0f, float offsetZ = 0f);
	public float Height { get; }   // absolute y-Lage des Rings
	public float Width { get; }    // volle Breite
	public float Depth { get; }    // volle Tiefe
	public float OffsetX { get; }
	public float OffsetZ { get; }
}

public static Mesh Loft(LoftProfile[] profiles, LoftShape shape, Color color, bool capBottom, bool capTop);
```

- [ ] **Step 1: Failing Tests ergänzen** — in `EidrenMeshFactoryTests.cs` anhängen:

```csharp
		[Test]
		public void Loft_Rechteck_DreiecksZahlStimmt()
		{
			LoftProfile[] profil =
			{
				new LoftProfile(0f, 1f, 0.6f),
				new LoftProfile(0.2f, 0.9f, 0.55f),
				new LoftProfile(0.35f, 0.6f, 0.4f)
			};
			Mesh mesh = EidrenMeshFactory.Loft(profil, LoftShape.Rect, new Color(0.4f, 0.3f, 0.2f), capBottom: true, capTop: true);
			// Seiten: 4 Flaechen x 2 Ringe x 2 Dreiecke = 16; Deckel: je 2 => 20 Dreiecke.
			Assert.That(mesh.triangles.Length, Is.EqualTo(60));
			Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount));
		}

		[Test]
		public void Loft_Oktogon_DreiecksZahlStimmt()
		{
			LoftProfile[] profil =
			{
				new LoftProfile(0f, 1f, 1f),
				new LoftProfile(0.3f, 0.8f, 0.8f)
			};
			Mesh mesh = EidrenMeshFactory.Loft(profil, LoftShape.Oct, new Color(0.4f, 0.3f, 0.2f), capBottom: true, capTop: true);
			// Seiten: 8 x 1 x 2 = 16; Deckel: 8-Eck-Faecher = je 6 => 28 Dreiecke.
			Assert.That(mesh.triangles.Length, Is.EqualTo(84));
			Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount));
		}

		[Test]
		public void Loft_MitVersatz_VerschiebtDenRing()
		{
			LoftProfile[] profil =
			{
				new LoftProfile(0f, 1f, 1f),
				new LoftProfile(0.5f, 1f, 1f, 0.3f, 0.1f)
			};
			Mesh mesh = EidrenMeshFactory.Loft(profil, LoftShape.Rect, Color.gray, capBottom: false, capTop: false);
			float maxX = float.MinValue;
			foreach (Vector3 punkt in mesh.vertices)
			{
				maxX = Mathf.Max(maxX, punkt.x);
			}
			Assert.That(maxX, Is.EqualTo(0.8f).Within(0.001f), "0.5 halbe Breite + 0.3 Versatz");
		}
```

- [ ] **Step 2: Testlauf — die drei neuen Tests scheitern** (Filter wie Task 1 Step 4, Ergebnis `TestResults-stil-e1-t2a.xml`, Log `stil-e1-t2a.log`). Erwartet: `error CS0246` (LoftProfile unbekannt).

- [ ] **Step 3: Implementierung** — in `EidrenMeshFactory` ergänzen (oberhalb der `Builder`-Klasse):

```csharp
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

		/// <summary>Profilstapel: verbindet die Ringe mit Flat-Shading-Flaechen; optional Deckel.</summary>
		public static Mesh Loft(LoftProfile[] profiles, LoftShape shape, Color color, bool capBottom, bool capTop)
		{
			Builder builder = new Builder();
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
					builder.Quad(ringe[ring][seite], ringe[ring][naechste], ringe[ring + 1][naechste], ringe[ring + 1][seite], color);
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
```

- [ ] **Step 4: Alle Fabrik-Tests laufen lassen** (Filter `Eidren.Tests.EditMode.EidrenMeshFactoryTests` wie Task 1 Step 4, Ergebnis `TestResults-stil-e1-t2.xml`, Log `stil-e1-t2.log`). Erwartet: alle acht Tests `Passed`.

- [ ] **Step 5: Prüfnachweis** — Log und XML abgelegt.

---

### Task 3: Shader `Eidren/World/VertexLit` und Material

**Files:**
- Create: `Assets/_Game/Shaders/World/EidrenWorldVertexLit.shader`
- Create: `Assets/_Game/Editor/EidrenWorldStyleAssets.cs`
- Test: `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs`

**Interfaces:**
- Produces (Task 6 setzt das Material auf alle Renderer):

```csharp
public static class EidrenWorldStyleAssets
{
	public const string ShaderName = "Eidren/World/VertexLit";
	public const string MaterialPath = "Assets/_Game/Art/World/Materials/M_EidrenWorld_VertexLit.mat";
	public static Material EnsureWorldMaterial();   // laedt oder erzeugt das gemeinsame Material
}
```

- [ ] **Step 1: Failing Test schreiben** — `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs`:

```csharp
using Eidren.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class EidrenWorldStyleTests
	{
		[Test]
		public void WeltShader_ExistiertUndHatTintProperty()
		{
			Shader shader = Shader.Find(EidrenWorldStyleAssets.ShaderName);
			Assert.That(shader, Is.Not.Null, "Shader Eidren/World/VertexLit fehlt");
			Material material = new Material(shader);
			Assert.That(material.HasProperty("_Tint"), Is.True, "_Tint-Property fehlt");
			Object.DestroyImmediate(material);
		}

		[Test]
		public void WeltMaterial_WirdErzeugtUndNutztDenShader()
		{
			Material material = EidrenWorldStyleAssets.EnsureWorldMaterial();
			Assert.That(material, Is.Not.Null);
			Assert.That(material.shader.name, Is.EqualTo(EidrenWorldStyleAssets.ShaderName));
		}
	}
}
```

- [ ] **Step 2: Shader schreiben** — `Assets/_Game/Shaders/World/EidrenWorldVertexLit.shader`. Inhaltlich identisch mit `Assets/_Game/Shaders/Actors/WandererVertexLit.shader` (dort nachlesen), nur: Shadername `"Eidren/World/VertexLit"`, Kopfkommentar:

```hlsl
Shader "Eidren/World/VertexLit"
{
	// Vertexfarben-Shader fuer statische Weltobjekte aus der EidrenMeshFactory.
	// Ton-Variation und Kontakt-Abdunklung sind in den Vertexfarben eingebacken;
	// das Flat Shading kommt aus den Normalen. Inhaltlich ein Ableger von
	// Eidren/Actors/WandererVertexLit ohne Figuren-Kontext.
	...restlicher Inhalt 1:1 aus WandererVertexLit.shader uebernehmen...
}
```

Falls `UsePass "Universal Render Pipeline/Lit/..."` dort mit URP-Versionsvermerk gelöst ist, denselben Weg übernehmen.

- [ ] **Step 3: Materialhelfer schreiben** — `Assets/_Game/Editor/EidrenWorldStyleAssets.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Stellt die gemeinsamen Stil-Assets der Weltobjekte bereit: den
	/// Vertexfarben-Shader und das eine geteilte Material.
	/// </summary>
	public static class EidrenWorldStyleAssets
	{
		public const string ShaderName = "Eidren/World/VertexLit";

		public const string MaterialPath = "Assets/_Game/Art/World/Materials/M_EidrenWorld_VertexLit.mat";

		public static Material EnsureWorldMaterial()
		{
			Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
			if (material == null)
			{
				string ordner = "Assets/_Game/Art/World/Materials";
				if (!AssetDatabase.IsValidFolder(ordner))
				{
					AssetDatabase.CreateFolder("Assets/_Game/Art/World", "Materials");
				}
				material = new Material(Shader.Find(ShaderName)) { name = "M_EidrenWorld_VertexLit" };
				AssetDatabase.CreateAsset(material, MaterialPath);
			}
			return material;
		}
	}
}
```

- [ ] **Step 4: Tests laufen lassen** (Filter `Eidren.Tests.EditMode.EidrenWorldStyleTests`, Ergebnis `TestResults-stil-e1-t3.xml`, Log `stil-e1-t3.log`). Zusätzlich Log auf `Shader error` prüfen. Erwartet: keine Shader-/Compile-Fehler, beide Tests `Passed`.

- [ ] **Step 5: Prüfnachweis** — Log und XML abgelegt.

---

### Task 4: WorldChestVisual — Loot-Füllstand

**Files:**
- Modify: `Assets/_Game/Scripts/Presentation/WorldChestVisual.cs`
- Test: `Assets/_Game/Editor/Tests/WorldChestVisualStateTests.cs`

**Interfaces:**
- Produces (Task 6 verdrahtet die neuen Parameter):

```csharp
// Signatur NEU (zwei optionale Parameter am Ende — Bestandsaufrufer kompilieren unveraendert):
public void Configure(Transform configuredLid, GameObject configuredClosed, GameObject configuredOpened,
	GameObject configuredEmptied, SpriteRenderer configuredLeftHand = null, SpriteRenderer configuredRightHand = null,
	GameObject configuredLootFull = null, GameObject configuredLootPartial = null);
// Apply-Regeln: lootFull aktiv nur bei Opened; lootPartial aktiv nur bei PartiallyEmptied.
```

- [ ] **Step 1: Failing Test schreiben** — `Assets/_Game/Editor/Tests/WorldChestVisualStateTests.cs`:

```csharp
using Eidren.Presentation;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class WorldChestVisualStateTests
	{
		[Test]
		public void VierZustaende_ErgebenVierUnterscheidbareSichtbarkeiten()
		{
			GameObject wurzel = new GameObject("Kiste");
			try
			{
				Transform deckel = new GameObject("LidPivot").transform;
				deckel.SetParent(wurzel.transform);
				GameObject zu = Kind(wurzel, "ClosedDetails");
				GameObject offen = Kind(wurzel, "OpenedDetails");
				GameObject leer = Kind(wurzel, "EmptiedDetails");
				GameObject lootVoll = Kind(wurzel, "LootFill_Full");
				GameObject lootTeil = Kind(wurzel, "LootFill_Partial");
				WorldChestVisual visual = wurzel.AddComponent<WorldChestVisual>();
				visual.Configure(deckel, zu, offen, leer, null, null, lootVoll, lootTeil);
				HashSet<string> kombinationen = new HashSet<string>();
				foreach (WorldChestVisualState status in new[]
				{
					WorldChestVisualState.Closed, WorldChestVisualState.Opened,
					WorldChestVisualState.PartiallyEmptied, WorldChestVisualState.Emptied
				})
				{
					visual.Apply(status);
					kombinationen.Add($"{zu.activeSelf}|{offen.activeSelf}|{leer.activeSelf}|{lootVoll.activeSelf}|{lootTeil.activeSelf}");
				}
				Assert.That(kombinationen.Count, Is.EqualTo(4), "jeder Zustand braucht eine eigene Sichtbarkeitskombination");
				visual.Apply(WorldChestVisualState.Opened);
				Assert.That(lootVoll.activeSelf, Is.True);
				Assert.That(lootTeil.activeSelf, Is.False);
				visual.Apply(WorldChestVisualState.PartiallyEmptied);
				Assert.That(lootVoll.activeSelf, Is.False);
				Assert.That(lootTeil.activeSelf, Is.True);
				visual.Apply(WorldChestVisualState.Emptied);
				Assert.That(lootVoll.activeSelf, Is.False);
				Assert.That(lootTeil.activeSelf, Is.False);
			}
			finally
			{
				Object.DestroyImmediate(wurzel);
			}
		}

		private static GameObject Kind(GameObject eltern, string name)
		{
			GameObject kind = new GameObject(name);
			kind.transform.SetParent(eltern.transform);
			return kind;
		}
	}
}
```

- [ ] **Step 2: Testlauf — muss scheitern** (Filter `Eidren.Tests.EditMode.WorldChestVisualStateTests`, Ergebnis `TestResults-stil-e1-t4a.xml`, Log `stil-e1-t4a.log`). Erwartet: `error CS1501` (Configure kennt keine 8 Argumente).

- [ ] **Step 3: Implementierung** — in `WorldChestVisual.cs`:

Felder ergänzen (nach `rightHand`):

```csharp
		[SerializeField]
		private GameObject lootFull;

		[SerializeField]
		private GameObject lootPartial;
```

`Configure` erweitern (Signatur siehe Interfaces-Block; im Rumpf vor `_closedRotation = …`):

```csharp
			lootFull = configuredLootFull;
			lootPartial = configuredLootPartial;
```

`Apply(WorldChestVisualState status)` ergänzen (nach dem `emptiedDetails`-Block, vor dem `SetOpeningProgress`-Abschluss):

```csharp
			if (lootFull != null)
			{
				lootFull.SetActive(status == WorldChestVisualState.Opened);
			}
			if (lootPartial != null)
			{
				lootPartial.SetActive(status == WorldChestVisualState.PartiallyEmptied);
			}
```

- [ ] **Step 4: Tests laufen lassen** (Filter wie Step 2, Ergebnis `TestResults-stil-e1-t4.xml`, Log `stil-e1-t4.log`). Erwartet: `Passed`.

- [ ] **Step 5: Prüfnachweis** — Log und XML abgelegt.

---

### Task 5: Nahaufnahme-Werkzeug und Vorher-Captures

**WICHTIG: Dieser Task muss VOR Task 6 laufen — er fotografiert die alten Kisten.**

**Files:**
- Create: `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs`

**Interfaces:**
- Produces: `WorldChestStyleCaptureUtility.CaptureAll(string ordner)` und Batch-Einstieg `CaptureVorher()` / `CaptureNachher()`; PNGs `TempReview/StilumbauE1/<vorher|nachher>/<Familie>_<Zustand>.png` — Task 7 vergleicht die Paare.

- [ ] **Step 1: Implementierung** — `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs`:

```csharp
using Eidren.Presentation;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Rendert jede Weltkisten-Familie in allen vier Sichtzustaenden aus einer
	/// festen Kameraposition in PNGs. Positionsgleiche Vorher/Nachher-Paare
	/// sind die Bildabnahme der Stilumbau-Etappe 1.
	/// </summary>
	public static class WorldChestStyleCaptureUtility
	{
		private static readonly string[] Familien = { "WorldChest_Common", "WorldChest_Guarded", "WorldChest_Hidden" };

		private static readonly WorldChestVisualState[] Zustaende =
		{
			WorldChestVisualState.Closed, WorldChestVisualState.Opened,
			WorldChestVisualState.PartiallyEmptied, WorldChestVisualState.Emptied
		};

		[MenuItem("Eidren/V0.2/Stilumbau/Weltkisten Vorher-Captures")]
		public static void CaptureVorher()
		{
			CaptureAll("TempReview/StilumbauE1/vorher");
		}

		[MenuItem("Eidren/V0.2/Stilumbau/Weltkisten Nachher-Captures")]
		public static void CaptureNachher()
		{
			CaptureAll("TempReview/StilumbauE1/nachher");
		}

		public static void CaptureAll(string ordner)
		{
			Directory.CreateDirectory(ordner);
			foreach (string familie in Familien)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Loot/WorldChests/" + familie + ".prefab");
				if (prefab == null)
				{
					Debug.LogError("[StilE1] Prefab fehlt: " + familie);
					continue;
				}
				GameObject instanz = Object.Instantiate(prefab);
				GameObject lichtObjekt = new GameObject("CaptureLicht");
				Light licht = lichtObjekt.AddComponent<Light>();
				licht.type = LightType.Directional;
				licht.intensity = 1.1f;
				lichtObjekt.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
				GameObject kameraObjekt = new GameObject("CaptureKamera");
				Camera kamera = kameraObjekt.AddComponent<Camera>();
				kamera.clearFlags = CameraClearFlags.SolidColor;
				kamera.backgroundColor = new Color(0.16f, 0.17f, 0.18f);
				kameraObjekt.transform.position = new Vector3(1.6f, 1.5f, 2.1f);
				kameraObjekt.transform.LookAt(new Vector3(0f, 0.35f, 0f));
				WorldChestVisual visual = instanz.GetComponent<WorldChestVisual>();
				try
				{
					foreach (WorldChestVisualState zustand in Zustaende)
					{
						visual.Apply(zustand);
						RenderTexture ziel = new RenderTexture(768, 768, 24);
						kamera.targetTexture = ziel;
						kamera.Render();
						RenderTexture.active = ziel;
						Texture2D bild = new Texture2D(768, 768, TextureFormat.RGB24, mipChain: false);
						bild.ReadPixels(new Rect(0f, 0f, 768f, 768f), 0, 0);
						bild.Apply();
						string pfad = Path.Combine(ordner, $"{familie}_{zustand}.png");
						File.WriteAllBytes(pfad, bild.EncodeToPNG());
						RenderTexture.active = null;
						kamera.targetTexture = null;
						Object.DestroyImmediate(ziel);
						Object.DestroyImmediate(bild);
						Debug.Log("[StilE1] geschrieben: " + pfad);
					}
				}
				finally
				{
					Object.DestroyImmediate(instanz);
					Object.DestroyImmediate(kameraObjekt);
					Object.DestroyImmediate(lichtObjekt);
				}
			}
		}
	}
}
```

Hinweis: `Object.Instantiate` ruft im EditMode kein `Awake`; `Apply` schaltet aber nur `SetActive`/Rotation über die serialisierten Felder — das genügt für die Bilder.

- [ ] **Step 2: Vorher-Captures erzeugen** — **ohne `-nographics`** (unter `-nographics` gibt es keine GPU, `Camera.Render()` liefert schwarze Bilder — Lehre aus G-001):

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureVorher -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t5-vorher.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t5-vorher.log" -Pattern "error CS|\[StilE1\]"
Get-ChildItem "C:\Users\phine\Documents\Projekt Eidren\TempReview\StilumbauE1\vorher"
```

Erwartet: 12 PNGs (3 Familien × 4 Zustände), keine `error CS`.

- [ ] **Step 3: Prüfnachweis** — die 12 Vorher-PNGs stichprobenartig ansehen (nicht leer, Kiste sichtbar); Log abgelegt.

---

### Task 6: WorldChestContentBuilder auf den neuen Stil umstellen

**Files:**
- Modify: `Assets/_Game/Editor/WorldChestContentBuilder.cs`
- Test: `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` (Ergänzung)

**Interfaces:**
- Consumes: `EidrenMeshFactory.TaperedBox/Loft` (Task 1/2), `EidrenWorldStyleAssets.EnsureWorldMaterial()` (Task 3), `WorldChestVisual.Configure(…, lootFull, lootPartial)` (Task 4).
- Produces: neu gebaute Prefabs `WorldChest_{Common,Guarded,Hidden}.prefab` mit Vertexfarben-Meshes, einem Material, `LootFill_Full`/`LootFill_Partial`; Colliderwerte und Teil-Namen unverändert.

**Bestandsverträge (exakt erhalten):** `BoxCollider.size = size`, `center = up*size.y*0.5`; `SphereCollider` Trigger Radius `1.45`; Größen `Common (1.15, 0.54, 0.72)`, `Guarded (1.45, 0.7, 0.88)`, `Hidden (1, 0.48, 0.64)`; Teil-Namen `CarvedBase`, `LidPivot`, `ArchedLid`, `ClosedDetails`, `CarvedClasp`/`SealLock`, `ForgedBand_N`, `RootWrap` (nur Hidden), `OpenedDetails`, `DarkInterior`, `EmptiedDetails`, `EmptyLining`, `OpeningHand_Left/Right`; `WorldChestContainer.ConfigureVisual(visual, null, 2.6f)`; LidPivot-Position `(0, size.y*0.62, size.z*0.42)`.

- [ ] **Step 1: Failing Tests ergänzen** — in `EidrenWorldStyleTests.cs` anhängen:

```csharp
		[TestCase("WorldChest_Common", 1.15f, 0.54f, 0.72f)]
		[TestCase("WorldChest_Guarded", 1.45f, 0.7f, 0.88f)]
		[TestCase("WorldChest_Hidden", 1f, 0.48f, 0.64f)]
		public void Weltkiste_IstImVertexfarbenStil(string name, float breite, float hoehe, float tiefe)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Loot/WorldChests/" + name + ".prefab");
			Assert.That(prefab, Is.Not.Null, name);
			int dreiecke = 0;
			foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
			{
				Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
					name + "/" + filter.name + " ohne Vertexfarben");
				dreiecke += filter.sharedMesh.triangles.Length / 3;
			}
			Assert.That(dreiecke, Is.LessThanOrEqualTo(400), name + " ueberschreitet den Dreieckskorridor");
			foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
			{
				Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
					name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
			}
			Assert.That(prefab.transform.Find("LootFill_Full"), Is.Not.Null, name + " ohne LootFill_Full");
			Assert.That(prefab.transform.Find("LootFill_Partial"), Is.Not.Null, name + " ohne LootFill_Partial");
			BoxCollider collider = prefab.GetComponent<BoxCollider>();
			Assert.That(collider.size, Is.EqualTo(new Vector3(breite, hoehe, tiefe)), name + " Collider veraendert");
			foreach (string teil in new[] { "CarvedBase", "LidPivot", "ClosedDetails", "OpenedDetails", "EmptiedDetails" })
			{
				Assert.That(prefab.transform.Find(teil), Is.Not.Null, name + " Teil fehlt: " + teil);
			}
		}
```

- [ ] **Step 2: Testlauf — muss scheitern** (Filter `Eidren.Tests.EditMode.EidrenWorldStyleTests`, Ergebnis `TestResults-stil-e1-t6a.xml`, Log `stil-e1-t6a.log`). Erwartet: FAIL (alte Prefabs ohne Vertexfarben/LootFill).

- [ ] **Step 3: Builder umstellen** — in `WorldChestContentBuilder.cs`:

1. `using`-Zeile ergänzen: keine nötig (gleicher Namespace `Eidren.Editor`).
2. Die privaten Methoden `TaperedBox`, `ArchedPrism` **löschen**; `MaterialFor` **löschen**; `MeshFrom` bleibt (persistiert Fabrik-Meshes) und wird umbenannt zu `Persist`:

```csharp
	private static Mesh Persist(Mesh mesh)
	{
		string path = string.Format("{0}/WorldChestMesh_{1:000}_{2}.asset", MeshRoot, _meshSequence++, mesh.name);
		Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
		if (existing != null)
		{
			EditorUtility.CopySerialized(mesh, existing);
			UnityEngine.Object.DestroyImmediate(mesh);
			EditorUtility.SetDirty(existing);
			return existing;
		}
		AssetDatabase.CreateAsset(mesh, path);
		return mesh;
	}
```

3. Palettenstruktur ergänzen (Grundfarben = bisherige Builder-Werte, abgeleitete Töne deterministisch):

```csharp
	private readonly struct ChestPalette
	{
		public ChestPalette(Color body, Color accent)
		{
			Body = body;
			Plank = Color.Lerp(body, Color.white, 0.12f);
			Accent = accent;
			Metal = Color.Lerp(accent, Color.black, 0.35f);
			Interior = new Color(0.035f, 0.028f, 0.02f);
			Lining = Color.Lerp(body, Color.white, 0.28f);
			Gold = new Color(0.84f, 0.66f, 0.29f);
		}

		public Color Body { get; }
		public Color Plank { get; }
		public Color Accent { get; }
		public Color Metal { get; }
		public Color Interior { get; }
		public Color Lining { get; }
		public Color Gold { get; }
	}
```

4. `MeshObject` behält Signatur; alle Aufrufe übergeben `EidrenWorldStyleAssets.EnsureWorldMaterial()` als Material.
5. `BuildChestPrefab` vollständig ersetzen:

```csharp
	private static GameObject BuildChestPrefab(string name, WorldChestFamily family, Color bodyColor, Color accentColor, Vector3 size)
	{
		ChestPalette palette = new ChestPalette(bodyColor, accentColor);
		Material material = EidrenWorldStyleAssets.EnsureWorldMaterial();
		GameObject root = new GameObject(name);

		// Korpus: Sockel + zwei Plankenlagen + geschnitzte Frontplatte unter "CarvedBase".
		GameObject basis = new GameObject("CarvedBase");
		basis.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("Sockel", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 1.06f, size.y * 0.11f, size.z * 1.1f), 1f, palette.Accent)), material, basis.transform);
		MeshObject("Planke_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x, size.y * 0.28f, size.z), 1f, palette.Body)), material, basis.transform).transform.localPosition = Vector3.up * (size.y * 0.11f);
		MeshObject("Planke_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x, size.y * 0.26f, size.z), 0.98f, palette.Plank)), material, basis.transform).transform.localPosition = Vector3.up * (size.y * 0.39f);
		MeshObject("Frontplatte", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.58f, size.y * 0.3f, size.z * 0.06f), 0.92f, palette.Accent)), material, basis.transform).transform.localPosition = new Vector3(0f, size.y * 0.16f, (0f - size.z) * 0.5f);

		// Deckel: Bogen-Loft + Firstleiste am LidPivot (Position wie Bestand).
		GameObject lidPivot = new GameObject("LidPivot");
		lidPivot.transform.SetParent(root.transform, worldPositionStays: false);
		lidPivot.transform.localPosition = new Vector3(0f, size.y * 0.62f, size.z * 0.42f);
		EidrenMeshFactory.LoftProfile[] bogen =
		{
			new EidrenMeshFactory.LoftProfile(0f, size.x * 0.98f, size.z * 0.92f),
			new EidrenMeshFactory.LoftProfile(size.y * 0.16f, size.x * 0.92f, size.z * 0.88f),
			new EidrenMeshFactory.LoftProfile(size.y * 0.3f, size.x * 0.76f, size.z * 0.72f),
			new EidrenMeshFactory.LoftProfile(size.y * 0.42f, size.x * 0.5f, size.z * 0.5f)
		};
		GameObject lid = MeshObject("ArchedLid", Persist(EidrenMeshFactory.Loft(bogen, EidrenMeshFactory.LoftShape.Rect, palette.Body, capBottom: false, capTop: true)), material, lidPivot.transform);
		lid.transform.localPosition = new Vector3(0f, 0f, (0f - size.z) * 0.42f);
		MeshObject("Firstleiste", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.62f, size.y * 0.06f, size.z * 0.3f), 0.9f, palette.Plank)), material, lidPivot.transform).transform.localPosition = new Vector3(0f, size.y * 0.42f, (0f - size.z) * 0.42f);

		// Nur-geschlossen-Details: Schnalle/Schloss + Beschlagbaender.
		GameObject closed = new GameObject("ClosedDetails");
		closed.transform.SetParent(root.transform, worldPositionStays: false);
		string schlossName = ((family == WorldChestFamily.Guarded) ? "SealLock" : "CarvedClasp");
		float schlossBreite = ((family == WorldChestFamily.Guarded) ? (size.x * 0.22f) : (size.x * 0.15f));
		MeshObject(schlossName, Persist(EidrenMeshFactory.TaperedBox(new Vector3(schlossBreite, size.y * 0.34f, size.z * 0.1f), 0.8f, palette.Metal)), material, closed.transform).transform.localPosition = new Vector3(0f, size.y * 0.42f, (0f - size.z) * 0.5f);
		int bandCount = ((family == WorldChestFamily.Guarded) ? 3 : 2);
		for (int index = 0; index < bandCount; index++)
		{
			// Baender haengen wie im Bestand an der Wurzel und bleiben in jedem Zustand sichtbar.
			float x = ((bandCount == 3) ? ((float)(index - 1) * size.x * 0.32f) : (((index == 0) ? (-1f) : 1f) * size.x * 0.31f));
			MeshObject($"ForgedBand_{index + 1}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.08f, size.y * 1.02f, size.z * 1.06f), 0.98f, palette.Metal)), material, root.transform).transform.localPosition = new Vector3(x, 0f, 0f);
		}
		if (family == WorldChestFamily.Guarded)
		{
			for (int ecke = 0; ecke < 4; ecke++)
			{
				float ex = (((ecke & 1) == 0) ? (-1f) : 1f) * size.x * 0.47f;
				float ez = (((ecke & 2) == 0) ? (-1f) : 1f) * size.z * 0.42f;
				MeshObject($"Kantenpanzer_{ecke + 1}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.09f, size.y * 0.5f, size.x * 0.09f), 0.9f, palette.Accent)), material, basis.transform).transform.localPosition = new Vector3(ex, 0f, ez);
			}
		}
		if (family == WorldChestFamily.Hidden)
		{
			EidrenMeshFactory.LoftProfile[] wurzel =
			{
				new EidrenMeshFactory.LoftProfile(0f, size.x * 1.14f, size.z * 1.14f),
				new EidrenMeshFactory.LoftProfile(size.y * 0.2f, size.x * 1.02f, size.z * 1.02f)
			};
			GameObject rootWrap = MeshObject("RootWrap", Persist(EidrenMeshFactory.Loft(wurzel, EidrenMeshFactory.LoftShape.Oct, palette.Accent, capBottom: false, capTop: false)), material, root.transform);
			rootWrap.transform.localPosition = new Vector3(0.06f, size.y * 0.12f, 0.02f);
			rootWrap.transform.localRotation = Quaternion.Euler(0f, 17f, -7f);
			MeshObject("Moos_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.2f, size.y * 0.08f, size.z * 0.24f), 0.6f, new Color(0.29f, 0.39f, 0.18f))), material, lidPivot.transform).transform.localPosition = new Vector3((0f - size.x) * 0.18f, size.y * 0.38f, (0f - size.z) * 0.36f);
			MeshObject("Moos_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.14f, size.y * 0.06f, size.z * 0.16f), 0.6f, new Color(0.29f, 0.39f, 0.18f))), material, lidPivot.transform).transform.localPosition = new Vector3(size.x * 0.22f, size.y * 0.34f, (0f - size.z) * 0.5f);
		}

		// Offen/Leer-Details wie Bestand, nur mit Fabrik-Meshes.
		GameObject opened = new GameObject("OpenedDetails");
		opened.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("DarkInterior", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.78f, 0.035f, size.z * 0.7f), 0.97f, palette.Interior)), material, opened.transform).transform.localPosition = new Vector3(0f, size.y * 0.64f, 0f);
		GameObject emptied = new GameObject("EmptiedDetails");
		emptied.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("EmptyLining", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.62f, 0.025f, size.z * 0.52f), 0.98f, palette.Lining)), material, emptied.transform).transform.localPosition = new Vector3(0f, size.y * 0.645f, 0f);

		// Loot-Fuellstand: voller Haufen und flacher Rest.
		GameObject lootFull = new GameObject("LootFill_Full");
		lootFull.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("Gold_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.42f, size.y * 0.18f, size.z * 0.4f), 0.55f, palette.Gold)), material, lootFull.transform).transform.localPosition = new Vector3((0f - size.x) * 0.08f, size.y * 0.66f, 0f);
		MeshObject("Gold_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.26f, size.y * 0.12f, size.z * 0.26f), 0.5f, palette.Gold)), material, lootFull.transform).transform.localPosition = new Vector3(size.x * 0.16f, size.y * 0.66f, (0f - size.z) * 0.1f);
		MeshObject("Gold_3", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.16f, size.y * 0.1f, size.z * 0.16f), 0.45f, palette.Gold)), material, lootFull.transform).transform.localPosition = new Vector3(0f, size.y * 0.78f, size.z * 0.06f);
		GameObject lootPartial = new GameObject("LootFill_Partial");
		lootPartial.transform.SetParent(root.transform, worldPositionStays: false);
		MeshObject("GoldRest_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.2f, size.y * 0.07f, size.z * 0.18f), 0.55f, palette.Gold)), material, lootPartial.transform).transform.localPosition = new Vector3((0f - size.x) * 0.22f, size.y * 0.655f, size.z * 0.14f);
		MeshObject("GoldRest_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(size.x * 0.14f, size.y * 0.05f, size.z * 0.12f), 0.55f, palette.Gold)), material, lootPartial.transform).transform.localPosition = new Vector3(size.x * 0.24f, size.y * 0.65f, (0f - size.z) * 0.18f);

		// Collider, Haende, Verdrahtung: exakt wie Bestand.
		BoxCollider boxCollider = root.AddComponent<BoxCollider>();
		boxCollider.size = new Vector3(size.x, size.y, size.z);
		boxCollider.center = Vector3.up * size.y * 0.5f;
		SphereCollider sphereCollider = root.AddComponent<SphereCollider>();
		sphereCollider.isTrigger = true;
		sphereCollider.radius = 1.45f;
		Sprite handSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Resources/Art/UI/ui_interaction_hand.png");
		SpriteRenderer leftHand = OpeningHand(root.transform, "OpeningHand_Left", handSprite, new Vector3((0f - size.x) * 0.25f, size.y * 0.72f, (0f - size.z) * 0.62f), mirrored: false);
		SpriteRenderer rightHand = OpeningHand(root.transform, "OpeningHand_Right", handSprite, new Vector3(size.x * 0.25f, size.y * 0.72f, (0f - size.z) * 0.62f), mirrored: true);
		WorldChestVisual visual = root.AddComponent<WorldChestVisual>();
		visual.Configure(lidPivot.transform, closed, opened, emptied, leftHand, rightHand, lootFull, lootPartial);
		root.AddComponent<WorldChestContainer>().ConfigureVisual(visual, null, 2.6f);
		visual.Apply(WorldChestVisualState.Closed);
		int dreiecke = 0;
		foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			dreiecke += filter.sharedMesh.triangles.Length / 3;
		}
		Debug.Log($"[StilE1] {name}: {dreiecke} Dreiecke");
		string path = "Assets/_Game/Prefabs/Loot/WorldChests/" + name + ".prefab";
		GameObject result = PrefabUtility.SaveAsPrefabAsset(root, path);
		UnityEngine.Object.DestroyImmediate(root);
		return result;
	}
```

Hinweis: Die Schauseite der Kiste ist **−z** — dort sitzen im Bestand Schloss und Öffnungshände. Frontplatte und Schloss liegen deshalb auf −z; nichts an den Hand-Positionen ändern.

- [ ] **Step 4: Builder headless laufen lassen** (baut Prefabs neu):

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.WorldChestContentBuilder.RebuildPrefabsForFixes -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t6-build.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t6-build.log" -Pattern "error CS|\[StilE1\]"
```

Erwartet: drei `[StilE1] …: N Dreiecke`-Zeilen mit N ≤ 400, keine `error CS`.

- [ ] **Step 5: Tests laufen lassen** (Filter `Eidren.Tests.EditMode.EidrenWorldStyleTests`, Ergebnis `TestResults-stil-e1-t6.xml`, Log `stil-e1-t6.log`). Erwartet: alle `Passed`, auch die Vier-Zustände- und Fabrik-Tests aus Task 1–4 bleiben grün (Filter zusätzlich einmal ohne Einschränkung auf die vier neuen Testklassen laufen lassen: `-testFilter "Eidren.Tests.EditMode"`).

- [ ] **Step 6: Prüfnachweis** — Logs, XML, Dreieckszahlen notiert.

---

### Task 7: Nachher-Captures, Regressionslauf und Abnahme

**Files:** keine Quellcode-Änderung.

**Interfaces:**
- Consumes: Vorher-PNGs (Task 5), Baseline-XML (Task 0), umgestellte Prefabs (Task 6).

- [ ] **Step 1: Nachher-Captures erzeugen** — wie in Task 5 **ohne `-nographics`**, damit gerendert wird:

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureNachher -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e1-t7-nachher.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Get-ChildItem "C:\Users\phine\Documents\Projekt Eidren\TempReview\StilumbauE1\nachher"
```

Erwartet: 12 PNGs.

- [ ] **Step 2: Volle EditMode-Suite und Namensdiff gegen Baseline**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform EditMode -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-stil-e1-final.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e1-final.log"
Wait-Process -Name Unity -Timeout 1800 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
function Get-Fehlgeschlagen($pfad) {
  ([xml](Get-Content $pfad)).SelectNodes("//test-case[@result='Failed']") | ForEach-Object { $_.fullname } | Sort-Object
}
$basis = Get-Fehlgeschlagen "C:\Users\phine\Documents\Projekt Eidren\TestResults-stil-e1-baseline.xml"
$neu = Get-Fehlgeschlagen "C:\Users\phine\Documents\Projekt Eidren\TestResults-stil-e1-final.xml"
Compare-Object $basis $neu
```

Erwartet: `Compare-Object` liefert **keine Einträge mit `=>`** (keine neuen Fehlschläge). Einträge mit `<=` (verschwundene Fehlschläge) sind zulässig und werden notiert.

- [ ] **Step 3: Bildpaare begutachten**

Alle 12 Vorher/Nachher-Paare nebeneinander ansehen. Abnahmekriterien aus der Spec:

1. Kein Nachher-Bild ist leer oder einfarbig.
2. Planken, Sockel und Bogendeckel sind erkennbar (Segmentierung sichtbar).
3. Familien bleiben unterscheidbar (Holz / Eisen+3 Bänder+Kantenpanzer / Bewuchs).
4. Die vier Zustände sind je Familie unterscheidbar; `Opened` zeigt den vollen, `PartiallyEmptied` den flachen Loot, `Emptied` keinen.
5. Farbwelt entspricht den Vorher-Bildern (gleiche Grundtöne, keine Neuabstimmung).

- [ ] **Step 4: Abnahmebericht schreiben**

`Documentation/Etappen/Stilumbau/STILUMBAU_E1_ABNAHME.md`: je Kriterium ✓/✗ mit Bildverweis, Dreieckszahlen je Familie, Namensdiff-Ergebnis, Liste der erzeugten/geänderten Assets. Dem Auftraggeber die Bildpaare zeigen und Rückmeldung einholen, bevor die Etappe für abgeschlossen erklärt wird.

---

## Selbstreview-Vermerk

- Spec-Abdeckung Etappe 1: Fabrik (Task 1/2), Shader/Material (Task 3), Loot-Füllstand (Task 4), Builder-Umstellung (Task 6), Captures/Abnahme (Task 5/7), Sicherung/Baseline (Task 0). Etappen 2–5 folgen als eigene Pläne nach Abnahme des Piloten.
- Bestandsverträge stehen wörtlich in Task 6 (Colliderwerte, Teil-Namen, Größenvektoren, `ConfigureVisual`-Aufruf).
- Typkonsistenz: `EidrenMeshFactory.LoftProfile`/`LoftShape` werden in Task 6 voll qualifiziert als geschachtelte Typen verwendet — Task 2 definiert sie **innerhalb** der Klasse `EidrenMeshFactory` (geschachtelt), damit `EidrenMeshFactory.LoftProfile` gültig ist.
