# Schmiede Etappe 4 — Der Formwall: Umsetzungsplan

> **Status: Mechanik umgesetzt und abgenommen, Darstellung offen
> (16.08.2026).** Nachweis: `EidraForgeFormwallSite.cs`, erweiterte
> `EidraForgeDungeonService.cs` und `EidraForgeRunState.cs`, Persistenz in
> `SaveGameData.cs` und `SaveEidraForgeMapper.cs`, elf Tests in
> `EidraForgeFormwallTests.cs`. `EidraForgeBalance.TotalMarks` und
> `TotalExperience` sind unverändert.
>
> **Abweichung vom Plan:** Der Abschnitt „Nicht Teil dieser Etappe" schließt
> die Anbindung an die Spielereingabe aus. Sie ist trotzdem entstanden — der
> Bauplatz ist ein `IInteractable` mit Halten zum Bauen und Antippen zum
> Ernten, gebunden über `EidraForgeSceneController.BindFormwall`.
>
> **Was fehlt:** die sichtbare Wirkung. Nach dem Bau sollte eine stehende
> Formwand mit laufender Gießrinne und eigenem Warmlicht den Raum sichtbar
> heller machen; gebaut ist bisher nur der Zustand davor. Das Ereignis
> `EidraForgeFormwallSite.Ausgefuehrt` ist dafür vorgesehen. Siehe
> `Documentation/PROGRAMMIERBIBEL.md` **M14.2**.

> **Für agentische Bearbeiter:** ERFORDERLICHE UNTER-SKILL: `superpowers:subagent-driven-development` oder `superpowers:executing-plans`. Schritte nutzen Checkbox-Syntax (`- [ ]`).

**Ziel:** Der Einbruch wird zum Sonderraum. Wer den Formwall wieder aufbaut, macht die Schmiede an der Stelle betriebsfähig, an der sie gestorben ist — und bekommt ab dann verlässlich Schmiedebeschläge statt Glückswürfe.

**Architektur:** Reine Spiellogik, keine Szene. Der Zustand kommt als zwei Felder in `EidraForgeRunState` und verhält sich wie `IgnivarCaptured`: er überlebt Läufe und bezahlte Resets. Die Bauprüfung und die Ernte liegen als Methoden im `EidraForgeDungeonService`, damit die Regeln an einer Stelle stehen und ohne Szene testbar bleiben.

## Globale Vorgaben (aus `SCHMIEDE_ENTWURF.md` Abschnitt 6)

- **Bedingung:** Der Bauplatz ist gesperrt, solange die vier AshRunner im Einbruch leben. Deren Spawn-Ids lauten `forge.enemy.ash_runner.03` bis `.06`.
- **Kosten, einmalig:** 24 `stone_block`, 16 `plank`, 10 `copper_bar`, **6** `smithing_fitting`.
- **Ertrag:** 2 `smithing_fitting` pro Lauf, garantiert. Am fertigen Formwall abzuholen, **einmal pro Lauf**; danach bis zum nächsten Lauf erschöpft. Der Ertrag **sammelt sich nicht an** — wer den Einbruch in einem Lauf auslässt, verliert ihn.
- **Einstufig.** Kein Ausbaupfad.
- **Persistenz:** `FormwallGebaut` überlebt `BeginRun` wie `IgnivarCaptured` und `FirstCompletionGranted`. `FormwallErtragOffen` wird bei jedem `BeginRun` auf den Wert von `FormwallGebaut` gesetzt.
- `EidraForgePopulationRules`, `EidraForgeBalance` und `EidraForgeContainerRules` werden **nicht** angefasst.
- `Assets/` ist nicht versioniert — vor dem Ändern bestehender Dateien Kopie ins Scratchpad.
- Vor jedem Unity-Lauf prüfen, ob ein fremder Unity-Prozess läuft. Exit-Code lügt bei Compile-Fehlern.

---

## Dateistruktur

| Datei | Änderung |
|---|---|
| `Scripts/Core/EidraForgeRunState.cs` | zwei Felder plus `Copy()` |
| `Scripts/Core/EidraForgeDungeonService.cs` | Baukosten, `TryBaueFormwall`, `TryErnteFormwall`, `BeginRun` |
| `Scripts/Core/SaveGameData.cs` | zwei Felder in `SaveEidraForgeData` |
| `Scripts/Core/SaveEidraForgeMapper.cs` | beide Richtungen |
| `Editor/Tests/EidraForgeFormwallTests.cs` (neu) | Regeln des Formwalls |

---

## Aufgabe 1: Zustand und Persistenz

- [ ] **Schritt 1: Test schreiben, der noch fehlschlägt**

```csharp
	[Test]
	public void Formwall_UeberlebtEinenNeuenLauf()
	{
		EidraForgeDungeonService dienst = new EidraForgeDungeonService();
		dienst.TryBeginFirstRun(1);
		dienst.MutableState.FormwallGebaut = true;
		dienst.MutableState.FormwallErtragOffen = false;
		Assert.That(dienst.TryComplete(DateTime.UtcNow, out _), Is.True);
		dienst.MutableState.CompletedUtcTicks = DateTime.UtcNow.AddDays(-2).Ticks;

		PlayerInventory beutel = VollerBeutel();
		Assert.That(dienst.TryPaidReset(beutel, DateTime.UtcNow, 2, playerInside: false,
			Array.Empty<ItemStack>(), out _), Is.True);

		Assert.That(dienst.MutableState.FormwallGebaut, Is.True, "Der Ausbau muss den Reset ueberleben.");
		Assert.That(dienst.MutableState.FormwallErtragOffen, Is.True,
			"Der Ertrag muss zu Laufbeginn wieder bereitstehen.");
	}
```

- [ ] **Schritt 2: Fehlschlag bestätigen** — erwartet `error CS1061` für `FormwallGebaut`.

- [ ] **Schritt 3: Felder ergänzen**

In `EidraForgeRunState`:

```csharp
		/// <summary>Ist der Formwall im Einbruch wieder aufgebaut? Ueberlebt Laeufe
		/// und bezahlte Resets, wie IgnivarCaptured.</summary>
		internal bool FormwallGebaut;

		/// <summary>Steht der Ertrag dieses Laufs noch zur Abholung? Wird bei jedem
		/// Laufbeginn auf FormwallGebaut gesetzt - der Ertrag sammelt sich nicht an.</summary>
		internal bool FormwallErtragOffen;
```

In `Copy()` beide mitkopieren, in `BeginRun`:

```csharp
			bool formwallGebaut = _state.FormwallGebaut;
			// ... im Initialisierer:
			FormwallGebaut = formwallGebaut,
			FormwallErtragOffen = formwallGebaut,
```

- [ ] **Schritt 4: Grün bestätigen.**

---

## Aufgabe 2: Bauen

- [ ] **Schritt 1: Tests schreiben**

```csharp
	[Test]
	public void Formwall_LaesstSichNichtBauenSolangeDasNestLebt()
	{
		EidraForgeDungeonService dienst = new EidraForgeDungeonService();
		dienst.TryBeginFirstRun(1);
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out InventoryTransactionFailure grund), Is.False,
			"Solange die AshRunner leben, ist der Bauplatz gesperrt.");
		Assert.That(dienst.MutableState.FormwallGebaut, Is.False);
	}

	[Test]
	public void Formwall_BrauchtDasVolleMaterial()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		PlayerInventory knapp = new PlayerInventory(Inhalt(), 40);
		knapp.TryAdd(new ItemStack("stone_block", 10), out _);
		Assert.That(dienst.TryBaueFormwall(knapp, out _), Is.False);
		Assert.That(dienst.MutableState.FormwallGebaut, Is.False);
	}

	[Test]
	public void Formwall_WirdGebautUndZiehtDasMaterialAb()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		PlayerInventory beutel = VollerBeutel();
		Assert.That(dienst.TryBaueFormwall(beutel, out _), Is.True);
		Assert.That(dienst.MutableState.FormwallGebaut, Is.True);
		Assert.That(beutel.Zaehle("stone_block"), Is.EqualTo(0), "Die Kosten muessen abgezogen sein.");
	}

	[Test]
	public void Formwall_LaesstSichNichtZweimalBauen()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out _), Is.True);
		Assert.That(dienst.TryBaueFormwall(VollerBeutel(), out _), Is.False);
	}
```

Die Hilfsmethode `NestGeraeumt()` legt einen laufenden Dienst an und markiert `forge.enemy.ash_runner.03` bis `.06` als besiegt. Der genaue Weg dorthin hängt davon ab, wie `ForgeEnemyState.Defeated` gesetzt wird — vor dem Schreiben in `EidraForgePopulationRules.CreateEnemyStates` nachsehen und den vorhandenen Weg benutzen, keinen neuen erfinden.

- [ ] **Schritt 2: Umsetzen**

```csharp
		private static readonly InventoryItemAmount[] FormwallKosten = new InventoryItemAmount[4]
		{
			new InventoryItemAmount("stone_block", 24),
			new InventoryItemAmount("plank", 16),
			new InventoryItemAmount("copper_bar", 10),
			new InventoryItemAmount("smithing_fitting", 6)
		};

		private static readonly string[] NestWaechter = new string[4]
		{
			"forge.enemy.ash_runner.03", "forge.enemy.ash_runner.04",
			"forge.enemy.ash_runner.05", "forge.enemy.ash_runner.06"
		};

		/// <summary>
		/// Der Bauplatz ist gesperrt, solange die vier AshRunner im Schutt nisten.
		/// Damit ist der Einbruch eine Aufgabe und kein Automat: erst raeumen,
		/// dann bauen.
		/// </summary>
		public bool IstBauplatzFrei()
		{
			foreach (string id in NestWaechter)
			{
				if (!IsEnemyDefeated(id))
				{
					return false;
				}
			}
			return true;
		}

		public bool TryBaueFormwall(PlayerInventory inventory, out InventoryTransactionFailure failure)
		{
			failure = InventoryTransactionFailure.InvalidRequest;
			if (inventory == null || _state.Status != EidraForgeRunStatus.InProgress
				|| _state.FormwallGebaut || !IstBauplatzFrei())
			{
				return false;
			}
			if (!inventory.TryApplyTransaction(FormwallKosten, string.Empty, 0, out failure))
			{
				return false;
			}
			_state.FormwallGebaut = true;
			/* Im Bau-Lauf selbst gibt es noch nichts abzuholen: der Auslauf muss
			   erst erkalten. Der Ertrag beginnt mit dem naechsten Lauf. */
			_state.FormwallErtragOffen = false;
			return true;
		}
```

- [ ] **Schritt 3: Grün bestätigen.**

---

## Aufgabe 3: Ernten

- [ ] **Schritt 1: Tests schreiben**

```csharp
	[Test]
	public void Ertrag_GibtZweiBeschlaegeUndNurEinmalProLauf()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		dienst.MutableState.FormwallGebaut = true;
		dienst.MutableState.FormwallErtragOffen = true;
		PlayerInventory beutel = new PlayerInventory(Inhalt(), 40);

		Assert.That(dienst.TryErnteFormwall(beutel, out _), Is.True);
		Assert.That(beutel.Zaehle("smithing_fitting"), Is.EqualTo(2));
		Assert.That(dienst.TryErnteFormwall(beutel, out _), Is.False, "Nur einmal pro Lauf.");
		Assert.That(beutel.Zaehle("smithing_fitting"), Is.EqualTo(2), "Kein zweiter Ertrag.");
	}

	[Test]
	public void Ertrag_SammeltSichNichtAn()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		dienst.MutableState.FormwallGebaut = true;
		dienst.MutableState.FormwallErtragOffen = true;
		// Lauf ohne Ernte beenden und neu beginnen.
		Assert.That(dienst.TryComplete(DateTime.UtcNow, out _), Is.True);
		dienst.MutableState.CompletedUtcTicks = DateTime.UtcNow.AddDays(-2).Ticks;
		Assert.That(dienst.TryPaidReset(VollerBeutel(), DateTime.UtcNow, 3, playerInside: false,
			Array.Empty<ItemStack>(), out _), Is.True);

		PlayerInventory beutel = new PlayerInventory(Inhalt(), 40);
		Assert.That(dienst.TryErnteFormwall(beutel, out _), Is.True);
		Assert.That(beutel.Zaehle("smithing_fitting"), Is.EqualTo(2),
			"Der verpasste Ertrag darf sich nicht aufsummieren.");
	}

	[Test]
	public void Ertrag_GibtNichtsOhneFormwall()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		Assert.That(dienst.TryErnteFormwall(new PlayerInventory(Inhalt(), 40), out _), Is.False);
	}
```

- [ ] **Schritt 2: Umsetzen**

```csharp
		public const int FormwallErtrag = 2;

		/// <summary>
		/// Holt den Ertrag des fertigen Formwalls ab: zwei Schmiedebeschlaege,
		/// einmal pro Lauf. Zum Vergleich liefern die Truhen im Mittel 0,45 pro
		/// Lauf - der Ausbau macht aus Glueck Verlaesslichkeit.
		/// </summary>
		public bool TryErnteFormwall(PlayerInventory inventory, out InventoryTransactionFailure failure)
		{
			failure = InventoryTransactionFailure.InvalidRequest;
			if (inventory == null || _state.Status != EidraForgeRunStatus.InProgress
				|| !_state.FormwallGebaut || !_state.FormwallErtragOffen)
			{
				return false;
			}
			if (!inventory.TryAdd(new ItemStack("smithing_fitting", FormwallErtrag), out failure))
			{
				return false;
			}
			_state.FormwallErtragOffen = false;
			return true;
		}
```

**Achtung:** Die genaue Signatur von `TryAdd` vor dem Schreiben prüfen — sie kann `out int rest` statt `out InventoryTransactionFailure` liefern. Den vorhandenen Aufruf im Projekt als Vorlage nehmen.

- [ ] **Schritt 3: Grün bestätigen.**

---

## Aufgabe 4: Speichern und Laden

- [ ] **Schritt 1: Test schreiben**

```csharp
	[Test]
	public void Formwall_UeberstehtSpeichernUndLaden()
	{
		EidraForgeDungeonService dienst = NestGeraeumt();
		dienst.MutableState.FormwallGebaut = true;
		dienst.MutableState.FormwallErtragOffen = false;

		SaveEidraForgeData gespeichert = SaveEidraForgeMapper.Save(dienst.Capture());
		EidraForgeRunState geladen = SaveEidraForgeMapper.Load(gespeichert, null, null);

		Assert.That(geladen.FormwallGebaut, Is.True);
		Assert.That(geladen.FormwallErtragOffen, Is.False);
	}
```

Die genauen Namen von `Save`/`Load` vor dem Schreiben in `SaveEidraForgeMapper` nachsehen.

- [ ] **Schritt 2:** Zwei Felder in `SaveEidraForgeData` ergänzen (`formwallGebaut`, `formwallErtragOffen`), in beiden Mapper-Richtungen durchreichen.

- [ ] **Schritt 3: Altstände prüfen.** Ein Spielstand ohne die neuen Felder muss laden, als wäre der Formwall nicht gebaut — bei `bool` ist das der Standardwert `false`, also ohne Migration korrekt. **Trotzdem mit einem Test belegen**, nicht annehmen: `SaveGameMigration` ansehen und prüfen, ob dort eine Versionsnummer hochzuziehen ist.

---

## Aufgabe 5: Abnahme

- [ ] Volle EditMode-Suite — Baseline 1094 plus die neuen Tests.
- [ ] Volle PlayMode-Suite — 120 bestanden, 3 `Explicit` übersprungen.
- [ ] Die Balancewerte gegenprüfen: `EidraForgeBalance.TotalMarks` und `TotalExperience` dürfen sich **nicht** geändert haben.

---

## Nicht Teil dieser Etappe

- Bedienoberfläche für Bau und Ernte — der Bauplatz steht als Objekt in der Szene, die Anbindung an die Spielereingabe ist eine eigene Aufgabe.
- Mehrstufiger Ausbau, Bedienung durch Eidra (`eidraFactor`).
- Feinabgleich der Beleuchtung aus Etappe 3.
