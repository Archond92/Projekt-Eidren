# Eidren v0.3.2 – Windows-Testversion

Die zweite Fixrunde nach dem v0.3-Test. Neun gemeldete Punkte sind umgesetzt,
dazu zwei neue Bedienhilfen. Der Schwerpunkt liegt diesmal auf dem Kampf: Ein
Grundsatz aus dieser Runde hat mehrere versteckte Sperren aufgedeckt, die
Fähigkeiten an Gegnertypen banden.

Bestehende Spielstände werden übernommen. Wie bisher beginnt die Ausgabe bei
null — es liegt kein vorgefertigter Spielstand bei.

## Der Grundsatz dieser Runde

> Jede Fähigkeit und alle Waffen sollen immer an allen Gegnern, Bossen und
> Eidra im Kampf nutzbar sein.

Die Prüfung dieses Satzes hat drei Dinge gefunden, von denen nur eines
gemeldet war:

- **Noctarions Rückenmal verlangte einen Boss.** Gegen jeden gewöhnlichen
  Gegner meldete es „ZIEL KANN NICHT MARKIERT WERDEN".
- **Ignivars Schmelzbrand verlangte einen Schutzwert.** Den haben nur drei
  von elf Gegnern — Garon nicht eingeschlossen.
- **Im Bosskampf war das Ziel immer der Boss**, ohne Rücksicht auf die
  Reichweite. Stand er zu weit weg, meldete jede Fähigkeit „AUSSER
  REICHWEITE", obwohl ein Feldgegner direkt danebenstand.

Alle drei Sperren sind entfallen. Der Schmelzbrand senkt gegen gepanzerte
Ziele weiterhin den Schutz und macht ungepanzerte stattdessen um denselben
Betrag verwundbarer (+15 % erlittener Schaden für 6 Sekunden). Ein
Wächtertest hält den Grundsatz ab jetzt fest.

## Neu in dieser Ausgabe

### Tooltips an den Fähigkeitsknöpfen

Mit der Maus über eine Fähigkeit fahren — auf dem Handy kurz halten — erklärt,
was sie tut: Wirkung, Reichweite und Abklingzeit. Die Texte werden aus den
Werten der Fähigkeit gebaut und können deshalb nichts versprechen, was im
Kampf nicht passiert. Kurzes Antippen löst weiterhin aus; der Tooltip kostet
keinen Tastendruck. Lange Texte brechen um, der Balken wächst auf zwei oder
mehr Zeilen mit.

### Eidra-Gespann verwalten — Taste `G`

Ein gefangenes Eidra jenseits der zwei aktiven Plätze war bisher **für immer
verloren**: Der Fang trug es nur bei freiem Platz ein, die Umschalttaste
wechselte nur zwischen den beiden Aktiven, und eine Oberfläche gab es nicht.
Das neue Fenster zeigt alle gefangenen Eidra und lässt jedes auf einen der
Plätze setzen; der Wechsel wirkt sofort. Auf dem Gamepad liegt es auf D-Pad
links.

## Behoben

### Ignivar hat endlich ein Passiv — und das HUD sagt die Wahrheit darüber

Seine Werte standen auf „kein Effekt", während die Anzeige „+18 %
Rückenschaden" versprach. Neu ist **Treffer entzünden**: 20 % des
Trefferschadens brennen über 4 Sekunden nach, ein neuer Treffer frischt auf
statt zu stapeln. Damit sind die drei Eidra klar getrennt — Terrock hält aus,
Noctarion setzt den harten Treffer von hinten, Ignivar hält Druck. Der
Passivtext im HUD wird jetzt aus den Daten gebaut und kann nichts mehr
ankündigen, was im Kampf nicht passiert.

### Das Leben steigt auf den vollen Höchstwert

Mit voller Stoffrüstung liegt das Maximum bei 110 — gefüllt wurde bei der
Ankunft in einer Zone aber immer nur bis 100, weil die Initialisierung den
Rüstungsbonus verwarf und danach niemand nachfüllte. Ein Besuch der
Heimatbasis heilt jetzt auf das echte Maximum; der Trank tat es schon vorher.

### Gebäude werden angewählt, wenn man davorsteht

Bisher zog ein Nachbargebäude das Ziel an sich, während die Figur schon an der
Werkbank stand — und umgekehrt ließ sich ein Gebäude noch aus knapp zwei
Zellbreiten Entfernung anwählen. Rang und Blickrichtung entscheiden jetzt nur
noch den Gleichstand (zusammen höchstens 0,49 m statt 1,46 m auf einem Raster
mit 1 m Zellabstand), und die Reichweite der Gebäude sinkt auf **2,0 m** ab
Zellmitte — genau eine Kachel Luft über der Berührung; beim doppelt so großen
Acker 2,5 m.

### Man läuft nicht mehr durch die Einrichtung der Schmiede

Esse, Windrohre und Geländer hatten keine Kollision — man konnte mitten in
ihnen stehen. Der Bauhelfer setzt jetzt standardmäßig Kollider; nur flache
Bodenzeichnungen (Glutrisse, Glutadern, Glutschein) bleiben bewusst begehbar.
Das erklärt auch den älteren Bericht vom „Durchlaufen einer Verlieswand": Die
Wände waren immer fest, die Requisiten nie.

### Kisten verstecken sich nicht mehr hinter Felsen

Die Sichtlinien-Ausblendung galt nur für Figuren und Kreaturen. Im Verlies
schwebte deshalb der Markenpreis frei im Raum, während die Kiste darunter im
Fels steckte. Kisten werden jetzt genauso freigehalten.

## Empfohlener Testpfad

1. **Fähigkeiten gegen gewöhnliche Gegner** einsetzen — beide Fähigkeiten
   jedes Eidra, ohne Boss in der Nähe. Nichts darf mehr „KEIN GÜLTIGES ZIEL"
   oder „ZIEL KANN NICHT MARKIERT WERDEN" melden.
2. **Mit Ignivar zuschlagen** und auf „BRENNT" über dem Gegner achten: Sein
   Leben sinkt danach vier Sekunden lang weiter.
3. **Ein drittes Eidra fangen** und über `G` auf einen der beiden Plätze
   setzen.
4. **Über die Fähigkeitsknöpfe fahren** — der Tooltip erscheint und bricht bei
   langem Text um.
5. **An Werkbank, Lagerkiste und Acker treten**: Es muss das Gebäude
   angewählt werden, vor dem man steht, nicht der Nachbar.
6. **Stoffrüstung anlegen und in die Heimatbasis gehen** — geheilt wird auf
   110, nicht auf 100.
7. **Durch die Schmiede laufen** und versuchen, in Esse oder Windrohre zu
   treten. Der Glutschein am Boden bleibt begehbar.
8. Einen **v0.3.1-Spielstand laden** und alles erneut prüfen.

## Abnahme

- vollständiger EditMode-Lauf: **1235 bestanden, 0 fehlgeschlagen**
- vollständiger PlayMode-Lauf: **144 bestanden, 0 fehlgeschlagen**, 7
  bewusste Übersprünge (Grafik- und Diagnoseläufe)
- Windows-x64-Build erstellt und mit Prüfsumme versehen

Neu seit dieser Runde: Die Testsuite arbeitet mit **fester Weltsaat**. Ein
grüner Wiederholungslauf bedeutet damit tatsächlich etwas — vorher wurden die
Zonen bei jedem Lauf neu gewürfelt.

## Technische Daten

- Plattform: Windows x64
- Grafikpfad: Direct3D 11
- Buildtyp: Unity Development Build
- Unity: `6000.3.0f1`
- Download: 265.718.795 Bytes / 253,41 MiB
- entpackt: 367.001.193 Bytes / 350,00 MiB
- Dateien im Paket: 101
- SHA-256: `e7ffcbe61842378ca9d4cdd3d410490dab34c8f5ae60744bf7fed759377f335d`

Der Build ist nicht digital signiert. Eine Windows-SmartScreen-Warnung ist
daher möglich. Bitte nur das direkt aus diesem Repository geladene ZIP testen.

## Bekannte Einordnung

Dies ist eine Testversion zur mechanischen und inhaltlichen Abnahme, ohne
Mehrspielerfunktionen. Bekannt und noch offen:

- Die Außengebiete werden weiterhin pro Start neu ausgewürfelt. Im Spiel ist
  das unverändert; festgenagelt ist die Weltsaat nur für die Tests.
- Reichweiten messen bis zum Mittelpunkt eines Objekts, nicht bis zu seiner
  Wand — das gilt auch für die Nahkampf-Hitbox und ist bei großen Gegnern
  spürbar. Die Umstellung ist notiert, aber zurückgestellt.
- Werkbank und Lagerkiste zeigen im Baumenü Engine-Renderbilder; gemalte
  Fassungen im Stil der übrigen Icons stehen noch aus.
- Die weiteren offenen Punkte stehen gesammelt in
  `Documentation/Eidren_V0.2/OFFENE_PUNKTE.md`.

## Was als Nächstes kommt

Vor der nächsten Inhaltsrunde steht ein **Grafikupgrade auf Mid Poly**: alle
sichtbaren Modelle werden neu interpretiert — Figuren, Kreaturen, Gebäude,
Ressourcen und Umgebung. Silhouetten, Maße, Rigs, Animationen und
Gameplaywerte bleiben dabei erhalten. Die Umstellung läuft assetfamilienweise
mit Zwischenabnahmen, nicht als Austausch auf einen Schlag.
