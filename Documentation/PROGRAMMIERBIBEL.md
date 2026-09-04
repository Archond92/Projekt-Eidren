# Eidren – Programmierbibel

Versionsunabhängige, verbindliche Regeln für allen Code **und alle Mechaniken**
in diesem Projekt.

Diese Datei steht **über** den Dokumenten in `Eidren_V0.x/`. Planungsordner sind
versionsgebunden und werden Historie; diese Datei nicht. Bei Widerspruch gilt
diese Datei.

Arbeitsaufträge sind keine dauerhafte Regelfassung. Nach ihrer Abnahme werden
Ergebnisse hier eingearbeitet, sichtbare Änderungen im `CHANGELOG.md` erfasst
und Prüfstände unter `Documentation/Releases/` abgelegt. Der Auftrag selbst
wird anschließend entfernt; die Historie bleibt im Git-Verlauf beziehungsweise
im Versions-Tag erhalten.

**Aktuelle Plattformstrategie:** Gebaut, gestartet, profiliert und abgenommen
wird vorerst ausschließlich unter Windows. Mobile Versionen kommen später.
Architektur, Eingaben, UI, Safe Areas, Speicherformat und Performance-Budgets
bleiben trotzdem plattformneutral und portierbar. Mobile Toolchains,
plattformspezifische Buildkonfigurationen und Gerätebuilds werden erst in einer
eigenen späteren Portierungsphase eingeführt.

Sie bündelt und ersetzt alle früheren, über mehrere Planungsdokumente
verteilten Architektur- und Designregeln. Ältere Dokumente dürfen nur noch
historische Herleitungen oder Verweise hierher enthalten — keine
konkurrierenden Zweitfassungen.

---

## Wie diese Datei funktioniert

Die Datei hat zwei Teile mit getrennter Nummerierung:

- **Teil A (§1 ff.) – Technik.** Wie gebaut wird. Ändert sich selten.
- **Teil B (M1 ff.) – Mechanik.** Was gebaut wird und warum es so funktioniert.
  Ändert sich laufend, besonders bei Balancing-Werten.

Eine Datei, nicht zwei: Das Aufsplitten über mehrere Dokumente ist genau der
Grund, aus dem die Vorgängerregeln verrottet sind.

### Durchsetzungsstufen (Teil A)

| Stufe | Bedeutung |
| --- | --- |
| **[E]** | **Erzwungen.** Verstoß bricht die Kompilierung oder einen Test. Kein Mensch nötig. |
| **[T]** | **Testbar.** Ein Test könnte das prüfen. Existiert er noch nicht, steht das dabei. |
| **[K]** | **Konvention.** Nur durch Review prüfbar. Bewusst die schwächste Stufe. |

### Entscheidungszustände (Teil B)

| Zustand | Bedeutung |
| --- | --- |
| **[entschieden]** | Gilt. Änderung nur mit Migrationsplan. |
| **[vorläufig]** | Arbeitsstand, meist Balancing. Ändert sich erwartungsgemäß. |
| **[offen]** | Bewusst nicht entschieden. Vorschläge sind als solche markiert und nicht bindend. |

### Änderungsregel

1. Eine neue Regel wird **nur** mit Stufe bzw. Zustand aufgenommen.
2. Eine **[K]**-Regel, die dreimal verletzt wurde, hat zwei zulässige Enden:
   sie wird nach **[T]** gehoben — oder sie wird **gelöscht**.
   Sie wird nicht ein viertes Mal wiederholt.
3. Regeln in Teil A beschreiben, was in *diesem* Projekt schiefging. Allgemeine
   C#-Ratschläge gehören nicht hierher.
4. **[offen]** wird nie stillschweigend zu **[entschieden]**. Wer entscheidet,
   schreibt die Begründung dazu.

---

# TEIL A — Technik

## §1 Schichten [E]

```
Data (0 Deps)  ←  Core  ←  Gameplay  ←  UI  ←  Composition  ←  Editor
Presentation (0 Deps)  ←────────────────┘
```

- `Eidren.Data` und `Eidren.Presentation` referenzieren **nichts**.
- Keine Referenz zeigt nach oben. Keine Zyklen.
- Eine neue Assembly erweitert den Graphen, sie verzweigt ihn nicht rückwärts.

**Durchsetzung:** `.asmdef`-Dateien. Ein Verstoß ist ein Compile-Fehler.
Das funktioniert bereits und ist die stärkste Struktur im Projekt. Nicht aufweichen —
insbesondere keine Assembly-Referenz „nur kurz zum Testen" hinzufügen.

---

## §2 Statischer Zustand braucht einen Reset [E]

Jedes veränderliche `static`-Feld im Runtime-Code braucht:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void Reset() { /* leeren */ }
```

**Warum:** In Unity 6 ist *Domain Reload deaktivieren* Standard. Ohne Reset
überleben Caches den Play-Modus und enthalten beim zweiten Start zerstörte Objekte.
Der Fehler zeigt sich erst im zweiten Durchlauf und wird deshalb systematisch
übersehen.

**Erzwungen durch** `ArchitectureGuardTests.MutableStaticState_HasDomainReloadReset`.
Der Test sammelt per Reflection alle veränderlichen statischen Felder der
Runtime-Assemblies und verlangt einen Reset-Einstiegspunkt in der deklarierenden
Klasse.

**Ausgenommen sind readonly Nachschlagetabellen aus reinen Werttypen oder
Zeichenketten** (etwa `static readonly float[]` mit konstanten Winkeln). Sie
können keine zerstörten Unity-Objekte halten — und genau darum geht es hier.

Der Test fand beim Einführen drei Fälle, die niemandem aufgefallen waren:
`EidrenServiceRoot.Instance`, `StyleProofVisualLibrary._instance` und den
Collider-Puffer in `LootDropPositionResolver`. Alle drei sind behoben.

---

## §3 Gameplay-Timer akkumulieren keine rohen Frame-Deltas [T]

Ein Timer, dessen Ablauf eine **nicht zurücknehmbare** Aktion auslöst, muss:

1. das Delta pro Frame klemmen — ein Frame darf nie das ganze Fenster verbrauchen,
2. eine Mindestanzahl Ticks verlangen, bevor er auslösen darf,
3. seine Vorbedingung am Auslösepunkt erneut prüfen (siehe §4).

```csharp
// falsch — ein 400-ms-Frame verbraucht ein 400-ms-Fenster vollständig
Remaining -= deltaTime;

// richtig
Remaining -= Mathf.Min(deltaTime, Duration * 0.25f);
if (++_ticks < 3) return false;
```

**Warum:** Szenenladen, Shader-Kompilierung und GC-Spitzen erzeugen regelmäßig
Frames von mehreren hundert Millisekunden. Der Spieler bekommt dann keine
Gelegenheit zu reagieren — der Timer springt von „gerade gestartet" auf
„bestätigt".

**Belegt durch:** `MapExitCountdown.Tick` verletzt alle drei Punkte bei 0,4 s
Fensterdauer. Der Test `ReturningInside_CancelsAndReentryRestartsCleanly` schlägt
dadurch reproduzierbar fehl. Im Testcode wurde das Symptom mit einem zusätzlichen
`yield return null` behandelt, nicht die Ursache.

**Test existiert** — er ist aktuell rot und darf nicht durch Anpassen der
Wartezeiten grün gemacht werden (siehe §14).

---

## §4 Irreversible Aktionen prüfen am Commit-Punkt erneut [K]

Vor dem endgültigen Schritt wird die Vorbedingung noch einmal gelesen — nicht auf
ein Ergebnis vertraut, das mehrere Frames alt sein kann.

Betrifft: Szenenwechsel, Speicherstand überschreiben, Item verbrauchen,
Ressource abbauen, Bossphase wechseln.

**Belegt durch:** `MapExitCoordinator.ConfirmExit()` lädt die Zielszene allein
auf Basis des Tick-Ergebnisses und liest die Spielerposition nicht erneut.
Eine letzte Prüfung kostet nichts und fängt die gesamte Fehlerklasse aus §3 ab.

---

## §5 Services werden injiziert, nicht gezogen [E]

`EidrenServiceRoot.Instance` ist zulässig in:

- der Kompositionswurzel selbst,
- Testcode.

Überall sonst kommen Services über `Initialize(...)` bzw. `Configure(...)` herein.

**Warum:** Das ist bereits der Projektstandard und der Grund, warum sich
Gameplay-Klassen isoliert testen lassen. Aktuell gibt es genau **eine**
Runtime-Ausnahme (`ZoneLootController`). Bei einer Ausnahme bleibt es eine
Ausnahme; bei fünf ist es ein Service-Locator und die Testbarkeit ist weg.

**Erzwungen durch** `ArchitectureGuardTests.ServiceRoot_IsNotPulledOutsideComposition`.
Der Test zählt Referenzen auf `EidrenServiceRoot.Instance` außerhalb von
`Composition` gegen eine Obergrenze, die **nur sinken darf**. Sie steht auf 1.

---

## §6 Eine Wahrheitsquelle pro Artefakt [K]

Ein Editor-Builder hat genau zwei zulässige Rollen:

- **Einmal-Migration** — er läuft, das Ergebnis wird zum gepflegten Asset,
  der Builder wird gelöscht. *Oder:*
- **Generator** — sein Ergebnis steht in `.gitignore` und wird nie von Hand
  bearbeitet.

**Niemals beides.** Genau das ist der aktuelle Zustand: Die Szenen liegen als
`.unity`-Assets im Projekt *und* werden von
`EidrenSceneStructureBuilder.BuildZoneScene()` per `NewScene()` überschrieben.
Jede Editor-Anpassung an einer Zone wird beim nächsten Menüklick kommentarlos
vernichtet — ohne Rückfrage, ohne Diff, ohne Undo.

Für ein Spiel mit Level-Design ist Rolle 1 die richtige.

---

## §7 Klassenbudget [E]

| Typ | Grenze |
| --- | --- |
| MonoBehaviour | 400 Zeilen |
| Reine Logikklasse | 300 Zeilen |
| Öffentliche Member pro Klasse | 12 |

Über der Grenze wird aufgeteilt, nicht diskutiert.

**Warum:** Die frühere unbezifferte Regel „Keine God Classes" hat
`GameSession` (522 Zeilen: Weltkarte + Inventar + Waffen + Lager + Flags +
Lebenszustand + Zonenübergänge) und `CombatHUD` (über 1300 Zeilen) nicht
verhindert.
Eine Regel ohne Zahl ist keine Regel. Eine Zahl ist messbar und damit testbar.

**Erzwungen durch** `ArchitectureGuardTests.NoNewFileExceedsTheClassBudget`.

**Bestandsschutz:** Die 18 Dateien, die bei Einführung der Regel bereits über
400 Zeilen lagen, stehen in einer Ausnahmeliste im Test. Ein zweiter Test
(`GrandfatheredBudgetList_ContainsNoStaleEntries`) schlägt fehl, sobald ein
Eintrag erledigt ist und nicht entfernt wurde — **die Liste kann damit nur
kürzer werden.** Wer eine neue Datei einträgt statt sie aufzuteilen, umgeht §7.

---

## §8 `Update()` ist heilig [K]

Nicht in `Update`, `LateUpdate`, `FixedUpdate` oder pro Frame durchlaufenen
Schleifen:

- `Camera.main`, `GetComponent`, `Find*`
- LINQ
- String-Interpolation oder -Konkatenation ohne Änderungsprüfung
- Allokationen jeder Art

**Belegt durch:** `CombatFeedback` ruft `Camera.main` an zwei Stellen pro Frame
und pro Instanz auf — bei vielen Schadenszahlen ist das N Lookups pro Frame.

**Positiv anzumerken:** Die String-Erzeugung daneben ist bereits durch eine
`displayedTenth`-Prüfung geschützt und läuft nur zehnmal pro Sekunde. Genau so.

---

## §9 Logik gehört nicht ins MonoBehaviour [K]

Entscheidungslogik kommt in reine C#-Klassen ohne Unity-Abhängigkeit. Das
MonoBehaviour liest Input, ruft die Logik und schreibt das Ergebnis zurück.

**Warum das nicht verhandelbar ist:** 232 Tests, die meisten davon EditMode,
laufen in **0,3 Sekunden**. Die PlayMode-Tests brauchen 20. Dieser Faktor 60 ist
der Grund, warum überhaupt getestet wird. Er existiert nur, weil
`MapExitCountdown`, `MapExitDirectionResolver` und `CombatTargetGeometry` keine
Szene brauchen.

Jede Logik, die in ein MonoBehaviour rutscht, wandert von der 0,3-Sekunden-Seite
auf die 20-Sekunden-Seite — oder wird gar nicht getestet.

---

## §10 Versionskontrolle [K]

- **Vor** jedem Lauf eines Editor-Builders wird committet. Builder überschreiben
  ohne Rückfrage (§6).
- Generierte und Werkzeugartefakte gehören nicht ins Repo:
  `TestResults-*.xml`, `.utmp/`, `.doc-review/`, `.doc_review/`,
  `Assets/InitTestScene*.unity`.
- Ein roter Test wird nicht committet, ohne dass er im Commit benannt wird.

**Bestandsabweichung, und sie ist ernst.** Auf dem Release-Branch besteht
`.gitignore` aus `*` mit fünf Ausnahmen. `Assets/` und `Documentation/` sind
dort **nicht** versioniert — `git status` meldet „sauber", während ein
kompletter Szenenneubau auf der Platte liegt. Der erste Punkt oben ist damit
auf diesem Branch nicht erfüllbar.

Solange das so ist, tritt an die Stelle des Commits eine **Sicherung unter
`Backups/<datum>-<vorhaben>/`** mit einer `LIESMICH.md`, die benennt, welche
Datei warum gesichert wurde und was zum Zurückdrehen nötig ist. Neu angelegte
Dateien werden dort nicht gesichert, sondern aufgezählt — sie zu löschen
genügt. Wer ohne Sicherung an ausgerollte Assets geht, hat keinen Weg zurück.

Diese Abweichung ist kein Dauerzustand, sondern eine offene Baustelle: die
Bibel selbst lag zwischenzeitlich nur in der Git-Historie eines anderen
Branches und war im Arbeitsbaum nicht auffindbar.

---

> **§11 ist absichtlich leer.** Der Abschnitt hieß ursprünglich
> „Weltgenerierung und Ökonomie" und wurde beim Aufteilen in Teil A und Teil B
> zu **M1**. Die Nummer bleibt frei, damit bestehende Querverweise gültig bleiben.

---

## §12 Stabile IDs [T]

IDs sind **Save-Keys**. `PlayerInventory.ExportSlots()` schreibt sie direkt ins
JSON; `GameSession.SetStorageState` wirft sogar explizit, wenn eine Container-ID
fehlt. Eine umbenannte ID ist ein kaputter Spielstand — bei einem Spiel mit
Monatshorizont (M0) keine Theorie.

- Format `lower_snake_case`.
- Konstanten leben an **genau einer Stelle je Domäne** (`ItemIds`,
  `CraftingRecipeIds`, `AudioEventIds`, `BossProgressIds`, `EidrenScenes`).
- IDs werden **nie aus Anzeigenamen abgeleitet**. Der Anzeigename darf sich
  jederzeit ändern, die ID nie.
- IDs tragen **keine veränderlichen Eigenschaften**: kein Tier, keine Seltenheit,
  keine Zone. Solche Angaben sind Felder (M1.4).
- Eine ausgemusterte ID wird **nie wiederverwendet**. Sonst bekommen alte
  Spielstände stillschweigend das falsche Objekt.

  **Ausgemustert und gesperrt:** `hammer_upgrade_level_2` (Item) und
  `upgrade_hammer_level_2` (Rezept) — entfallen mit den Waffen-Upgrades (M2,
  M8.8). Kommen Upgrades zurück, bekommen sie **neue** IDs.
- JSON speichert ausschließlich primitive Werte und stabile IDs, **niemals
  Unity-Objektreferenzen**.

**Beispiel für die dritte Regel:** Wird aus dem Bufffood künftig „Brot" (M8.4),
bleibt die ID `buff_food`. Sie beschreibt die *Funktion*, und die ist stabil —
der Anzeigename ist es nicht.

**Test fehlt noch.** Alle ID-Konstanten einsammeln und auf Format, Eindeutigkeit
und Kollision prüfen.

---

## §13 Unity-Null ist nicht C#-Null [K]

Auf `UnityEngine.Object` immer `if (x != null)`. **Nie `?.`, nie `??=`,
nie `is null`.**

`??=` und `?.` prüfen auf **Referenz-Null** und umgehen Unitys überladenen
`==`-Operator. Ein zerstörtes Objekt ist für `==` null, für `??=` aber nicht.

**Belegt durch:** `EidrenServiceRoot:72–82` legt zehn Komponenten per `??=` an —
wäre eine davon zerstört, würde sie nicht neu erzeugt und der folgende Zugriff
würfe. Häufiger trifft es `?.`: `feedback?.Show(...)`,
`_gameSession?.SetAreaStatus(...)` in `MapExitCoordinator` rufen auch auf einem
zerstörten Ziel auf und werfen `MissingReferenceException` — mitten im
Szenenwechsel und damit maximal unangenehm zu finden.

Heute feuert das nicht, weil die Komponenten auf demselben GameObject liegen wie
die Wurzel. Es ist eine geladene Waffe, kein Schuss.

---

## §14 Testdisziplin [K]

- **Ein roter Test wird nicht durch Wiederholen erledigt.**
  Beleg: `TestResults-Items-MapExitRetry{,2,3}.xml` — dreimal isoliert gelaufen,
  dreimal rot, dann liegen gelassen.
- **Zeitabhängige Tests werden nicht durch Anpassen der Wartezeiten grün
  gemacht.** Beleg: der Workaround-Kommentar in `MapExitIntegrationTests`
  behandelt das Symptom, §3 blieb verletzt.
- **EditMode ist der Standard, PlayMode die Ausnahme.** Faktor 60 (0,3 s gegen
  20 s, §9). Wer einen PlayMode-Test schreibt, begründet, warum eine Szene nötig
  ist.

---

## §15 Daten validieren sich selbst [T]

Jedes gameplayrelevante ScriptableObject bringt eine eigene Prüfung mit.
Das Muster existiert bereits: `ItemDefinition.ValidateDefinition()` liefert
Fehler *und* Warnungen, `CraftingRecipeDefinition.TryValidate(out error)` prüft
leere IDs, doppelte Zutaten und fehlende Ergebnisse.

**Was fehlt, ist die Klammer:** ein Test, der **jedes Asset im Projekt** durch
seine eigene Validierung schickt. Ohne den greift die Validierung nur dort, wo
jemand sie zufällig aufruft.

Mit wachsendem Materialbestand (M1.6) wird das wichtiger, nicht unwichtiger —
ein vergessenes Feld fällt sonst erst im Spiel auf.

---

## §16 UI wird autoriert, nicht erzeugt [K]

Fenster, HUDs und Menüs sind **Prefabs**. Sie werden nicht zur Laufzeit aus Code
zusammengebaut.

**Belegt durch:** `InventoryWindow`, `CraftingWindow`, `StorageWindow` und
`PauseMenu` waren bereits Prefabs. `CombatHUD` war die Ausnahme und baute sich
mit über 1300 Zeilen und sechs `Build*`-Methoden selbst zusammen — eine offene
Inkonsistenz mit einem klaren Sieger.

**Warum das über Stil hinausgeht:** Genau dieser Aufbau macht den ersten Frame
nach einem Szenenwechsel teuer — und ein einzelner teurer Frame ist die Ursache
des Fehlers aus §3. Die beiden Regeln hängen zusammen.

**Nachgeführt am 20.08.2026 — die Ausnahme ist geschlossen.**
`Assets/_Game/Scripts/UI/CombatHUD.cs` hat **98 Zeilen** und **keine einzige
`Build*`-Methode** mehr; das HUD liegt als
`Assets/_Game/Resources/UI/CombatHUD.prefab`. Ergänzungen kommen seither
ausschließlich über Editor-Builder nach §6 hinein (`WeaponIconHudBuilder`,
`FixCollectionHudBuilder`, `HudTooltipBuilder`), `CombatHudPrefabTests` prüft
das Prefab auf Vollständigkeit, und `CombatHUD.ValidateReferences` wirft bei
fehlender Referenz — ein Laufzeit-Notbehelf ist damit ausgeschlossen statt nur
unerwünscht. Alle zehn Fenster unter `Resources/UI/` sind heute Prefabs.

Der alte Beleg bleibt oben stehen, weil er zeigt, wogegen die Regel geschrieben
wurde. Die Regel selbst gilt unverändert: Sie hat diesen Fall entschieden und
verhindert den Rückfall.

---

## §17 Save-Format-Politik [T]

Das Gate in `SaveGameService:289` ist bereits richtig gebaut — es scheitert
**laut** statt still zu korrumpieren: passende Version lädt, neuere meldet
*„newer than supported"*, ältere ohne Migration meldet *„No migration exists"*.

Was fehlt, ist die Regel dazu:

- Jede Änderung am Save-Format **erhöht `CurrentSaveVersion`** und bringt eine
  Migration mit. Kein Feld wird stillschweigend ergänzt.
- Zusätzlich zur Save-Version trägt der Spielstand die **`GeneratorVersion`**
  der Weltgenerierung (M1.2). Beide sind unabhängig und werden nicht vermischt.
- Ein Spielstand wird **nie stillschweigend repariert**. Entweder es gibt eine
  Migration, oder das Laden scheitert mit Meldung.

**Warum das hier steht:** M0 setzt Monatshorizonte an. Damit ist eine Migration
über die Lebensdauer eines Spielstands keine Möglichkeit, sondern eine
Gewissheit.

---

## §18 Grundregeln

Die früher verteilten Grundregeln sind hier mit Durchsetzungsstufe
zusammengeführt.

- **[K]** ScriptableObjects nur für **statische Daten**. Keine Laufzeitdaten
  darin.
- **[K]** Keine magischen Zahlen. Werte kommen aus Datenobjekten oder
  serialisierten Feldern.
- **[K]** UI entscheidet nicht über Gameplay, sondern reagiert auf Events und
  ViewModels.
- **[K]** Keine externen Frameworks ohne Freigabe. Kein Netzwerk, kein Backend,
  kein Multiplayer.

Die frühere JSON-Regel ist in §12 aufgegangen, die Abbruchregel für zeitbasierte
Aktionen in M2.

---

## §19 Ereignisse werden abgemeldet [K]

Wer `+=` schreibt, schreibt das passende `-=` im selben Arbeitsgang — in
`OnDestroy` **und** vor jedem Neubinden.

Das Muster stimmt bereits: `MapExitCoordinator.UnbindDeath()` wird sowohl in
`OnDestroy` als auch am Anfang von `BindPlayer` aufgerufen, `CombatHUD` meldet
in `OnDestroy` ab. Diese Regel ist **Bestandsschutz**, keine Korrektur.

---

## §20 Wohin gehört eine neue Datei [K]

| Inhalt | Assembly |
| --- | --- |
| ScriptableObject, reine Datenstruktur, ID-Konstanten | `Eidren.Data` |
| Entscheidungslogik ohne Unity-Abhängigkeit, Services | `Eidren.Core` |
| MonoBehaviour mit Szenenbezug: Spieler, KI, Interaktion | `Eidren.Gameplay` |
| Sichtbares ohne Spiellogik: Visuals, Effekte, Kamera | `Eidren.Presentation` |
| Fenster, HUD, Menüs | `Eidren.UI` |
| Verdrahtung, Bootstrap, Zonenaufbau | `Eidren.Composition` |
| Werkzeuge, Builder, Menüpunkte | `Eidren.Editor` |

Im Zweifel gilt §1: Die Assembly, die **am wenigsten** referenzieren muss, ist
die richtige.

**Kein gerätespezifisches Mobile-Budget in dieser Liste.** Mobile Versionen sind
ein späteres Ziel, aber aktuell liegen nur Windows-Messwerte vor. Eine Regel auf
Verdacht verstößt gegen Änderungsregel 3. Bis zur Portierungsphase gelten
plattformneutrale Budgets; gerätespezifische Grenzwerte werden erst nach realen
Messungen ergänzt.

---

## §21 Ein Kompositionspfad, und er ist vollständig [E]

**Es gibt genau einen Weg, auf dem ein spielbarer Zustand entsteht.** Zwei
parallele Aufbauwege laufen unweigerlich auseinander, und zwar lautlos.

**Der konkrete Auslöser ist behoben:** `ZonePlayerSpawner` bindet über
`PlayerPrefabBindings` das initialisierte Eidra-Team, HUD, Interaktion, Kampf,
Inventar, Crafting, Lager, Gegner, Boss, Kamera, Ausgänge, Tod und Pause.
`CompositionCompletenessTests.EveryZone_SpawnsAFullyWiredPlayer` hält diesen
einzigen produktiven Aufbaupfad fest. Der frühere Prototyp-Composition-Pfad ist
entfernt.

**Regeln daraus:**

- **Ein Kompositionspfad.** Solange zwangsläufig zwei existieren (Prototyp und
  Zonen), gilt jede Ergänzung an einem als Fehler am anderen, bis sie dort
  ebenfalls ankommt.
- **Vollständigkeit wird geprüft, nicht aufgezählt.** Ein Test verlangt vom
  frisch gespawnten Spieler, dass **jedes** Pflichtsystem verdrahtet ist. Die
  Prüfung listet die Systeme an einer Stelle, damit ein neues System die Prüfung
  bricht, statt still zu fehlen.
- **Fehlende Verdrahtung ist ein Fehler, kein Nichts.** Die Fehlerart hier war
  stille Auslassung: nichts wirft, nichts warnt, das System ist einfach nicht da.
  Pflichtabhängigkeiten melden sich, wenn sie fehlen.

**Erzwungen durch** `CompositionCompletenessTests.EveryZone_SpawnsAFullyWiredPlayer`.
Der Test lädt alle fünf Szenen und verlangt vom gespawnten Spieler jedes
Pflichtsystem — einschließlich eines **initialisierten** Eidra-Teams mit
aktivem Eidra, denn die bloße Existenz der Komponente genügt nicht.

Er fängt nicht nur den gefundenen Fall, sondern jede künftige Auslassung: Wer
ein neues System in den Spawner einbaut und hier nicht einträgt, bekommt einen
roten Test statt eines stillen Lochs.

---

## §22 Szenen und Prefabs sind Text [T]

Alle Szenen und Prefabs werden als YAML gespeichert
(`m_SerializationMode: 2`, ForceText).

**Warum:** Binäre Szenen sind **nicht diffbar und nicht mergebar**. Man kann
eine Änderung weder prüfen noch einen Konflikt auflösen — und man kann nach
einem Eingriff nicht belegen, dass nur das Gewollte passiert ist.

**Belegt durch:** Beim Nachtragen der Eidra-Referenzen ließ sich nicht durch
einen Diff zeigen, dass die Style-Proof-Arbeit in Greenwood unversehrt blieb.
Der Nachweis musste über Dateigröße und Trefferzahlen geführt werden — ein
Ersatz, kein Beweis.

**Bestandsabweichung:** `m_SerializationMode` steht bereits auf ForceText, aber
`Zone_Greenwood`, `Zone_Quarry`, `Zone_Marsh`, `Zone_EmberRuins` und
`EidraForge` liegen trotzdem **binär** vor. `HomeBase` ist YAML. Die
Einstellung wirkt nur auf neu gespeicherte Assets und hat die Zonen nie
erfasst — auch der vollständige Neubau der Schmiede am 16.08.2026 hat daran
nichts geändert, weil der Builder die vorhandene Datei fortschreibt.

**Konvertierungsversuch, gescheitert — Stand festgehalten:**

- `AssetDatabase.ForceReserializeAssets(...)` auf die vier Szenenpfade lässt sie
  **binär**. Kein Fehler, keine Wirkung.
- `EditorSceneManager.SaveScene(...)` erhält das vorhandene Format, statt es auf
  Text zu bringen — belegt durch die Eidra-Migration, die Greenwood öffnete,
  änderte und speicherte, ohne dass die Datei Text wurde.
- Der Dateikopf weist sie eindeutig als Unity-`SerializedFile` aus
  (Version 22, danach die Versionszeichenkette `6000.3.0f1`).

**[offen] Bewusst nicht erzwungen.** Der verlässliche Weg wäre, den
Serialisierungsmodus projektweit umzuschalten und Unity alles neu schreiben zu
lassen. Das rührt an **jedes** Asset im Projekt, einschließlich der
Style-Proof-Arbeit in Greenwood. Der Nutzen ist real, aber die Umstellung ist
eine eigene, bewusst zu terminierende Aktion — kein Nebenschritt in einem
Fehlerbehebungsdurchgang. Vorher gilt §10: committen.

**Solange die Szenen binär sind, gilt:** Änderungen an ihnen lassen sich nicht
per Diff belegen. Ersatzweise werden Dateigröße und ein gezieltes Gegenlesen der
geänderten Felder herangezogen (§23). Textsuche in einer Binärdatei ist **kein**
Beleg — sie liefert zufällige Treffer.

**Und: was in einer Szene steht, erreicht kein Prefab-Umbau.** Ein Objekt, das
einmal in eine Szene ausgerollt wurde, hängt nicht mehr am Prefab. Wer es
ändern will, ändert es in der Szene — per Editor-Skript, das die Szene öffnet,
schreibt und speichert. Das gilt für Inventarstände, ausgerollte Requisiten und
alles andere Instanziierte. Ein korrigiertes Prefab allein bewirkt nichts;
deshalb gehört zu jeder Fabrik-Korrektur ein Rollout-Lauf (M10.11).

---

## §23 Editor-Schreibzugriffe werden gegengelesen [K]

Wer aus einem Editor-Skript in Assets oder Szenen schreibt, liest das Ergebnis
**von der Platte** zurück und prüft es. Ein Rückgabewert genügt nicht.

**Belegt durch:** `SerializedObject.ApplyModifiedPropertiesWithoutUndo()` setzte
die Referenz im Speicher — die Rückkontrolle am selben Objekt war grün, und
`EditorSceneManager.SaveScene` meldete `true`. Auf der Platte stand trotzdem
`{fileID: 0}`. Erst der direkte Feldschreibzugriff über die öffentliche
`Configure`-API plus `EditorUtility.SetDirty` persistierte.

Ohne Gegenlesen **nach erneutem Öffnen** wäre die Migration als erfolgreich
durchgegangen und hätte nichts bewirkt.

**Zusatzregel:** Migrationen sind **idempotent** und überschreiben vorhandene
Zuweisungen nie. Ein zweiter Lauf ist dann zugleich die Erfolgskontrolle.

---

## §24 Visuelle Inhalts-Assets sind eindeutig [T]

Jeder semantisch eigenständige Spielinhalt besitzt eine **eigene
Primärdarstellung**. Kein Item, Material, Werkzeug, Gebäude, Rohstoff,
Ressourcenknoten, Eidra, Gegner, Weltkartenknoten, Marker oder vergleichbarer
Inhalt darf dasselbe Sprite, dieselbe Textur oder dasselbe identitätsstiftende
Prefab wie ein anderer Inhalt verwenden.

Eine umbenannte Dateikopie mit identischem Bildinhalt erfüllt diese Regel
ebenfalls **nicht**. Eindeutigkeit wird sowohl über die Asset-Referenz als auch
über den Bildinhalt geprüft. Insbesondere sind Platzhalter-Zuordnungen wie
„Beeren zeigen das Brot-/Fleischbild“ oder „Axt, Sense und Spitzhacke zeigen
dasselbe Werkzeug“ unzulässig.

Gemeinsam genutzt werden dürfen ausschließlich technische Systembausteine, die
keine Inhaltsidentität tragen: Shader, Fonts, generische UI-Rahmen,
Vorschau-Materialien, Partikel-Grundsysteme und vergleichbare Infrastruktur.
Sobald ein Baustein ein konkretes Objekt oder einen konkreten Zustand
identifiziert, braucht dieser Inhalt ein eigenes Asset.

**Erzwungen durch:** Content- und Art-Tests prüfen doppelte Asset-Referenzen und
bei Rastergrafiken zusätzlich identische Dateiinhalte. Eine absichtliche
Ausnahme muss in der Bibel selbst benannt sein; eine lokale Test-Ausnahmeliste
ist nicht zulässig.

**[entschiedene Ausnahme für v0.2: reguläre Waffentiers.]** Reguläre
Tier-Varianten derselben Waffengattung teilen zunächst Primärdarstellung,
Gattungsicon und gehaltenen Waffenlayer. Hammer, Kupferhammer und Eisenhammer
verwenden damit dieselbe reguläre Hammerdarstellung; entsprechend gelten
Dolche/Kupferdolche/Eisendolche sowie Kupferspeer/Eisenspeer als je eine
visuelle Familie. Name, Tierfeld, Werte und Haltbarkeit unterscheiden die
Gegenstände im UI. Die drei benannten Waffen Siegelbrecher, Aschenzähne und
Glutdorn benötigen dagegen jeweils eine eigene Primärdarstellung. Diese enge
Ausnahme ist in den Eindeutigkeitstests zentral zu hinterlegen und darf nicht
auf Werkzeuge, Rüstung, Materialien, Gegner oder andere Items ausgeweitet
werden.

**Mehrere Darstellungen für einen Inhalt sind erlaubt, mehrere Inhalte für eine
Darstellung nicht.** Die Regel verbietet **Teilen**, nicht **Vielfalt**. Ein
Inhalt darf mehrere Bilder tragen, wenn ein **Feld** entscheidet, welches gilt —
so wie der Ressourcenknoten je Gebiet eine eigene Gestalt bekommt (M11.4). Dann
gilt zusätzlich:

- Die Varianten sind **untereinander verschieden**, sonst sind sie Dateikopien
  und fallen unter Absatz 2.
- Sie tragen ein **gemeinsames Erkennungsmerkmal**, damit der Spieler denselben
  Inhalt wiedererkennt. Beim Baum ist es die Axt mit dem ockerfarbenen Tuch
  (M11.4).
- Jede Variante ist in **allen** Zuständen vollständig. Ein aktiver Zustand mit
  vier Gestalten und ein erschöpfter mit einer ist keine Variante, sondern eine
  halbe.

**Diese Regel sagt nichts über die Größe.** Ein eindeutiges Bild in falscher
Größe erfüllt §24 vollständig und ist trotzdem falsch. Größen stehen in M10.

---

## §25 Objektgrößen stehen im Prefab, nicht in einer Methode [T]

**Gilt für Geometrie genauso wie für Sprites.** Der Abschnitt ist an einem
Billboard-Fehler entstanden und beschreibt ihn deshalb an Sprites; die Regel
selbst hängt nicht an der Bauform. Ein Modell, dessen Größe aus einem
Methodenaufruf statt aus dem `Transform` kommt, verliert sie beim Speichern
genauso (M10.8, M10.9).

Die Weltgröße eines darstellenden Objekts ist **serialisierter Zustand**. Sie
gehört in den `Transform` des Prefabs oder in das Sprite selbst — niemals in
einen Methodenaufruf, den ein Editor-Builder vor dem Speichern absetzt.

**Der konkrete Fehler, der diese Regel erzwungen hat.**
`RuntimeSpriteVisual.SetPose(...)` schreibt in `_poseScale` — ein privates Feld
**ohne** `[SerializeField]`. Unity serialisiert es nicht; angewandt wird es erst
in `LateUpdate` und nur bei gesetztem `_poseEnabled`, das ebenfalls nicht
serialisiert ist. `StyleProofContentBuilder` setzt genau diesen Aufruf ab und
speichert danach das Prefab. Ergebnis: das Sprite überlebt, die Höhe nicht.

**21 Prefabs sind so entstanden** — 16 Deko-Props, die vier aktiven
Ressourcenknoten und das Wildling-Visual. Sie rendern seither die rohe
Atlas-Zellgröße. Weil eine Atlas-Zelle für alle Motive gleich groß ist, sind
Baum, Faserpflanze und Kupferader auf die Zehntelstelle **gleich hoch**
(1,41 Einheiten) — kleiner als der Spieler. Nichts daran hat gewarnt: kein
Compilerfehler, kein Test, keine Konsolenmeldung, kein Blick in die Prefab-YAML,
weil dort schlicht `m_LocalScale: {1, 1, 1}` steht und das harmlos aussieht.

**Gegenprobe:** Was durch `WorldVisualAssetBuilder.AddSpriteVisual` läuft, setzt
`transform.localScale` direkt und hat die gewollte Höhe — Beerenstrauch und die
neun Gebäude. Zwei Mechanismen im selben Projekt, einer davon funktioniert.

Daraus folgt:

- **Editor-Builder setzen `transform.localScale`** oder erzeugen das Sprite mit
  passendem `pixelsPerUnit`. Zum Setzen einer Höhe rufen sie **kein** `SetPose`.
- **`SetPose` bleibt für Laufzeitposen zulässig** — Billboard-Rotation,
  Todespose, Treffer-Zucken. Es ist kein Autorenwerkzeug.
- **Laufzeitpfade, die die Höhe in das Sprite rechnen, sind zulässig.**
  `RuntimeSpriteVisual.SetArt` tut das über
  `pixelsPerUnit = texture.height / worldHeight` und funktioniert. Genau darum
  sind Spieler, Eidra und Garon richtig groß, während die Prefabs es nicht sind.
- **Wer eine Pose auf eine Skalierung multipliziert, setzt voraus, dass die
  Skalierung da ist.** Die Todespose in `WildlingController` rechnet
  `Vector3.Scale` auf `localScale` und war deshalb doppelt falsch: der Faktor
  wirkte auf 1 statt auf die gebackene Höhe. Solche Faktoren werden nach jeder
  Größenkorrektur neu geprüft.
- **Höhe und Collider stammen aus derselben Zeile** einer Tabelle (M10.3). Ein
  Sprite, das unabhängig von seinem Collider skaliert wird, ist ein Fehler, auch
  wenn es gut aussieht.

**Erzwungen durch:** einen Test, der für jedes Visual-Prefab die sichtbare
Silhouettenhöhe gegen die Tabelle aus M10.3 prüft. **Existiert noch nicht** —
bis dahin gilt diese Regel als [T] und wird bei jeder Art-Änderung von Hand
gegengelesen. Mit dem Test wird sie [E].

---

## §26 Begehbarkeit wird als Weg geprüft, nicht als Fläche [T]

Ein Test, der prüft, **dass** ein NavMesh existiert, prüft nichts. Geprüft
wird, **ob man irgendwo hinkommt**: ein vollständiger Pfad vom Startpunkt bis
zum entferntesten Pflichtziel der Szene.

**Der konkrete Fehler.** Beim Neubau der Eidra-Schmiede war die Bossarena
unerreichbar, ohne dass ein einziger Test es bemerkte. Alle vorhandenen
NavMesh-Tests waren Existenzprüfungen. Erst der Wegtest in
`EidraForgeSceneTests` — Windfang bis Grube — hat es gefunden.

Drei Randbedingungen, die dabei Zeit gekostet haben und die niemand zweimal
entdecken muss:

- **Rampen müssen echte Schrägen sein, keine Stufen.** Der Agent hat Radius
  0,5 und erodiert von jeder Kante einen halben Meter. Ein Auftritt von einem
  Meter behält null Breite — die Stufen bleiben unverbunden. Längere Auftritte
  sprengen bei sechs Metern Gefälle die Kletterhöhe 0,75. Nur die durchgehende
  Schräge hält beide Grenzen: 10 Einheiten Lauf auf 6 Gefälle, rund 31°. Eine
  Rampe belegt deshalb die volle Schenkelbreite.
- **Eine geschlossene Hilfsfläche über dem ganzen Grundriss muss vom Backen
  ausgenommen werden** (`NavMeshModifier` mit `ignoreFromBuild`). `WalkableGround`
  spannt sich als Kasten über die gesamte Zone, Oberkante auf 0; gebacken
  entsteht daraus eine flache Laufebene über Grube und Rampen, und alles
  darunter ist unerreichbar. Der **Collider muss trotzdem bleiben** —
  `ZoneController` verlangt die Referenz per `Require`, und
  `LootDropPositionResolver` raycastet darauf. Löschen ist keine Lösung.
- **Wände dürfen an Rampen nicht entstehen, auch nicht von der Nachbarfläche
  aus.** Sonst läuft die Stützwand des Nachbarn über die volle Länge und
  mauert den Rampenausgang zu. Die Prüfung muss **beide** Seiten der Kante
  ansehen, nicht nur die eigene Zelle.

**Erzwungen durch:** den Wegtest je begehbarer Szene. Für die Schmiede prüft
`EidraForgeSceneTests` den vollen Weg vom Windfang bis in die Grube. Die
Außenzonen prüfen in `ZoneStructurePlayModeTests` bisher nur eine 20 Einheiten
lange Strecke durch die Kartenmitte — das ist eine Durchlässigkeitsprobe, kein
Erreichbarkeitsbeweis für Ziele am Rand. Sobald eine Außenzone Pflichtziele
bekommt, gilt für sie dieselbe Anforderung wie für die Schmiede.

---

## §27 Ein Messwert gilt nur unter seinen Bedingungen [K]

Messen ist besser als schätzen — aber ein Messwert trägt seine Messbedingungen
mit sich, und wer ihn davon löst, hat wieder geschätzt.

**Der konkrete Fehler.** Die Albedowerte der Schmiede sind zweimal korrigiert
worden. Ursprünglich lagen sie bei 0,12 und waren unlesbar. Die erste
Korrektur auf 0,22 entstand in `ForgeGlowProbe` — dort stand aber ein
einzelnes Objekt unter starkem Richtungslicht nah an der Kamera. In einem
Raum, in den eine 5,5 hohe Wand ihren Schatten wirft und den nur ein schwaches
Punktlicht erreicht, gilt diese Messung nicht mehr: Boden und Wände blieben
schwarz.

`ForgeLichtProbe` hat den Grund mit Zahlen geklärt. Ein Punktlicht hebt eine
Fläche mit Albedo 0,5 um das **Vierzehnfache** und damit stärker als der
eingebaute URP-Lit-Shader im selben Aufbau (7,7-fach). Der Zusatzlichtpfad im
Weltshader arbeitet also einwandfrei — dunkel blieb das Verlies, weil die
Flächen fast nichts zurückwarfen. **Licht kann nicht sichtbar machen, was kein
Licht reflektiert.**

Daraus folgt:

- Zu jedem übernommenen Messwert gehört notiert, **unter welchen Bedingungen**
  er entstanden ist: Lichtquelle, Abstand, Beschattung, Kameralage.
- Ein Wert, der unter Richtungslicht stimmt, stimmt im Schatten nicht. Wer ihn
  dort braucht, misst dort erneut.
- Eine Probe mit einem einzelnen freistehenden Objekt ist kein Beleg für einen
  Raum. Die Probe muss die Bedingungen des Einsatzorts nachstellen.

Dasselbe gilt für die Lichtwerte aus G-003: die Sonnentiefe ist ein fixer
Sollwert, der Bildwinkel dagegen ein Messwert — und Tonemapping bleibt in
Gamma + LDR wirkungslos, egal was das Volume-Profil behauptet.

---

# TEIL B — Mechanik

## M0 Leitlinien

Steht bewusst vor allem anderen. Wer eine Einzelmechanik ändert, prüft sie
gegen diese drei Sätze.

- **[entschieden] Das Spiel darf schwer sein.** Es soll ausdrücklich *nicht*
  einfach sein — und das gilt für alle Systeme, nicht nur den Kampf.
  Wer eine Mechanik „zugänglicher" macht, begründet das gegen diese Zeile.
- **[entschieden] Eidra sind wichtig.** Sie sind keine Begleit-Deko. Sie tragen
  Kampf (M2, M3) *und* Wirtschaft (M1.5, M6). Änderungen, die Eidra optional
  machen, verfehlen den Kern.
- **[entschieden] Zielhorizont sind Monate.** Die Endstufe zu erreichen soll
  monatelanges Spielen verlangen. Das ist die Referenz für jede Balancing-Frage,
  nicht ein Wochenende.

**Zur Auslegung:** *Schwer* und *langsam* sind getrennte Achsen (M1.5). Beide
dürfen hoch stehen. Aber wenn eine Änderung nur die langsame Achse hochzieht,
ist sie meistens keine Verbesserung der ersten Leitlinie.

---

## M1 Weltgenerierung und Ökonomie

### M1.1 Regeneration

- **[entschieden]** Außengebiete regenerieren bei Heimkehr in die Heimatbasis.
  Dungeons reagieren nicht auf diese Zonenregeneration: Ihr fester Aufbau und
  ihre laufende Instanz bleiben erhalten. Ein abgeschlossenes Gewölbe darf nur
  über seinen ausdrücklich definierten eigenen Cooldown- und Resetvertrag neu
  bestückt werden. Für die erste Eidra-Schmiede gilt der bezahlte Reset aus
  M13.6.
- **[entschieden]** Einziger Auslöser ist die **Heimkehr in die Heimatbasis**.
  Kein Realzeit-Timer in v0.1. Ein solcher darf später nur *additiv* dazukommen.
- **[entschieden]** Reroll durch Heimlaufen ist akzeptiert. Die Laufkosten und
  der Abbauaufwand bleiben bestehen; das genügt als Bremse.
- **[entschieden]** **Alle Außengebietskarten sind gleich groß.** Keine Zone ist
  größer oder kleiner als eine andere. Nur der Inhalt variiert.
  *Begründung:* Gleiche Größe macht Zonen direkt vergleichbar — der Spieler
  wählt die Zone nach dem, was sie liefert, nicht danach, welche „die große" ist.
  Und es macht Balancing trivial: eine Knotenzahl bedeutet in jeder Zone dasselbe.
- **[entschieden]** Kartengröße **80** (Spielfläche 76 × 76).
  Als Konstante `EidrenSceneStructureBuilder.StandardZoneSize` hinterlegt; die
  Heimatbasis hat ihre eigene Konstante. **Umgesetzt** — Steinbruch, Nebelmoor
  und Glutruinen wurden von 64 bzw. 72 angeglichen, Greenwood stand bereits
  richtig und blieb unangetastet.
  Abgesichert durch `AllOutdoorZones_ShareTheStandardSize`.
  Was diese Zahl in Spielergrößen bedeutet, steht in M10.2: 76 Einheiten
  Spielfläche sind **38 Spielerhöhen**.
- **Achtung bei künftigen Änderungen:** `BuildCoreSceneStructure()` und
  `BuildMapExitScenes()` bauen **alle** Zonen mit `NewScene()` neu und würden
  Greenwood mitsamt der dortigen Style-Proof-Arbeit überschreiben (§6, M9.6).
  Für gezielte Eingriffe existiert `ResizeOutdoorZonesToStandard()`, das
  Greenwood ausspart.
- **[entschieden]** Die **Heimatbasis ist von dieser Regel ausgenommen**. Sie ist
  kein Außengebiet und muss Platz für Gebäude bieten. Ihre Größe richtet sich
  nach dem Basisbau (M6), nicht nach dieser Regel.
- **[entschieden] Die Heimatbasis trägt einen endlichen Einmalvorrat, der nie
  nachwächst.** Er ist der Anfangsbestand des Spielers und die einzige Ausnahme
  von „die Basis ist kein Abbaugebiet" (M6) — was hier abgebaut ist, ist für
  immer weg. `ResourceRespawnMode.None` existiert dafür bereits.
  Ein vollständig abgearbeiteter Knoten verschwindet mitsamt seinem
  Restvisual; bereits gutgeschriebene Rohstoffe werden nicht noch einmal als
  Bild oder Weltobjekt dargestellt. Seine Entfernung bleibt über
  `HarvestedNodeIds` beziehungsweise den persistenten Zonenzustand nach
  Speichern, Laden und erneutem Betreten erhalten.
  *Begründung:* **Der leere Vorrat ist die Ausgangstür des Einstiegs.** Die Basis
  muss dem Spieler nicht erklären, dass er hinausgehen soll — sie geht einfach
  aus.
- **[vorläufig] Umfang des Vorrats: rund 26 Holz-, 18 Stein- und 10 Faserknoten,
  dazu einige Beerensträucher.** Von Hand (Ertrag 1, M1.9) trägt das genau bis
  Werkbank, handgefertigte Axt, Lagerkiste und Hammer — drei
  Technologiefreischaltungen plus das von Anfang an bekannte Axtrezept, danach
  ist die Basis leer. **In der Heimatbasis ist der Engpass der
  Technologiepunkt, nicht das Material** (M8.6); draußen ist es umgekehrt.
  Bei zu hoher Knotendichte ist der Hebel **wenigere, ergiebigere Vorratsknoten**
  — ein Holzstapel ist kein Baum und darf mehr auf einmal geben.

### M1.2 Determinismus

- **[entschieden]** Layout wird **aus einem Seed abgeleitet, nicht gespeichert**.
  Der Spielstand hält `{ZoneId, Seed, GeneratorVersion, HarvestedNodeIds[]}`.
  Damit bleibt er unabhängig von der Spieldauer konstant klein.
- **[entschieden]** `GeneratorVersion` ist Pflicht. Ändert sich der Generator,
  erzeugt derselbe Seed eine andere Karte — ohne dieses Feld ist das nicht
  einmal erkennbar. Es ergänzt das bestehende `SaveGameData.saveVersion`,
  ersetzt es nicht.
- **[entschieden]** Kein globaler `UnityEngine.Random` in der Generierung.
  Pro Zone eine eigene `System.Random`-Instanz aus dem Zonen-Seed —
  wie in `LootRoller.Roll(table, deterministicSeed)` bereits etabliert.
  Ein globaler Unity-RNG mit State-Restore ist ausdrücklich unzulässig.
- **[entschieden]** Alles Gewürfelte einer Zone stammt aus deren Seed, auch
  Knotenerträge. `ResourceNode.AvailableYield` per `UnityEngine.Random` ist
  damit unzulässig — sonst liefert dieselbe Karte unterschiedliche Ausbeute.
- **Folge:** Innerhalb eines Regenerationsfensters ergibt Verlassen und
  Wiederbetreten dieselbe Karte mit denselben abgebauten Knoten. Das schließt
  das Reroll-Loch ohne Zusatzmechanik.

### M1.3 Ressourcenverteilung

- **[entschieden]** Menge und Art kommen aus
  `ZoneDefinition.ResourceAllocations`. Diese Daten existieren bereits, werden
  aber heute nur vom Editor-Builder gelesen. Der Konsument wandert auf die
  Runtime-Seite; die Datenform bleibt.
- **[entschieden]** **Die Knotenanzahl je Zone ist fest. Keine Streuung.**
  Variation entsteht ausschließlich aus der **Platzierung**, die vollständig
  aus dem Zonen-Seed gewürfelt wird. Damit sieht jede Karte neu aus und liefert
  trotzdem planbare Mengen.
  *Begründung:* Optik und Menge sind unabhängige Ziele. Streuung auf der Menge
  erkauft keine zusätzliche Optik, kostet aber Planbarkeit — und bei Heimkehr
  als einzigem Regenerationsauslöser kostet ein schlechter Wurf einen
  vollständigen Rückweg.
- **[entschieden]** Zufall wandert in **Bonusfunde und Weltkisten**, nicht in
  die garantierte Knotenzahl. Ein Außengebiet darf kein Rohmaterial oder Item
  eines Tiers erzeugen, das für dieses Gebiet noch nicht zugelassen ist.
  Insbesondere liefern die T0/T1-Bestandsgebiete keine T2-Knoten und keine
  T2-Ausrüstung. Seltene Funde zeigen innerhalb der erlaubten Tiergrenze nach
  oben, nie über die Fortschrittssperre hinweg.
- **[entschieden]** Zonen sind materialtypisiert. Jede Zone bleibt dauerhaft die
  beste Quelle für mindestens ein Material (siehe M1.7).

### M1.4 Tiers

> **Umfang v0.2: T0, T1 und T2.** Die vier Bestandsgebiete tragen T0-Rohstoffe
> und Kupfererz. Nach dem ersten Abschluss der Eidra-Schmiede werden drei
> vollständige T2-Gebiete mit Hartholz, Sumpfhanf, Granit und Eisenerz
> freigeschaltet. T3 bleibt Zukunft. Das Datenmodell trägt `tier` als Feld.

- **[entschieden]** Tier ist ein **Feld**, kein Namensbestandteil.
  `ItemDefinition` erhält `materialFamily` (z. B. `"wood"`) und `tier`.
  Item-IDs sind Save-Keys — ein späteres `wood` → `wood_t1` wäre eine Migration.
- **[entschieden] T0 ist die Stufe, mit der das Spiel beginnt.** Weichholz,
  Bruchstein und Pflanzenfaser, dazu Beeren und Weizensamen. Aus ihnen entstehen
  die ersten Werkzeuge, die ersten Waffen und die ersten Gebäude. T0-Material
  geht **ohne Veredelung** direkt ins Rezept.
- **[entschieden] Das Erzfeld hat keine T0-Stufe.** Kupfer ist vollständig T1.
  *Begründung:* Genau deshalb sind die ersten Werkzeuge aus Holz, Stein und
  Faser, und genau deshalb ist der erste Barren ein Ereignis statt einer
  Zwischenstufe. Die Materialtabelle erzählt die Progression von selbst.
- **[entschieden] Veredelung wird je Familie und Tier ausdrücklich als Rezept
  definiert; sie ist nicht automatisch rekursiv.** T2-Holz, -Stein und -Erz
  werden direkt aus ihrem T2-Rohstoff veredelt. Robustes Tuch bindet zusätzlich
  ein Seil und hält dadurch die ältere Faserwirtschaft relevant. Die exakten
  Rezepte stehen in M13.3.
- **[entschieden]** Rezeptdaten und sämtliche Craftingansichten unterstützen
  ein bis höchstens fünf verschiedene Zutatenarten. Dieselbe zentrale Grenze
  validiert Daten und UI (M5).
- **[entschieden]** Alte Rohstoffe behalten ihren Wert durch ausdrücklich
  autorierte Fertigwarenrezepte, Bindematerialien, Resetkosten und
  Basisproduktion — nicht durch eine unsichtbare allgemeine Rekursionsformel.
- **[vorläufig]** Mengen je Veredelung und Fertigware bleiben der
  Balancinghebel. Neue Zwischenprodukte werden nur eingeführt, wenn sie eine
  eigene wirtschaftliche Rolle besitzen.

### M1.5 Ökonomische Kopplung

- **[entschieden]** Basis-Produktion ist **kein Nebenfeature**. Höhere Rezepte
  binden ausgewählte ältere Materialien wie Brett, Seil und Steinblock sowie
  Resetkosten. Automatisierung und effizientere Werkzeuge verhindern, dass
  dieser bleibende Bedarf den Anteil der Spielzeit in niedrigen Gebieten
  unkontrolliert erhöht.
- **[entschieden]** Techbaum und Rezeptkurve werden **zusammen** getunt.
  Richtwert: Automatisierung für Tier N schaltet frei, wenn der Spieler
  Tier N+1 betritt. Zu früh entwertet die Zonen, zu spät erzeugt Grind.
- **[vorläufig]** Basis zuerst als **Abbau** (Eidra fördert Rohstoff am
  Gebäude, reiner Zeitgeber). **Veredelung** (Eidra verarbeitet nach Rezept,
  braucht Warteschlange) folgt später als eigenes System.
- **Merksatz für Balancing:** *Schwer* und *langsam* sind getrennte Achsen.
  Kampf ist schwer, Ökonomie ist langsam. Beide dürfen hoch stehen, aber sie
  werden getrennt gedreht und getrennt beobachtet.

### M1.6 Materialmatrix

Vier Familien, für die bereits Prefabs existieren: `Tree`, `StoneDeposit`,
`FiberPlant`, `CopperVein`.

**Jedes Tier ist ein eigener sprechender Name, kein Suffix.** Dadurch bleiben die
vier Bestands-IDs unverändert gültig — keine Save-Migration.

| Familie | **T0 — Rohstoff** | **T1** | **T2 — v0.2** | T3 — später |
| --- | --- | --- | --- | --- |
| `wood` | **`wood` · Weichholz** | **`plank` · Brett** ← T0-Holz | **Hartholz → Hartholzbrett**, 2:1 | Glutholz, offen |
| `stone` | **`stone` · Bruchstein** | **`stone_block` · Steinblock** ← T0-Stein | **Granit → behauener Granit**, 2:1 | Basalt, offen |
| `fiber` | **`plant_fiber` · Pflanzenfaser** | **`rope` · Seil** ← T0-Faser | **2 Sumpfhanf + 1 Seil → robustes Tuch** | Glutranke, offen |
| `ore` | **—** | **`copper_ore` → `copper_bar`** | **3 Eisenerz → Eisenbarren** | Aschenerz, offen |

- **[entschieden]** Die Spalten **T0 und T1** sind der vollständige
  Materialbestand von v0.1.
- **[entschieden] Rohstoff und Veredelungsprodukt tragen dasselbe Tier.**
  T1 ist die **Veredelungsstufe**; das Erz ist die einzige Familie, in der auf
  T1 zusätzlich ein neuer Rohstoff hinzukommt (M1.4).
- **[entschieden] Materialien haben Ressorts.** Stein geht ab T1 zunehmend in den
  **Gebäudebau**, Metall in **Waffen und Werkzeuge**, Brett in beides, Seil ist
  die **Bindung**. Das gilt für alle folgenden Tiers.
- **[entschieden]** Die T2-Spalte ist Inhalt von v0.2. T3-Namen bleiben nur
  Richtungsbegriffe und sind keine Zusage.
- **[entschieden]** `materialFamily` und `tier` sind Felder auf `ItemDefinition`
  (M1.4), nicht Teil der ID.

**Ertrag eines T0-Knotens mit T0-Werkzeug: 2 Einheiten. Von Hand: 1.**
Werkzeuge höherer Stufe erhöhen diesen Wert — siehe M1.9.

### M1.7 Zonenbudgets [vorläufig]

Alle vier Gebiete sind gleich groß (80, Spielfläche 76 × 76) und tragen
**dieselbe Gesamtzahl Knoten: 72**. Sie unterscheiden sich ausschließlich in der
**Mischung**.

Die Verteilung folgt in jedem Gebiet demselben Muster — **45 / 18 / 6 / 3** —,
nur rotiert:

| Gebiet | Weichholz | Pflanzenfaser | Bruchstein | Kupfererz |
| --- | --- | --- | --- | --- |
| **Greenwood** · Holz | **45** | 18 | 6 | 3 |
| **Nebelmoor** · Faser | 18 | **45** | 3 | 6 |
| **Steinbruch** · Stein | 6 | 3 | **45** | 18 |
| **Glutruinen** · Kupfer | 3 | 6 | 18 | **45** |
| *Summe je Material* | *72* | *72* | *72* | *72* |

Ertrag je Gebiet mit gleichstufigem T0-Werkzeug bei 2 Einheiten pro Knoten:
**90 / 36 / 12 / 6**.

Nach dem ersten Abschluss der Eidra-Schmiede kommen drei vollständige
T2-Außengebiete hinzu. Sie verwenden dieselbe Größe, dieselben 72
Wirtschaftsknoten und dasselbe rotierte Muster:

| Gebiet | 45 | 18 | 6 | 3 |
| --- | --- | --- | --- | --- |
| **Dämmerhain** | Hartholz | Sumpfhanf | Granit | Eisenerz |
| **Schleiermoor** | Sumpfhanf | Hartholz | Eisenerz | Granit |
| **Grauklüfte** | Granit | Eisenerz | Hartholz | Sumpfhanf |

T0- und T1-Wirtschaftsknoten belegen keine dieser 72 Positionen. Mit einem
gleichstufigen T2-Werkzeug beträgt der Ertrag 3 je Knoten und damit
**135 / 54 / 18 / 9** je Gebiet; ein T1-Mindestwerkzeug liefert 2 je Knoten.
Beeren und andere Überlebensressourcen bleiben Nebenknoten außerhalb des
Budgets.

**Zur Gesamtzahl 72:** Sie ist aus der Fläche abgeleitet, damit die *Dichte*
gleich bleibt. Bei 76 × 76 liegt ein Knoten je rund 80 m² — praktisch derselbe
Abstand wie zuvor bei kleinerer Karte. Wären es weiterhin 48 Knoten, wäre die
Karte um gut 40 % dünner besetzt, und das ist nicht dasselbe wie „fordernd",
sondern leer (M0).

**Warum diese Struktur:**

- Jedes Material hat genau **eine** beste Quelle, und der Abstand zur
  zweitbesten ist **2,5×**. Groß genug, dass die Zonenwahl zählt; klein genug,
  dass eine Nebenquelle nicht wertlos ist.
- Greenwood und Nebelmoor sind Spiegelbilder (organisch), Steinbruch und
  Glutruinen ebenfalls (mineralisch). Zwei Paare statt vier Sonderfälle.
- Kupfer — das einzige T1-Material, das verhüttet wird (`copper_bar`) — ist im
  gefährlichsten Gebiet am dichtesten. Die Crafting-Kette zieht den Spieler nach
  innen.
- Ein Muster mit vier Rotationen ist beim Balancing **eine** Zahl statt sechzehn.

**Prüfkriterium bei jeder Änderung:** Jedes **Wirtschaftsmaterial** hat genau
eine beste Quelle, und das Verhältnis zur zweitbesten liegt zwischen 2× und 3×.
Fällt es unter 2×, verlieren die Gebiete ihre Identität; steigt es über 3×,
werden Nebenquellen wertlos.

**Glutruinen** trägt dieselbe Knotenzahl wie alle anderen. Seine Schwierigkeit
kommt aus dem Boss und den Gegnern, **nicht aus Ressourcenknappheit** — sonst
würde das Gebiet doppelt bestraft.

**[entschieden] Nebenknoten stehen außerhalb des 72er-Budgets.** Es darf weitere
Knotentypen geben, die **kein Wirtschaftsmaterial** liefern — also in keine
Fertigungskette eingehen. Sie folgen einer eigenen Regel und werden vom
Prüfkriterium oben nicht erfasst.

Erster Fall sind **Beeren** (M8.4): Sie kommen in **allen vier Gebieten** vor, in
**Greenwood etwas dichter**. *Begründung:* Als Faser-Nebenprodukt läge die
Heilung im Nebelmoor, und ausgerechnet die Einstiegszone hätte fast keine. Eine
Überlebensressource folgt nicht der Zonenidentität.

**[entschieden] Der Engpass eines Ausflugs ist der Rucksack, nicht die Zone.**
Bei 16 Plätzen und Stapelgröße 10 trägt ein Ausflug weiterhin nur einen Teil
eines 72-Knoten-Gebiets. Die konkrete Zahl abbaubarer Knoten hängt nun vom
relativen Werkzeugertrag ab. Die Ertragszahlen oben beschreiben, was ein Gebiet
**enthält**, nicht was ein Ausflug **einbringt**. Die Zonenidentität bleibt
dadurch wichtig: Mit begrenzten Plätzen geht man dorthin, wo das gesuchte
Material dicht liegt.

### M1.8 Dungeons und Bosse

- **[entschieden]** Boss-Zustand liegt in der **Progression**
  (`_progressFlags` / `_completedWorldMapNodes`), niemals im Zonen-Layout.
  Eine normale Bosszone regeneriert wie jede andere Zone; Erstabschluss,
  Wiederholbarkeit und Belohnungsstatus bleiben getrennte Flags.
- **[entschieden]** Dungeons haben einen festen, autorierten Aufbau. Eine
  laufende Instanz regeneriert weder bei Heimkehr noch durch Zeitablauf und
  bleibt über Sitzungen erhalten.
- **[entschieden]** Ein erfolgreich abgeschlossenes Gewölbe darf über einen
  eigenen expliziten Vertrag wiederholbar werden: Abklingzeit, Resetkosten,
  wiederholbare Spawn-/Lootgruppen und einmalige Flags werden getrennt
  gespeichert. Ein unvollständiger Durchlauf wird nicht manuell neu gewürfelt.
- **[entschieden] Zonen liefern verlässliche Menge; Dungeons liefern Erlaubnis
  und kontrollierte Spitzenbelohnungen.** Ein Gewölbe darf Baupläne,
  Technologiefreischaltungen, Eidra, volle Ausrüstung, seltene Komponenten und
  eine eigene Markenökonomie enthalten. Seine Mengen und Cooldowns müssen so
  begrenzt sein, dass Weltabbau und Crafting Hauptquellen bleiben.
- **[entschieden] Bosse sind Tore, keine Abschlüsse.**
  Der Sieg über Garon **öffnet den ersten Dungeon**, und erst dessen Abschluss
  schaltet T2 frei:

  ```
  Garon besiegt  →  erster Dungeon offen  →  Dungeon bestanden  →  T2
  ```

  Damit endet v0.1 nicht mit einem Abspann, sondern mit einem sich öffnenden
  Eingang (M8).
- **[entschieden]** Daraus folgt: **Garon ist nicht optional.** Der Fortschritt
  führt zwingend über ihn.
- **[entschieden] Erster Kill und Wiederholung sind zwei verschiedene Dinge.**
  Der erste Sieg setzt ein **Fortschritts-Flag** (Tor, einmalig). Späteres
  erneutes Antreten ist **Farmen** und läuft über einen eigenen Cooldown.
  Die Trennung existiert architektonisch bereits und muss erhalten bleiben.
- **[entschieden für die erste Eidra-Schmiede]** Erster Zugang kostenlos,
  anschließend 24 reale Stunden nach Abschluss und danach atomare Resetzahlung
  aus dem Spielerinventar. Exakte Kosten, Laufzustände und Belohnungen stehen in
  M13.6. Für andere Bosse oder Gewölbe wird daraus kein automatischer Standard
  abgeleitet.

### M1.9 Werkzeuge

**Drei Werkzeuge:** **Axt** (Holz), **Sense** (Faser), **Spitzhacke** (Stein und
Erz). Drei Werkzeuge decken vier Familien ab; die Spitzhacke bedient Stein *und*
Erz und ist damit das wertvollste — was passt, weil Kupfer im gefährlichsten
Gebiet liegt (M1.7).

- **[entschieden] Alle drei T0-Werkzeugrezepte sind von Spielbeginn an
  freigeschaltet und werden als Handarbeit ohne Werkbank hergestellt.**
  Technologiepunkte werden dafür nicht ausgegeben. Dieselben Rezepte sind
  zusätzlich an der Werkbank verfügbar; die Werkbank ist Komfort, keine
  Voraussetzung. Sie bleibt außerdem für Waffen und spätere Ausrüstung
  relevant.
- **[entschieden]** Nur T0-Holz, T0-Stein und T0-Faser lassen sich von Hand mit
  Ertrag 1 abbauen. Erz sowie alle Rohstoffe ab T1 benötigen mindestens das
  Werkzeug des vorherigen Tiers. Damit bleibt Handabbau die erste Lektion, aber
  keine Umgehung späterer Fortschrittsgrenzen.
- **[entschieden] Werkzeugertrag wird relativ zur Rohstoffstufe berechnet.**

  | Rohstoffstufe | Hand | T0-Werkzeug | T1-Werkzeug | T2-Werkzeug | später T3 |
  | --- | ---: | ---: | ---: | ---: | ---: |
  | T0 | 1 | 2 | 3 | 4 | 4 |
  | T1 | – | 2 | 3 | 4 | 4 |
  | T2 | – | – | 2 | 3 | 4 |

  Das Mindestwerkzeug liefert 2, das gleichstufige 3 und ein um eine Stufe
  höheres 4. Weitere Überstufen erhöhen den Ertrag zunächst nicht.
- **[entschieden] Werkzeugverschleiß ist in v0.2 aktiv.** Ein Werkzeug verliert
  erst nach vollständig abgeschlossenem Knoten Haltbarkeit; ein Abbruch kostet
  nichts. Der Verlust hängt vom relativen Tier ab:

  | Verhältnis Werkzeug zu Rohstoff | Verlust je Knoten |
  | --- | ---: |
  | ein Tier darunter | 4 |
  | gleiches Tier | 3 |
  | ein Tier darüber | 2 |
  | mindestens zwei Tiers darüber | 1 |

  T0/T1/T2-Werkzeuge besitzen 100/120/140 Maximalhaltbarkeit. Bei null
  zerbrechen sie und verschwinden. Reparatur und Zerlegen gehören nicht zu
  v0.2.
- **[entschieden] Werkzeuge liegen im Rucksack, nicht in einem Ausrüstungsplatz**
  (M5). Sie berühren den Waffenwechsel (`Q`) nicht; beim Abbau wird
  kontextabhängig das passende Werkzeug herangezogen.
- **[entschieden] Ressourcenabbau startet mit einmaligem Auslösen.** Der
  Zeitfortschritt läuft danach ohne gehaltene Taste bis zum Abschluss oder
  einem anderen Abbruchgrund weiter. Währenddessen ist das passende Werkzeug
  sichtbar in der Hand und führt eine einfache Abbauanimation aus. Das aktuell
  anwählbare Ressourcenziel trägt einen grünen Kreis am Boden.
- **[entschieden]** Das anvisierte Ressourcenziel meldet
  „Werkzeug unzureichend", „Belastung hoch", „Belastung normal" oder
  „Belastung gering". Die exakte Verlustformel muss nicht dauerhaft im HUD
  stehen.

**Was daran wichtig ist:**

- **Werkzeuge sind das zweite Druckventil.** M1.5 beschreibt das Problem: Bei
  rekursiven Rezepten wächst die T0-Nachfrage, während die Beschaffungsrate
  konstant bleibt. Basisautomatisierung ist das eine Gegenmittel — ein besseres
  Werkzeug ist das andere, denn es beschleunigt genau das Farmen alter
  Materialien.
  **Folge: Werkzeuge und Basisproduktion müssen zusammen getunt werden**, sonst
  wird das Problem doppelt gelöst und die Low-Level-Gebiete verlieren doch noch
  ihre Bedeutung.
- **[entschieden] Ertrag und Haltbarkeit wachsen kontrolliert auf zwei
  getrennten, flachen Kurven.** Höhere Werkzeuge besitzen mehr
  Maximalhaltbarkeit und erleiden gegen niedrigere Rohstofftiers weniger
  Verschleiß. Die Werte werden gemeinsam als Nettoertrag je hergestelltem
  Werkzeug getestet; weitere Multiplikatoren durch Basisproduktion oder
  Zufallswerte sind unzulässig.
- **Kein Konflikt mit M1.3.** Werkzeuge verändern den Ertrag *deterministisch*.
  Die Regel „feste Knotenzahl, keine Streuung" bleibt unberührt.

---

## M2 Kampf

- **[entschieden]** Der Spieler ist die Hauptfigur. Eidra unterstützen, sie
  ersetzen ihn nicht.
- **[entschieden]** Der Spieler führt **genau zwei Waffen gleichzeitig** und
  schaltet im Kampf zwischen ihnen um. Das ist die Regel — *nicht*, dass es im
  Spiel nur zwei Waffen gibt. Weitere Waffen dürfen später dazukommen und
  ausgetauscht werden.
- **[entschieden]** In v0.1 sind das **Hammer** (Dreierkombo) und **Dolche**
  (Viererkombo). Beide werden **gebaut**, nicht mitgeliefert (M8.2).
- **[entschieden]** v0.2 ergänzt den Speer als dritte Waffengattung. Waffen
  besitzen keine aktiven Waffenfähigkeiten und keine zweite Angriffstaste. Der
  normale Angriff spielt die Kombo der Gattung; Identität entsteht aus
  Moveset, Treffergeometrie und genau einer Signaturmechanik.
- **[entschieden] Waffen verschleißen ab v0.2 im Kampf.** Eine erfolgreiche
  Angriffsaktion kostet einen Haltbarkeitspunkt, unabhängig von der Zahl der
  getroffenen Ziele; ein Fehlschlag kostet nichts. Bei null zerbricht das
  Exemplar und verschwindet. Exakte Maximalwerte stehen in M13.2.
- **[entschieden]** Waffen sitzen in **Ausrüstungsplätzen** und fallen beim Tod
  nicht (M4, M5). Eine Ersatzwaffe kostet dagegen einen Rucksackplatz.
- **[entschieden] Der Kern des Spiels ist die Kombination, nicht eine Kette.**
  Aus den zwei geführten Waffen und den Fähigkeiten des aktiven Eidras entsteht
  ein **Kampfstil**, den der Spieler an die Mechanik des jeweiligen Gegners
  anpasst. Bosse und Dungeons haben unterschiedliche Mechaniken — die Antwort
  darauf ist eine andere Zusammenstellung. **Davon lebt das Spiel.**

Daraus folgen drei Verpflichtungen. Ein Kombinationssystem entsteht nicht von
selbst, es muss verteidigt werden:

- **[entschieden] Es darf keine dominante Kombination geben.** Gibt es eine
  Zusammenstellung, die überall die beste ist, ist das System tot — dann bleibt
  wieder eine einzige Kette übrig und die Wahl bedeutet nichts.
  **Das ist das wichtigste Balancing-Kriterium im Kampf.**
- **[entschieden] Jeder Boss und jeder Dungeon braucht eine eigene Mechanik**,
  auf die verschiedene Kombinationen unterschiedlich gut antworten. Ein Gegner
  ohne solche Eigenheit ist ein Gegner, bei dem die Wahl folgenlos bleibt — und
  damit ein verschenkter Gegner.
- **[entschieden] Zwei Entscheidungsebenen, beide müssen bedeutsam bleiben.**
  *Vor* dem Kampf: welche zwei Waffen, welches Eidra — strategisch.
  *Im* Kampf: wann gewechselt wird — taktisch.

**Einordnung der bekannten Kette.**
*Hammer + Terrock → Stagger → Dolche + Noctarion → Rücken-Burst* ist die
**erste gelehrte Kombination**, nicht das Gesetz. Sie hat im Prototyp bewiesen,
dass das darunterliegende System trägt, und sie ist genau die richtige erste
Lektion für v0.1 (M8). Sie ist ein **Beispiel, kein Rahmen** — das README
beschreibt sie zu Recht als Hypothese des *Prototyps*, nicht als Designgesetz.

**Folge für weitere Waffen und Eidra:** Sie sind kein Content-Nachschub, sondern
**Systemtiefe**. Jede zusätzliche Waffe und jedes zusätzliche Eidra vervielfacht
die Zahl möglicher Antworten auf eine Gegnermechanik. Das ist der Grund, warum
sie kommen — nicht die Menge.
- **[entschieden]** **Stagger ist ein eigener Schadenskanal** (`IStaggerable`),
  getrennt von Lebenspunkten. Er öffnet ein befristetes Schadensfenster.
- **[entschieden] Wildlinge zeigen Lebenspunkte und Stagger getrennt über dem
  Kopf.** Beide Leisten folgen demselben Weltanker und lesen die bestehenden
  Kampfzustände; sie führen keine eigenen Werte. Bei Tod, Despawn oder außerhalb
  der vorgesehenen Sichtbarkeit verschwinden sie.
- **[entschieden]** **Rückentreffer** geben Bonusschaden. Erkennung geometrisch
  über `CombatTargetGeometry.IsBackAttack`, Schwelle `dot < -0.35` — das
  entspricht einem Kegel von etwa 140° hinter dem Ziel.
- **[entschieden]** Ausweichen kostet Ausdauer und gewährt kurze
  Unverwundbarkeit. Ausdauer regeneriert automatisch, nicht während des
  Ausweichens.
- **[entschieden]** Jede zeitbasierte Aktion bricht bei **Tod, Deaktivierung und
  Szenenwechsel** ab (`ITransitionCancellable`). Keine Aktion überlebt einen
  Zonenwechsel.
- **[entschieden]** Grund- und Taumelschaden sind feste Daten der konkreten
  Variante. Reguläre Multiplikatoren T0/T1/T2 beginnen bei 1,00/1,20/1,45;
  benannte T1-Waffen liegen als Testwert ungefähr acht Prozent über regulärem
  T2. Es gibt keine zufälligen Werte, Qualitätsstufen oder versteckten
  Tierboni gegen Gegner.
- **[entschieden] Waffen- und Werkzeug-Upgrades kommen nicht in v0.1.**
  Eine Waffe und ein Werkzeug haben in v0.1 genau eine Stufe. Es gibt kein
  Upgrade-Rezept und keinen Upgrade-Gegenstand (M8.8).
- **[entschieden] Reguläre Tiers und benannte Versionen sind eigene feste
  Gegenstände, keine Aufwertung eines bestehenden Exemplars.** Jede Variante
  besitzt eine stabile ID, festes Tier, feste Werte und eigene
  Maximalhaltbarkeit. Ein Exemplar wird niemals in das nächste Tier
  umgewandelt. Das alte `WeaponProgressionState`-/`weaponUpgrades`-Modell wird
  mit der v0.2-Save-Migration ohne Bonus entfernt.
- **[entschieden]** Alle Varianten einer Gattung teilen Kombo, Reichweite,
  Angriffstempo, Treffergeometrie und Signatur. Reguläre Tiers teilen in v0.2
  zusätzlich ihre Darstellung; Siegelbrecher, Aschenzähne und Glutdorn sind die
  drei visuell besonderen benannten T1-Waffen (§24, M13.2).

---

## M3 Eidra

- **[entschieden]** Zwei Eidra im Team, aber **genau eines aktiv und sichtbar**.
- **[entschieden] „Sichtbar" ist eine Zusicherung, kein Idealfall.**
  Solange der Spieler in einer spielbaren Szene steht und lebt, steht das aktive
  Eidra **sichtbar neben ihm** — in jeder Zone, nach jedem Szenenwechsel, nach
  jedem Respawn und nach jedem Eidra-Wechsel.
  *Diese Regel bestand bereits sinngemäß, wurde aber trotzdem verletzt:*
  In den echten Zonenszenen wird das Eidra-Team nie initialisiert (§21). Nach
  Änderungsregel 2 ist sie damit von Konvention auf **prüfpflichtig** gehoben.
- **[entschieden]** Das UI zeigt **exakt zwei Fähigkeiten** des aktiven Eidras.
  Nicht mehr, nicht weniger — das ist eine Design-, keine Layoutentscheidung.
- **[entschieden]** Das aktive Eidra ist **die halbe Zusammenstellung**, nicht
  eine Zugabe zur Waffe. Die Wahl vor dem Kampf ist eine Eidra-Wahl genauso wie
  eine Waffenwahl (M2).
- **[entschieden]** Terrock und Noctarion sind die ersten beiden. Terrock trägt
  die Stagger-Seite, Noctarion die Burst-Seite — das sind **ihre** Profile, nicht
  die einzig vorgesehene Reihenfolge.
- **[entschieden]** Neue Eidra werden über ihr **Profil** entworfen, nicht über
  Zahlen: Jedes soll eine Gegnermechanik beantworten, die die vorhandenen
  schlecht beantworten. Ein Eidra, das nur stärker ist als ein bestehendes,
  verletzt das Dominanzverbot aus M2.
- **[entschieden]** Das aktive Eidra unterstützt autonom, ohne Mikromanagement.
- **[entschieden]** Eidra sind der Träger der Basis-Produktion (M1.5). Sie sind
  damit sowohl Kampf- als auch Wirtschaftsressource — das ist beabsichtigt und
  macht die Zuteilung zu einer echten Entscheidung.
- **[entschieden] Höchstens zwei Eidra sind gleichzeitig aktiv** (M8.3). Ein
  Eidra kämpft **oder** produziert, nie beides — wer eine Produktionsstätte
  besetzen will, fängt ein weiteres Exemplar. Damit hat Basisproduktion Kosten,
  ohne dem Spieler sein Kampfteam wegzunehmen, und die frühere Sorge dieses
  Punktes ist beantwortet.
  **In v0.1 wird das noch nicht sichtbar:** Produktionsstätten laufen dort
  vollständig ohne Eidra (M6).
- **[entschieden]** v0.2 ergänzt **Ignivar** als drittes vollwertiges Eidra mit
  Feuerprofil. Alle gefangenen Instanzen bleiben im Roster; das gewählte Team
  besitzt weiterhin zwei Plätze und genau ein aktives sichtbares Eidra.
  Teamauswahl erfolgt zuhause außerhalb eines Kampfes. Direkt nach Ignivars
  erstem Fang erlaubt die sichere Bindungskammer einmalig eine Neuwahl für den
  Kernwächter und setzt die Eidra-Fähigkeitencooldowns zurück.
- **[entschieden]** In v0.2 kann nur das erste Ignivar-Exemplar dauerhaft
  gefangen werden. Dieses artbezogene Einmalflag ist eine ausdrücklich enge
  Ausnahme von der allgemeinen Instanzliste und verhindert keine späteren
  Mehrfachfänge anderer Arten. Ignivar gibt keine Gegner-EP und keinen passiven
  Bonus. Fangwerte und die Fähigkeiten Glutkreis und Schmelzbrand stehen in
  M13.6.

---

## M4 Tod, Respawn und Kartenverlassen

- **[entschieden]** Respawn erfolgt am **letzten sicheren Knoten**
  (`GameSession.LastSafeNodeId` / `LastSafeSceneKey`), nicht am Todesort.
- **[entschieden]** Jede Außengebietskarte lässt sich **an jedem Punkt ihrer
  vier Seiten** verlassen. Ecken dürfen nicht blockieren.
- **[entschieden]** Verlassen ist ein **kurzer Countdown**, kein sofortiger
  Wechsel. Er bricht ab, wenn der Spieler in die Sicherheitszone zurückkehrt
  oder stirbt. Technische Anforderungen dazu in §3 und §4.
- **[entschieden]** Die Heimatbasis ist persistent. Außengebiete werden beim
  erneuten Betreten neu bestückt (M1.1).
- **[entschieden] Todesstrafe: Der Rucksack fällt, die Ausrüstungsplätze
  bleiben.** Der gesamte Inventarinhalt wird als **ein Todesbeutel** im
  Todesgebiet abgelegt und ist wieder aufsammelbar. Waffen, Fanggerät, Batterie
  und alles andere in einem Ausrüstungsplatz (M5) bleiben beim Spieler, ebenso
  die Eidra.
- **[entschieden] Ein neuer Tod ersetzt den alten Todesbeutel.**
  *Begründung:* Darin liegt die eigentliche Härte. Die Strafe ist nicht der
  Verlust, sondern der **Rückweg unter Druck** — wer auf dem Weg zum Beutel
  erneut stirbt, hat den ersten für immer verloren. Das ist die richtige Achse
  für M0, ohne die Beschaffungsrate aus M1.5 zu verzerren.
- **[offen] Sperrt der Tod die Regeneration des Todesgebiets?**
  Der Respawn erfolgt in der Heimatbasis, und Heimkehr würfelt nach M1.1 **alle**
  Zonen neu — der Beutel liegt danach in einer neu erzeugten Karte. Eine Sperre
  bis zur Bergung würde das lösen und zugleich die Frage erübrigen, was passiert,
  wenn an der Fundstelle jetzt ein Fels steht.
  **Umsetzung ist billig**, solange die Regeneration wie in M1.2 als expliziter
  Invalidierungsschritt je Zone gebaut wird: Die Sperre ist dann eine zusätzliche
  Bedingung, kein neues System. **Der Haken muss beim Bau vorgesehen werden, die
  Entscheidung darf später fallen.**
- **[offen] Auflegeregel für den Beutel**, falls die Sperre nicht kommt.
- **[entschieden für die Eidra-Schmiede]** Ein bezahlter Gewölbereset darf
  keinen noch vorhandenen Todesrucksack löschen. Vor dem Reset wird sein Inhalt
  atomar in einen eindeutig beschrifteten Bergungscontainer im sicheren
  Eingangsbereich verschoben. Andere lose Drops der abgeschlossenen Instanz
  werden gelöscht. Diese Ausnahme gilt nicht automatisch für Außengebiete.

---

## M5 Inventar, Lager und Crafting

- **[entschieden]** Crafting braucht grundsätzlich die im Rezept angegebene
  Station. **Ausnahme:** Axt, Sense und Spitzhacke sind T0-Handarbeit und
  brauchen keine Werkbank, dürfen aber auch an der Werkbank gefertigt werden.
  Das Handwerksfenster hält den Spieler während der Herstellung trotzdem an.
- **[entschieden]** Eine Station zeigt alle zu ihr gehörenden Rezepte,
  einschließlich noch nicht erforschter. Gesperrte Rezepte bleiben auswählbar
  für ihre Details, werden deutlich ausgegraut und können nicht hergestellt
  werden. Lange Listen sind scrollbar.
- **[entschieden] Handarbeit/Crafting und Technologiebaum sind globale
  Spielerfenster.** `C` und `T` öffnen sie in der Heimatbasis und in jedem
  Außengebiet über denselben Spieler-Kompositionspfad. Das Öffnen ist nicht an
  eine Szene gebunden; Stationsvoraussetzungen einzelner Rezepte bleiben
  unverändert bestehen.
- **[entschieden]** Lagerbehälter sind über eine **stabile Container-ID**
  persistent (`GameSession.SetStorageState` erzwingt das bereits per Exception).
- **[entschieden] In der Heimatbasis bilden Spielerinventar und alle dortigen
  persistenten Lagerkisten einen gemeinsamen Materialbestand** für Bau,
  Handarbeit, Werkbank und andere Produktionsstätten. Kostenanzeige,
  Freigabeprüfung und Verbrauch verwenden dieselbe Berechnung. Verbraucht wird
  atomar und deterministisch: zuerst aus dem Spielerinventar, danach aus den
  Lagerkisten nach stabiler Container-ID. Reicht die Gesamtmenge nicht, bleibt
  jeder Container unverändert. Lager außerhalb der Heimatbasis werden niemals
  automatisch einbezogen.
- **[entschieden]** Maximal **fünf verschiedene Zutatenarten** je Rezept —
  zentrale Daten- und UI-Grenze. Handarbeit, Werkbank und sämtliche
  Produktionsstationen verwenden dieselbe Validierung. Die Oberfläche bleibt
  bei ein bis fünf Zutaten in Touch-, Gamepad- und Mausbedienung vollständig
  lesbar; mehr als fünf wird als Inhaltsfehler abgelehnt.
- **[entschieden] Rucksack: 16 Plätze.**
- **[entschieden] Inventarplätze lassen sich per Drag-and-drop umordnen.**
  Ein Ablageziel führt vorhandene Stapel zusammen, verschiebt in einen leeren
  Platz oder tauscht zwei unterschiedliche Gegenstände. Die Inventarlogik
  bleibt dabei die einzige Wahrheit; die UI schreibt keine Stapel direkt.
- **[vorläufig] Stapelgröße 10 für alles Stapelbare.** Kein Gegenstand stapelt
  höher. `ItemDefinition.MaximumStackSize` ist **je Item** gesetzt, die
  Stellschraube ist also Daten, nicht Code.
- **[entschieden] Rucksack und Lagerkiste teilen sich dieselbe Stapelgröße.**
  Sie hängt am Gegenstand, nicht am Behälter — `ItemStack` erzwingt sie an
  genau einer Stelle, und der Transfer zwischen beiden kann sie deshalb nicht
  umgehen.
- **[offen]** Ob Kisten später **größere** Stapel fassen als der Rucksack. Käme
  das, bekäme der Behälter einen eigenen Faktor. Heute gibt es ihn bewusst
  nicht: Ein zweiter Grenzwert an einer zweiten Stelle wäre genau die
  Doppelwahrheit, die §6 verbietet.
- **[entschieden] Werkzeuge, Waffen und das Fanggerät sind nicht stapelbar.**
  Zusammen mit dem Verschleiß aus M1.9 heißt das: Ersatzwerkzeuge kosten je einen
  Rucksackplatz. **Tragekapazität wird dadurch zur Entscheidung.**

**[entschieden] Ausrüstungsplätze sind vom Rucksack getrennt.**
Was in einem Ausrüstungsplatz steckt, belegt keinen Rucksackplatz und fällt beim
Tod nicht (M4).

- **In v0.1 belegt:** zwei **Waffen**, **Kopf**, **Brust**,
  **Handschuhe**, **Beine**, **Fanggerät** und **Batterie**.
- **[entschieden] Der Spieler beginnt ohne Rüstung.** Sein dauerhaftes
  Basismodell zeigt Haut, Haare und eine blickdichte Unterhose. Dieses
  Basismodell ist kein ausrüstbarer Gegenstand und bleibt unter angelegter
  Rüstung erhalten.
- **[entschieden] Sichtbare Rüstung folgt unmittelbar dem
  Ausrüstungszustand.** Kopf-, Brust-, Handschuh- und Beinrüstung sind getrennt
  schaltbare **Slotmeshes** am Akteursmodell. Sie müssen in jeder Richtung und
  jedem Animationszustand dieselbe Kleidung zeigen — bei einem Modell ergibt
  sich das von selbst, es ist trotzdem zu prüfen. `PlayerEquipment.Changed` sowie das Laden eines
  Spielstands schalten das zum Gegenstand gehörende Visual ein; Ablegen oder ein
  leerer Platz schaltet es aus. UI, Präsentation und Spielercontroller führen
  keine zweite Ausrüstungswahrheit.
- **[entschieden] Die vier v0.1-Rüstungsteile bilden gemeinsam die
  T0-Stoffrüstung:** Stoffkapuze, Stoffmantel, Stoffarmschienen und Stoffschuhe.
  Die bestehenden IDs `armor_wanderer_hood`, `armor_wanderer_coat`,
  `armor_wanderer_bracers` und `armor_wanderer_legs` bleiben als Save-Keys
  unverändert; Anzeigenamen werden nie zu IDs (§12). Die sichtbare Kleidung
  folgt der 3D-Akteurspipeline aus M10.10; die historischen Sprite-Sets sind
  aus dem aktiven Projekt entfernt.
- **[entschieden]** Die v0.1-Stoffrüstung war zunächst visuell. Ab v0.2 tragen
  alle vier Rüstungsslots additive direkte Schutzwerte und persistente
  Haltbarkeit. Vollständiges T0/T1/T2 liefert 10/20/30 Prozent Schutz; die
  konfigurierbare Gesamtkappe beginnt bei 60 Prozent. Der vor einem Treffer
  berechnete Schutz gilt noch für diesen Treffer, anschließend verliert jedes
  getroffene getragene Teil einen Haltbarkeitspunkt. Bei null verschwindet es
  und seine Darstellung wird entfernt. Exakte Teilwerte stehen in M13.3.
- **[entschieden] Werkzeuge bekommen keinen Platz** und bleiben im Rucksack
  (M1.9).
- **[entschieden] Die Aufzählung der Plätze wird vollständig angelegt, auch wenn
  v0.1 nur acht davon benutzt:** Waffe ×2, Kopf, Brust, Handschuhe, Beine,
  Ringe, Amulett, Ohrring, Rucksack, Fanggerät, Batterie.
  *Begründung:* Dieselbe Logik wie beim `tier`-Feld in M1.4 — die Aufzählung
  kostet heute nichts und erspart eine Save-Migration. Der Spielstand hält die
  Ausrüstung als **Zuordnung Platz → Gegenstand**, nicht als feste Felder (§17).
- **[entschieden] Rucksäcke vergrößern das Inventar** und sitzen in einem eigenen
  Ausrüstungsplatz. **Folge: Die 16 werden nirgends fest verdrahtet**, sondern
  aus Grundwert plus Rucksack abgeleitet. Inhalt für später, Feld ab sofort.

**[entschieden] Verschleiß ist ein persistenter Zustand je Exemplar.** In v0.1
war der Verbrauch abgeschaltet; v0.2 aktiviert ihn für Waffen, Werkzeuge und
Rüstung. Itemdefinitionen tragen nur Maximalwerte, gespeicherte Instanzen die
aktuelle Haltbarkeit. `instanceId` und Haltbarkeit bleiben beim Verschieben,
Ablegen, Looten, Ausrüsten und Speichern erhalten.

- **[entschieden] Ein verbrauchter Gegenstand zerbricht und ist weg.**
  Reparieren ist nicht vorgesehen: keine Reparaturstation, keine zusätzliche
  Mechanik, und es ließe sich später **rein additiv** nachrüsten, ohne dass eine
  getroffene Entscheidung zurückgenommen werden müsste.
- **Warum das Feld trotzdem sofort kommt:** Ein nachträglich eingeführter
  Exemplarzustand hieße, dass jeder bereits gespeicherte Gegenstand einen
  Vorgabewert braucht und der Spielstand eine Version hochzählt (§17). Das Feld
  kostet heute nichts — dieselbe Rechnung wie beim `tier`-Feld in M1.4.
- **[entschieden] Der Batterieplatz hält genau eine Batterie.** Ersatzbatterien
  liegen im Rucksack und fallen damit beim Tod (M4). Andernfalls wäre das Fangen
  von der Todesstrafe vollständig abgekoppelt, weil Ausrüstungsplätze nicht
  fallen.

**[entschieden] Lootcontainer sind keine kurzlebigen UI-Würfe.** Eine erzeugte
Welt- oder Gewölbekiste besitzt eine stabile Instanzidentität, einen einmal aus
Zone beziehungsweise Durchlauf abgeleiteten Inhalt und einen persistenten
Entnahmezustand. Öffnungsreihenfolge, Teilentnahme, voller Rucksack, Speichern
und Laden würfeln niemals neu. Eine Kiste wird erst durch die nächste zulässige
Zonenregeneration beziehungsweise den bezahlten Gewölbereset ersetzt.

Schmiedemarken sind normale stapelbare Inventargegenstände. Die drei physischen
Markenkisten bezahlen ausschließlich atomar aus dem Spielerinventar. Nach der
Zahlung bleibt ihr Inhalt gespeichert, bis er vollständig entnommen wurde; bis
dahin ist eine erneute Zahlung an derselben Kiste gesperrt.

---

## M6 Basis und Technologie

- **[entschieden]** Die Heimatbasis ist **kein Abbaugebiet**. Sie ist
  Produktions- und Fortschrittsort und trägt **keine regenerierenden**
  Ressourcenknoten. Einzige Ausnahme ist der endliche Einmalvorrat aus M1.1.
- **[entschieden]** Basis-Produktion ist das **Druckventil** der Ökonomie
  (M1.5), nicht ein Komfortfeature.
- **[vorläufig]** Erst **Abbau** (Zeitgeber), später **Veredelung**
  (Warteschlange mit Rezepten).

**[entschieden] Gebäudeliste: eine Produktionsstätte je Materialfamilie.**

| Gebäude | Tier | Wandelt |
| --- | --- | --- |
| Werkbank | T0 | alle T0-Rezepte, Crafting-Station |
| Lagerkiste | T0 | 24 Plätze, stabile Container-ID |
| Acker | T0 | Weizensamen → Weizen |
| Mauer / Boden / Tür | T0 | — |
| **Schmelzofen** | T1 | Kupfererz → Kupferbarren |
| **Sägewerk** | T1 | T0-Holz → Brett |
| **Seilerei** | T1 | T0-Faser → Seil |
| **Steinmetz** | T1 | Bruchstein → Steinblock |
| Kochtopf | T1 | Weizen → Brot, Heiltrank |

- **[entschieden] Das Baumenü zeigt je Eintrag Bild und Namen.** Maus-/Touch-
  Klick sowie UI-Fokus wählen denselben Eintrag zuverlässig aus; erst danach
  startet die bestehende Platzierungsphase mit sichtbarer Grün-/Rot-Vorschau.
- **[entschieden] Die Bauvorschau bleibt nach der Auswahl steuerbar.** Das
  grüne beziehungsweise rote Rasterquadrat lässt sich mit der aktiven
  Eingabefamilie bewegen; Rastersnapping, Drehen, Bestätigen und Abbrechen
  funktionieren weiter. Der Menüfokus darf die Platzierungseingabe nicht
  blockieren. Die freigeschaltete Lagerkiste ist ein regulärer Eintrag unter
  `B` und keine Sonderaktion.
- **[entschieden] Der Acker ist eine ruhige 2×2-Bodenfläche.** Leerer,
  bepflanzter und erntereifer Zustand sind aus der Spielkamera eindeutig
  unterscheidbar. Überlagerte, gequetschte oder perspektivisch widersprüchliche
  Dekoration ist unzulässig; Grundfläche und Collider bleiben mechanisch 2×2.

- **[entschieden] Der Schmelzofen kostet reines T0.** Sonst bräuchte man einen
  Barren, um den Ofen zu bauen, der Barren macht. Sägewerk, Seilerei und
  Steinmetz kosten T0 **plus einen Kupferbarren** (Sägeblatt, Spindel, Meißel) —
  damit steht die Reihenfolge innerhalb von T1 in den Kosten und braucht keine
  zusätzlichen Baumkanten (M8.6).
- **[entschieden] Das Lagerfeuer entfällt.** Seine beiden dokumentierten Aufgaben
  sind vergeben: der Barren an den Schmelzofen, der Heiltrank an den Kochtopf
  (M8.4). Ein Gebäude ohne Rezept wäre ein Technologieknoten, der nichts lehrt.

**[entschieden] Höhere Tiers bringen keine parallelen Produktionsstätten.**

> **Die Basis wächst in die Tiefe, nicht in die Breite.**

Ein Materialstrang hat über **alle** Tiers genau **eine** Produktionsstätte.
Neue Rezepte erweitern diese vorhandene Station. Vier Familien über mehrere
Tiers dürfen nicht zu parallelen Gebäudeduplikaten anwachsen.

**[entschieden für v0.2]** Der eine Technologieknoten `T2-Verarbeitung`
schaltet Hartholzbrett im Sägewerk, behauenen Granit im Steinmetz, robustes Tuch
in der Seilerei und Eisenbarren im Schmelzofen frei. Er verändert keine
gespeicherte Gebäudeinstanzstufe, kostet kein Stationsupgrade und erzeugt kein
neues Stationsmodell. Spätere Versionen dürfen sichtbare Aufwertungen als
eigenes System entscheiden; sie sind keine Voraussetzung der T2-Rezepte.

- **[entschieden]** Das vorbereitete Stufenfeld einer Gebäudeinstanz bleibt im
  Save, wird durch v0.2 aber nicht erhöht. Rezeptfreischaltung und sichtbare
  Gebäudestufe sind getrennte Daten.
- **[entschieden] Rasterbelegung: eine Produktionsstätte = ein Baufeld.**
  Der **Acker** ist mit 2 × 2 die erste Ausnahme und setzt das Muster für spätere
  große Bauten.
- **[offen]** Ob Upgrades die Grundfläche vergrößern. **Das Datenmodell trägt die
  Grundfläche trotzdem ab sofort als `(breite, tiefe)` je Stufe**, nicht als
  Konstante je Typ — sonst ist genau diese offene Frage später eine Migration.

**[entschieden] Alle Produktionsstätten produzieren auch ohne Eidra.**
Eidra **verstärken** die Produktion, sie ermöglichen sie nicht. Damit ist die
Frage „binden Gebäude Eidra" für v0.1 beantwortet, und der spätere Ausbau ist
rein additiv: Produktionsstätten tragen `grundrate` und `eidraFaktor`
(Vorgabe 1,0). In v0.1 ist der Faktor immer 1; später wird nur der Wert gedreht,
  nicht die Struktur.

### M6.1 Modularer Basisbau ab v0.2

- **[entschieden]** Das Bauraster bleibt 1 × 1 Welteinheit mit einem stabilen,
  autorierten Ursprung in der Heimatbasis.
- **[entschieden]** Belegung besitzt vier getrennte Schichten: Boden auf
  Zellen, Wände/Türen auf kanonischen Zellkanten, Objektgebäude auf einer oder
  mehreren Zellen sowie eine vorbereitete nicht blockierende Dekoschicht.
  Verschiedene Schichten dürfen dieselbe Fläche verwenden; widersprüchliche
  Belegungen derselben Schicht nicht.
- **[entschieden]** Ost/West beziehungsweise Nord/Süd derselben Nachbarkante
  sind eine identische Kante. Eine Tür ersetzt eine Wand dort atomar.
- **[entschieden]** Werkbank, Lager und Produktionsstätten benötigen
  vollständigen Boden. Der Acker verlangt natürlichen Boden; der Kochtopf
  erlaubt vollständig natürlichen Boden oder vollständig verlegten Boden.
- **[entschieden]** Boden kann einzeln, als Linie oder Rechteck, Wand als
  einzelne Kante oder gerade Kantenlinie gesetzt werden. Die Gesamtaktion wird
  am Commit-Punkt atomar geprüft und bezahlt.
- **[entschieden]** Ein Raum ist eine zusammenhängende Bodenfläche mit
  vollständig durch Wände oder Türen geschlossenem Rand. Das daraus abgeleitete
  Dach ist in v0.2 rein visuell, kostet nichts und blendet für Kamera oder
  Spieler weich aus. Offene Flächen erhalten kein Dach.
- **[entschieden]** Rasterkoordinate, kanonische Kante, Schicht,
  Untergrundregel und Gebäudeinstanzzustand werden gespeichert. Abgeleitete
  Wandverbindungen, Raumlisten, Dächer und Vorschauzustände sind keine zweite
  Save-Wahrheit.

- **[offen]** Welche Eidra-Rolle welche Stätte verstärkt. Richtung aus dem
  bisherigen Gespräch, **nicht bindend**: Acker durch Pflanze und Wasser,
  Kochtopf durch Feuer, Werkbank durch Arbeiter. Nicht in v0.1 — aber
  `EidraData` bekommt das Rollenfeld ab sofort (M8.3).

---

## M7 Weltkarte und Reisen

- **[entschieden]** Die Weltkarte ist **knotenbasiert**. Zonen hängen an Knoten
  (`ZoneDefinition.WorldMapNodeId`).
- **[entschieden]** Heimkehr in die Basis invalidiert **alle** Zonen-Seeds
  (M1.1). Das macht die Heimatbasis mechanisch bedeutsam statt nur bequem.
- **[entschieden] In v0.1 ist Reisen kostenlos.** Reisekosten kommen später,
  nicht jetzt.
- **[entschieden]** Dämmerhain, Schleiermoor und Grauklüfte sind sichtbare,
  zunächst gesperrte Weltkartenknoten. Der erste Abschluss der Eidra-Schmiede
  schaltet alle drei über dasselbe zentrale T2-Flag dauerhaft frei. Sie folgen
  derselben kostenlosen Reise- und Heimkehrregeneration wie die vier
  Bestandsgebiete; es gibt keinen Schlüssel, Eintrittspreis oder eigenen
  Gebietscooldown.
- **[entschieden] Die daraus folgende Abnutzbarkeit ist bekannt und für v0.1
  akzeptiert.** Bei kostenlosem Reisen ist die optimale Spielweise, nach jedem
  vollen Rucksack heimzulaufen — und weil Heimkehr die einzige Uhr ist (M1.1),
  wird die Zonenregeneration damit zur Formsache. Das ist **keine Lücke, sondern
  ein bewusst offen gelassener Wert**: M1.1 akzeptiert Reroll durch Heimlaufen
  bereits ausdrücklich und nennt Laufweg und Abbauaufwand als Bremse. In v0.1 ist
  diese Bremse schwach. Sie zu verstärken ist eine spätere Änderung, keine
  Korrektur.
- **[vorläufig] Später kostet Reisen Zeit.** Das ist die vorgesehene Richtung.
- **[offen]** Alternativ oder ergänzend ein **Energiesystem**: Je weiter die
  Reise, desto mehr Energie. Als Idee erfasst, nicht zugesagt.
  **Wenn es kommt, gilt eine harte Randbedingung:** Energie darf **nicht in
  Echtzeit** nachwachsen, sondern nur bei Heimkehr. Sonst entsteht die zweite
  Uhr, die M1.1 ausdrücklich ausschließt — und Warten würde zur Strategie.
- **[entschieden] Nichts davon in v0.1** (M8.8).

---

## M8 Umfang v0.1

> **Historischer Releaseumfang.** Dieses Kapitel dokumentiert die didaktische
> und wirtschaftliche Kante des veröffentlichten v0.1-Stands. Aussagen darüber,
> was „später" geschehen sollte, werden durch die inzwischen entschiedenen
> v0.2-Regeln in M1–M7 und M13 ersetzt; sie dürfen nicht als aktueller
> Implementierungsauftrag gelesen werden.

- **[entschieden] v0.1 ist der Einstieg ins Spiel.** Kein Vertical Slice aus der
  Spielmitte, sondern die Phase, in der dem Spieler **alle Mechanismen
  nacheinander beigebracht** werden. Alles endet beim ersten Boss.
- **[entschieden] v0.1 ist vollständig T1.** T2 und T3 kommen später (M1.4).

### M8.1 Inhalt

- Die **vier Gebiete** (M1.7), alle gleich groß, mit T0-Rohstoffen und
  Kupfererz.
- **Garon** als erster Boss in den Glutruinen.
- Alle Grundmechaniken, die nötig sind, um ihn legen zu können.
- **Basisbau**: Werkbank, Lagerkiste, Acker, Mauern, Böden und Tür, dazu die vier
  Produktionsstätten Schmelzofen, Sägewerk, Seilerei und Steinmetz sowie der
  Kochtopf (M6).
- **Werkzeuge** Axt, Sense und Spitzhacke (M1.9) — ohne Verschleiß, aber mit
  angelegtem Haltbarkeitsfeld (M5).
- **Eidra fangen** über Fanggerät und Batterie (M8.3).
- **Erfahrung, Level, Technologiepunkte und der Technologiebaum** gemäß M8.6.

### M8.2 Einstiegszustand

Der Spieler bekommt nichts geschenkt, sondern erarbeitet sich den Kernfortschritt.
`GameSession.StartNewGame()` erzeugt deshalb einen leeren, aber spielbaren
Einstiegszustand.

| Inhalt | Regel |
| --- | --- |
| Hammer und Dolche | **werden gebaut** |
| Terrock und Noctarion | **werden gefangen** |
| Heiltränke und Bufffood | **werden gecraftet** |

**Die Startausrüstung wird zentral durch `GameSession.StartNewGame()` bestimmt.**
Szenen- und Boss-Composition dürfen keine Gegenstände zusätzlich vergeben.

### M8.3 Eidra fangen

Die Fangmechanik ist als deterministische Halte-Interaktion umgesetzt.
`EidraCaptureInteractable` prüft Ausrüstung und Fangbedarf,
`EidraRosterService` übernimmt gefangene Instanzen und
`EidraTeamController` verwaltet das aktive Team.

**Zwei Gegenstände, nicht ein Sortiment:**

| | |
| --- | --- |
| **Fanggerät** | Werkbank, T1: Kupferbarren + Brett + Seil. Permanent, nicht stapelbar, **kein Ladungszustand**. Liegt in einem Ausrüstungsplatz (M5) |
| **Batterie** | Werkbank, verbrauchbar, stapelbar. T1: **1 Kupferbarren + 2 Seil** (Kupferspule). Eigener Ausrüstungsplatz |

- **[entschieden] Die Batteriestufe ersetzt das Ballsortiment.** Es gibt genau
  ein Fanggerät; die Stufe der eingesetzten Batterie bestimmt die Ladung.
  *Begründung:* Kein Inventarballast, keine Frage „welchen Ball nehme ich", und
  das Aufrüsten deckt sich mit dem Muster der Produktionsstätten (M6).
- **[entschieden] Der Fang würfelt nicht.** Jedes Eidra hat einen **Fangbedarf**,
  der **mit sinkenden Lebenspunkten fällt**. Der Fang gelingt, wenn die Ladung
  den aktuellen Bedarf deckt.
  *Begründung:* M1.2 verbietet globalen Zufall, M1.3 verbietet Streuung nach
  unten — ein Fangwurf mit 40 % wäre der schlimmste Fall davon: Man verlöre
  Ladung *und* Ziel, ohne etwas falsch gemacht zu haben. Stattdessen hat der
  Spieler **zwei Hebel, die beide „spielen" heißen**: eine bessere Batterie, oder
  das Ziel härter niederkämpfen.
- **Folge:** Eine T1-Batterie fängt ein T1-Eidra bequem und ein T2-Eidra nur
  knapp über der Fluchtschwelle. Das ist die Stufung eines Ballsortiments —
  deterministisch, und mit Können als zweiter Achse.
- **[entschieden] Die Batterie wird nur bei Erfolg verbraucht.** Ein Abbruch
  kostet nichts. `CanInteract` blockt einen aussichtslosen Fang ohnehin vorher.
- **[entschieden] Fangen ist eine Halte-Interaktion**, dieselbe wie beim Abbau.
  `IInteractable` trägt alles Nötige: `CanInteract(out blockedReason)` meldet
  „Ladung zu gering" **vor** dem Halten, `UpdateInteraction(normalizedProgress)`
  liefert den Fangbalken, `CancelInteraction(reason)` bricht bei Schaden aus
  anderer Quelle, Ausweichen, Angriff und Reichweite ab. **Kein neues
  Interaktionssystem.**
- **[entschieden] Das aktuelle Fangziel verursacht ab Beginn eines gültigen
  Fangversuchs keinen Schaden mehr.** Laufende Angriffe, Trefferfenster und
  zielgebundene Projektile werden neutralisiert; nur dieses eine Eidra wechselt
  in den Fangzustand. Andernfalls würde sein eigener Treffer den Fangkanal
  abbrechen und das Ziel wäre praktisch nie fangbar. Bei Abbruch nimmt es sein
  normales Verhalten wieder auf, bei Erfolg wechselt es ins Team.
- **[entschieden] Der Druck kommt aus der Verwundbarkeit gegenüber der
  Umgebung, nicht aus Eigenschaden des Fangziels und nicht aus einem Timer.**
  Andere Gegner dürfen den Spieler weiterhin treffen und den Fang abbrechen.
  Dazu kommt die **Fluchtschwelle**: Unter einem Anteil seiner Lebenspunkte
  versucht das Eidra zu fliehen — man kann den Bedarf also nicht beliebig weit
  herunterprügeln.
- **[entschieden] Das eigene Team deckt den Spieler während des Kanals.**
  Es hält andere Gegner vom stillstehenden Spieler fern. Damit bleibt das Team
  mechanisch relevant, ohne dass der erste Fang durch unvermeidbaren Schaden
  seines eigenen Ziels blockiert wird — die Einlösung von M0 („Eidra sind keine
  Begleit-Deko").
- **[entschieden] Höchstens zwei Eidra sind gleichzeitig aktiv.** Dieselbe Art
  darf **mehrfach** gefangen werden, um später Produktionsstätten zu verstärken
  (M6).
  **Folge für den Spielstand: Er hält eine *Liste von Eidra-Instanzen*, keine
  Artenflags.** Ein `hatTerrock: true` in v0.1 wäre später eine Migration mitten
  im Speicherformat (§17). Dazu ein Feld für das aktive Zweierteam.
- **[entschieden]** `EidraData` bekommt ab sofort ein **Rollenfeld** (Pflanze,
  Wasser, Feuer, Arbeiter) sowie **Fangbedarf und Fluchtschwelle**. Die Rolle
  wird erst nach v0.1 benutzt und kostet heute nichts (M1.4-Präzedenz).
**[entschieden] Wo Eidra stehen:**

- **Jede Art hat genau ein Heimatgebiet.** Nicht jedes Gebiet hat eine Art — mit
  wachsendem Bestand füllen sich die Gebiete auf. In v0.1: **Terrock im
  Steinbruch, Noctarion im Nebelmoor.**
  *Begründung:* Das setzt die Zonenidentität aus M1.7 fort. Ein Gebiet ist dann
  nicht nur die beste Quelle für ein Material, sondern die **einzige** Quelle für
  eine Art — und die Zonenwahl zählt zweimal.
- **Feste Anzahl je Gebiet, Platzierung aus dem Zonen-Seed** (M1.2, M1.3). Keine
  Streuung auf der Menge, genau wie bei Ressourcenknoten.
- **Sie regenerieren mit der Zone** (M1.1). Das ist Pflicht, nicht Komfort: Ohne
  nachwachsende Quelle versiegt der Nachschub für die späteren
  Produktions-Eidra (M6).
- **Sie kommen aus `ZoneDefinition.enemyAllocations`.** Das Feld existiert
  bereits, und ein Eidra ist datenseitig ein Gegner mit Fangdaten — **kein
  zweites Spawnsystem.**
- **[entschieden] Eidra greifen nicht von sich aus an.** Sie sind neutral, bis
  der Spieler sie angreift. Damit ist der Kampf eine **Entscheidung** statt eines
  Überfalls, und niemand verliert ein Ziel, das er gar nicht bemerkt hat.
- **[entschieden] Eidra sterben nicht — sie fliehen.** Unter der Fluchtschwelle
  bricht das Eidra ab und verschwindet. Es gibt keine Eidra-Beutetabelle und
  keinen „töten statt fangen"-Pfad.
  *Begründung:* Der Fehlschlag ist damit „es ist entkommen", nicht „ich habe
  versehentlich zerstört, was ich wollte" — und er ist über die Regeneration
  behebbar. M1.3s Grundsatz, dass ein Fehlwurf sich nicht wie eine Strafe
  anfühlen darf, gilt hier genauso.

**[entschieden] Freilassen kommt nicht in v0.1.** Solange Produktions-Eidra
fehlen (M6), gibt es keinen Grund, Doppelfänge anzulegen, und der Bestand bleibt
bei zwei. Die Regel wird trotzdem jetzt festgelegt, damit sie später nicht als
Wirtschaftsaktion missverstanden wird: **Freilassen gibt nichts zurück, weder
Batterie noch Material.** Andernfalls wäre Fangen risikoloses Ausprobieren.
Wenn es kommt, ist es ein Commit-Punkt nach §4.

### M8.4 Nahrungskette

Bufffood hat bis heute **kein Rezept** — es existiert nur als Startgegenstand und
als Loot. Für v0.1 bekommt es eine eigene Lieferkette:

```
Faserknoten  →  Weizensamen  →  Feld  →  Weizen  →  Kochtopf  →  Brot
```

- **[entschieden] Samen sind ein Nebenprodukt der Faserknoten**, kein eigener
  Knotentyp. Ein fester Anteil der Faserknoten einer Zone trägt zusätzlich Samen.
  *Begründung:* Der Heiltrank kommt bereits aus Faser (2× Pflanzenfaser) — die
  gleiche Kette zweimal wäre langweilig. Ein fünfter Knotentyp würde dagegen die
  30/12/4/2-Symmetrie aus M1.7 brechen. Das Nebenprodukt kostet weder ein fünftes
  Material noch ein fünftes Prefab und gibt **Nebelmoor** eine eigene Identität
  als Versorgungsgebiet vor dem Boss.
- **[entschieden] Nebenprodukte sind deterministisch** (M1.2): fester Anteil aus
  dem Zonen-Seed, **keine Wurfchance je Abbau**. Sonst kippt die Planbarkeit aus
  M1.3.
- **[entschieden] Das Feld wächst pro Heimkehr, nicht in Echtzeit.**
  M1.1 legt die Heimkehr als **einzige** Uhr fest; ein echtzeitwachsendes Feld
  würde dem widersprechen. So treibt eine einzige Uhr sowohl die
  Zonenregeneration als auch die Basisproduktion, und die Schleife
  *rausgehen → farmen → heimkommen → Feld wächst, Zonen erneuern sich*
  schließt sich von selbst.
  **Das Feld ist damit der erste Fall von Basisproduktion** (M1.5, M6) und setzt
  das Muster für alle weiteren Produktionsgebäude.
- **[entschieden] Der Kochtopf ist eine eigene Crafting-Station.**
  `CraftingStationType` kennt heute nur `None` und `Workbench` und muss auf
  **fünf** Stationen erweitert werden: Werkbank, Schmelzofen, Sägewerk,
  Seilerei, Steinmetz, Kochtopf (M6).

**[entschieden] Beeren sind die erste Heilquelle — und der Heiltrank wird T1.**

```
Beeren (T0, roh, sofort, schwach, ohne Station)  →  Heiltrank (T1, Kochtopf)
```

- **[entschieden] Heiltrank: 1 Kupferbarren + 3 Beeren.** Der Barren ist die
  **Phiole**, nicht das Getränk. Das alte Rezept (2 Pflanzenfaser am Lagerfeuer)
  entfällt; die ID `healing_potion` bleibt (§12).
- **[entschieden] Der Heiltrank ist bewusst teuer.** Er konkurriert mit
  Werkzeugen und Waffen um Kupfer, das einzige knappe T1-Material. Das ist eine
  Abwägung, keine Sperre — und die erste spürbare Progressionsstufe des Spiels
  liegt damit zwischen roher Beere und Trank.
- **[entschieden] Brot behält die ID `buff_food`.** Nur der Anzeigename ändert
  sich. Die ID beschreibt die Funktion, und die bleibt stabil — siehe §12.
- **[entschieden] Weizensamen werden beim Einsetzen verbraucht.** Eine Ernte
  liefert **keine** neuen Samen; für den nächsten Anbau braucht es neue Samen aus
  Faserknoten.
  *Begründung:* Der Acker soll sich lohnen, aber nicht sich selbst tragen. Ohne
  Verbrauch wäre er nach dem ersten Samen eine geschlossene Schleife und die
  Faserknoten wären für die Nahrungskette bedeutungslos.
  **Folge: Der Samenanteil der Faserknoten ist die einzige Drossel der gesamten
  Nahrungskette.** Er ist die Zahl, an der Brot und Bufffood balanciert werden —
  nicht die Rezeptmengen dahinter.
- **[vorläufig]** Rezeptmengen und Samenanteil stehen vollständig in **M8.9**.

### M8.5 Neue Gegenstände in v0.1

| ID | Anzeigename | Tier | Herkunft |
| --- | --- | --- | --- |
| `berry` | Beeren | T0 | Beerenstrauch, alle Gebiete (M1.7) |
| `wheat_seed` | Weizensamen | T0 | Nebenprodukt der Faserknoten |
| `axe` | Axt | T0 | Handarbeit, ab Start |
| `scythe` | Sense | T0 | Handarbeit, ab Start |
| `pickaxe` | Spitzhacke | T0 | Handarbeit, ab Start — Tor zu T1 (M1.9) |
| `armor_wanderer_hood` | Stoffkapuze | T0 | Werkbank, nach Seilerei |
| `armor_wanderer_coat` | Stoffmantel | T0 | Werkbank, nach Seilerei |
| `armor_wanderer_bracers` | Stoffarmschienen | T0 | Werkbank, nach Seilerei |
| `armor_wanderer_legs` | Stoffschuhe | T0 | Werkbank, nach Seilerei |
| `plank` | Brett | T1 | Sägewerk |
| `rope` | Seil | T1 | Seilerei |
| `stone_block` | Steinblock | T1 | Steinmetz |
| `wheat` | Weizen | T1 | Acker (Basis) |
| `catch_device` | Fanggerät | T1 | Werkbank (M8.3) |
| `battery` | Batterie | T1 | Werkbank (M8.3) |
| `buff_food` | Brot | T1 | Kochtopf — ID bestehend, Anzeigename neu |

Bestehende IDs, deren Einordnung sich ändert: `wood`, `stone`, `plant_fiber`
werden **T0**; `copper_ore`, `copper_bar`, `healing_potion` bleiben **T1**.
**Keine ID ändert sich** — `tier` ist ein Feld (M1.4, §12).

**Die Stoffrüstung ist die bewusste Ausnahme von der üblichen
Materiallesart:** Ihre Ausrüstungsqualität ist T0, obwohl Seil als
T1-Veredelungsprodukt ihre Verfügbarkeit bis zur Seilerei verzögert. Sie
verwendet weder Kupfer noch Schutzwerte; „T0“ beschreibt hier die erste
Rüstungsqualität, nicht eine Startfreischaltung.

### M8.6 Progressionsstufen und der Technologiebaum

Wenn v0.1 das Einführungskapitel ist, dann ist der Technologiebaum seine
Struktur: **Jede Freischaltung führt genau eine Mechanik ein.** Damit ist die
Reihenfolge der Knoten keine Balancing-Frage, sondern eine didaktische — und der
Baum bekommt ein Entwurfskriterium statt „irgendwelche Upgrades".

**Zwei getrennte Währungen. Sie werden nie gegeneinander getauscht:**

```
Spielen            →  Level  →  Technologiepunkte  →  Knoten innerhalb der Stufe
Boss → Dungeon     →  nächste Progressionsstufe    →  neues Levelmaximum,
                                                      neue Knoten
```

- **[entschieden] Jede Progressionsstufe hat ein Maximallevel.** Am Maximum
  bringt weiteres Spielen keine Technologiepunkte mehr.
- **[entschieden] Die nächste Stufe wird erspielt, nicht gekauft.** Der einzige
  Weg ist: Boss besiegt → Dungeon offen → Dungeon bestanden → nächste Stufe
  (M1.8).
  *Begründung:* Ohne Maximallevel wäre Sammeln der schnellste Weg durch den Baum
  und Bosse wären optional. Mit ihm ist **Sammeln die Breite und der Dungeon die
  Tiefe** — der Spieler kann seine Stufe ausschöpfen, aber nicht überspringen.
- **[entschieden] Überschüssige Erfahrung am Maximallevel verfällt.** Sie wird
  nicht für die nächste Stufe gutgeschrieben.
  *Begründung:* Das Maximallevel ist ein Wegweiser zum Boss, kein Sparkonto.
  Vorfarmen würde genau die Sperre aushebeln, für die es da ist.
- **[entschieden] Eine Progressionsstufe entspricht einer Tierstufe.**
  Ausnahme ist **Stufe 1**, die **T0 und T1 zusammen** trägt: Vor Garon gibt es in
  v0.1 keinen Boss, der eine Stufe öffnen könnte. Der Übergang T0 → T1 ist
  deshalb über Rezepte und Gebäudekosten gestaffelt, nicht über ein Tor (M6).
- **[vorläufig] Maximallevel Stufe 1: 20.** Muss ausgespielt werden.
- **[entschieden] In Stufe 1 reichen die Punkte für alle Knoten.** Es ist das
  Lehrkapitel — jede Mechanik muss gezeigt werden. **Ab Stufe 2 sind Punkte
  knapper als Knoten**, erst dort wird Spezialisierung zur Entscheidung.
- **[entschieden] Erfahrungsquellen.** Sammeln **je Einheit**, nicht je Knoten —
  dadurch beschleunigt ein besseres Werkzeug auch den Fortschritt (M1.9). Das
  **erste** Herstellen eines Rezepts und der **erste** Bau eines Gebäudetyps
  geben einen großen Betrag. **Wiederholung gibt nahezu nichts.**
  *Begründung:* Ohne diese Trennung wird das billigste Rezept in Schleife
  hergestellt. Der Baum belohnt die Lektion, nicht die Wiederholung.
- **[entschieden] In v0.1 speisen sich die Knoten aus Spielfortschritt**, nicht
  aus Dungeons. M1.8 sieht Dungeons als Quelle der Freischaltungen vor — der
  erste Dungeon öffnet sich aber erst *nach* Garon und damit erst nach v0.1.

**Technologiebaum Stufe 1 [vorläufig]** — 23 stabile Knoten-IDs. Axt, Sense und
Spitzhacke sind Startfreischaltungen und kosten keine Punkte; die übrigen 20
Knoten kosten je 1 Punkt. Stufe 1 vergibt bis Level 20 insgesamt 20 Punkte:
Alle Forschungen sind damit exakt freischaltbar, es bleibt kein Punkt übrig.

**Darstellungsreihenfolge:** Axt, Sense und Spitzhacke stehen auf den Positionen
1 bis 3; die Werkbank steht auf Position 4. Die stabilen IDs und ihre Nummern
ändern sich dadurch nicht.

```
START — Handarbeit ohne Werkbank und ohne Technologiepunkte
  ②  AXT            ⑤  SENSE            ⑨  SPITZHACKE ─────► öffnet Kupfer

T0 — Werkbank an vierter UI-Position; danach aus dem Einmalvorrat
  ①  WERKBANK ──► ③  Lagerkiste ──► ④  Hammer ──► ⑥  Dolche
       ──► ⑦  Mauer/Boden/Tür ──► ⑧  Acker

T1 — die Wende der Wirtschaft
  ⑧ ──► ⑩  SCHMELZOFEN   (reine T0-Kosten; Kupferabbau braucht ⑨ als Item)
       ├── ⑪  FANGGERÄT + Batterie ──► ⑫  Zweiter Eidra-Platz
       ├── ⑬  Sägewerk    ⑭  Seilerei    ⑮  Steinmetz
       │                  ├── ⑳  Stoffkapuze
       │                  ├── ㉑  Stoffmantel
       │                  ├── ㉒  Stoffarmschienen
       │                  └── ㉓  Stoffschuhe
       ├── ⑯  Kochtopf ──► ⑰  Heiltrank
       └── ⑱  T1-Werkzeuge    ⑲  T1-Waffen
```

Die vier Stoffrüstungsteile verwenden die neuen stabilen Technologie-IDs
`technology.20.cloth_hood`, `technology.21.cloth_coat`,
`technology.22.cloth_bracers` und `technology.23.cloth_shoes`. Sie liegen als
vier parallele Felder hinter der Seilerei und schalten jeweils genau ein Rezept
frei. Ein gemeinsamer Sammelknoten ist unzulässig.

**Der wirtschaftliche Angelpunkt ist die Spitzhacke ⑨, aber sie ist kein
kaufbarer Knoten mehr.** Ihr Rezept ist von Anfang an bekannt; gebaut wird sie
per Hand. Sie bleibt das einzige T0-Item, das nicht nur den Ertrag erhöht,
sondern ein Material *aufschließt* (M1.9). Der Schmelzofen hängt im Baum am
Acker ⑧, praktisch bleibt Kupfer durch den Besitz der Spitzhacke gesperrt.

**Das Fanggerät steht bewusst früh in T1, direkt hinter Sägewerk und
Seilerei** (seit v0.3.1/F31-011; zuvor direkt hinter dem Schmelzofen, was
seine Zutaten Brett und Seil überging). Der zweite Eidra-Platz kommt seither
mit dem Fanggerät-Knoten selbst, nicht mehr als eigener Knoten.
*Begründung:* M0 verlangt, dass Eidra tragend sind. Stünde das Fangen am Ende des
Baums, spielte der Spieler den größten Teil von v0.1 ohne Team — und die
Deckung durch das eigene Team beim Fangen (M8.3) käme genau einmal zum Tragen,
nämlich nie beim ersten Mal. Früh eingehängt begleiten die Eidra die gesamte
zweite Hälfte, in der die Gegner härter werden.

**Zwei Prüfpunkte für die Erfahrungskurve** statt erfundener Zahlen:

- Der Einmalvorrat der Heimatbasis (M1.1) trägt bis **Level 4** — also genau
  durch Werkbank, handgefertigte Axt, Lagerkiste und Hammer. Dann ist die Basis
  leer.
- **Garon ist frühestens auf Level 18 legbar**, damit die letzten Knoten nicht
  nach dem Boss kommen.

- **[offen]** Die Erfahrungskurve selbst. Bewusst erst nach dem ersten
  Durchlauf, gegen die zwei Prüfpunkte oben.
- **[offen]** Ob Technologiepunkte rückerstattbar sind.

### M8.7 Das Ende von v0.1

**[entschieden]** v0.1 endet mit dem Sieg über Garon und dem sich öffnenden
Eingang des ersten Dungeons (M1.8). Der Dungeon selbst und alles dahinter —
T2, weitere Waffen, weitere Eidra — gehört in v0.2.

Durch M8.6 hat dieses Ende eine präzise Kante: Der Spieler steht **am
Maximallevel von Stufe 1**, hat Garon gelegt und sieht einen offenen
Dungeoneingang, hinter dem sein nächstes Level liegt. Weiterspielen bringt keine
Technologiepunkte mehr — es gibt also kein Auslaufen ins Leere.

Kein Abspann, sondern eine offene Tür.

### M8.8 Nicht in v0.1

- T2- und T3-Materialien und -Rezepte.
- **Der erste Dungeon selbst.** In v0.1 wird nur sein Eingang freigeschaltet.
- Weitere Waffen und weitere Eidra über die ersten zwei hinaus.
- Boss-Wiederholung und Boss-Cooldown (M1.8).
- **Haltbarkeitsverlust.** Das Feld wird angelegt und gespeichert, der Verbrauch
  bleibt abgeschaltet (M1.9, M2, M5).
- **Waffen- und Werkzeug-Upgrades.** Jede Waffe und jedes Werkzeug hat in v0.1
  genau eine Stufe. `WeaponProgressionState` und das Save-Feld bleiben angelegt
  und ruhen (M2, §17). **Folge:** Neben `axe`, `scythe`, `pickaxe`, `hammer`
  und `daggers` existieren in v0.1 genau vier Rüstungsteile:
  `armor_wanderer_hood`, `armor_wanderer_coat`,
  `armor_wanderer_bracers` und `armor_wanderer_legs`. Ihre Anzeigenamen sind
  Stoffkapuze, Stoffmantel, Stoffarmschienen und Stoffschuhe (M5, M8.5).
- **Reparieren** (M5).
- **Eidra an Produktionsstätten.** Alle Stätten laufen in v0.1 ohne Eidra (M6).
- **Freilassen** gefangener Eidra (M8.3).
- **Reisekosten**, weder Zeit noch Energie (M7).

### M8.9 Rezeptmengen und Kosten [vorläufig]

Alle Werte sind Balancing und ändern sich erwartungsgemäß. Sie stehen hier
vollständig, damit die Kette **einmal durchgerechnet** ist statt an vier Stellen
geschätzt.

**T0 — Handarbeit oder Werkbank (ab Spielbeginn freigeschaltet)**

| Gegenstand | Kosten |
| --- | --- |
| Axt | 3 Holz + 2 Stein + 2 Faser |
| Sense | 2 Holz + 2 Stein + 3 Faser |
| Spitzhacke | 3 Holz + 4 Stein + 2 Faser |

**T0 — Werkbank**

| Gegenstand | Kosten |
| --- | --- |
| Hammer | 4 Holz + 3 Stein + 3 Faser |
| Dolche | 2 Holz + 5 Stein + 3 Faser |

**T0 — Stoffrüstung an der Werkbank (nach Freischaltung der Seilerei)**

| Gegenstand | Kosten |
| --- | --- |
| Stoffkapuze | 2 Seil + 2 Pflanzenfaser |
| Stoffmantel | 4 Seil + 4 Pflanzenfaser |
| Stoffarmschienen | 2 Seil + 2 Pflanzenfaser |
| Stoffschuhe | 3 Seil + 2 Pflanzenfaser |

**T0 — Gebäude**

| Gebäude | Kosten | Baufelder |
| --- | --- | --- |
| Werkbank | 8 Holz + 6 Stein | 1 |
| Lagerkiste | 10 Holz + 4 Stein | 1 |
| **Schmelzofen** | 6 Holz + 12 Stein | 1 |
| Acker | 6 Holz + 4 Faser | **2 × 2** |
| Mauer · Boden | je 2 Holz | — |
| Tür | 4 Holz + 2 Faser | — |

**T1 — Veredelung.** Je **eine** Zutat, weil es unter T1 keine Vorstufe gibt
(M1.4).

| Ergebnis | Kosten | Station |
| --- | --- | --- |
| 1 Brett | 2 Holz | Sägewerk |
| 1 Seil | 2 Faser | Seilerei |
| 1 Steinblock | 2 Stein | Steinmetz |
| 1 Kupferbarren | 3 Kupfererz | Schmelzofen |

**Der Umrechnungsfaktor 2 : 1 ist der Balancinghebel aus M1.4.** Er wirkt
rekursiv: Ein T3-Brett enthält damit achtmal T0-Holz. Wer ihn auf 3 : 1 hebt,
landet bei siebenundzwanzigmal — das ist dieselbe Zahl, die M1.5 als Nachfrage
beschreibt, und sie wird **nur hier** gedreht.

**T1 — Produktionsgebäude.** T0-Material plus Kupferbarren; die Reihenfolge
innerhalb von T1 steht damit in den Kosten (M6).

| Gebäude | Kosten |
| --- | --- |
| Sägewerk | 10 Holz + 6 Stein + 1 Kupferbarren |
| Seilerei | 8 Holz + 4 Stein + 1 Kupferbarren |
| Steinmetz | 6 Holz + 10 Stein + 1 Kupferbarren |
| Kochtopf | 4 Holz + 8 Stein + 2 Kupferbarren |

**T1 — Ausrüstung (Werkbank)**

| Gegenstand | Kosten |
| --- | --- |
| Fanggerät | 2 Brett + 2 Seil + 2 Kupferbarren |
| Batterie | 2 Seil + 1 Kupferbarren |

- **[entschieden] In v0.1 gibt es keine Werkzeug- und Waffen-Upgrades** (M2,
  M8.8). Die Zeilen dafür standen hier und sind entfallen; die Kosten werden
  neu festgelegt, wenn die Upgrades kommen.
- **[historisch, durch v0.2 ersetzt]** Der damalige Plan sah Upgrades innerhalb
  derselben ID vor. M1.9 und M2 entscheiden stattdessen eigene feste
  Werkzeug- und Waffenvarianten je Tier; die alte Prognose ist nicht mehr
  bindend.

**T1 — Verbrauchsgüter (Kochtopf)**

| Ergebnis | Kosten |
| --- | --- |
| 1 Heiltrank | 1 Kupferbarren + 3 Beeren |
| 2 Brot | 6 Weizen |

**Nahrungskette** (M8.4)

| Schritt | Menge |
| --- | --- |
| Samenanteil | **jeder dritte Faserknoten** trägt zusätzlich 1 Weizensamen |
| Acker | **4 Plätze, einer je Baufeld.** 1 Samen je Platz |
| Ernte | **3 Weizen je Samen, je Heimkehr.** Der Samen ist danach verbraucht |
| Kochtopf | 6 Weizen → 2 Brot |

Ein voll bestellter Acker liefert damit **12 Weizen und 4 Brot je Heimkehr** und
kostet 4 Samen, also den Ertrag von 12 Faserknoten. Nebelmoor trägt 45 davon —
die Nahrungskette hängt dauerhaft an dieser einen Zone, genau wie in M8.4
beabsichtigt.

**Zwei Proben, die bei jeder Änderung neu zu rechnen sind:**

1. **Der Einmalvorrat der Heimatbasis muss die ersten vier Einstiegsschritte
   tragen** (M1.1, M8.6). Werkbank + handgefertigte Axt + Lagerkiste + Hammer
   kosten **25 Holz,
   15 Stein, 5 Faser**; der Vorrat liefert von Hand 26 / 18 / 10. Es bleiben
   1 Holz, 3 Stein und 5 Faser übrig — die Basis ist danach praktisch leer, und
   das fehlende Holz ist es, das den Spieler hinausschickt.
2. **Der vollständige T1-Satz kostet 8 Kupferbarren, also 24 Kupfererz.**
   Fanggerät 2, Batterie 1, Sägewerk 1, Seilerei 1, Steinmetz 1, Kochtopf 2.
   Bei Stapelgröße 10 (M5) sind das **mindestens drei Ladungen** — Kupfer ist
   damit der eigentliche Zeitfaktor von T1, nicht die Rezepte.
   *Vorher standen hier 15 Barren; die Differenz sind die entfallenen
   Werkzeug- und Waffen-Upgrades (M2).*
   **Das ist die Zahl, die im ersten Durchlauf zu beobachten ist.** Wird sie zu
   zäh, ist der erste Hebel die Stapelgröße, nicht die Rezeptmenge: Sie steht in
   M5 als `[vorläufig]` und ist je Item ein Datenwert.

---

## M9 Abschluss von Version 0.1 und Grenze zu v0.2

Version 0.1 ist der belastbare Ausgangspunkt für die weitere Entwicklung.
Der geprüfte Lieferstand und seine Artefakte stehen in
`Documentation/Releases/v0.1.0.md`; Änderungen werden ab jetzt im
`CHANGELOG.md` fortgeschrieben. Abgeschlossene Arbeitsaufträge bleiben nicht
als parallele Dokumentation im Projekt. Ihre verbindlichen Ergebnisse stehen in
dieser Bibel, im Changelog und in den Release Notes; die Detailhistorie bewahrt
Git über Commits und Tags.

### M9.1 Finaler technischer Stand

- Der reguläre Szenenfluss besteht aus `Bootstrap`, `MainMenu`, `WorldMap`,
  `HomeBase`, `Zone_Greenwood`, `Zone_Quarry`, `Zone_Marsh` und
  `Zone_EmberRuins`. Nur diese acht Szenen stehen in den Build Settings.
- Ressourcen, Herstellung, Basisbau, Lagerung, Kampf, Eidra-Fang und -Team,
  Weltkarte, Gebietsausgänge, Tod/Wiederbelebung, Audio, Pause sowie Speichern
  und Laden sind durch EditMode- und PlayMode-Regressionstests abgesichert.
- Die Welt verwendet ausschließlich die in M10 festgelegte hybride Bildsprache:
  3D-Weltobjekte, 2D-Akteure und 2D-Oberfläche. Alte prozedurale oder
  skelettbasierte 3D-Akteure sind weder Produktionsinhalt noch Laufzeit-Fallback.
- Die Größentabelle enthält nur produktive Assets. Prototyp-spezifische Zeilen
  und doppelte Ressourcenpfade sind entfernt.
- Der Windows-Release wird ausschließlich über
  `WindowsReleaseBuilder.Build` erzeugt. Entwicklungs-, Capture-, Review- und
  Sandbox-Szenen werden zusätzlich durch die allgemeine Release-Filterregel
  ausgeschlossen.

### M9.2 Abgeschlossener Prototyp-Rückbau

Die frühere `Prototype_Arena` samt `PrototypeBootstrap`, ihren einmaligen
Buildern, eigenen Ressourcen und alten 3D-Akteuren wurde nach der Migration der
Tests auf echte Spielszenen entfernt. Der Laufzeit-Zonengenerator,
`ZonePlayerSpawner`, der zentrale Service-Root und die produktiven Szenen
übernehmen alle zuvor noch tragenden Aufgaben. Es gibt keinen zweiten
Composition-Pfad mehr.

### M9.3 Dokumentations- und Asset-Hygiene

- Dauerhafte Regeln gehören in diese Programmbibel; visuelle Regeln zusätzlich
  in `Documentation/ART_BIBLE.md`, Architekturentscheidungen in
  `Documentation/ARCHITECTURE.md` beziehungsweise in ADRs.
- Nutzerrelevante Änderungen gehören ins `CHANGELOG.md`; ein freigegebener
  Stand erhält genau eine Release-Note unter `Documentation/Releases/`.
- Abgenommene Arbeitsordner, Review-Bilder, temporäre Capture-Dateien,
  duplizierte Runtime-Assets und einmalige Migrationsskripte bleiben nicht im
  aktiven Projekt.
- `Library/`, `.utmp/`, lokale Logs und Testresultate sind regenerierbar und
  werden nicht versioniert. Die gebündelte Unity-Editor-Installation bleibt
  lokal erhalten, weil sie die reproduzierbare Entwicklung und Prüfung von
  v0.2 ermöglicht.
- Neue v0.2-Arbeit erweitert diesen Stand. Sie darf keine als „offen“ formulierte
  Altbestandsliste aus v0.1 reaktivieren; neue Ziele werden ausdrücklich
  entschieden, umgesetzt, getestet und dokumentiert.

---

## M10 Darstellung, Maßstab und Objekthöhen

Steht am Ende, weil es zuletzt aufgeschrieben wurde — nicht weil es unwichtig
ist. M1 (Zonengröße), M2 (Reichweiten) und M6 (Grundflächen) rechnen alle in
Welteinheiten und setzen damit voraus, was hier festgelegt wird. Wer neu
anfängt, liest M10 vor M1.

### M10.1 Die Darstellungsform

- **[entschieden] Eidren ist räumlich: 3D-Welt, 3D-Akteure, 2D-Oberfläche.**
  Boden, Bäume, Felsen, Ressourcenknoten, Gebäude, Ruinen, Deko und andere
  unbewegliche Weltobjekte sind Low-Poly-Geometrie. Spieler, Wildling, wilde
  und aktive Eidren sowie Bosse sind handgebaute Low-Poly-Modelle in
  derselben Welt. HUD, Fenster, Weltkarte, Gegenstands- und
  Fähigkeitssymbole bleiben 2D.
  *Begründung:* Figur und Welt entstehen aus derselben Fabrik-Pipeline
  (M10.11) und beantworten dasselbe Licht. Ein gemaltes Bild bringt sein
  eigenes Licht mit und arbeitet gegen das gerechnete; genau daran ist die
  frühere Hybridfassung gescheitert.
- **[entschieden] Der Bestand ist die Grundlage, nicht der Abfall.** Bei
  unbeweglichen Weltobjekten dürfen vorhandene generierte Bilder als Textur-
  oder Formreferenz dienen. Bei Akteuren dienen die abgenommenen
  Schlüsselbilder als **Vorlage für das Modell** — Farbe, Proportion und
  Silhouette sind verbindlich, das Bild selbst ist es nicht mehr. Historische
  2D-Sprites sind kein Laufzeit-Fallback; ihre Entwicklungshistorie liegt in
  Git.
- **[entschieden] Die Kamera dreht sich nicht.** Orthografisch, feste Rotation
  52°/45° (M10.5). Das war vorher eine Folge der Darstellungsform; jetzt steht
  es für sich — und es **senkt** die Kosten statischer 3D-Weltobjekte, weil sie
  nur aus einem Winkel zu sehen sind. Bei ihnen dürfen abgewandte Flächen
  einfach bleiben. **Für Akteure gilt das nicht:** sie drehen sich frei zur
  Bewegungs- oder Zielrichtung und werden aus jeder Richtung gesehen. Die acht
  Sektoren N, NE, E, SE, S, SW, W und NW bleiben als `ActorFacing8` erhalten,
  sind aber nur noch die 45°-Quantisierung für den Rückfall, nicht mehr acht
  gezeichnete Ansichten.
- **[entschieden] Bewegliche Akteure verwenden handgebaute Modelle mit echten
  Animationsclips.** Ein Würfel, ein Primitivkörper, eine extrudierte
  Silhouette oder eine kameragedrehte Frontfläche erfüllen die Regel nicht.
  Jeder sichtbare Gameplayzustand braucht einen Clip; wo einer fehlt, ist
  eine benannte prozedurale Pose zulässig, aber kein stillschweigender
  Ersatz durch einen fremden Clip. Spieler und Eidra erhalten die höchste
  Animationspriorität; Bosse dürfen mehr Zustände und spektakulärere Effekte
  tragen.
- **[entschieden] Der Stil ist modern, nicht nostalgisch.** Keine Pixelart,
  keine Retro-Palette, kein Nearest-Neighbor-Look und keine dauerhaft
  unbeleuchteten Figuren. Facettierte Formen, Vertexfarben, Outline,
  Partikel, Nebelreaktion, dynamische Beleuchtung und dynamische
  Boden-/Kontaktschatten binden die Akteure an die Welt. Texturen und Normal
  Maps sind bei Akteuren **nicht** vorgesehen — die Farbe steckt im Mesh.
- **[entschieden] Tiefe wird durch mehrere Signale erzeugt.** 3D-Verdeckung,
  Größenverhältnisse, Bodenkontakt, Normal-Map-Licht, Schatten, Partikel und
  gebietsgebundene Beleuchtung müssen zusammenarbeiten. Ein Akteur schreibt
  beziehungsweise prüft Tiefe so, dass Bäume, Felsen, Mauern und Gebäude ihn
  korrekt verdecken; er darf nicht wie ein UI-Sticker vor der Welt liegen.
- **[entschieden] Root Motion bleibt aus.** `CharacterController`,
  `NavMeshAgent` und die bestehenden Gameplaycontroller bewegen die Wurzel und
  bestimmen Zustand, Trefferfenster, Schaden und Reichweite. Animation und
  Clips stellen diese Wahrheit dar, entscheiden sie aber nicht.
  Animationsereignisse dürfen keine zweite Gameplaylogik eröffnen.
- **[entschieden] Beleuchtung ist Teil der Art Direction.** Die Welt verwendet
  URP-Licht, gebietsgebundene Stimmungsprofile und dynamische Schatten.
  Akteurs-Albedos werden lichtneutral genug produziert, dass gemaltes Licht
  nicht gegen das gerechnete arbeitet. Der Akteursshader verarbeitet mindestens
  Albedo, Normal Map, Umgebungslicht, Hauptlicht, Nebel und eine kontrollierbare
  Outline. Emission ist für Augen, Runen und Fähigkeiten optional.
- **[entschieden] Akteure haben dynamische Boden-/Kontaktschatten.** Ein
  projizierter, weicher Schatten am echten Weltfußpunkt darf für v0.1 günstiger
  sein als ein vollständiger Alpha-Schattenwurf. Richtung, Abstand, Größe und
  Deckkraft reagieren aber auf Hauptlicht, Pose, Höhe und Bossmaßstab. Ein
  schwarzer Kreis oder ein in das Albedo gemalter Bodenschatten ist kein
  Endzustand.

> **Änderungshistorie.** Drei Fassungen, in dieser Reihenfolge: Welt aus
> Billboards, dann 3D-Welt mit gemalten 2D-Akteuren, seit dem 14.08.2026
> durchgehend 3D. Die ersten beiden sind verworfen.
>
> Der zweite Umstieg ist der lehrreiche. Die Hybridfassung war nicht an
> Bildqualität gescheitert, sondern an drei Dingen, die sich nicht auflösen
> ließen: Ein Sprite braucht acht gezeichnete Ansichten je Zustand, und jeder
> neue Zustand kostet acht neue Bilder — bei fünfzehn Kreaturen war das die
> Bremse. Ein gemaltes Albedo trägt sein Licht mit sich; im Verlies und im
> Abendlicht stimmte es nie gleichzeitig. Und eine Figur, die eine Waffe
> tragen soll, muss die Waffe im Bild haben — jede Kombination aus Rüstung und
> Waffe wäre ein weiterer Atlas gewesen. Ein Modell mit Slotmeshes löst alle
> drei nebenbei.
>
> Preis des Umstiegs: 545 MB Sprite-Atlanten entfernt, das Paket halbiert.
> Der abgeschlossene Weltstand steht in **M10.9**, die produktive
> Akteurspipeline in **M10.10**, die Fabrik in **M10.11**.

### M10.2 Der Maßstab

- **[entschieden] Bezugsgröße ist der Spieler, und der ist 2,0 Welteinheiten
  hoch.** Das ist keine neue Zahl, sondern die, die schon gilt: der
  `CharacterController` im Player-Prefab steht auf `height 2.0 / radius 0.48`,
  und daran hängen NavMesh, Schrittweite, Ausweichdistanz und die Reichweiten
  aus M2. **1 Spielerhöhe (SH) = 2,0 Einheiten.**
- **[entschieden] Die Darstellung folgt dem Collider, nicht umgekehrt.**
  `IActorPresentation.WorldHeight`, die Größentabelle und der
  `CharacterController` des Spielers stehen auf 2,0 Einheiten.
  *Begründung:* Zwei bereits entschiedene Werte geben dem Recht.
  **Baufeldraster 1,0** (`BuildingPlacementRule.GridSize`, M6) ist bei einem
  2,0-Spieler ein halber Spieler und damit ein brauchbares Mauersegment; bei 2,9
  wäre es ein Drittel, und jede Mauer bräuchte drei Felder. **Kartengröße 80**
  (M1.1) ergibt bei 2,0 eine Spielfläche von 38 Spielerhöhen, bei 2,9 nur 26.
  Beide Systeme sind gegen den 2,0-Anker gebaut; die Akteurspipeline hält ihn
  durch Wächtertests stabil.
- **[entschieden] Ableitung: 1 Welteinheit ≈ 0,9 m.** Die Angabe dient dem
  Augenmaß beim Zeichnen und der Plausibilitätsprüfung. Gerechnet wird in
  Einheiten und SH, nie in Metern.
- **[entschieden] Höhe meint die sichtbare Silhouette.** Gemessen wird an der
  sichtbaren Renderer-Ausdehnung, bei Akteuren an der Ruhepose. Sonst
  entscheidet ein unsichtbarer Meshüberstand über die Objektgröße. Die alte
  Alphaschwelle 0,02 galt für Sprites und ist mit ihnen weggefallen.
- **[entschieden] Eine Tabelle, ein Ort.** Höhen sind statische Inhaltsdaten und
  liegen als solche (§18, §15). Prefabs persistieren nur das aus der Tabelle
  gebackene Ergebnis; Wächtertests verhindern abweichende zweite Wahrheiten.
- **[entschieden] Visual und Collider kommen aus derselben Zeile** (§25). Ein
  Objekt hat eine Größe, nicht zwei. Ob das Visual ein Sprite oder ein
  3D-Renderer ist, ändert den Maßstab nicht.

### M10.3 Die Höhentabelle

Die **Werte sind [vorläufig]** — sie sind Augenmaß und werden nach dem ersten
gespielten Durchlauf justiert. **[entschieden]** ist, dass es genau diese eine
Tabelle gibt, dass sie die einzige Quelle ist und dass Visual und Collider
gemeinsam daraus folgen.

> **Die Höhe allein macht kein Objekt richtig.** Sie sagt nichts über Breite,
> Tiefe und Aufbau. Wie aus diesen Höhen Geometrie wird, steht in **M10.8**.
> (Bis zur Umstellung auf 3D stand hier ein Verweis auf M10.7 und ein
> Breitenbudget je Objektklasse — das galt für Billboards und ist aufgehoben,
> siehe M10.9.)

**Akteure**

| Objekt | Höhe | SH | Anmerkung |
| --- | --- | --- | --- |
| Spieler | 2,0 | 1,00 | Anker. Collider `2.0 / 0.48` bleibt unverändert |
| Wildling | 1,9 | 0,95 | gebückt; Gameplay-Collider `2.1 / 0.52` |
| Eidra, kleine Art (Terrock, Noctarion) | 1,0 | 0,50 | vierbeinige Begleitgröße |
| Wildes Eidra derselben Art | 1,0 | 0,50 | gleiche Art, gleicher Wert |
| Boss Garon | 4,5 | 2,25 | Telegraphen-Lesbarkeit gegenprüfen (M1.8) |
| Ignivar, aktiv oder wild | 1,0 | 0,50 | kleines Feuer-Eidra; gleiche Fang-/Begleitgröße |
| Rissling | 1,7 | 0,85 | schneller T2-Nahkämpfer |
| Wurzelstürmer | 2,2 | 1,10 | Sturmtelegraph gegen Gruppen prüfen |
| Moorwerfer | 1,9 | 0,95 | Projektilursprung am Körperanker |
| Granitpanzer | 2,4 | 1,20 | geschützte schwere Silhouette |
| Risswächter | 3,0 | 1,50 | T2-Elite |
| Glutzehrer | 1,6 | 0,80 | kleiner Gewölbenahkämpfer |
| Aschenläufer | 1,8 | 0,90 | schneller Gewölbegegner |
| Schmiedewächter | 2,5 | 1,25 | schwerer geschützter Wächter |
| Siegelwächter | 3,0 | 1,50 | optionaler Gewölbe-Elitegegner |
| Kernwächter | 4,5 | 2,25 | Boss; offener Kern muss im Kamerarahmen lesbar bleiben |

Die neuen v0.2-Akteurshöhen sind **[vorläufige Produktionswerte]**. Änderung
erfolgt ausschließlich in dieser Tabelle und zieht Collider-, Prefab- und
Telegraphentests gemeinsam nach sich.

**Ressourcenknoten** — je aktiver und erschöpfter Zustand

| Objekt | aktiv | erschöpft | SH aktiv | Anmerkung |
| --- | --- | --- | --- | --- |
| Baum (Holz) | 6,0 | 0,7 | 3,00 | Stumpf muss aus Laufentfernung sichtbar bleiben |
| Beerenstrauch | 0,9 | 0,7 | 0,45 | abgeerntet bleibt der Strauch stehen |
| Faserpflanze | 0,9 | 0,25 | 0,45 | für klare Lesbarkeit angehoben |
| Steinvorkommen | 1,1 | 0,5 | 0,55 | |
| Kupferader | 1,3 | 1,3 | 0,65 | Wirtsfels bleibt, nur das Erz verschwindet |
| Hartholzbaum | 6,0 | 0,7 | 3,00 | gleiche Baumklasse, aktiver Stamm deutlich kräftiger |
| Sumpfhanf | 0,9 | 0,25 | 0,45 | gleiche Faserklasse, aus Laufentfernung T2-lesbar |
| Granitvorkommen | 1,1 | 0,5 | 0,55 | gleiche Steinklasse |
| Eisenerzader | 1,3 | 1,3 | 0,65 | Wirtsfels bleibt, Erzauflage verschwindet |

**Gebäude Stufe 1** — Grundfläche ein Baufeld (1,0 × 1,0), Acker 2 × 2 (M6)

| Objekt | Höhe | SH | Anmerkung |
| --- | --- | --- | --- |
| Mauer | 2,6 | 1,30 | **muss den Spieler überragen** — worüber man hinwegsieht, ist keine Mauer |
| Tür | 2,6 | 1,30 | Durchgang 2,0 hoch, also genau Spielerhöhe |
| Boden | Bodenebene | — | kein Billboard, siehe M10.4 |
| Acker | Bodenebene | — | kein Billboard, 2 × 2 Felder |
| Werkbank | 1,1 | 0,55 | Arbeitshöhe. Eine Werkbank ist Möbel, keine Wand |
| Kochtopf | 1,0 | 0,50 | |
| Schmelzofen | 2,0 | 1,00 | |
| Sägewerk | 2,2 | 1,10 | |
| Seilerei | 1,6 | 0,80 | |
| Steinmetz | 1,4 | 0,70 | |
| Lagerkiste | 0,8 | 0,40 | |

**Deko und Weltobjekte**

| Objekt | Höhe | SH | Anmerkung |
| --- | --- | --- | --- |
| Baum groß / mittel / klein | 7,5 / 6,0 / 4,0 | 3,75 / 3,00 / 2,00 | **Der mittlere Wert ist der des Holzknotens.** Ein Wald, in dem nur der Kümmerling fällbar ist, ist kaputt |
| Fels groß / mittel / klein | 2,4 / 1,1 / 0,5 | 1,20 / 0,55 / 0,25 | mittlerer Wert **gleich** dem Steinvorkommen |
| Busch / Farn / Blumen | 0,9 / 0,7 / 0,4 | 0,45 / 0,35 / 0,20 | Busch = Beerenstrauch, sonst wird gesucht, was nicht abbaubar ist |
| Bodendecker (Gras, Moos) | Bodenebene | — | M10.4 |
| Ruinenmonument / Ruinenwand | 5,0 / 2,6 | 2,50 / 1,30 | Ruinenwand = Mauer |
| Rune / Leuchtpilze | 0,8 / 0,35 | 0,40 / 0,18 | |
| Weltgegenstand (liegender Loot) | 0,4 | 0,20 | |
| Todesbeutel | 0,7 | 0,35 | muss aus etwa 15 Einheiten Entfernung auffindbar sein (M4) |

**Loot- und Gewölbecontainer**

| Objekt | Höhe | SH | Anmerkung |
| --- | ---: | ---: | --- |
| gewöhnliche / versteckte Weltkiste | 0,7 / 0,6 | 0,35 / 0,30 | versteckt heißt nicht unsichtbar |
| bewachte Weltkiste | 0,9 | 0,45 | aus Kampfentfernung lesbar |
| Versorgungskiste / optionale Schatzkiste | 0,8 | 0,40 | unterschiedliche Silhouette trotz gleicher Höhe |
| Elitekiste | 1,0 | 0,50 | Siegelwächterbelohnung |
| Abschlusskiste | 1,2 | 0,60 | klare Bossbelohnung, kein Leuchtstrahl nötig |
| Markenkiste klein / mittel / groß | 0,7 / 0,9 / 1,2 | 0,35 / 0,45 / 0,60 | Wertigkeit über Form und Volumen |
| Bergungscontainer | 0,8 | 0,40 | eindeutig vom normalen Loot unterscheidbar |

**Zwei Regeln, die aus der Tabelle folgen und beim Zeichnen mitgelten:**

1. **Wo ein Deko-Objekt und ein Ressourcenknoten dasselbe darstellen, tragen sie
   denselben Wert.** Baum mittel = Holzknoten, Busch = Beerenstrauch, Fels mittel
   = Steinvorkommen. Sonst lernt der Spieler die Größe als Abbaubarkeitssignal —
   und das ist ein Signal, das §24 über das Bild lösen soll, nicht über den
   Maßstab.
2. **Alles über 1 SH kann den Spieler verdecken, ab 2 SH verdeckt es ihn
   zuverlässig.** Die Kamera steht fest; wer hinter einem höheren Objekt steht,
   ist weg. Betroffen sind nach dieser Tabelle: Bäume, Boss, Ruinenmonument
   (über 2 SH) sowie Mauer, Tür, Ruinenwand, Sägewerk und großer Fels
   (über 1 SH). Sie brauchen eine Behandlung für „Spieler steht dahinter".
   *Technik [offen]* — Kronen ausblenden, Teiltransparenz oder Platzierungsregel.
   Zu entscheiden, bevor der Zonengenerator Bäume streut (M1.3).
3. **Die Tabelle gilt je Objektklasse, nicht je Gebietsvariante.** Vier
   Baumbilder für vier Gebiete (M11.4) tragen **eine** Höhe — die aus der Zeile
   „Baum (Holz)". Wer die Tabelle je Gebiet vervierfacht, hat M10.2 („eine
   Tabelle, ein Ort") aufgegeben.

### M10.4 Sonderklasse Bodenebene

- **[entschieden]** Boden, Acker, Wegplatten, Decals, Reichweiten- und
  Vorschauanzeigen **stehen nicht, sie liegen**. Sie sind flach in der XZ-Ebene
  und tragen eine **Grundfläche**, keine Höhe. Mit Geometrie (M10.8) ist das
  eine liegende Platte; die Klasse bleibt trotzdem, weil sie sagt, dass diese
  Objekte keine Höhe haben.
- Die produktiven Boden- und Acker-Prefabs sind flach gebacken und werden durch
  die Größentests gegen ihre XZ-Ausdehnung geprüft.
- **[entschieden]** Die Tabelle in M10.3 trägt für diese Objekte den Eintrag
  `Bodenebene` statt einer Zahl. Wer dort eine Höhe einträgt, hat das Objekt in
  die falsche Klasse gesteckt.

### M10.5 Kamerarahmen

- **[entschieden]** Orthografisch, feste Rotation 52°/45°. Begründung in M10.1.
- **[entschieden]** Die Verfolgung selbst bleibt, wie sie ist: `SmoothDamp`,
  Look-Ahead und Bounds-Clamping in `IsometricCamera`. Sie war nie das Problem.
- **[vorläufig] `BaseOrthographicSize` 7,4** — das sind 14,8 Einheiten sichtbare
  Höhe und damit 7,4 SH. Der Spieler nimmt gut ein Siebtel der Bildhöhe ein; das
  trifft die Referenz und bleibt zunächst unangetastet.
- **Achtung:** `MinimumOrthographicSize` 5,5 (11 Einheiten) muss **neu geprüft
  werden**, sobald Bäume 6,0 hoch sind — ein einzelner Baum deckt dann über die
  Hälfte der sichtbaren Höhe. Die Werte stehen je Zone in
  `ZoneDefinition.Camera`, nicht global.

### M10.6 Der tote Gegner [offen]

Bewusst **nicht** in v0.1 und hier nur als Hinweis für später festgehalten.

- **Stand heute:** Es gibt keinen Leichen-*Inhalt*. `EnemyControllerBase` setzt
  `EnemyState.Dead`, schaltet die Navigation ab und lässt das Objekt stehen.
  Seit dem 3D-Umstieg spielt dabei ein echter Tod-Clip statt der alten
  gekippten Billboard-Pose — der Körper sieht richtig aus, ist aber weiterhin
  nur Darstellung. Der Beutel fällt daneben.
- **Warum das später ein Mechanikthema ist, kein Grafikthema:** Ein Körper, der
  liegen bleibt und *benutzbar* ist — ausnehmen, häuten, verwerten — berührt
  Beute (M5), Interaktion und die Nahrungskette (M8.4) gleichzeitig. Er gehört
  damit in eine eigene Feature-Änderung und nicht in einen Art-Durchgang.
- **Was dann gilt:** Eine benutzbare Leiche ist ein **eigener Zustand** mit
  eigenem Behälter und eigener Interaktion, nicht die Endpose des Tod-Clips.
  Sie bekommt eine eigene Zeile in M10.3, liegend gemessen: Länge statt Höhe,
  Richtwert etwa 1 SH für einen Vierbeiner.

### M10.7 Proportion und Blickwinkel — aufgehoben

**Dieser Abschnitt galt für die frühere Fassung, in der die gesamte Welt aus
Billboards bestand, und bindet für 3D-Weltobjekte nicht mehr** (M10.1, M10.9).
Er stand hier, weil ein zur Kamera gedrehtes Bild seine Breite aus dem Bild
bekommt: ein quadratisches Bild auf Höhe 6,0 war auch 6,0 breit. Daraus folgten
Breitenbudgets je Objektklasse, ein Einpass-Mechanismus und die Markierung
„Bild fehlt".

**Mit Geometrie entfällt der Grund dafür.** Ein Modell hat kein
Seitenverhältnis, das getroffen werden muss, und keinen Blickwinkel, in dem es
gezeichnet ist. Das gilt inzwischen für Akteure genauso wie für Weltobjekte —
mit dem 3D-Umstieg ist der Abschnitt vollständig gegenstandslos geworden. An
die Stelle der alten Weltregeln tritt **M10.8**, an die der alten
Akteursregeln **M10.10**.

Zwei Sätze überleben, weil sie nie von der Bauform abhingen:

- **Ein Gebäude ragt nie über seine Grundfläche hinaus** (M6). Das war eine
  Bildvorgabe und ist jetzt eine Geometrievorgabe.
- **Ein Bild, das seine Tabellenzeile nicht tragen kann, wird ersetzt — nicht
  die Zeile.** Gilt für Geometrie genauso: die Tabelle beschreibt, wie groß die
  Welt ist.

Der Wächtertest zum Breitenbudget prüft, dass kein Gebäude sein Baufeld
verlässt.

### M10.8 3D-Welt aus vorhandenen Bildern

Wie aus dem Bestand eine schöne Low-Poly-Welt wird, ohne bei null anzufangen.

- **[entschieden] Unbewegliche Weltobjekte verwenden bewusst geformte
  Low-Poly-Geometrie mit lichtreagierenden Materialien.** Vorhandene Bilder
  dürfen als Textur oder Formreferenz dienen, sind aber keine Pflicht. Kästen,
  Zylinder und andere Primitive sind nur Ausgangskörper; der fertige Gegenstand
  braucht eine eigenständige, aus der Spielkamera sofort lesbare Silhouette.
  Kein hochauflösendes Modellieren und keine unnötigen LOD-Ketten.
  Normal Maps kommen weder bei Weltobjekten noch bei Akteuren vor; die Farbe
  steckt in den Vertices (M10.11).
  *Begründung:* Die Qualität kommt aus Silhouette, Proportion, Material,
  Licht, Komposition und sichtbarer Funktion, nicht aus maximaler Polygonzahl
  oder einer aufgesetzten Frontillustration.
- **[entschieden] Laub und Pflanzen sind gekreuzte Alpha-Flächen, keine Kugeln.**
  Stamm als Zylinder, Krone aus zwei bis drei gekreuzten Quads mit dem
  vorhandenen Baumbild und Alphakanal. *Begründung:* „Zylinder plus Kugel" war
  der verworfene Platzhalterzustand. Wer bei „einfacher Geometrie" dorthin
  zurückgeht, hat den Kreis geschlossen, ohne etwas gewonnen zu haben.
  Gekreuzte Flächen sind echte Geometrie: sie drehen richtig und verdecken sich
  selbst.
- **[entschieden] Unbewegliche Weltobjekte werden als vollständige 3D-Formen
  gebaut.** Unterseiten und dauerhaft verdeckte Innenflächen dürfen entfallen;
  Vorderseite, Seiten und Rückseite bleiben jedoch aus mehreren zulässigen
  Kamerawinkeln und bei Baurotation plausibel. Eine dekorierte Frontplatte mit
  unbehandelter Rückseite ist kein fertiges Weltobjekt.
- **[entschieden] Produktionsstätten zeigen ihre Funktion in der Geometrie.**
  Sägeblatt, Seilstränge, Mahl- oder Schneidstein, Feuerstelle, Blasebalg,
  Werkzeuge und Werkstücke sind lesbare Formmerkmale statt Beschriftung. Kleine
  Details ordnen sich einer kräftigen Hauptsilhouette unter.
- **[entschieden] Die freigegebene Material- und Formsprache der Weltobjekte ist
  handwerklich und malerisch.** Facettierte, leicht unregelmäßige Formen,
  warmes Holz, gebrochener Stein, dunkles Eisen sowie sparsame Gold-, Glut- und
  Kupfer-/Patinaakzente bilden eine Familie. Reine Standardprimitive, sterile
  Symmetrie, fotorealistische Materialien und laute Vollflächenfarben sind kein
  Endzustand.
- **[entschieden] Ressourcenknoten besitzen zwei vollständig lesbare Zustände.**
  Aktiv und erschöpft teilen Grundform und Fußpunkt. Beim Kupfer bleibt der
  Wirtsfels erhalten, während Erzauflage, Glanz und Patina zurückgehen. Abbau
  darf die Materialität durch kurze Funken-, Staub- und Fragmentimpulse
  verstärken; Feedback ersetzt nie die Zustandsgeometrie (§24).
- **[entschieden] Bewegliche Akteure folgen denselben Formregeln, aber einer
  eigenen Pipeline** (M10.10). Verboten bleiben ein Würfel, ein
  Primitive-Körper, eine extrudierte Silhouette oder eine kameragedrehte
  Frontfläche als fertiger Akteursersatz. Anders als Weltobjekte werden
  Akteure aus **allen** Richtungen gesehen: abgewandte Flächen dürfen bei
  ihnen nicht entfallen, und eine Figur ist erst geprüft, wenn sie auch von
  hinten geprüft wurde.
- **[entschieden] Sichtbare Spielerausrüstung bleibt Pflicht.** Kapuze,
  Mantel/Schal/Gürtel, Armschienen/Handschuhe sowie Hose/Stiefel müssen im
  leeren und ausgerüsteten Zustand korrekt sichtbar sein. Gelöst wird das
  über geschaltete Slotmeshes am Modell; frei kombinierbar, ohne dass
  Gameplaylogik in die Grafikschicht wandert. Was nicht getragen wird, ist
  auch nicht in der Hand — ein dauerhaft angehängtes Werkzeug ist ein Fehler
  (W-003).
- **[entschieden] Die Höhentabelle bleibt die Prüfgröße** (M10.3). Gemessen
  wird durchgehend an der sichtbaren Renderer-Ausdehnung. Die Werte ändern
  sich durch die Bauform nicht.
- **[entschieden] Weltmaterialien reagieren auf die Gebietsbeleuchtung.**
  Gemalte Texturen werden so neutralisiert oder materialseitig begrenzt, dass
  das gemalte Licht nicht gegen Hauptlicht und Schatten arbeitet. Die
  vorhandenen Stimmungsprofile, ACES, Nebel und Gameplay-Farbrollen bleiben
  maßgeblich.

### M10.9 Abgeschlossene Migration der Weltobjekte

Die unbewegliche Welt ist vollständig auf autorierte 3D-Geometrie umgestellt.
Größen werden gegen die sichtbare Renderer-Ausdehnung geprüft; bei Gebäuden
gilt zusätzlich das Baufeld als Breiten- und Tiefenbudget. Bodenobjekte liegen
in der XZ-Ebene. Aktiver und erschöpfter Zustand von Ressourcenknoten teilen
Fußpunkt und Grundform.

Die früheren Billboard- und Prototyp-Pfade sind entfernt. Neue Weltobjekte
dürfen nur über die produktiven Prefabs und stabilen Ressourcen-IDs angebunden
werden. Eine zusätzliche Laufzeit- oder Editor-Fallback-Geometrie ist
unzulässig.

### M10.10 Produktive 3D-Akteurspipeline

Alle beweglichen Akteure verwenden handgebaute Modelle. Das umfasst den
Wanderer und die fünfzehn Kreaturen der Serie — Wildling, Riftling, Garon,
Terrock, Noctarion, Ignivar, EmberEater, RootCharger, GraniteShell, AshRunner
und die übrigen. Sprite-Atlanten sind kein Laufzeitpfad mehr.

**Zwei Präsentationsschichten, und sie sind bewusst getrennt:**

- `MeshActorPresentation` trägt den Wanderer. Sie löst Clips über **Haltungen**
  auf (`Ruhe_Dolche`, `Angriff_Hammer`, `Abbau_Axt`), weil dieselbe Figur je
  nach getragenem Gerät anders steht. Was die `Wanderer.glb` nicht als Clip
  hat — Treffer, Taumeln, Tod, Erscheinen, Ausweichen, Ernten — stellt sie
  prozedural dar. Sie schaltet außerdem die Rüstungs- und Waffen-Slotmeshes.
- `CreatureMeshPresentation` trägt die Kreaturen. Sie löst **direkt** auf: ein
  Zustand, ein Clipname, kein Ersatz. Fehlt ein Clip, bleibt der laufende
  stehen. *Begründung:* Kreaturen haben keine Haltungen, und sie haben Treffer,
  Taumeln, Tod und Erscheinen als echte Clips — ein prozeduraler Rückfall wäre
  bei ihnen ein Rückschritt. Sichtbar falsch ist besser als unsichtbar
  erfunden.

Die Zustandszahl ist nicht überall gleich: acht bei den elf V02-Kreaturen,
zehn bei den Eidra, neun beim Boss, vierundzwanzig beim Wanderer.
`ActorVisualState` fasst nur acht; darüber hinaus läuft alles über
`SetAuthoredState(stem)` mit frei benannten Zuständen.

**Gemeinsame technische Regeln:**

1. Akteure **drehen sich frei** zur Bewegungs- oder Zielrichtung. `ActorFacing8`
   bleibt als Schnittstelle bestehen, liefert aber nur noch die
   45°-Quantisierung als Rückfall — keine acht gezeichneten Ansichten mehr.
2. Ein Präsentationsadapter übersetzt Gameplayzustand, Richtung und
   normalisierte Zeit in den passenden Clip. Gameplaycode kennt keinen
   konkreten Renderer-Typ.
3. Farbe steckt in den Vertices, Licht kommt aus dem Vertex-Lit-Shader
   (`WandererVertexLit`, `M_Kreatur_VertexLit`). Keine Texturen, keine Normal
   Maps. Emission ist über das Glut-Material möglich (M10.11).
4. Fußpunkt und `WorldHeight` entsprechen der Größentabelle (M10.3) und sind
   über alle Zustände stabil.
5. Ausrüstung sind geschaltete Slotmeshes, keine gemalten Ebenen. Ein Werkzeug
   ist genau dann in der Hand, wenn es benutzt wird.
6. Partikel hängen an Weltankern — Fuß, Körpermitte, Kopf, Hand, Waffenende
   oder Fähigkeitspunkt.
7. Der dynamische Bodenschatten sitzt am Weltfußpunkt und reagiert auf
   Hauptlicht, Pose, Höhe und Objektklasse.
8. Animation stellt Telegraph, Execute und Recover dar, verändert aber weder
   Trefferfenster noch Schaden. Clips und Animation Events eröffnen keine
   zweite Spiellogik.
9. Neue Akteure werden erst produktiv eingebunden, wenn Pflichtzustände,
   Lichtreaktion, dynamischer Schatten, Tiefe, Maßstab, echte Zonencaptures,
   Regressionstests und Windows-Build bestanden sind — **und die Figur in
   Unity aus allen Richtungen angesehen wurde, ausdrücklich von hinten.**

**Ein bestandenes Prüfskript heißt bei Figuren nicht fertig.** Die Serie hat
das zweimal gezeigt. Der Quell-Viewer der Modelle zeichnet Rückseiten, Unity
cullt sie: was im Viewer geschlossen aussieht, kann im Spiel durchsichtig sein.
Und eine Figur kann jede Zahlenprüfung bestehen und ihrer Vorlage trotzdem
nicht ähneln. Der Render gehört neben die Vorlage gehalten, nicht neben den
Testbericht.

### M10.11 Die Fabrik-Pipeline

Sowohl Weltobjekte als auch Akteure entstehen aus `EidrenMeshFactory` im
Editor — kein externes Modellierwerkzeug im Produktivpfad, keine importierten
Meshes außer den Akteurs-GLBs.

Drei Grundkörper tragen alles: `TaperedBox` (Kasten mit verjüngter
Deckfläche), `Wedge` (Keil) und `Loft` (Profilstapel mit Rect- oder
Oct-Querschnitt). Aus Profilstapeln entstehen Rohre, Essen, Schalen und Bögen;
ein Bogen kommt aus dem seitlichen Versatz der Profilringe, nicht aus einer
Rotation.

Vier Dinge, die beim Bauen immer wieder kosten:

- **`TaperedBox` und `Wedge` wachsen von der Basisfläche bei y=0 nach oben.**
  Sie sind nicht zentriert. Wer sie wie einen Unity-Würfel positioniert, setzt
  sie eine halbe Höhe zu hoch.
- **Die Wicklung der Seitenflächen muss nach außen zeigen.** Am 14.08.2026 war
  sie bei allen Lofts einwärts gewickelt; die Objekte waren dadurch im Spiel
  durchsichtig, und im Viewer fiel es nicht auf (M10.10). Nach einer Korrektur
  an der Fabrik müssen **alle** betroffenen Assets neu gebaut werden — der
  Fehler steckt in den erzeugten Meshes, nicht im Code.
- **`kontaktAo: false` bei selbstleuchtenden Flächen.** Die eingebackene
  Kontaktabdunklung bleicht die Glut sonst an den Rändern aus.
- **Große Flächen bekommen keinen Verlauf.** Flat Shading erzeugt pro Fläche
  genau einen Ton. Wer einen Verlauf will, zerlegt die Fläche in Segmente mit
  gestaffelten Vertexfarben und zahlt einen Profilring mehr.

Selbstleuchtende Teile verwenden `M_EidrenWorld_VertexGlow` (Shader
`Eidren/World/VertexGlow`), der die Vertexfarbe unbeleuchtet ausgibt. Das
Projekt rendert im Gamma-Farbraum, und **kein Volume-Profil enthält
Nachbearbeitung** — es gibt kein Bloom. Glut entsteht aus Farbe und Kontrast,
nicht aus Abstrahlung. Eine Glutfläche braucht deshalb kein eigenes
Punktlicht; Licht wird nur gesetzt, wo die *Umgebung* mitgefärbt werden soll.

---

## M11 Gebietsbild

M10 sagt, **wie groß** ein Objekt ist. Dieses Kapitel sagt, **wie ein Gebiet
aussieht** — und es ist ein eigenes Kapitel, weil das eine ohne das andere nicht
reicht: Vier Gebiete mit korrekten Objekthöhen und identischem Bild sind vier
Mal dieselbe Karte.

**Aktueller v0.1-Stand:** Alle fünf Gebiete besitzen ein eigenes
`ZoneAreaArtDefinition`-Asset und genau einen `AreaArt`-Root. Boden, Dekoration,
Stimmung, Spawns und gebietseigene Ressourcenvarianten werden dort gebündelt.
Die früheren grauen Testprimitive, der Greenwood-Doppelboden und der
Style-Proof-Referenzpfad sind entfernt. Die Weltobjekt-Verfeinerung vom
1. August 2026 ersetzt zusätzlich die generischen Produktionsstätten, Bauobjekte,
Felsen sowie Stein- und Kupferknoten durch die verbindliche Formsprache aus
M10.8, ohne stabile Ressourcen- oder Gebäude-IDs zu ändern.

**Verbindliches v0.2-Ziel:** Dämmerhain, Schleiermoor und Grauklüfte ergänzen
den Bestand auf acht Gebietsbild-Assets einschließlich Heimatbasis. Die feste
Eidra-Schmiede ist eine eigene autorierte Dungeonszene und kein regenerierendes
Außengebietsbild.

### M11.1 Jedes Gebiet hat ein Gebietsbild, und es liegt an einer Stelle

- **[entschieden] Ein Gebietsbild je Gebiet, als eigenes Datenasset**, von
  `ZoneDefinition` referenziert. Es trägt Bodenschichten, Deko-Bestand,
  Knotenvarianten und Stimmung.
  *Begründung:* Die Frage „wie sieht Greenwood aus" ist heute an **drei** Stellen
  beantwortet — Materialien und Prop-Positionen in `StyleProofContentBuilder`,
  die Bodenprimitive in `EidrenSceneStructureBuilder`, die Knotenvisuals in
  `ResourceNodeVisualTable`. Drei Dateien, eine Frage: genau der Zustand, den §6
  verbietet. Form ist ein ScriptableObject (§18: statische Daten), es validiert
  sich selbst (§15) und liegt in `Eidren.Data` (§20).
- **[entschieden] Das Gebietsbild sagt, WELCHES Bild gilt — niemals, wie groß es
  ist.** Größen kommen aus M10.3, und zwar aus derselben Zeile für alle
  Gebietsvarianten desselben Objekts.
  *Begründung:* Zwei Größentabellen sind schlimmer als keine. Wer die Höhe ins
  Gebietsbild schreibt, hat M10.2 („eine Tabelle, ein Ort") aufgegeben, ohne es
  zu merken.
- **[entschieden] Die Heimatbasis trägt ebenfalls ein Gebietsbild.** Sie ist kein
  Außengebiet (M1.1), aber sie ist der Ort, den der Spieler am häufigsten sieht.

### M11.2 Der Boden ist mehrschichtig

- **[entschieden] Jedes Gebiet trägt einen Grundboden und eine bis zwei
  Beimischungen** — nie nur einen Boden, nie mehr als drei.
  *Begründung für die Untergrenze:* Ein einziger gekachelter Boden liest sich bei
  fester Kamera als Tischdecke. Ohne Perspektive und ohne Verdeckung (M10.1) ist
  der Boden die einzige zusammenhängende Fläche im Bild; ist sie überall gleich,
  hat das Auge keinen Anhaltspunkt, und der Spieler kann zwei Stellen derselben
  Karte nicht unterscheiden.
  *Begründung für die Obergrenze:* Jede Schicht ist eine transparente Fläche über
  dem Boden und kostet Overdraw auf einem Mobilziel (M0). Drei Töne sind genug,
  um eine Fläche zu gliedern; ab vier wird aus Gliederung Unruhe.
- **[entschieden] Beimischungen liegen als Bodenebene** (M10.4): flach in der
  XZ-Ebene, weiche Kante **aus dem Alphakanal**, nicht aus der Geometrie.
  *Beleg, dass das heute fehlt:* `GroundTransition_West` ist ein um 9° gedrehter
  Würfel, `GroundTransition_North` ein plattgedrückter Zylinder. Beide haben eine
  harte Kante, weil es keine Klasse für „weicher Übergang" gibt.
- **[entschieden] Eine Bodenkachel ist 6 Welteinheiten groß**, also 3
  Spielerhöhen (M10.2), und damit ein Vielfaches des Baufeldrasters 1,0 (M6).
  *Begründung:* Bei `BaseOrthographicSize` 7,4 sind 14,8 Einheiten Bildhöhe
  sichtbar (M10.5). Eine 6er-Kachel wiederholt sich rund zweieinhalb Mal pro
  Bildhöhe — sichtbar als Material, nicht als Muster. Heute steht
  `M_SP_Ground` auf Texturskalierung 2,6 über einer 78 Einheiten breiten Fläche;
  das ist eine Kachel von 30 Einheiten und der Grund, aus dem der Waldboden
  matschig wirkt. **Die Kachelgröße ist eine Zahl in Welteinheiten, kein
  Texturfaktor** — der Faktor folgt aus der Zonengröße.
- **[entschieden] Es gibt genau eine Bodenfläche je Zone**, und das ist
  `WalkableGround`. Sie trägt den Grundboden.
  *Begründung:* Heute ist ihr Renderer in Greenwood **abgeschaltet** und ein
  zweiter Würfel `Ground_StyleLayer` liegt darüber. Zwei Böden, einer sichtbar,
  einer als Collider — das ist die Doppelwahrheit aus §6, nur in Geometrie.
- **[entschieden] Die Bodenmischung ist autoriert, nicht gewürfelt.** Sie liegt
  im Szenen-Asset und ändert sich nicht bei der Regeneration.
  *Begründung:* Der Boden ist Gelände, nicht Inhalt. Ein Gebiet, dessen Boden sich
  bei jeder Heimkehr neu verteilt, ist bei jedem Betreten ein anderer Ort — und
  genau die Wiedererkennbarkeit ist der Zweck von M1.1 („alle Karten gleich groß,
  nur der Inhalt variiert").
- **[vorläufig] Verworfen, aber nicht endgültig: der Splat-Shader.** Ein Shader
  Graph, der zwei bis drei Böden über eine Maske verblendet, sieht besser aus als
  aufgelegte Flächen. Er kostet einen eigenen Shader plus eine Maskentextur je
  Zone und ist auf dem Zielgerät **nicht gemessen** — dieselbe Lage wie beim
  fehlenden Mobile-Budget in §20. Solange nicht gemessen ist, gilt die Lösung
  ohne neuen Shader.

**Bodenmischung je Gebiet [vorläufig]** — die Zahl der Schichten ist
[entschieden], die Motive sind Augenmaß und werden nach dem ersten Durchlauf
justiert.

| Gebiet | Grundboden | Beimischung 1 | Beimischung 2 |
| --- | --- | --- | --- |
| **Greenwood** · Wald | grüne Wiese | braune, offene Erde | Waldboden, dunkler Humus — **vorhanden** als `greenwood_ground_v01` |
| **Nebelmoor** · Faser | nasses Torfmoos | Schlick mit Pfützen | bleiches Riedgras |
| **Steinbruch** · Stein | Bruchgrus, Schotter | freigelegter Felsboden | staubiger Lehm |
| **Glutruinen** · Kupfer | Asche | gesprungene Ruinenplatten | Schlacke mit Glutspalten |
| **Heimatbasis** | getretener Lehm | Grasnarbe | — |
| **Dämmerhain** · Hartholz | dunkler Waldboden | offene Wurzelerde | überwachsene Steinreste |
| **Schleiermoor** · Sumpfhanf | nasser Torf | Schlick/Wasserflächen | bleiches Sumpfgras |
| **Grauklüfte** · Granit | Bruchfels | staubiger Schotter | sichtbare Eisenspuren |

**Zur Greenwood-Zeile:** Der vorhandene Boden ist sehr dunkel und wird vom
Grundboden zur Beimischung — als Schatten unter den Kronen. Grundboden wird die
Wiese. *Begründung:* Der Grundboden bestimmt die Helligkeit des ganzen Bildes;
ein Einstiegsgebiet, dessen Fläche fast schwarz ist, liest sich als Nacht.

### M11.3 Kein Pfad

- **[entschieden] Außengebiete haben keine Wege.** Der Referenzpfad in Greenwood
  wird entfernt, nicht ersetzt.
  *Begründung:* Ein Weg ist ein Versprechen darüber, wo etwas ist. Die
  Knotenmenge wird aus dem Zonen-Seed platziert und liegt nach jeder
  Regeneration anders (M1.2, M1.3) — ein festgezeichneter Weg führt damit
  nirgendwohin. Der vorhandene Pfad wurde gegen ein **gebackenes** Knotenfeld
  autoriert, das im produktiven Zonengenerator nicht mehr existiert. Ein Weg, der nichts
  verbindet, lehrt den Spieler eine Route, die falsch ist; das ist schlechter als
  gar keine Führung.
- **[entschieden] Was den Weg ersetzt, ist die Bodenmischung und die
  Deko-Dichte** (M11.2, M11.6) — **Landmarken statt Routen.** Eine Lichtung, ein
  Felsrücken, ein Ruinenrest sagen „hier war ich schon", ohne zu behaupten, wohin
  es weitergeht.
- **Folge:** `greenwood_path_v01.png`, die Materialien `M_SP_Path` und
  `M_SP_PathEdge` sowie `Route`/`BuildRoute` in `StyleProofContentBuilder`
  verlieren ihren Inhalt und werden **gelöscht**, nicht aufbewahrt. Ein Bild ohne
  Inhalt ist kein Bestand (§24, §6).

### M11.4 Derselbe Rohstoff, mehrere Gebietsvarianten

- **[entschieden] Ein Ressourcenknoten hat eine ID und mehrere Gestalten.** Das
  Gebiet entscheidet, welche gilt. **Die ID trägt das Gebiet nie** (§12); Ertrag,
  Werkzeugbedarf, Interaktion und Ausgabegegenstand sind in allen Gebieten
  identisch.
  *Begründung:* M1.7 macht jedes Gebiet zum besten Ort für ein Material und
  damit zu einem Ort mit Identität. Diese Identität liegt **im Aussehen** — es
  gibt keine andere Ebene, auf der sich zwei Gebiete unterscheiden könnten. Ein
  Baum, der im Wald und in den Glutruinen gleich aussieht, sagt dem Spieler, dass
  beides derselbe Ort ist.
- **[entschieden] Eine Variante ist Darstellung, keine Mechanik.** Gleiche Höhe
  aus derselben Zeile (M10.3), gleicher Collider, gleicher Ertrag, gleiche
  Reichweite. Wer davon etwas ändert, hat einen **zweiten Knotentyp** gebaut und
  braucht eine eigene ID.
- **[entschieden] Der Erkennungsmarker bleibt über alle Gebiete gleich.** Der
  abbaubare Baum trägt eine **Axt mit ockerfarbenem Tuch** am Stamm — abzulesen
  an Zelle 08 von `greenwood_vegetation_v01`. Das ist das Signal „hier kann
  abgebaut werden", und es ist der Grund, aus dem der Spieler den Holzknoten vom
  Deko-Baum unterscheidet, obwohl beide gleich hoch sind (M10.3, Regel 1).
  *Begründung:* Ohne diese Regel muss der Spieler vier abbaubare Bäume lernen
  statt einen. Die Gestalt wechselt mit dem Gebiet, der Marker nie. Damit trägt
  §24 weiter: die Abbaubarkeit steckt im Bild, nicht in der Größe.
- **[entschieden] Pflicht ist die Variante beim Baum in jedem Gebiet und beim
  Leitmaterial des Gebiets** (M1.7). Alles andere darf die Vorgabe benutzen; eine
  fehlende Variante ist **kein Fehler**.
  *Begründung:* Der Baum ist das höchste und häufigste Objekt der Karte (6,0
  Einheiten, 45 Stück im Holzgebiet) — er bestimmt den Gesamteindruck allein. Das
  Leitmaterial ist der Grund, aus dem der Spieler das Gebiet betritt; es soll wie
  dieses Gebiet aussehen. Damit sind es sieben Pflichtvarianten statt sechzehn,
  und die Auswahl steht in M1.7 statt im Geschmack.

| Gebiet | Baum | Leitmaterial |
| --- | --- | --- |
| **Greenwood** | Pflicht — **liegt vor** | Holz, also derselbe Baum |
| **Nebelmoor** | Pflicht | Faserpflanze |
| **Steinbruch** | Pflicht | Steinvorkommen |
| **Glutruinen** | Pflicht | Kupferader |
| **Dämmerhain** | Hartholzvariante Pflicht | Hartholz, derselbe Knoten |
| **Schleiermoor** | Hartholzvariante Pflicht | Sumpfhanfvariante Pflicht |
| **Grauklüfte** | Hartholzvariante Pflicht | Granitvariante Pflicht |

- **[entschieden] Aktiver und erschöpfter Zustand sind je Variante zwei Bilder**
  (§24). Eine Variante mit nur einem Zustand ist unvollständig —
  `ResourceNodeDefinition.GetValidationErrors()` verlangt beide, und für eine
  Variante gilt dieselbe Anforderung.
- **[entschieden] Die Variante wird beim Aufbau der Zone gesetzt, nicht im
  Knoten-Prefab hinterlegt.** Es gibt weiterhin **ein** Knoten-Prefab je Knoten
  (M1.2) — vier Prefabs je Knoten wären vier Wahrheiten (§6).

### M11.5 Der Spieler betritt ein Gebiet am Rand

- **[entschieden] Jeder Spawnpunkt eines Außengebiets liegt am Kartenrand**,
  eingerückt um dasselbe Maß wie die vier Richtungsspawns (7 Einheiten von der
  Kartengröße, also ±33 bei Größe 80), und **blickt zur Kartenmitte**.
- **Stand heute:** `Spawn_Default` liegt auf (0 / 0,05 / −12) — in einer
  Spielfläche von 76 × 76 also nahezu in der Mitte. Der Spieler startet mitten im
  Knotenfeld.
- *Begründung:* Ein Gebiet ist zum Durchlaufen gebaut. M1.3 verteilt 72 Knoten
  über die **ganze** Fläche, und M1.7 sagt ausdrücklich, dass der Engpass der
  Rucksack ist — ein Ausflug ist damit eine Route und kein Radius. Wer in der
  Mitte startet, halbiert jeden Weg und macht alle vier Richtungen gleich lang;
  es gibt dann keinen Grund, irgendwohin zu gehen, und die Karte hat kein Innen.
  Vom Rand aus bekommt sie eine Tiefenachse, und der Rückweg wird eine
  Entscheidung.
- **Nebenwirkung, gewollt:** Der Weg zum Todesbeutel wird länger, weil der
  Wiedereintritt am Rand beginnt. Das verstärkt genau die Achse, die M4 als
  eigentliche Strafe benennt — den Rückweg unter Druck. **Zu beobachten**, nicht
  vorab zu dämpfen; der Beutel muss aus etwa 15 Einheiten auffindbar bleiben
  (M10.3).
- **[entschieden] `Spawn_Default` liegt auf der Südkante** und ist die Ankunft
  von der Weltkarte. Die vier Richtungsspawns behalten ihre Seiten.
  *Begründung:* Eine Seite muss gewählt werden, und es muss in **jedem** Gebiet
  dieselbe sein — sonst kann der Spieler nicht lernen, wo er hereinkommt. Welche
  Seite es ist, ist beliebig; dass es immer dieselbe ist, ist es nicht.
- **[entschieden] `Spawn_Default` ist gegen `Spawn_FromSouth` entlang der Kante
  versetzt**, mindestens 6 Einheiten.
  *Begründung:* Beide bedeuten „am Südrand ankommen und nach Norden gehen" und
  lägen sonst auf demselben Punkt. Fünf unterscheidbare Punkte sind im Szenenbild
  und in einer Testmeldung nachvollziehbar, ein doppelt belegter Punkt nicht.
- **Bestandsschutz, nicht neu zu bauen:** Der Generator hält Spawnpunkte bereits
  frei — `ZonePhysicsPlacementSpace.SpawnClearance` steht auf 5 Einheiten und
  fragt alle fünf Punkte ab. Das ist der Grund, aus dem ein Randspawn überhaupt
  zulässig ist: `ZoneController.IsSpawnValid` verwirft einen Spawn, sobald ihn
  irgendetwas außer dem Boden berührt, und **schaltet die Zone ab**. Wer die
  Einrückung ändert, prüft beides nach.
- **Achtung, die Marge ist 5 Einheiten:** Das Ausgangsvolumen einer Seite beginnt
  bei ±38 (`size · 0,5 − 2`). Ein Spawn bei ±33 hat davon 5 Einheiten Abstand.
  `ZoneController.ValidateSpawnOutsideExits` sichert das ab — es ist keine
  Rundungsreserve.
- **[entschieden] Die Heimatbasis ist ausgenommen**, aber nicht regellos: Ihr
  Spawn liegt **außerhalb der reservierten Baufläche** (M6).
  *Beleg, dass das heute nicht stimmt:* Der Basisspawn steht auf (0 / 0,05 / −4),
  und `ZonePhysicsPlacementSpace.HomeBuildAreaRadius` reserviert 12 Einheiten um
  die Mitte für den Basisbau. Der Spieler erscheint damit auf einem Feld, das er
  bebauen soll.

### M11.6 Was autoriert ist und was aus dem Seed kommt

- **[entschieden] Drei Schichten, und die Grenze ist scharf:**

  | Schicht | Woher | Ändert sich |
  | --- | --- | --- |
  | **Gebietsbild** — Boden, Deko, Licht | autoriert, im Szenen-Asset | nur, wenn jemand es ändert |
  | **Inhalt** — Ressourcenknoten, Gegner, Eidra und Weltkisten | aus dem Zonen-Seed zur Laufzeit (M1.2) | bei jeder Regeneration (M1.1) |
  | **Bauwerke** | aus dem Spielstand (M6) | wenn der Spieler baut |

  *Begründung:* Diese Grenze ist es, die §6 hält. Greenwood mischt heute Schicht 1
  und 2 — im Style-Proof-Bereich stehen autorierte Bäume dort, wo früher Knoten
  gebacken lagen. Wer Inhalt in die Szene backt, legt eine zweite Wahrheit neben
  den Seed, und `ZoneScenes_CarryNoBakedResourceNodes` ist der Test, der das
  bereits verbietet.
- **[entschieden] Deko sieht nie aus wie ein Knoten.** Sie darf nicht abbaubar
  wirken und trägt deshalb den Erkennungsmarker nicht (§24, M11.4). Ein
  Deko-Baum, der aussieht wie der Holzknoten, macht den Marker wertlos.
- **[entschieden] Deko wird nicht dorthin gesetzt, wo sie einen Knoten
  verdeckt.** Da die Knoten erst zur Laufzeit entstehen, ist das keine
  Platzierungsprüfung, sondern eine Dichtegrenze: Je mehr autorierte Objekte über
  1 SH in einer Zone stehen, desto häufiger steht ein Knoten dahinter (M10.3,
  Regel 2). **Die Zahl wird je Gebiet genannt und beobachtet**, nicht geraten.
- **[entschieden] Autorierte Deko lässt dem Knotenbudget seinen Platz.** Jeder
  Collider außer dem Boden verkleinert die belegbare Fläche —
  `ZonePhysicsPlacementSpace.IsPlaceable` verwirft jede Stelle, an der etwas
  anderes im Freiraum steht. Der bestehende Test
  `EveryOutdoorZone_ProvidesRoomForItsGeneratedNodes` ist die Absicherung; er
  wird nach jeder Deko-Ergänzung erneut ausgeführt.
- **[entschieden] Jedes Gebiet nennt mindestens ein hohes Silhouettenobjekt**,
  außer die Abwesenheit ist seine Identität — im Steinbruch sind es Felsrücken,
  keine Bäume.
  *Begründung:* Ohne ein Objekt über 2 SH ist die Skyline flach, und die relative
  Höhe als wichtige Tiefeninformation hat keinen Bezugspunkt. Verdeckung,
  Schatten, Normal-Map-Licht und Nebel ergänzen sie nach M10.1.

### M11.7 Bildbedarf je Gebiet

Der **Zielstil ist entschieden und wird hier nicht neu erfunden**: die malerisch
semirealistische Formsprache aus M10.8. Dieser Abschnitt beschreibt den
produktiven Gebietsbestand.

> **Nachtrag durch die Umstellung auf 3D** (M10.1, M10.9): Hier stand ein
> Verweis auf M10.7 — „gezeichnet im Kamerawinkel mit sichtbarem Bodenansatz".
> Das galt für Billboards und ist aufgehoben; Bilder sind ab jetzt **Texturen auf
> Geometrie** (M10.8). Bodenkacheln bleiben davon unberührt.
> **Die frühere Billboard-Stückliste ist abgeschlossen.** Weltobjekte verwenden
> Geometrie; fünf Gebietsbild-Assets und die sieben Pflichtvarianten sind
> angebunden. Die folgende Tabelle beschreibt den aktuellen
> Bestand; neue Varianten sind Qualitätsausbau, keine offene Migration.

| Gruppe | Vorhanden | Fehlt | Anmerkung |
| --- | --- | --- | --- |
| Gebietsbild-Assets | 5 | 0 | Greenwood, Nebelmoor, Steinbruch, Glutruinen, Heimatbasis |
| Weiche Übergangsmasken | gemeinsam nutzbar | 0 Pflichtmigration | technische Bausteine, kein Motivbestand (§24) |
| Baumvarianten Knoten | 4 × aktiv/erschöpft | 0 | Greenwood, Nebelmoor, Steinbruch, Glutruinen; Marker aus M11.4 |
| Leitmaterial-Varianten | 3 × aktiv/erschöpft | 0 | Faser, Stein und Kupfer; gemeinsame Grundformen bleiben vorhanden |
| Deko-Bestand | je Gebiet eigener Satz | 0 Pflichtmigration | weiterer Ausbau ist Gebietsproduktion, kein Platzhalterabbau |
| Stimmungsprofil | 5 | 0 | Licht, Nebel und Hintergrund je Gebietsbild |

**Zusätzlicher v0.2-Produktionsbedarf:**

| Gruppe | Zielzuwachs | Anmerkung |
| --- | ---: | --- |
| Gebietsbild-Assets | +3 | Dämmerhain, Schleiermoor, Grauklüfte |
| Weltkartenknoten und Icons | +3 | gesperrt/freigeschaltet über dasselbe T2-Flag |
| Hartholzvarianten | 3 × aktiv/erschöpft | in jedem T2-Gebiet Pflicht |
| Leitmaterialvarianten | 2 × aktiv/erschöpft zusätzlich | Sumpfhanf im Schleiermoor, Granit in Grauklüften |
| Stimmungs- und Audioprofile | +3 | je T2-Gebiet eindeutig |
| feste Dungeonszene | +1 | Eidra-Schmiede, außerhalb der Außengebietszählung |

- **[entschieden] Der produktive Greenwood-Baum ist die Referenz für alle
  weiteren Baumvarianten.** Die Gebietsvarianten übernehmen seine Proportion
  und seinen Marker.
  *Begründung:* Andernfalls entstehen vier Bäume in zwei verschiedenen Händen,
  und die gemeinsame Art Direction hätte keine überprüfbare Referenz.
- **[entschieden] Ein Deko-Satz je Gebiet ist mindestens: drei Felsen, drei
  Pflanzen, ein Bodendecker, ein Akzent.** Das ist die Struktur, die Greenwood
  bereits trägt, und sie ist die Untergrenze, nicht das Ziel.
- **Zur Menge:** Das sind über fünfzig Bilder. Wer das in einem Durchgang
  erzeugt, sollte die Reihenfolge nach Sichtbarkeit wählen: Boden zuerst, dann
  die Bäume, dann die Leitmaterialien, dann die Deko. Der Boden ist die einzige
  Fläche, die der Spieler **immer** sieht.

---

## M12 Mobiles HUD

M2 und M3 sagen, **welche Entscheidungen** der Spieler im Kampf trifft. Dieses
Kapitel sagt, wie diese Entscheidungen auf einem Landschaftsbildschirm
erreichbar bleiben, ohne die Welt mit Bedienflächen zuzudecken.

**Referenz, nicht Vorlage:** Der Vergleich mit Frostborn hat eine brauchbare
Ordnung gezeigt: ein dominantes Hauptziel, kleinere Aktionen im Daumenbogen,
Icons statt dauernder Beschriftung und Helligkeit als Zustandsanzeige. Eidren
übernimmt diese Prinzipien, **nicht** die konkrete Grafik, Farbe oder
Tastenbelegung. Insbesondere entsteht daraus kein automatischer Kampf.

**Stand, gegen den dies geschrieben ist:** `CombatHUD` erzeugt Canvas, Formen,
Texte und Bedienelemente zur Laufzeit in über 1300 Zeilen. Rechts unten stehen
Angriff, Ausweichen, eine Interaktionsfläche, zwei Fähigkeiten, zwei
Verbrauchsgüter und zwei Wechselpillen auf drei Ebenen. Zusätzlich erzeugt
`ExplorationInteractionPrompt` eine zweite Interaktionsfläche für denselben
`InteractionSnapshot`. Im laufenden Bild liegen beide übereinander.

### M12.1 Mobile zuerst, andere Eingaben bleiben vollständig

- **[entschieden] Das Spiel wird zuerst für Touch im Querformat gesetzt.**
  Bezug ist weiterhin die Canvas-Auflösung 1920 × 1080. Pflichtformate sind
  16:9, 20:9 mit seitlicher Safe Area und 16:10.
  *Begründung:* Mobile Versionen sind das spätere Ziel (§20); ein Desktop-HUD,
  das erst am Ende auf einen kleinen Bildschirm herunterskaliert wird, ist
  keine tragfähige Portierungsgrundlage.
- **[entschieden] Touch, Tastatur/Maus und Gamepad lösen dieselben Aktionen
  aus.** Das HUD darf die Darstellung nach der zuletzt benutzten Eingabefamilie
  ändern, niemals die Gameplay-Funktion. `PlayerInputReader` bleibt die eine
  Eingabequelle.
- **[entschieden] Der virtuelle Joystick ist nur im Touchmodus sichtbar.**
  Tastatur und Gamepad brauchen seine Rückmeldung nicht. Die Aktionssymbole
  bleiben dagegen sichtbar, weil sie Fähigkeiten, Abklingzeiten, Mengen und
  Wechselzustände anzeigen.
- **[entschieden] Eingabehinweise sind zustandsabhängig.** `[1]`, `[2]`, `[3]`
  oder ausgeschriebene Tastennamen erscheinen nur bei Tastatur/Maus; passende
  Glyphen nur beim Gamepad; im Touchmodus stehen dort keine Hardwarehinweise.
  Fest in den Buttontext geschriebene Tasten sind verboten.
- **[entschieden] Ein Wechsel der Eingabefamilie baut das HUD nicht neu.** Er
  schaltet vorbereitete Unterobjekte und Glyphen um. Kein `Destroy`/`Instantiate`
  als Folge eines Gerätewechsels (§3, §8).

### M12.2 Der rechte Daumenbogen

- **[entschieden] Alle Kampfaktionen bilden genau eine geordnete Gruppe rechts
  unten.** Kein zweiter Aktionsstreifen, keine Reihe aus Textpillen über der
  Gruppe. Die Gruppe liegt vollständig in der Safe Area und belegt bei
  1920 × 1080 höchstens **580 × 480 Referenzpixel**.
- **[entschieden] Die Gruppe verwendet genau drei Touchgrößen.** Angriff:
  144 × 144 Referenzpixel; reguläre Aktionen: 100 × 100; Waffen- und
  Eidra-Wechsel: 72 × 72. Die kleinste Größe bleibt damit fingerbedienbar,
  ohne Wechselaktionen mit Kampfaktionen gleichzugewichten.
- **[entschieden] Der Angriff ist die größte und tiefste Fläche.** Er zeigt das
  Symbol der aktiven Waffe; „ANGRIFF" steht nicht dauerhaft daneben. Richtwert:
  148 Referenzpixel Durchmesser.
  *Begründung:* M2 macht die Waffe zur ersten Hälfte des Kampfstils. Das aktive
  Werkzeug ist damit zugleich Inhalt und Bedeutung der Hauptaktion.
- **[entschieden] Dash ist die zweite motorische Aktion** und liegt rechts
  unterhalb des Angriffs. Links daneben liegt die eine kontextabhängige
  Kontrollfläche für Abbauen, Öffnen und Produktionsstätten. Beide zeigen
  Zustand und Sperre über Symbolik, nicht über Dauertext.
- **[entschieden] Exakt zwei Eidra-Fähigkeiten liegen oberhalb des
  Hauptbuttons** (M3). Beide sind gleich groß und gleich gewichtet; keine dritte
  Attrappe, kein leeres Feld. Richtwert: je 104 Referenzpixel.
- **[entschieden] Waffen- und Eidra-Wechsel bleiben im Kampf direkt
  erreichbar** (M2, M3), aber als sekundäre, an ihre Gruppe gebundene
  Wechselkontrollen. Sie sind keine großen Textpillen und werden nicht mit
  Angriff oder Fähigkeit verwechselt. Mindest-Touchfläche: 88 × 88
  Referenzpixel.
- **[entschieden] Trank und Bufffood sind zwei direkte Touchaktionen.** Sie
  stehen als symmetrisches Paar links neben dem Fähigkeitspaar und oberhalb des
  Waffenwechsels. Es gibt keinen vorgeschalteten Schnellverbrauchswechsel.
- **[entschieden] Waffen- und Eidra-Wechsel zeigen ihr inaktives Ziel.** Der
  Waffenwechsel zeigt die zweite Waffe, der Eidra-Wechsel das nicht aktive
  Eidra. Der Angriff zeigt weiterhin die aktive Waffe.
- **[entschieden] Kein Auto-Kampf in v0.1.** Der „Auto"-Schalter der Referenz
  wird weder grafisch noch mechanisch übernommen. M2 verlangt taktische
  Wechselentscheidungen; eine Automatik wäre eine neue Mechanik, kein
  Layoutdetail.

### M12.3 Eine Interaktion, eine Anzeige

- **[entschieden] Es gibt genau eine sichtbare Interaktionsfläche.** Sie liest
  den einen `InteractionSnapshot` und sendet über den bestehenden
  `InteractionButton` an `PlayerInputReader`. `CombatHUD` und
  `ExplorationInteractionPrompt` dürfen denselben Zustand nicht parallel
  präsentieren (§6).
- **[entschieden] Die Fläche ist kontextuell.** Ohne Ziel ist sie unsichtbar
  und belegt weder Raum noch Raycast. Mit Ziel erscheint sie am inneren Rand des
  Daumenbogens, nicht über Angriff oder Ausweichen.
- **[entschieden] Zeitfortschritt wird als Ring gezeigt.** Der Ring übernimmt
  `normalizedProgress`; Prozenttext ist nicht nötig. Ressourcenabbau läuft nach
  einmaligem Auslösen automatisch weiter und wird durch Loslassen nicht
  abgebrochen. Fangen bleibt eine Halte-Interaktion und endet beim Loslassen.
  Schaden, Angriff, Ausweichen, Tod, Deaktivierung und Szenenwechsel brechen
  beide Varianten über das bestehende Interaktionssystem ab (M8.3, §3).
- **[entschieden] Die Interaktionsfläche darf ein kurzes Verb tragen**, wenn das
  Symbol allein das Ziel nicht erklärt — „ABBAUEN", „FANGEN", „ÖFFNEN".
  Blockiergründe erscheinen als eine kurze Meldung nahe der Fläche, nicht
  dauerhaft zweizeilig im Button. Die Gameplay-Entscheidung, ob interagiert
  werden darf, bleibt in `InteractionController` (§18).

### M12.4 Zustände statt Beschriftungen

- **[entschieden] Dauerhafte Aktionen sind icon-first.** Namen wie „ANGRIFF",
  „WAFFE", „EIDRA", „SKILL 1", „DASH AUSWEICHEN", „BUFFFOOD" und „NICHT
  VERFÜGBAR" sind keine Normalbeschriftung des mobilen HUDs.
- **[entschieden] Jeder Zustand hat mindestens zwei Signale.** Farbe allein
  genügt nicht:

  | Zustand | Pflichtsignale |
  | --- | --- |
  | bereit | volles Symbol + klare Kontur |
  | Abklingzeit | radialer Keil + verbleibende Zeit ab 1 s |
  | gesperrt | verringerte Deckkraft + Schloss/Sperrmarke |
  | Menge null | entsättigt + Mengenbadge `0` |
  | Halteaktion | Fortschrittsring + gedrückter Zustand |
  | fehlende Ressource | kurze Meldung + Rückstoß/Flash der Fläche |

- **[entschieden] Farbe bezeichnet Bereitschaft und Gefahr, nicht
  Buttonkategorien.** Vollflächiges Korall, Jade, Violett, Blau und Gold auf
  benachbarten Buttons macht jedes Element gleich laut. Eidren benutzt dunkles
  Obsidian/Blaugrün als Grundfläche, Creme für Konturen, Gold für Fokus und
  Korall ausschließlich für Schaden/Gefahr. Eidra-Farbe darf als kleiner Akzent
  erscheinen.
- **[entschieden] Die Welt bleibt sichtbar.** Sekundäre Flächen sind
  transparent oder nur gerahmt. Ein inaktives Element tritt zurück, statt als
  dunkler Vollkreis dieselbe Fläche zu beanspruchen.

### M12.5 Der Rest des HUDs unterstützt den Blick

- **[entschieden] Der Spielerstatus links oben ist kompakt.** Pflicht sind
  Portrait, Lebenspunkte, Ausdauer und Resonanz. Name, Klasse, Stufe und
  dauerhaftes „noch kein Eidra" gehören nicht in den Kampfblick.
- **[entschieden] Bossstatus und Ziel teilen den oberen Mittelpunkt.** Im
  Bosskampf ersetzt der Bossstatus das Ziel; beide stehen nie übereinander.
- **[entschieden] Ressourcenzuwachs ist Rückmeldung, kein permanenter
  Zähler.** `InventoryFeedbackPresenter` zeigt den Fund kurz. Ein dauerhafter
  „ERZ 0"-Chip entfällt aus dem Kampf-HUD.
- **[entschieden] Menüaktionen sind Symbole am Rand.** Rucksack und Pause
  erscheinen nicht als große Textrechtecke über der Welt.
- **[entschieden] Modale Fenster dimmen oder sperren das Kampf-HUD über einen
  Zustand**, sie erzeugen keine zweite Variante. Während Ergebnis-, Todes-,
  Inventar-, Crafting- oder Pausenfenster Eingabe besitzt, empfängt der
  Daumenbogen keine Raycasts.

### M12.6 Autorierung und Verantwortungen

- **[entschieden] Das HUD ist ein Prefab unter `Assets/_Game/Resources/UI/`.**
  RectTransforms, Bilder, Touchflächen und Texte sind serialisiert (§16). Eine
  einmalige Editor-Migration darf es anlegen und wird danach gelöscht (§6);
  das Prefab ist anschließend die einzige Wahrheit.
- **[entschieden] Das Prefab wird in jedem spielbaren Pfad auf dieselbe Weise
  instanziiert und gebunden.** `ZonePlayerSpawner` und Sandbox dürfen keine
  eigenen Canvasbäume erzeugen. Beide gehen über `PlayerPrefabBindings` (§21).
- **[entschieden] Der HUD-Code ist Presenter, nicht Zeichenprogramm.** Er bindet
  Ereignisse, übersetzt Werte in Viewzustände und schreibt serialisierte
  Referenzen. `BuildCanvas`, `BuildActions`, `CreateShapeSprite`, allgemeine
  `Label`-/`Place`-Fabriken und die Laufzeitladung einer Built-in-Schrift
  verschwinden aus dem Laufzeitpfad.
- **[entschieden] Der bisherige `CombatHUD` wird geteilt.** Kein neuer
  MonoBehaviour überschreitet 400 Zeilen, keine neue Ausnahme kommt in
  `ArchitectureGuardTests`, und `UI/CombatHUD.cs` verlässt die
  Bestandsschutzliste (§7). Sinnvolle Grenzen sind Status, Aktionen,
  Interaktion, Eingabedarstellung und Ergebnis — nicht eine Klasse je Image.
- **[entschieden] UI entscheidet nichts über Kampf oder Inventar.** Ob eine
  Fähigkeit bereit ist, ein Gegenstand vorhanden ist oder eine Interaktion
  möglich ist, kommt weiterhin aus den vorhandenen Gameplay-Systemen (§18).

### M12.7 Abnahme

Das HUD ist nicht mit einem einzelnen 1920×1080-Bild abgenommen.

- **Pflichtaufnahmen:** 16:9, 20:9 mit seitlicher Safe Area und 16:10; jeweils
  Touchmodus. Zusätzlich eine Tastatur/Maus-Aufnahme, in der der Joystick
  unsichtbar und die korrekten Hinweise sichtbar sind.
- **Pflichtzustände:** freie Erkundung, Halte-Interaktion bei 50 %, Kampf mit
  zwei verfügbaren Eidra-Fähigkeiten, eine Fähigkeit in Abklingzeit,
  Verbrauchsmenge null und Bosskampf.
- **Kein Überlappen:** Interaktion, Angriff, Dash, Fähigkeiten,
  Wechselkontrollen, Trank und Food liegen in keinem Pflichtformat übereinander
  oder außerhalb der Safe Area.
- **Ein Pfad:** Jede der fünf Szenen und die Sandbox instanziiert dasselbe
  Prefab; es existiert genau eine aktive Interaktionsfläche.
- **Ein Budget:** Nach dem Umbau ist `CombatHUD.cs` unter 400 Zeilen und aus der
  Bestandsschutzliste entfernt. Kein Layout wird im ersten Frame
  zusammengebaut.
- **Geräteprobe [spätere Portierungsphase]:** Diese Regeln ersetzen keine
  Messung auf einem echten mobilen Zielgerät. In der ersten Mobile-Portierung
  werden Touchgröße, Daumenbogen und der vorläufige Einzelplatz für
  Verbrauchsgüter überprüft.

---

## M13 Verbindlicher Umfang von Version 0.2

M13 ist die aktuelle Inhalts- und Balancingkante für v0.2. Die allgemeinen
Verträge bleiben in M1 bis M12; die folgenden Tabellen legen fest, welche
konkreten Inhalte und Startwerte v0.2 verwendet. M8 und M9 bleiben historische
v0.1-Dokumentation und überschreiben M13 nicht.

### M13.1 Basisbau

- 1-Meter-Raster mit stabiler Herkunft
- vier Belegungsschichten: Boden, kanonische Kante, Objekt, vorbereitete Deko
- Bodenrechtecke, Wandlinien und atomare Türersetzung
- Untergrundregeln `RequiresFloor`, `GroundOnly`, `GroundOrFloor`
- abgeleitete Wandverbindungen und geschlossene Räume
- rein visuelle, abgeleitete und ausblendbare Dächer ohne Kosten
- Save-Migration alter Gebäude, Böden, Wände und Türen auf Zellen/Kanten

Die mechanische Wahrheit steht in M6.1. Türschlösser, Dachkosten, frei
platzierbare Dachteile, Dekorationsinhalte und komplexe Türanimationen gehören
nicht zu v0.2.

### M13.2 Waffen, Signaturen und feste Varianten

Es gibt nur den normalen Angriff. Hammer, Dolche und Speer besitzen genau eine
Signatur; keine Waffe besitzt eine aktive Fähigkeit oder eigene Ressource.

| Gattung | Kombo und Signatur |
| --- | --- |
| Hammer | Dreierkombo; Wucht: hoher Taumelschaden, dritter Treffer `1,8` Taumelmultiplikator |
| Dolche | schnelle Viererkombo; Hinterhalt: Rückenangriffe `1,75` Schaden |
| Speer | dreiteilige lineare Stoßkombo; 4,2 m, etwa 35°, äußeres Reichweitendrittel `1,25` Schaden; Treffer `0,95 / 1,05 / 1,30` |

Komboabbruch erfolgt bei Waffenwechsel, Ausweichen, zu langer Angriffspause
oder tatsächlich unterbrechendem Gegnertreffer. Normale Bewegung im
Kombofenster bricht nicht ab.

| Waffe | Variante | Grundschaden | normaler Taumelschaden | Haltbarkeit |
| --- | --- | ---: | ---: | ---: |
| Hammer | T0 | 15,0 | 10,0 | 120 |
| Hammer | T1 | 18,0 | 12,0 | 150 |
| Hammer | T2 | 21,8 | 14,5 | 180 |
| Siegelbrecher | benannt T1 | 23,5 | 15,7 | 200 |
| Dolche | T0 | 12,0 | 2,5 | 220 |
| Dolche | T1 | 14,4 | 3,0 | 275 |
| Dolche | T2 | 17,4 | 3,6 | 330 |
| Aschenzähne | benannt T1 | 18,8 | 3,9 | 365 |
| Speer | T1 | 17,0 | 6,0 | 190 |
| Speer | T2 | 20,5 | 7,2 | 230 |
| Glutdorn | benannt T1 | 22,1 | 7,8 | 255 |

Reguläre Tiers derselben Gattung teilen in v0.2 ihre Darstellung. Nur die drei
benannten Waffen besitzen eigene Silhouette, Textur und dezente Glutdetails.
Sie erhalten keinen Feuerschaden, keine neue Signatur und keinen
Duplikatschutz. Benannte T1-Waffen sind stärker als reguläres T2, bleiben reine
Funde und können weder hergestellt noch repariert werden.

### M13.3 Rüstung, Haltbarkeit, Werkzeuge und Crafting

| Tier | Familie | Brust | Beine | Kopf | Handschuhe | Gesamt | Haltbarkeit je Teil |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| T0 | Stoff | 4 % | 3 % | 2 % | 1 % | 10 % | 80 |
| T1 | Kupfer | 8 % | 6 % | 4 % | 2 % | 20 % | 120 |
| T2 | Eisen | 12 % | 9 % | 6 % | 3 % | 30 % | 170 |

Schutz addiert sich direkt und ist zunächst bei 60 Prozent gekappt. Es gibt
keine Schadensarten, Setboni, benannte Rüstung oder Qualitätsstufen. Jedes
getragene Teil verschleißt gleichmäßig um einen Punkt pro tatsächlich
erlittenem Schadensereignis.

**Rüstung gibt zusätzlich Lebenspunkte** (seit v0.3.1/F31-018): je Teil so
viele Lebenspunkte, wie es Prozent Schutz beiträgt — der Wert ist Schutz × 100,
es gibt keine zweite Zahlenreihe. Vollständiges T0/T1/T2 gibt damit
+10/+20/+30 auf die Basis von 100. Der Bonus hebt nur das Maximum: Anlegen
heilt nicht; sinkt das Maximum unter den aktuellen Wert (Ablegen, Bruch), wird
der aktuelle Wert auf das Maximum geklemmt. Die 60-Prozent-Kappe gilt der
Schadensreduktion, nicht dem Lebensbonus.

Werkzeugertrag und -verschleiß folgen M1.9. Maximalhaltbarkeit T0/T1/T2 ist
100/120/140. Werkzeuge liegen weiter im Rucksack. T1- und T2-Versionen sind
eigene Gegenstände, keine Upgradezustände.

**T2-Veredelung in bestehenden Stationen:**

| Station | Ergebnis | Kosten |
| --- | --- | --- |
| Sägewerk | 1 Hartholzbrett | 2 Hartholz |
| Steinmetz | 1 behauener Granit | 2 Granit |
| Seilerei | 1 robustes Tuch | 2 Sumpfhanf + 1 Seil |
| Schmelzofen | 1 Eisenbarren | 3 Eisenerz |

Ein gemeinsamer Technologieknoten schaltet alle vier Rezepte frei. Es gibt
keine neue Schmiede in der Heimatbasis, keine Stationsaufwertung und kein neues
Stationsmodell.

**T1-Werkbankrezepte:**

| Gegenstand | Kosten |
| --- | --- |
| Kupferhammer | 3 Kupferbarren, 2 Bretter, 1 Steinblock, 2 Seile |
| Kupferdolche | 3 Kupferbarren, 1 Brett, 2 Seile |
| Kupferspeer | 3 Kupferbarren, 2 Bretter, 2 Seile |
| Kupferhelm | 1 Kupferbarren, 1 Seil |
| Kupferharnisch | 3 Kupferbarren, 3 Seile |
| Kupferhandschuhe | 1 Kupferbarren, 1 Seil |
| Kupferbeinschutz | 3 Kupferbarren, 2 Seile |
| Kupferaxt | 2 Kupferbarren, 2 Bretter, 1 Seil |
| Kupferspitzhacke | 2 Kupferbarren, 2 Bretter, 1 Steinblock |
| Kupfersense | 2 Kupferbarren, 1 Brett, 2 Seile |

**T2-Werkbankrezepte:**

| Gegenstand | Kosten |
| --- | --- |
| Eisenhammer | 4 Eisenbarren, 2 Hartholzbretter, 2 behauener Granit, 2 Seile, 2 Schmiedebeschläge |
| Eisendolche | 4 Eisenbarren, 1 Hartholzbrett, 2 robustes Tuch, 2 Schmiedebeschläge |
| Eisenspeer | 3 Eisenbarren, 3 Hartholzbretter, 2 robustes Tuch, 2 Seile, 2 Schmiedebeschläge |
| Eisenhelm | 2 Eisenbarren, 1 robustes Tuch, 1 Schmiedebeschlag |
| Eisenharnisch | 5 Eisenbarren, 3 robustes Tuch, 2 Schmiedebeschläge |
| Eisenhandschuhe | 2 Eisenbarren, 1 robustes Tuch, 1 Schmiedebeschlag |
| Eisenbeinschutz | 4 Eisenbarren, 2 robustes Tuch, 2 Schmiedebeschläge |
| Eisenaxt | 3 Eisenbarren, 2 Hartholzbretter, 1 robustes Tuch, 1 Seil, 1 Schmiedebeschlag |
| Eisenspitzhacke | 3 Eisenbarren, 2 Hartholzbretter, 2 behauener Granit, 1 Seil, 1 Schmiedebeschlag |
| Eisensense | 3 Eisenbarren, 2 Hartholzbretter, 1 robustes Tuch, 1 Seil, 1 Schmiedebeschlag |

Jedes Rezept erzeugt genau ein reguläres Exemplar mit voller Haltbarkeit und
festen Werten. Maximal fünf Zutatenarten sind zulässig. Schmiedebeschlag ist die
einzige neue seltene Komponente und in v0.2 nicht herstellbar. Kosten je
T2-Gegenstand: Werkzeug 1, Waffe 2, Helm 1, Handschuhe 1, Harnisch 2,
Beinschutz 2.

### M13.4 Technologie, Baupläne und Erfahrung

Alle Technologien außer den drei anfänglichen T0-Werkzeugen müssen gekauft
werden. Ein Bauplan macht seinen Technologieknoten kaufbar; er lernt das Rezept
nicht direkt.

- Nach Garons erstem Sieg: fünf Knoten — T1-Werkzeuge, Kupferhammer,
  Kupferdolche, Kupferspeer und Kupferrüstung. Garon gibt einmalig einen Punkt;
  Level 21 bis 24 je einen weiteren.
- Garon trägt einmalig den Speerbauplan. Bei Aufnahme wird er persistentes
  Bauplanwissen; bei vollem Inventar bleibt er in Garons persistentem
  Lootbehälter.
- Nach dem ersten Abschluss der Eidra-Schmiede: sechs Knoten —
  T2-Verarbeitung, T2-Werkzeuge, Eisenhammer, Eisendolche, Eisenspeer und
  Eisenrüstung. Der Abschluss gibt einen Punkt; Level 25 bis 29 je einen.
- Level 30 bis 40 geben in v0.2 keine Technologiepunkte. Alle genannten Knoten
  kosten einen Punkt. Gesperrte Knoten bleiben sichtbar und nennen ihre
  Voraussetzung.

Die Levelanforderung lautet:

`EP zum nächsten Level = auf 25 gerundet (125 + 55 × aktuelles Level)`

Stufe 1 endet bei Level 24, Stufe 2 bei Level 40. Am Stufenmaximum wird höchstens
eine weitere Levelanforderung als sichtbarer EP-Vorrat gespeichert; zusätzlicher
Überschuss verfällt.

| Quelle | T0 | T1 | T2 |
| --- | ---: | ---: | ---: |
| Ressourcenknoten abgeschlossen | 8 | 12 | 18 |
| Rezept zuerst / wiederholt | 25 / 1 | 50 / 2 | 90 / 4 |
| Gebäude zuerst / wiederholt | 40 / 1 | 75 / 2 | 125 / 4 |

Sammel-EP hängen nicht am Materialertrag. Kisten, Marken, Baupläne und reine
Abschlussaktionen geben keine EP. Wildling gibt 15, Garon 350 und Garons erster
Sieg zusätzlich 500 EP. Weitere Gegnerwerte stehen in M13.5 und M13.6.

**Tutorial-Questkette (v0.3.1, F31-006).** Eine geführte Kette aus 17
Schritten begleitet den Einstieg bis zum ersten Eidra-Fang und vergibt
zusammen **940 EP** — zusätzlich zu den regulären Erst-EP der Stationen.
Die Schritte docken ausschließlich an bestehende Ereignisse an (Knoten
abgeschlossen, Rezept gefertigt, Gebäude gebaut, Gegner besiegt, Eidra
gefangen); es gibt keinen Sammelzähler. Quelle der Kette ist
`QuestChain_V01` (Builder `QuestChainContentBuilder`), die EP je Schritt:

| # | Schritt | EP |
| --- | --- | ---: |
| 1 | Ernte 2 Faserpflanzen | 30 |
| 2 | Baue eine Werkbank | 40 |
| 3 | Fertige die Axt | 40 |
| 4 | Fälle 2 Bäume | 30 |
| 5 | Baue eine Lagerkiste | 40 |
| 6 | Fertige den Hammer | 40 |
| 7 | Baue 2 Steinvorkommen ab | 40 |
| 8 | Fertige die Spitzhacke | 50 |
| 9 | Fertige die Sense | 50 |
| 10 | Besiege 2 Wildlinge | 60 |
| 11 | Baue 2 Kupferadern ab | 50 |
| 12 | Baue den Schmelzofen | 60 |
| 13 | Fertige einen Kupferbarren | 60 |
| 14 | Baue das Sägewerk | 60 |
| 15 | Baue die Seilerei | 60 |
| 16 | Fertige das Fanggerät | 80 |
| 17 | Fange deinen ersten Eidra | 150 |

Der Queststand liegt versioniert im Spielstand (Version 15). Altbestände
leiten ihn beim Laden aus den Erstlisten ab: erledigte Rezept- und
Gebäudeschritte werden ohne EP übersprungen, ein vorhandener Eidra schließt
die Kette ab; EP fließen nur für echte, neue Abschlüsse.

### M13.5 Weltkisten und T2-Gebiete

Jede neu regenerierte Weltzone wählt aus autorierten erreichbaren Punkten
seedstabil 2–4 gewöhnliche, 0–1 bewachte und 0–1 versteckte Kisten. Inhalt und
Entnahmezustand bleiben bis zur Zonenregeneration stabil.

| Kistentyp | garantierter Inhalt | Bonusveredelung | reguläre Ausrüstung |
| --- | --- | --- | ---: |
| gewöhnlich | 1–3 regionale Rohmaterialien, 1 kleines Verbrauchsgut | – | 1 % |
| bewacht | 2–4 regionale Rohmaterialien, 1 nützliches Verbrauchsgut | 20 % auf 1–2 | 5 % |
| versteckt | 2–4 regionale Rohmaterialien, 1 wertvolles Verbrauchsgut | 50 % auf 1–2 | 10 % |

Gewöhnliche Ausrüstung besitzt 40–80 Prozent Haltbarkeit, bewachte und
versteckte Funde volle Haltbarkeit. Vor Garon wählen Bestandsgebiete nur T0;
danach 50 Prozent T0 und 50 Prozent T1. T2-Gebiete wählen bei erfolgreichem
Ausrüstungswurf 40 Prozent T2, 35 Prozent T1 und 25 Prozent T0. Versteckte
T2-Kisten würfeln unabhängig 10 Prozent auf einen Schmiedebeschlag und nach dem
Schmiedeabschluss 1 Prozent auf eine zufällige benannte Waffe. Es gibt keinen
Duplikatschutz.

Dämmerhain, Schleiermoor und Grauklüfte werden gemeinsam durch das T2-Flag
freigeschaltet. Ihre 72-Knoten-Budgets stehen in M1.7. Sie regenerieren wie die
Bestandsgebiete, kosten keinen Eintritt und besitzen in v0.2 keine
Umweltgefahren.

| Gegner | TP | Taumel | Schaden | Schutz | EP |
| --- | ---: | ---: | ---: | ---: | ---: |
| Rissling | 220 | 80 | 20 | 0 % | 25 |
| Wurzelstürmer | 380 | 140 | 32 | 0 % | 45 |
| Moorwerfer | 200 | 70 | 24 | 0 % | 30 |
| Granitpanzer | 450 | 180 | 30 | 20 % | 65 |
| Risswächter | 850 | 260 | 30 normal / 40 Fläche | 15 % | 180 |

Normale Gruppen enthalten zwei bis drei Gegner; gleichzeitig kämpfen maximal
vier normale oder ein Elitegegner mit Begleitern. Eine bewachte Kiste verwendet
einen Risswächter mit zwei normalen Begleitern. Es gibt kein Levelscaling,
Mindestlevel oder Ausrüstungsprüfung.

### M13.6 Eidra-Schmiede, Kernwächter und Ignivar

Die Eidra-Schmiede ist eine feste, handgefertigte und persistente Szene unter
den Glutruinen. Der erste Zugang nach Garon ist kostenlos. Ein laufender
Durchlauf kann verlassen und später fortgesetzt, aber nicht manuell
zurückgesetzt werden. Nach dem Kernwächterabschluss beginnen 24 reale Stunden.
Danach kostet der atomare Reset aus dem Spielerinventar:

| Ressource | Menge |
| --- | ---: |
| Bretter | 10 |
| Steinblöcke | 8 |
| Kupferbarren | 5 |

Der Reset erzeugt einen neuen Laufseed, Gegner, Boss, sieben Beutekisten und
Markendrops neu. Einmalige Baupläne, Ignivar-Fang und T2-Flag bleiben erhalten.
Nicht aufgesammelte Drops werden gelöscht; ein Todesrucksack wird vorher in den
Bergungscontainer am Eingang verschoben.

| Gegner | Anzahl | TP | Taumel | Schaden | Schutz | EP | Marken je Gegner |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Glutzehrer | 12 | 95 | 40 | 12 | 0 % | 8 | 1 |
| Aschenläufer | 6 | 180 | 70 | 22 | 0 % | 20 | 2 |
| Schmiedewächter | 4 | 360 | 170 | 30 | 20 % | 45 | 2 |
| Siegelwächter, optional | 1 | 750 | 240 | 30 / 38 Fläche | 15 % | 140 | 5 |
| Kernwächter | 1 | 3.000 | 400 | siehe unten | 35 % geschlossen / 0 % offen | 500 | 8 |

Vollständiges Leeren ergibt 45, ohne Siegelwächter 40 Schmiedemarken.

Der Kernwächter verursacht 34 Schmiedeschlag, 40 Bodenschlag, 28 Glutgeschoss
und 25 durch überhitzte Bodenflächen. Mit geschlossenem Kern führt er drei
normale Angriffe, dann Überhitzung und anschließend 8 Sekunden offenen Kern
aus. Volles Taumeln öffnet vorzeitig für 6 Sekunden; während offenem Kern wird
kein Taumel gespeichert. Unter 50 Prozent TP werden Angriffslücken ungefähr 10
Prozent kürzer und zwei Bodenflächen entstehen gleichzeitig; Telegraphen,
Schaden, Schutz und Öffnungsfenster bleiben gleich.

**Ignivar:** genau ein Exemplar in v0.2, 240 TP, 100 Taumel, 20 Schaden,
Fangbedarf 170 bei vollen TP und mindestens 70. Eine T1-Batterie liefert 100;
die Batterie wird nur bei Erfolg verbraucht. Flucht und erneutes Betreten der
Bindungskammer setzen die Begegnung zurück. Ignivar gibt keine EP oder Marken.

- **Glutkreis:** Ziel bis 8 m, Radius 2,75 m, 5 Sekunden, 6 Schaden je Sekunde,
  maximal 30 je Gegner, 10 Sekunden Cooldown, kein Taumel oder Eigenschaden.
- **Schmelzbrand:** 10 m, 6 Sekunden, 12 Sekunden Cooldown, minus 15
  Schutz-Prozentpunkte bis mindestens 0, kein Taumel. Beim geschlossenen
  Kernwächter sinkt Schutz von 35 auf 20 Prozent, ohne den Kern zu öffnen.

Pro Lauf existieren 3 Versorgungskisten, 2 optionale Schatzkisten, 1
Elitekiste und 1 Abschlusskiste. Versorgung liefert 1–2 Verbrauchsgüter,
1–3 gewöhnliche Materialien und zu 1 Prozent abgenutztes reguläres T1.
Optionale Kisten liefern 1–2 T1-Veredelungen, 1 Verbrauchsgut, zu 5 Prozent
volles T1 und zu 10 Prozent einen Beschlag. Elite liefert 2–3 Veredelungen, 2
Verbrauchsgüter, zu 10 Prozent volles T1 und zu 25 Prozent einen Beschlag.

Die erste Abschlusskiste enthält 2 Beschläge, 1 volles reguläres T1, 2–3
hochwertige Verbrauchsgüter und zu 25 Prozent eine benannte Waffe. Wiederholung
enthält 2 Beschläge, 1 volles reguläres T1, 1–2 hochwertige Verbrauchsgüter und
zu 15 Prozent eine benannte Waffe. Keine Gewölbekiste enthält T2.

| Markenkiste | Preis | garantierter Inhalt | zusätzliche Chancen |
| --- | ---: | --- | --- |
| klein | 15 | 1 Beschlag, 1–2 hochwertige Verbrauchsgüter | 5 % volles reguläres T1 |
| mittel | 30 | 2 Beschläge, 1 volles reguläres T1, 1–2 Verbrauchsgüter | 10 % zweites T1 |
| groß | 90 | 5 Beschläge, 1 volles reguläres T1, 3–4 Verbrauchsgüter | 25 % zweites T1; 25 % benannte Waffe |

Alle Markenkisten liefern volle Haltbarkeit, kein T2 und keinen
Duplikatschutz. Bezahlter Restinhalt bleibt persistent und blockiert erneute
Zahlung bis zur Leerung.

Ein kompletter erster Lauf einschließlich Siegelwächter gibt 1.936 EP, eine
Wiederholung 1.036 EP. Darin enthalten sind 500 reguläre EP für den
Kernwächter und beim ersten Sieg einmalig weitere 900 EP als Boss-Siegbonus.
Der erste Kernwächtersieg setzt unabhängig von der Abschlusskiste das T2-Flag
und gibt einen Technologiepunkt. Zielzeiten sind 35–45 Minuten im ersten und
20–30 Minuten im wiederholten Durchlauf.

### M13.7 Assetproduktion und v0.2-Grenzen

Die Assetproduktion beginnt mit einem versionierten Register und technischen
Musterassets, läuft parallel zu den Systemen und endet erst nach produktiver
Integration. Mindestumfang als Inhaltsfamilien:

- neun Waffen-Gegenstandsdefinitionen, aber nur vier neue
  Waffen-Primärdarstellungen: regulärer Speer und drei benannte Waffen;
- sechs neue Werkzeuge, acht neue Rüstungsteile, neun Materialicons und ein
  Speerbauplanicon — mindestens 28 neue Paket-A-Primärdarstellungen/Icons;
- elf neue achtgerichtete Akteure;
- vier T2-Ressourcenprefabs mit vollständigen Gebietsvarianten;
- elf Containerfamilien;
- drei vollständige T2-Außengebiete und eine feste Gewölbeszene;
- zwei Ignivar-Fähigkeitsicons sowie sämtliche erforderlichen UI-, VFX- und
  Audioereignisse.

Für jeden neuen Akteur gelten M10.10, für Größen M10.3, für Gebietsvarianten
M11 und für visuelle Eindeutigkeit §24 einschließlich der engen
Waffentierausnahme. Kein System gilt mit einem Testdouble als abgeschlossen.

Nicht Bestandteil von v0.2 sind Reparatur, Zerlegen, Tier-Aufwertung eines
Exemplars, benannte Waffenbaupläne, benannte/Setrüstung, T3, schwere
Gewölbemodi, prozedurale Gewölberäume, weitere Ignivar-Exemplare,
Umweltgefahren der T2-Gebiete oder verwertbare Gegnerleichen.

Jeder Teilauftrag erhöht bei Save-Änderungen `CurrentSaveVersion` und bringt
eine lückenlose Migration. Die Kette bewahrt stabile IDs, Inventar- und
Containerpositionen, Ausrüstung, Garonstatus und vorhandene Technologie. Alte
Waffenupgradefelder werden ohne Bonus entfernt; bestehende Items starten mit
voller Haltbarkeit; neue Zonen und das Gewölbe beginnen gesperrt beziehungsweise
ungestartet gemäß ihren Fortschrittsflags.

---

## M14 Verbindlicher Umfang von Version 0.3

M14 ist die aktuelle Inhaltskante. M13 bleibt die v0.2-Dokumentation und wird
davon nicht überschrieben; wo M14 etwas neu fasst, steht es hier ausdrücklich.

Der Schnitt ist der 15.08.2026 — der Stand, der als v0.2-Testpaket
veröffentlicht wurde.

### M14.1 Neubau der verlassenen Eidra-Schmiede

**[entschieden] Der Grundriss ist ein Schmiedewerk.** Der Spieler läuft von
Süden nach Norden den Weg, den die Luft im Betrieb nahm: vom Blasebalg durch
die Düse in die Gießhalle und von dort in die Esse. Gesamtausdehnung 88 × 72
statt bisher 58 × 26.

Der alte Aufbau — sechs Rechtecke auf einer Linie bei X=0, davon drei
identische Kampfhallen — ist ersetzt. Er hatte weder Verzweigung noch
Entscheidung, und beim Nachrechnen der Builder-Koordinaten kamen vier Fehler
zutage: beide Seitenwege lagen hinter durchgehenden Wänden ohne Öffnung, eine
optionale Truhe stand auf einer Stelle ohne Boden, die ersten sechs AshRunner
standen auf denselben Koordinaten wie die ersten sechs EmberEater, und der
18 breite Arenaboden lief durch Wände bei ±7,2.

**[entschieden] Verfall ist im Grundriss sichtbar.** Das Werk war symmetrisch
gebaut; die östliche Hälfte der Gießhalle ist eingestürzt. Die intakte
Westseite zeigt, wie es gemeint war. Die Asymmetrie ist Verfall, nicht
Willkür.

**[entschieden] Drei Wege von der Gießhalle auf die Galerie**, keiner Pflicht,
keiner Sackgasse: die Rinne (schnell, keine Truhe, beide ForgeGuardian im
Weg), die Masselreihe (geordnet, zwei Vorratstruhen), der Einbruch
(unübersichtlich, dichteste Gruppe, der Bauplatz). Jeder optionale Bereich
zahlt in einer eigenen Währung: der Einbruch in Dauerhaftigkeit, das
Hammerwerk in Beute, die Bindungskammer in einem Eidra. Wer geradeaus zum Boss
läuft, verpasst Ignivar und merkt es nie.

**[entschieden] Keine Decken.** Bei fester Draufsicht verdeckt eine Decke
genau das, was man sehen soll. Die Enge kommt aus Wandhöhe 5,5 (statt 3,2) mit
nach innen geneigter, gebrochener Krone, aus Stirnwänden an Nord- und Südseite
jedes Raums, aus acht bis zehn Einheiten langen Schrägschatten des flachen
Richtungslichts — und aus Balken auf Höhe 4,5 als einzigem Element über dem
Kopf.

**Platzierungsregel aus dem Kamerawinkel:** hohe Objekte an Nord- und
Ostwände, dort sind sie Kulisse; niedrige nach Südwesten, dort verdecken hohe
Objekte die Spielfigur.

**[entschieden] Die Balancewerte bleiben unangetastet.** 24 Gegner, sieben
Laufkisten in der Verteilung 3 Supply / 2 Optional / 1 Elite / 1 Completion,
45 Marken mit Siegelwächter und 40 ohne, 1.036 EP je Wiederholungslauf. Es ist
eine **Umverteilung, keine Aufstockung**; `EidraForgePopulationRules`,
`EidraForgeBalance` und `EidraForgeContainerRules` werden nicht angefasst.

**[vorläufig] Licht.** Umgebungslicht flach 0,10 / 0,105 / 0,13,
Richtungslicht Intensität 0,8 aus Westsüdwest bei rund 30°, Warmlicht der Esse
als Punkt mit Reichweite 18 und Intensität 7. Höchstens drei Lichtfarben im
Bild; **Blau kommt genau einmal vor**, durch die Bresche im Einbruch. Der
Feinabgleich der Intensitäten steht noch aus — das Verlies ist für flüssiges
Spielen weiterhin zu dunkel. Ursachen und Wirkungskette sind geklärt (§27).

**Renderergrenze:** höchstens vier Zusatzlichter pro Objekt, und die Auswahl
fällt pro Objekt. Deshalb sind die Böden in Kacheln von 4 × 4 zerlegt, rund
160 für den ganzen Dungeon. Kein Bodenstück darf von mehr als vier
Punktlichtern erreicht werden; im Essenkern belegt die Esse bereits einen
Platz auf jeder Kachel.

### M14.2 Der Formwall

**[entschieden] Der Einbruch bekommt statt einer Truhe einen Bauplatz.** Wer
ihn ausbaut, macht die Schmiede an dem Punkt wieder betriebsfähig, an dem sie
gestorben ist.

| Größe | Wert |
| --- | --- |
| Bedingung | die vier AshRunner im Schutt müssen tot sein |
| Kosten, einmalig | 24 Steinblöcke, 16 Bretter, 10 Kupferbarren, 6 Schmiedebeschläge |
| Ertrag | 2 Schmiedebeschläge je Lauf, garantiert |
| Ausbaustufen | eine, kein Pfad |

Erst räumen, dann bauen — damit ist der Raum eine Aufgabe und kein Automat.

**Die sechs Beschläge sind kein Balancehebel.** Als solcher wären sie fast
wirkungslos, weil sich der Wall bei zwei Beschlägen Ertrag ohnehin nach drei
Läufen auszahlt. Ihr Zweck ist die Abwägung: **Kosten und Ertrag sind
dieselbe Währung.** Ein Eisenteil kostet genau zwei Beschläge, also gibt man
für den Wall drei Rüstungsstücke auf, um später verlässlich zu bekommen, was
man bisher erwürfelt hat. Bei zwei Beschlägen wäre das keine Entscheidung,
sondern eine Verzögerung um ein einziges Teil.

Zum Vergleich: über die Truhen liefert ein Lauf im Mittel rund 0,45
Beschläge. Volle Eisenausrüstung kostet 20 — mit dem Formwall zehn Läufe statt
vierzig.

**[entschieden] Der Ertrag sammelt sich nicht an.** Einmal pro Lauf abholbar,
danach bis zum nächsten Lauf erschöpft. Wer den Einbruch in einem Lauf
auslässt, verliert den Ertrag dieses Laufs.

**[entschieden] Persistenz.** Der Ausbauzustand überdauert Läufe und bezahlte
Resets, analog zu `IgnivarCaptured` und `FirstCompletionGranted`. Im Save
stehen zwei zusätzliche Felder; Altstände ohne sie laden als **nicht gebaut**,
was inhaltlich richtig ist. Eine Migration war deshalb nicht nötig.

**[offen] Die sichtbare Wirkung fehlt noch.** Vorgesehen ist: vor dem Bau
Gerüststangen, leeres Materialgestell und ein abgesteckter Grund von 4 × 3 —
nach dem Bau eine stehende Formwand mit laufender Gießrinne und eigenem
Warmlicht, der Raum wird sichtbar heller. Gebaut ist bisher nur der Zustand
vor dem Bau. Bauen und Ernten funktionieren, aber der Bauplatz sieht danach
unverändert aus.

### M14.3 Truhenfreiraum in der Zonengenerierung

**[entschieden] Kein Ressourcenknoten steht in Reichweite einer Welttruhe.**
`ZoneLayoutGenerator` sperrte die in der `ZoneDefinition` hinterlegten
Truhenpunkte nie — ein Knoten konnte unmittelbar auf einer Truhe landen und
ihr die Zielauswahl wegziehen.

| Konstante | Wert | Herkunft |
| --- | ---: | --- |
| `WorldChestClearance` | 3,6 m | Interaktionsradius 2,5 + Knotenfreiraum 1,1; deckt zugleich den Wächterkranz bei 3,2 |
| `WorldChestMinimumClearance` | 2,5 m | bloßer Interaktionsradius, nur im Rasternotfall |

Gesperrt werden **alle zehn authorierten Punkte** je Zone, nicht nur die 2–6,
die tatsächlich bestückt werden. Damit bleibt der Layoutgenerator unabhängig
vom Truhengenerator und dessen abgeleitetem Seed. Der Abstand wird flach in XZ
gemessen, weil die Punkte auf `y = 0,12` authoriert sind.

Der `GridFallback` würfelt seinen Startversatz **einmal** und benutzt ihn für
beide Durchgänge — voller Freiraum, dann der bloße Interaktionsradius, dann
erst die Ausnahme. Damit gibt die Garantie im Notfall von „nichts steht im
Wächterkranz" auf „nichts steht in Truhenreichweite" nach, statt die Zone
abstürzen zu lassen.

**Alle Layouts verschieben sich**, weil die zusätzlichen Ablehnungen den
Zufallsstrom anders verbrauchen. Das ist der Zweck der Übung. Unverändert
bleiben Determinismus, Knotenzahlen, die Mischung 45/18/6/3, die
Weizensamen-Regel und die Instanz-IDs als Save-Keys. `GeneratorVersion` bleibt
bei 2: die Konstante wird von keiner Stelle gelesen, ein Hochzählen wäre
Kosmetik mit Desync-Risiko.

**Das ersetzt keine gesetzte Saat.** `ZoneStateService` würfelt weiterhin pro
Lauf neu (M1.2). Dieser Eingriff nimmt der Würfelei nur die
Truhenüberlappung.

### M14.4 Nicht Bestandteil von Version 0.3

- mehrstufiger Formwall mit steigendem Ertrag
- Bedienung des Formwalls durch Eidra (`eidraFactor`)
- Einbahn-Abkürzung vom Essenkern zurück zum Windfang
- volle Decken mit kameraabhängiger Ausblendung
- begehbarer Schutt im Einbruch
