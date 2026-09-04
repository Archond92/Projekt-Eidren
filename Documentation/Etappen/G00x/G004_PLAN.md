# G-004 Bodenschatten und Objektverankerung — Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** Jede Figur und jedes stehende Weltobjekt steht sichtbar auf dem Boden — echter weicher Schattenwurf trägt die Form, ein builder-eigenes Kontaktdecal trägt die Standposition.

**Architektur:** `WorldContactShadowBuilder` wird vom Nachlauf-Werkzeug zum gemeinsamen Helfer mit der neuen Methode `Attach(GameObject)`. Jeder der sieben erzeugenden Builder ruft sie in seinem `Save()`-Engpass auf, bevor das Prefab geschrieben wird — dadurch ist die Erdung Teil des reproduzierbaren Bauwegs und kann von keinem künftigen Builder-Lauf mehr verloren gehen. Ein EditMode-Test über alle 80 Prefabs macht die Kopplung zum Vertrag. Zusätzlich werden weiche Schatten im URP aktiviert.

**Spezifikation:** [G004_ENTWURF.md](G004_ENTWURF.md)

**Tech Stack:** Unity 6000.3 (lokal unter `.unity-editor/`), URP 17.3 (Gamma + LDR), NUnit (EditMode in `Assets/_Game/Editor/Tests/`), C#-Editor-Builder.

## Globale Vorgaben

- Projektpfad: `C:\Users\phine\Documents\Projekt Eidren` (Leerzeichen! Immer quoten, Aufrufoperator `&`, nie `Start-Process`).
- **Nur** der mitgelieferte Editor: `C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe`, voller Pfad in jedem Dispatch. Ein `GetInstanceID`-Fehler bedeutet falscher Editor → STOPP.
- **Exit-Code lügt.** Erfolg nur, wenn das `-logFile` frei von `error CS` ist bzw. die Ergebnis-XML existiert und die erwarteten Ergebnisse meldet. Bei Player-Builds zusätzlich auf den PASS-Marker prüfen.
- **`&` kehrt zurück, bevor Unity fertig ist.** In dieser Umgebung mehrfach belegt (11.08.2026: Player-Build, Höhenlauf, voller Suite-Lauf) — `$LASTEXITCODE` bleibt leer und das Log ist unvollständig.

  **`Wait-Process -Name Unity` allein genügt NICHT.** Ist Unity zum Aufrufzeitpunkt noch nicht als Prozess registriert, findet `Wait-Process` nichts und kehrt sofort zurück; der Tool-Aufruf endet und **nimmt Unity mit**. Genau so ist der erste volle Suite-Lauf nach 2496 statt 3333 Logzeilen ohne Ergebnis-XML gestorben.

  Verbindliches Muster: Lauf im Hintergrund starten, erst auf das **Erscheinen**, dann auf das **Verschwinden** des Prozesses warten:

```bash
powershell -NonInteractive -Command "& '<voller Unity-Pfad>' -batchmode ..." &
sleep 15
for i in $(seq 1 240); do
  if ! tasklist 2>/dev/null | grep -qi "Unity.exe"; then break; fi
  sleep 10
done
```

  Erst danach Log und Ergebnis-XML prüfen. Ohne das wird ein noch laufender Lauf als fehlgeschlagen fehlgedeutet — oder abgeschossen.
- Vor JEDEM Unity-Aufruf: `Test-Path "Library\UnityLockfile"` — wenn vorhanden, arbeitet der parallele codex-Agent. Warten, **niemals löschen**.
- `-runTests` **nie** mit `-quit`. Capture-Läufe **ohne** `-nographics`.
- Unity-Läufe im Vordergrund; auf `Test run completed` im Log UND Existenz der XML warten.
- **Kein `git commit` für Quelldateien** — das Repo trackt per `.gitignore` (`*`) nur fünf Release-Textdateien. Beleg je Task sind Logdatei + Ergebnis-XML mit dem Präfix `g004-…`.
- Code-Sprache: deutsche Kommentare, Tabs als Einrückung, `sealed`-Klassen — wie im Bestand.
- Für Dateisuchen `Select-String` statt Grep (binäre Szenen!).
- Regressionsmaßstab: **Namensdiff** gegen `TestResults-stil-e5-final.xml` (831 Tests, 760 bestanden, 71 bekannte Fehlschläge), **nicht** `failed="0"`.
- Regressionsfilter ab Task 4 immer inklusive `Eidren.Tests.EditMode.VisualAssetTests`, `VisualScaleTests`, `EidrenWorldStyleTests`, `V02ContainerVisualTests`, `StyleProofContentTests`.
- Keine erfundenen Belege. Ein nicht erbrachter Nachweis wird als nicht erbracht gemeldet.

## Bestandsverträge (unverändert)

- Prefabpfade und -namen aller sieben Gruppen.
- Collider, Interaktionsreichweiten, Abbau-Erträge, Bauraster-Footprints.
- `SunEuler = (71.7, 156.2, 0)` und `SunShadowStrength = 0.55f` — G-003-Invariante.
- `DynamicActorGroundShadow` und die sechs Akteur-Schattentexturen.
- Gestalt des Decals aus G-003: Spitzendeckung `111/255`, Farbton `(0.11, 0.15, 0.18)`, Alpha `0,38`, Breitenfaktor `1,1`, Tiefenfaktor `0,55`, Bodenabstand `0,018`, Deckel `4,5 × 3,5`.

---

### Task 0: Vorbedingung klären und sichern

**Files:** keine Quellcode-Änderung.

**Interfaces:**
- Produziert: Freigabe oder Rückmeldung zur A2/A3-Kollision; Sicherungsordner.

- [ ] **Schritt 1: A2/A3-Kollision gegenprüfen**

**Geprüft am 10.08.2026, 21:0x — die Kollision ist aufgelöst.** Die Abschlussarbeiten A1–A4 sind bereits gelaufen: `Documentation/Etappen/Stilumbau/STILUMBAU_ABSCHLUSS.md` existiert (A4), `Assets/_Game/Editor/BaumfixRebuildChain.cs` ist entfernt (A3), `Assets/_Game/Art/StyleProof/Materials/` ist abgebaut (A2). Der Stilumbau ist damit vollständig geschlossen, und dieser Plan kann ohne Wartezeit beginnen.

Der Schritt bleibt als Gegenprüfung stehen, weil der parallele codex-Agent zeitweise im selben Baum arbeitet: Vor Beginn einmal bestätigen, dass der Befund noch gilt. Weicht er ab, gilt die ursprüngliche Lage — dann greift die Begründung unten und die Reihenfolge ist mit dem Auftraggeber zu klären.

*Ursprüngliche Lage, zur Nachvollziehbarkeit:* Zwei der Abschlussarbeiten aus `STILUMBAU_E5_PLAN.md:39–43` kollidieren mit diesem Plan:

- **A3** entfernt tote Konstanten in `ForgeContainerVisualBuilder` und `T2ResourceVisualBuilder` — **dieselben Dateien**, die Task 4 hier anfasst.
- **A2** weist per GUID-Suche über alle Prefabs nach, dass abzubauende Materialien unreferenziert sind. Dieser Nachweis muss **nach** der letzten Prefab-Änderung laufen, sonst belegt er den falschen Stand.

Prüfen, ob A1–A3 bereits gelaufen sind:

```powershell
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\Documentation\*.md" -Pattern "STILUMBAU_ABSCHLUSS|A1 —|A2 —|A3 —" | Select-Object -First 10
Test-Path "C:\Users\phine\Documents\Projekt Eidren\Documentation\Etappen\Stilumbau\STILUMBAU_ABSCHLUSS.md"
Test-Path "C:\Users\phine\Documents\Projekt Eidren\Assets\_Game\Editor\BaumfixRebuildChain.cs"
```

Existiert `BaumfixRebuildChain.cs` wieder oder fehlt `STILUMBAU_ABSCHLUSS.md`, ist der Baum in einem anderen Zustand als bei der Planerstellung. Dann beim Auftraggeber die Reihenfolge erfragen. Empfehlung in dem Fall: **A1–A3 zuerst**, dann dieser Plan, dann A4.

- [ ] **Schritt 2: Quellstand sichern** (Quellcode ist NICHT versioniert — einzige Absicherung)

```powershell
$stamp = Get-Date -Format "yyyyMMdd-HHmm"
$ziel = "C:\Users\phine\Documents\Eidren-Sicherungen\vor-g004-$stamp"
New-Item -ItemType Directory -Force $ziel | Out-Null
Copy-Item "C:\Users\phine\Documents\Projekt Eidren\Assets\_Game" "$ziel\_Game" -Recurse
Copy-Item "C:\Users\phine\Documents\Projekt Eidren\Assets\_Game\Settings\Eidren_URP.asset" "$ziel\Eidren_URP.asset"
Get-ChildItem $ziel | Select-Object Name
```

Erwartet: Ordner `_Game` und die URP-Datei liegen im Sicherungsordner.

- [ ] **Schritt 3: Lockfile prüfen**

```powershell
Test-Path "C:\Users\phine\Documents\Projekt Eidren\Library\UnityLockfile"
```

Erwartet: `False`. Bei `True` warten.

---

### Task 1: Ist-Höhen messen und Schwellwert festlegen

**Files:**
- Create: `Assets/_Game/Editor/GroundContactAudit.cs`
- Create: `Documentation/Etappen/G00x/G004_HOEHENTABELLE.md`

**Interfaces:**
- Produziert: `GroundContactAudit.SchreibeHoehenbericht()` — Editor-Menüpunkt, schreibt eine CSV mit Prefabpfad und `bounds.size.y`. Task 3 übernimmt den bestätigten Schwellwert.

- [ ] **Schritt 1: Auditwerkzeug schreiben**

`Assets/_Game/Editor/GroundContactAudit.cs`:

```csharp
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/* G-004 Task 1: misst die zusammengefassten Rendererbounds jedes Weltprefabs.
	   Grundlage fuer den Hoehenschwellwert der Aufnahmeregel — gemessen statt geraten. */
	public static class GroundContactAudit
	{
		public static readonly string[] Ordner =
		{
			"Assets/_Game/Prefabs/Resources/Visuals",
			"Assets/_Game/Prefabs/Environment/AreaArtVariants",
			"Assets/_Game/Prefabs/Environment/StyleProof",
			"Assets/_Game/Prefabs/Buildings/Level01",
			"Assets/_Game/Prefabs/Loot/WorldChests",
			"Assets/_Game/Prefabs/Containers/Forge",
			"Assets/_Game/Prefabs/Stations"
		};

		[MenuItem("Eidren/V0.2/G004/Hoehenbericht schreiben")]
		public static void SchreibeHoehenbericht()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Ordner;Prefab;HoeheY;BreiteX;TiefeZ;Renderer");
			int gezaehlt = 0;
			foreach (string ordner in Ordner)
			{
				if (!Directory.Exists(ordner))
				{
					Debug.LogWarning("[G004] Ordner fehlt: " + ordner);
					continue;
				}
				foreach (string datei in Directory.GetFiles(ordner, "*.prefab"))
				{
					string pfad = datei.Replace('\\', '/');
					GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
					if (prefab == null)
					{
						continue;
					}
					Renderer[] renderer = prefab.GetComponentsInChildren<Renderer>(true);
					if (renderer.Length == 0)
					{
						sb.AppendLine($"{ordner};{Path.GetFileNameWithoutExtension(pfad)};0;0;0;0");
						continue;
					}
					Bounds b = renderer[0].bounds;
					for (int i = 1; i < renderer.Length; i++)
					{
						b.Encapsulate(renderer[i].bounds);
					}
					sb.AppendLine($"{ordner};{Path.GetFileNameWithoutExtension(pfad)};" +
						$"{b.size.y:F3};{b.size.x:F3};{b.size.z:F3};{renderer.Length}");
					gezaehlt++;
				}
			}
			File.WriteAllText("g004-hoehen.csv", sb.ToString(), Encoding.UTF8);
			Debug.Log($"[G004] Hoehenbericht: {gezaehlt} Prefabs nach g004-hoehen.csv");
		}

		public static void SchreibeHoehenberichtFuerAutomation()
		{
			SchreibeHoehenbericht();
		}
	}
}
```

- [ ] **Schritt 2: Bericht erzeugen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
  -executeMethod Eidren.Editor.GroundContactAudit.SchreibeHoehenberichtFuerAutomation -quit `
  -logFile "C:\Users\phine\Documents\Projekt Eidren\g004-t1-hoehen.log"
```

Danach prüfen: `Select-String -Path "g004-t1-hoehen.log" -Pattern "error CS|\[G004\] Hoehenbericht"`. Erwartet: kein `error CS`, eine Zeile mit der Prefabzahl (Sollwert 80; jede Abweichung benennen).

- [ ] **Schritt 3: Schwellwert bestimmen und dokumentieren**

`g004-hoehen.csv` nach Höhe sortieren. `Documentation/Etappen/G00x/G004_HOEHENTABELLE.md` schreiben mit: vollständiger Tabelle je Gruppe, der Liste aller Prefabs unter 0,5 m, und der Festlegung des Schwellwerts.

Der Wert muss so liegen, dass `BLD_Floor_L01` und flacher Bodenbewuchs **darunter**, alle stehenden Objekte **darüber** liegen. Ergibt die Messung keine saubere Lücke, den Befund benennen und den Auftraggeber entscheiden lassen — **nicht** stillschweigend einen Wert wählen, der ein Objekt falsch einsortiert.

Erwartetes Ergebnis: ein belegter Zahlenwert, der in Task 3 als `MindestHoehe` eingesetzt wird.

---

### Task 2: Vorher-Captures

**Files:** keine Quellcode-Änderung.

**Interfaces:**
- Produziert: Vergleichsbasis unter `TempReview/G004/vorher/`.

- [ ] **Schritt 1: Lockfile prüfen, dann G-001-Strecke laufen lassen**

Den Aufruf aus `G001_ABNAHME.md` übernehmen (Capture-Läufe **ohne** `-nographics`), Ausgabeordner `G001-Captures/Stand-vor-G004/`.

- [ ] **Schritt 2: Nicht-Leerheit prüfen**

Jedes erzeugte PNG über die in G-001 etablierte Helligkeitsstreuungsprüfung laufen lassen. Erwartet: kein einfarbiges Bild. Ein schwarzes Capture ist ein Abbruchgrund, kein Schönheitsfehler — siehe G-001.

- [ ] **Schritt 3: Kopie als Vergleichsbasis sichern**

```powershell
$q = "C:\Users\phine\Documents\Projekt Eidren\G001-Captures\Stand-vor-G004"
$z = "C:\Users\phine\Documents\Projekt Eidren\TempReview\G004\vorher"
New-Item -ItemType Directory -Force $z | Out-Null
Copy-Item "$q\*.png" $z
(Get-ChildItem $z -Filter *.png).Count
```

---

### Task 3: `Attach` im Helfer, testgetrieben

**Files:**
- Modify: `Assets/_Game/Editor/WorldContactShadowBuilder.cs`
- Create: `Assets/_Game/Editor/Tests/GroundContactTests.cs`

**Interfaces:**
- Produziert: `WorldContactShadowBuilder.Attach(GameObject root)` → `bool` (true = Decal angehängt). `WorldContactShadowBuilder.MindestHoehe` → `float`. Task 4 ruft beides auf.
- Konsumiert: den in Task 1 festgelegten Schwellwert.

- [ ] **Schritt 1: Den fehlschlagenden Test schreiben**

`Assets/_Game/Editor/Tests/GroundContactTests.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using Eidren.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	/* G-004: Vertrag statt Konvention. Diese Klasse wird rot, sobald ein
	   Builder-Lauf die Erdung eines Weltobjekts wegwirft — genau der Fehler,
	   der die G-003-Decals im Stilumbau restlos gekostet hat. */
	public sealed class GroundContactTests
	{
		private static IEnumerable<string> AllePrefabs()
		{
			foreach (string ordner in GroundContactAudit.Ordner)
			{
				if (!Directory.Exists(ordner))
				{
					continue;
				}
				foreach (string datei in Directory.GetFiles(ordner, "*.prefab"))
				{
					yield return datei.Replace('\\', '/');
				}
			}
		}

		/* NUR MeshRenderer — dieselbe Regel wie WorldContactShadowBuilder.Attach.
		   Sprite-Glyphen (F-005) und Partikel zaehlen nicht zur festen Geometrie. */
		private static Bounds Rendererbounds(GameObject prefab, out int anzahl)
		{
			MeshRenderer[] renderer = prefab.GetComponentsInChildren<MeshRenderer>(true);
			anzahl = renderer.Length;
			if (anzahl == 0)
			{
				return new Bounds(Vector3.zero, Vector3.zero);
			}
			Bounds b = renderer[0].bounds;
			for (int i = 1; i < anzahl; i++)
			{
				b.Encapsulate(renderer[i].bounds);
			}
			return b;
		}

		[Test]
		[TestCaseSource(nameof(AllePrefabs))]
		public void Weltprefab_ErfuelltDieAufnahmeregel(string pfad)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
			Assert.That(prefab, Is.Not.Null, "Prefab nicht ladbar: " + pfad);

			Bounds bounds = Rendererbounds(prefab, out int rendererZahl);
			Transform decal = prefab.transform.Find(WorldContactShadowBuilder.ChildName);

			if (rendererZahl == 0 || bounds.size.y <= WorldContactShadowBuilder.MindestHoehe)
			{
				Assert.That(decal, Is.Null,
					$"{Path.GetFileName(pfad)} liegt mit {bounds.size.y:F2} m unter der " +
					$"Aufnahmeregel ({WorldContactShadowBuilder.MindestHoehe} m), " +
					"traegt aber ein Kontaktdecal.");
				return;
			}

			Assert.That(decal, Is.Not.Null,
				$"{Path.GetFileName(pfad)} steht {bounds.size.y:F2} m hoch, " +
				"hat aber kein Kontaktdecal (ContactShadow).");

			MeshRenderer renderer = decal.GetComponent<MeshRenderer>();
			Assert.That(renderer, Is.Not.Null, "ContactShadow ohne MeshRenderer: " + pfad);
			Assert.That(renderer.sharedMaterial, Is.Not.Null, "ContactShadow ohne Material: " + pfad);
			Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_World_ContactShadow"),
				"ContactShadow nutzt ein fremdes Material: " + pfad);
			Assert.That(renderer.shadowCastingMode,
				Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off),
				"Das Decal darf selbst keinen Schatten werfen: " + pfad);
		}

		[Test]
		[TestCaseSource(nameof(AllePrefabs))]
		public void Weltprefab_HatHoechstensEinKontaktdecal(string pfad)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
			Assert.That(prefab, Is.Not.Null, "Prefab nicht ladbar: " + pfad);
			int treffer = 0;
			foreach (Transform kind in prefab.transform)
			{
				if (kind.name == WorldContactShadowBuilder.ChildName)
				{
					treffer++;
				}
			}
			Assert.That(treffer, Is.LessThanOrEqualTo(1),
				$"{Path.GetFileName(pfad)} traegt {treffer} Kontaktdecals — " +
				"ein wiederholter Builder-Lauf hat verdoppelt.");
		}
	}
}
```

- [ ] **Schritt 2: Test laufen lassen und Rot belegen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
  -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.GroundContactTests" `
  -testResults "TestResults-g004-t3-red.xml" -logFile "g004-t3-red.log"
```

Erwartet: Die Klasse kompiliert **nicht**, weil `MindestHoehe` fehlt. Das ist das gewünschte Rot der ersten Runde. Nach Schritt 3 erneut laufen lassen; dann erwartet: alle Fälle über der Höhenregel schlagen fehl („hat aber kein Kontaktdecal"), die Fälle darunter bestehen. Fehlschlagzahl notieren — sie ist der Sollwert für Schritt 5 von Task 5.

- [ ] **Schritt 3: `Attach` und `MindestHoehe` ergänzen**

In `Assets/_Game/Editor/WorldContactShadowBuilder.cs`, nach der Konstanten `DepthScale` einfügen (Wert aus Task 1 einsetzen, hier der Vorschlagswert):

```csharp
	// G-004: Aufnahmeregel. Ein Kontaktdecal bekommt, was hoeher steht als
	// dieser Wert — Bodenplatten, Wege und flacher Bewuchs fallen darunter
	// heraus, ohne dass jemand eine Ausnahmeliste pflegen muss.
	// 0,2 statt der urspruenglich vorgeschlagenen 0,3: bei 0,3 fielen fuenf
	// Ressourcen im ABGEBAUTEN Zustand unter die Regel, waehrend ihr aktiver
	// Zustand darueber lag — der Bodenschatten waere beim Abbauen sichtbar
	// weggesprungen. Messung und Herleitung: G004_HOEHENTABELLE.md.
	public const float MindestHoehe = 0.2f;

	/* G-004: Haengt das Kontaktdecal an ein im Bau befindliches Objekt. Aufruf
	   aus dem Save()-Engpass jedes erzeugenden Builders, unmittelbar vor
	   SaveAsPrefabAsset. Damit ist die Erdung Teil des Bauwegs statt eine
	   nachtraegliche Dekoration — der Grund, warum die G-003-Decals im
	   Stilumbau verlorengingen. Idempotent. */
	public static bool Attach(GameObject root)
	{
		if (root == null || root.transform.Find(ChildName) != null)
		{
			return false;
		}
		// NUR MeshRenderer. Die drei Weltkisten tragen die Sprite-Glyphen
		// OpeningHand_Left/Right (F-005); ueber alle Renderer gemessen ergaeben
		// sie 11,4 x 6,15 x 8,26 statt der tatsaechlichen 1,2 x 0,59 x 0,79 —
		// das Decal waere am Breitendeckel abgeschnitten und laege als Platte
		// um die Kiste. Beleg: G004_HOEHENTABELLE.md, Abschnitt Messkorrektur.
		MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		if (renderers.Length == 0)
		{
			return false;
		}
		Bounds bounds = renderers[0].bounds;
		for (int i = 1; i < renderers.Length; i++)
		{
			bounds.Encapsulate(renderers[i].bounds);
		}
		if (bounds.size.y <= MindestHoehe)
		{
			return false;
		}
		CreateDecal(root.transform, bounds, EnsureMaterial());
		return true;
	}
```

`CreateDecal` und `EnsureMaterial` bleiben unverändert; `CreateDecal` ist bereits `private static` und im selben Typ erreichbar.

- [ ] **Schritt 4: Test erneut laufen lassen**

Befehl wie Schritt 2, aber `-testResults "TestResults-g004-t3-red2.xml" -logFile "g004-t3-red2.log"`.

Erwartet: kompiliert, `Weltprefab_HatHoechstensEinKontaktdecal` durchgehend grün, `Weltprefab_ErfuelltDieAufnahmeregel` rot für jedes Prefab über der Höhenregel. Genau dieses Rot behebt Task 4.

---

### Task 4: Die sieben Builder anbinden

**Files:**
- Modify: `Assets/_Game/Editor/T1ResourceVisualBuilder.cs`
- Modify: `Assets/_Game/Editor/T2ResourceVisualBuilder.cs`
- Modify: `Assets/_Game/Editor/AreaArtVariantPrefabBuilder.cs`
- Modify: `Assets/_Game/Editor/StyleProofPropBuilder.cs`
- Modify: `Assets/_Game/Editor/EidrenBuildingVisualBuilder.cs`
- Modify: `Assets/_Game/Editor/WorldChestContentBuilder.cs`
- Modify: `Assets/_Game/Editor/ForgeContainerVisualBuilder.cs`
- Modify: `Assets/_Game/Editor/StorageContentBuilder.cs`

**Interfaces:**
- Konsumiert: `WorldContactShadowBuilder.Attach(GameObject)` aus Task 3.

- [ ] **Schritt 1: Den Engpass je Builder finden**

Jeder Builder speichert über eine private Hilfsmethode. In `StyleProofPropBuilder` ist es `Save(GameObject root)` bei Zeile 473, unmittelbar vor `PrefabUtility.SaveAsPrefabAsset(root, path)` in Zeile 482. Für jeden der acht Dateien die entsprechende Stelle bestimmen:

```powershell
foreach ($b in @("T1ResourceVisualBuilder","T2ResourceVisualBuilder","AreaArtVariantPrefabBuilder","StyleProofPropBuilder","EidrenBuildingVisualBuilder","WorldChestContentBuilder","ForgeContainerVisualBuilder","StorageContentBuilder")) {
  $p = "C:\Users\phine\Documents\Projekt Eidren\Assets\_Game\Editor\$b.cs"
  Write-Output "--- $b"
  Select-String -Path $p -Pattern "SaveAsPrefabAsset" | Select-Object LineNumber, Line
}
```

Die Zeilennummern in den Task-Bericht schreiben. Baut ein Builder mehrere Prefabs über verschiedene Wege, gilt: **jeder** Pfad zu `SaveAsPrefabAsset` bekommt den Aufruf.

Erhebung vom 10.08.2026 als Sollwert — weicht die Zählung ab, hat sich der Bestand geändert und der Befund gehört in den Bericht:

| Builder | Speicherstelle | im Umfang |
| --- | --- | --- |
| `T1ResourceVisualBuilder` | 288 | ja |
| `T2ResourceVisualBuilder` | 210 | ja |
| `AreaArtVariantPrefabBuilder` | 54 | ja |
| `StyleProofPropBuilder` | 482 (in `Save()` ab 473) | ja |
| `EidrenBuildingVisualBuilder` | 99 (im `try` ab 79) | ja |
| `WorldChestContentBuilder` | 276 | ja |
| `ForgeContainerVisualBuilder` | 161 | ja |
| `StorageContentBuilder` | 109 → `Resources/UI/StorageWindow.prefab` | **nein — UI-Fenster** |
| `StorageContentBuilder` | 214 → `Resources/Prefabs/DeathBag.prefab` | **nein — außerhalb der sieben Ordner** |
| `StorageContentBuilder` | 244 → `Stations/StorageChest.prefab` | ja |
| **`CraftingContentBuilder`** | **238 → `Stations/Workbench.prefab`** | **ja** |

**Zwei Korrekturen gegenüber dem ersten Planentwurf**, beide bei der Erhebung am Bestand aufgefallen:

- `CraftingContentBuilder` fehlte ganz. `Workbench.prefab` wird nicht von `StorageContentBuilder` geschrieben; ohne diesen Eintrag bliebe die Werkbank dauerhaft ohne Erdung.
- `StorageContentBuilder` hat drei Speicherstellen, aber nur **eine** gehört in den Umfang. Ein Kontaktdecal am UI-Fenster wäre schlicht falsch.

`AreaArtAssetBuilder` hat **keine** eigene Speicherstelle; er treibt `AreaArtVariantPrefabBuilder` und wird deshalb nicht geändert, sondern in Task 5 nur aufgerufen.

- [ ] **Schritt 2: Aufruf einfügen**

Unmittelbar **vor** jedem `PrefabUtility.SaveAsPrefabAsset(root, path);` einfügen:

```csharp
		// G-004: Bodenerdung als Bauteil, nicht als Nachlauf.
		WorldContactShadowBuilder.Attach(root);
```

Der Variablenname des Wurzelobjekts kann je Builder abweichen (`root`, `go`, `prefabRoot`) — den tatsächlichen Namen der Speicherstelle verwenden. Bei `EidrenBuildingVisualBuilder` liegt die Stelle innerhalb eines `try`-Blocks um `LoadPrefabContents`/`UnloadPrefabContents` (Zeilen 79–103); der Aufruf gehört **vor** `SaveAsPrefabAsset` in Zeile 99, innerhalb des `try`.

- [ ] **Schritt 3: Kompilierung prüfen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
  -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.GroundContactTests" `
  -testResults "TestResults-g004-t4-compile.xml" -logFile "g004-t4-compile.log"
```

Erwartet: kein `error CS`. Die Testergebnisse sind hier noch unverändert rot — die Prefabs sind noch nicht neu gebaut. Das erledigt Task 5.

---

### Task 5: Builder-Läufe und Platzierungsbeleg

**Files:** keine Quellcode-Änderung (nur bei Befund).

**Interfaces:**
- Produziert: 80 Prefabs mit Kontaktdecal; grüne `GroundContactTests`.

- [ ] **Schritt 1: Zeitstempel vor dem Lauf festhalten**

```powershell
$ordner = @("Resources\Visuals","Environment\AreaArtVariants","Environment\StyleProof","Buildings\Level01","Loot\WorldChests","Containers\Forge","Stations")
foreach ($o in $ordner) {
  Get-ChildItem "C:\Users\phine\Documents\Projekt Eidren\Assets\_Game\Prefabs\$o" -Filter *.prefab -ErrorAction SilentlyContinue |
    Select-Object FullName, LastWriteTime
} | Export-Csv "C:\Users\phine\Documents\Projekt Eidren\g004-t5-vorher.csv" -NoTypeInformation
```

- [ ] **Schritt 2: Alle sieben Builder laufen lassen**

Lockfile prüfen. Dann in **einem** Unity-Start über die vorhandenen Menü-Einstiegspunkte:

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
  -executeMethod Eidren.Editor.GroundContactRebuildChain.BaueAlle -quit `
  -logFile "C:\Users\phine\Documents\Projekt Eidren\g004-t5-rebuild.log"
```

Dafür `Assets/_Game/Editor/GroundContactRebuildChain.cs` anlegen — ein temporärer Helfer nach dem Muster des in A3 zu entfernenden `BaumfixRebuildChain.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/* G-004 Task 5: ruft die sieben erzeugenden Builder in EINEM Unity-Start
	   auf. Temporaerer Helfer — nach der Abnahme wieder entfernen (Task 7). */
	public static class GroundContactRebuildChain
	{
		public static void BaueAlle()
		{
			// Einstiege am Bestand abgelesen (Task 4 Schritt 1), nicht geraten:
			// T1/T2/StyleProof/Building tragen `internal static void Build()`,
			// WorldChest/Forge `public static void Build()`, Storage `BuildAll()`.
			// AreaArtVariantPrefabBuilder hat KEINEN eigenen Einstieg — seine
			// Prefabs entstehen ueber die beiden Zonenvarianten-Laeufe.
			T1ResourceVisualBuilder.Build();
			T2ResourceVisualBuilder.Build();
			AreaArtAssetBuilder.BuildTierOne();
			AreaArtAssetBuilder.BuildTierTwo();
			StyleProofPropBuilder.Build();
			EidrenBuildingVisualBuilder.Build();
			WorldChestContentBuilder.Build();
			ForgeContainerVisualBuilder.Build();
			StorageContentBuilder.BuildAll();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("[G004] Rebuild-Kette abgeschlossen.");
		}
	}
}
```

Alle Aufrufe sind am Bestand verifiziert. `internal` genügt, weil der Helfer in derselben Assembly `Eidren.Editor` liegt.

**Korrektur aus der Ausführung — die beiden Stations-Prefabs lassen sich nicht neu bauen.** `BuildStorageChestPrefab` und `BuildWorkbenchPrefab` kehren früh zurück, solange das Prefab die handgebaute `Geometry_A14`-Geometrie trägt (Etappe-2-Entscheid des Stilumbaus: „StorageChest, Workbench und DeathBag bleiben unangetastet"). Beide tragen sie. Die `Attach`-Aufrufe in diesen Buildern laufen im Bestand also nie.

Zusätzlich scheitert `CraftingContentBuilder.BuildAll()` vollständig an einer fremden Lücke:

```
FileNotFoundException: Art source is missing: Assets/_Game/Art/Items/ITEM_TMP_SmithingMark.png
  at ItemContentAssetBuilder.BuildItemDefinitions()
  at CraftingContentBuilder.BuildAll()
```

Die Kette ruft deshalb **weder** `StorageContentBuilder.BuildAll()` **noch** `CraftingContentBuilder.BuildAll()` auf. Die beiden Stations-Prefabs bekommen ihr Decal direkt über `WorldContactShadowBuilder.AddToPrefab(pfad)`. Das ist hier unbedenklich: Bei Prefabs, die per Entscheid vor Neubau geschützt sind, gibt es keinen Lauf, der die Erdung wegwerfen könnte — und `GroundContactTests` bewacht sie trotzdem.

Die `Attach`-Aufrufe in beiden Buildern bleiben stehen, damit ein späterer Neubau die Erdung nicht verliert.

- [ ] **Schritt 3: Log inhaltlich prüfen**

```powershell
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\g004-t5-rebuild.log" -Pattern "error CS|Exception|\[G004\] Rebuild"
```

Erwartet: kein `error CS`, keine Exception, die Abschlusszeile vorhanden.

- [ ] **Schritt 4: Nur beabsichtigte Dateien geändert**

Zeitstempel erneut erheben und gegen `g004-t5-vorher.csv` diffen. Erwartet: genau die Prefabs der sieben Ordner sind neu, nichts außerhalb. Jede Abweichung benennen.

- [ ] **Schritt 5: `GroundContactTests` grün**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
  -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.GroundContactTests" `
  -testResults "TestResults-g004-t5-gruen.xml" -logFile "g004-t5-gruen.log"
```

Erwartet: `failed="0"` über alle Fälle beider Testmethoden.

- [ ] **Schritt 6: Idempotenz belegen**

Die Rebuild-Kette aus Schritt 2 ein **zweites** Mal laufen lassen (Log `g004-t5-rebuild2.log`), dann Schritt 5 wiederholen (`TestResults-g004-t5-gruen2.xml`). Erwartet: weiterhin `failed="0"`, insbesondere kein Prefab mit zwei Decals. Das belegt die Idempotenz von `Attach`.

---

### Task 6: Weiche Schatten und Bildratenmessung

**Files:**
- Modify: `Assets/_Game/Settings/Eidren_URP.asset:66`
- Modify: `Assets/_Game/Editor/ZoneLightingBuilder.cs:54`

**Interfaces:** —

- [ ] **Schritt 1: Bildrate vorher messen**

An den Kamerapositionen der G-001-Strecke bei voller Gegnerzahl. Das Verfahren aus der G-002-/G-003-Abnahme übernehmen (dort ist die Bildratenprüfung bereits etabliert) und die Zahlen festhalten. **Ohne Vorher-Zahl ist Kriterium 8 nicht nachweisbar** — diesen Schritt nicht überspringen.

- [ ] **Schritt 2: Weiche Schatten anschalten**

In `Assets/_Game/Settings/Eidren_URP.asset` Zeile 66:

```
  m_SoftShadowsSupported: 1
```

In `Assets/_Game/Editor/ZoneLightingBuilder.cs` Zeile 54:

```csharp
	public const LightShadows SunShadows = LightShadows.Soft;
```

Den Kommentarblock in den Zeilen 52–53 mitziehen: er begründet die Stärke 0,55 ausdrücklich mit `m_SoftShadowsSupported 0`. Die Begründung ist nach dieser Änderung überholt und muss neu gefasst werden, sonst steht im Code eine falsche Erklärung.

`SunEuler` und `SunShadowStrength` bleiben unverändert.

- [ ] **Schritt 3: Zonen neu beleuchten**

`ZoneLightingBuilder.Apply` über seinen vorhandenen Einstiegspunkt für alle acht Zonenszenen laufen lassen (Log `g004-t6-lighting.log`). Erwartet: kein `error CS`, alle Szenen verarbeitet. Nebeneffekt beachten: `Apply` ruft `AddDecorationContactShadows` — die eingebackenen Dekorations-Decals werden dabei aufgefrischt, was erwünscht ist.

- [ ] **Schritt 4: Bildrate nachher messen**

Verfahren wie Schritt 1, dieselben Positionen. Erwartet: kein messbarer Abfall.

Fällt die Rate messbar ab, gilt die Rückfallebene aus dem Entwurf: `SunShadows` zurück auf `LightShadows.Hard` und `m_SoftShadowsSupported: 0`, das Kontaktdecal trägt die Erdung allein. Diese Entscheidung im Abnahmebericht ausweisen, nicht stillschweigend treffen.

---

### Task 7: Abnahme

**Files:**
- Create: `Documentation/Etappen/G00x/G004_ABNAHME.md`
- Delete: `Assets/_Game/Editor/GroundContactRebuildChain.cs`

**Interfaces:** —

- [ ] **Schritt 1: Nachher-Captures**

G-001-Strecke wie in Task 2, Ausgabeordner `G001-Captures/Stand-G004/`, Kopie nach `TempReview/G004/nachher/`. Nicht-Leerheit prüfen.

- [ ] **Schritt 2: Bewegungsnachweis für Kriterium 4**

Je ein Capture stehend, in Bewegung und während der Ausweichrolle. Diese drei Bilder sind der einzige Nachweis, dass sich die Figur beim Rollen sichtbar vom Boden löst — ohne sie bleibt Kriterium 4 offen.

- [ ] **Schritt 3: Stichprobe je Objektgruppe**

Ein Bild je der sieben Gruppen mit sichtbarer Erdung, aus der tatsächlichen Spielkamera.

- [ ] **Schritt 4: Volle Suite und Namensdiff**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "C:\Users\phine\Documents\Projekt Eidren" `
  -runTests -testPlatform EditMode `
  -testResults "TestResults-g004-final.xml" -logFile "g004-final.log"
```

Namensdiff gegen `TestResults-stil-e5-final.xml` nach dem in `STILUMBAU_E4_ABNAHME.md` Abschnitt 2 dokumentierten Verfahren. Erwartet: **0 neue Fehlschläge** (`=>`). Die Gesamtzahl steigt um die Fälle der neuen `GroundContactTests`.

- [ ] **Schritt 5: Temporären Helfer entfernen**

`Assets/_Game/Editor/GroundContactRebuildChain.cs` und die zugehörige `.meta` löschen, danach Schritt 4 wiederholen, um zu belegen, dass nichts davon abhing.

- [ ] **Schritt 6: Abnahmebericht schreiben**

`Documentation/Etappen/G00x/G004_ABNAHME.md` nach dem Muster von `G003_ABNAHME.md`: Kurzfassungstabelle über die acht Kriterien, je Kriterium der konkrete Beleg (Bild, Zahl oder Testname), die beiden Abweichungen aus Entwurfsabschnitt 4 ausdrücklich ausgewiesen, offene Punkte, Nachvollziehen-Abschnitt mit allen Befehlen.

**Kriterium 5 wird als gegenstandslos ausgewiesen**, mit dem Codebeleg `EidrenSceneStructureBuilder:242–246`. Nicht als erfüllt behaupten.

Der Bericht spricht keine Freigabe aus — die Entscheidung liegt beim Auftraggeber.

---

## Selbstreview-Vermerk

**Spec-Abdeckung:** Entwurf 3.1 → Task 6; 3.2 → Tasks 3–5; 3.3 → Tasks 1 und 3; 3.4 (Nichtänderungen) → Bestandsverträge oben; Abschnitt 5 (acht Kriterien) → Task 7 Schritte 1–4 und 6; Abschnitt 6 (Tests) → Tasks 3 und 7; Abschnitt 7 (Abnahme) → Task 7; Abschnitt 9.1 → Task 0 Schritt 1, 9.2 → Task 1, 9.3 → Task 0 Schritt 1.

**Typkonsistenz:** `Attach(GameObject)` → `bool` und `MindestHoehe` → `float` werden in Task 3 definiert und in Task 4 sowie im Test aus Task 3 unter genau diesen Namen verwendet. `ChildName` ist Bestand (`WorldContactShadowBuilder:35`). `GroundContactAudit.Ordner` wird in Task 1 definiert und in Task 3 vom Test wiederverwendet — eine Ordnerliste, nicht zwei.

**Beim Selbstreview gefunden und behoben:** Die Rebuild-Kette in Task 5 rief zunächst `AreaArtVariantPrefabBuilder.Build()` und `StorageContentBuilder.Build()` auf — beide existieren nicht. Der Bestand trägt `AreaArtAssetBuilder.BuildTierOne()`/`BuildTierTwo()` beziehungsweise `StorageContentBuilder.BuildAll()`. Außerdem hat `StorageContentBuilder` **drei** Speicherstellen statt einer; Task 4 hätte sonst zwei davon übersehen und zwei Stationsprefabs wären ohne Erdung geblieben, ohne dass ein Test es gemerkt hätte — die betroffenen Prefabs liegen unter der Testabdeckung nur, wenn sie die Höhenregel erfüllen.

**Bewusst offen gelassen:** Der Schwellwert in Task 3 Schritt 3 trägt den Vorschlagswert 0,3; verbindlich ist die Messung aus Task 1.

**Beim Selbstreview aufgelöst:** Die zunächst als blockierend formulierte A2/A3-Kollision ist gegenstandslos — die Abschlussarbeiten sind am 10.08.2026 bereits gelaufen (Beleg in Task 0 Schritt 1). Der Schritt bleibt als Gegenprüfung stehen, weil der parallele codex-Agent im selben Baum arbeitet.

**Verbleibendes Risiko:** Genau dieser parallele Agent. Der Baum hat sich während der Entwurfsarbeit zweimal unter der Hand geändert. Vor jedem Unity-Lauf gilt die Lockfile-Wache, und Task 5 Schritt 4 prüft ausdrücklich, dass nur beabsichtigte Dateien angefasst wurden.
