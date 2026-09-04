# Stilumbau Etappe 2 — Forge-Kisten und Stationen: Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** Die acht Forge-Kistenfamilien und die drei Stationen (StorageChest, Workbench, DeathBag) rendern im Vertexfarben-Stil der Etappe 1; die fehlenden Forge-Prefabs unter `Prefabs/Containers/Forge` entstehen dabei neu und die EidraForge-Szene wird neu gebacken.

**Architektur:** `ForgeContainerVisualBuilder` und die `VisualCube`-Stellen in `StorageContentBuilder`/`CraftingContentBuilder` werden auf `EidrenMeshFactory` + `EidrenWorldStyleAssets.EnsureWorldMaterial()` umgestellt (Fundament aus Etappe 1, unverändert). Forge-Kisten erhalten Loot-Füllstand über die 8-Argument-`Configure` von `WorldChestVisual`. Vorher-Belege: Studio-Captures der Stationen-Prefabs plus In-Szene-Captures der gebackenen Forge-Kisten (Prefabs existieren vorher nicht).

**Tech Stack:** Unity 6000.3 (`.unity-editor/`), URP 17.3 (Gamma + LDR), NUnit EditMode in `Assets/_Game/Editor/Tests/`.

**Spezifikation:** `Documentation/Etappen/Stilumbau/STILUMBAU_WELTINVENTAR_ENTWURF.md` · **Vorlage:** Etappe-1-Plan `STILUMBAU_E1_PLAN.md` (Fundament-Aufgaben dort sind erledigt und werden hier nur konsumiert)

## Globale Vorgaben

- Projektpfad `C:\Users\phine\Documents\Projekt Eidren` (Leerzeichen — immer quoten, Aufrufoperator `&`).
- Unity headless wie in Etappe 1: Exit-Code lügt; Erfolg = Log ohne `error CS` (bei Builder-Läufen zusätzlich ohne `Shader error`) und ggf. Ergebnis-XML. `Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue` + Puffer.
- Vor JEDEM Unity-Aufruf `Test-Path "Library\UnityLockfile"` — bei `True` warten, niemals löschen.
- Capture-Läufe OHNE `-nographics` (Rendering braucht GPU); Testläufe MIT `-nographics`.
- **Kein `git commit`**; Belege mit Präfix `stil-e2-…`. **Niemals Belege erfinden** — der Task-4-Vorfall aus Etappe 1 (handgeschriebenes XML) wird in jedem Review geprüft.
- Deutsche Kommentare, Tabs, `sealed`.
- Strukturnotizen aus dem E1-Abschlussreview, hier verbindlich: (a) Mesh-Assets behalten das Sequenz-Index-Namensmuster ihres Builders (`Forge_{NNN}_{Name}` bzw. neu je MeshRoot) — Bauteile nur ANHÄNGEN, nie mitten im Ablauf einfügen; (b) `M_EidrenWorld_VertexLit.mat` niemals löschen, nur per Builder-Lauf anfassen; (c) Kontakt-AO ist mesh-lokal — bei den hier gebauten Einzelteilen gewollt, keine Änderung an der Fabrik.
- Bestandsverträge Forge (aus `V02ContainerVisualTests` und `EidraForgeSceneBuilder`): Prefabpfad `Assets/_Game/Prefabs/Containers/Forge/<Name>_2D.prefab`, Wurzelname `<Name>_3D`, `WorldChestVisual` + `BoxCollider` auf der Wurzel (`size = Spec.Size`, `center = up*Size.y*0.5`), Kinder `LidPivot`, `ClosedDetails`, `OpenedDetails`, `EmptiedDetails`, KEIN SpriteRenderer, ≥ 5 MeshFilter, je Familie eindeutige Silhouetten-Signatur (Bounds x/y + MeshFilter-Zahl), LidPivot bei `(0, Size.y*0.61, Size.z*0.45)`.
- Bestandsverträge Stationen: Wurzel-`BoxCollider` (Trigger) und `StorageContainer.Configure(...)`-Aufrufe unverändert (StorageChest `2.7/2/2.1` bei `0/0.75/0`, DeathBag `1.8/1.4/1.8` bei `0/0.4/0`); Teil-Namen bleiben (`ChestBody`, `ChestLid`, `FrontBand`, `Latch`; `Sack`, `Knot`; `WorkSurface`, `LegLeft`, `LegRight`, `ToolRail`); `Geometry_A14`-Wächter und Szenen-Platzierungslogik unangetastet; die `RES_*`-Materialien bleiben bestehen (Ressourcen nutzen sie bis Etappe 3), nur die Stationen lesen daraus jetzt ihre Farbwerte.
- Dreieckskorridore: Forge-Kiste ≤ 500, Station ≤ 400.

---

### Task 0: Sicherung, Vorflug und frische EditMode-Baseline

**Files:** keine Quellcode-Änderung.

**Interfaces:**
- Produces: Sicherung `vor-stilumbau-e2-<stamp>`; `TestResults-stil-e2-baseline.xml` (Etappe 1 hat den Testbestand verändert — die E1-Baseline ist verbraucht). Task 5 vergleicht per Namensdiff dagegen.

- [ ] **Step 1: Sichern** — wie E1-Task 0, Zielordner `C:\Users\phine\Documents\Eidren-Sicherungen\vor-stilumbau-e2-<stamp>` (Kopie `Assets\_Game` + `Packages\manifest.json`; Listing als Nachweis).
- [ ] **Step 2: Lockfile prüfen** — `Test-Path "Library\UnityLockfile"` erwartet `False`.
- [ ] **Step 3: Baseline** — voller EditMode-Lauf nach E1-Muster mit `-testResults "...\TestResults-stil-e2-baseline.xml" -logFile "...\stil-e2-baseline.log"`; Log frei von `error CS`, `failed`-Zahl aus `<test-run>` notieren (erwartet ≈ 72, darunter `V02ContainerVisualTests` wegen fehlender Forge-Prefabs).
- [ ] **Step 4: Prüfnachweis** — Sicherungslisting, Baseline-XML, failed-Zahl.

---

### Task 1: Vorher-Captures (Stationen im Studio, Forge-Kisten in der Szene)

**Files:**
- Modify: `Assets/_Game/Editor/WorldChestStyleCaptureUtility.cs` (zwei Methoden ergänzen)

**Interfaces:**
- Consumes: bestehendes Capture-Muster der Klasse (RenderTexture 768×768 bzw. 1280×720, PNG-Schreiben, `[StilE1]`-Logzeilen — neue Zeilen loggen mit `[StilE2]`).
- Produces: `CaptureStationen(string ordner)` mit MenuItems `Eidren/V0.2/Stilumbau/Stationen Vorher-Captures` → `TempReview/StilumbauE2/stationen-vorher` und `…Nachher-Captures` → `…/stationen-nachher`; `CaptureForgeSzene(string ordner)` mit MenuItems `…/Forge Vorher-Captures` → `TempReview/StilumbauE2/forge-vorher` und `…/Forge Nachher-Captures` → `…/forge-nachher`. Task 5 nutzt die Nachher-Varianten.

- [ ] **Step 1: Stationen-Studio-Capture ergänzen** — Muster von `CaptureAll` übernehmen, aber ohne Zustände (Stationen haben keinen `WorldChestVisual`):

```csharp
	private static readonly (string pfad, string name, float zielHoehe)[] Stationen =
	{
		("Assets/_Game/Prefabs/Stations/StorageChest.prefab", "StorageChest", 0.8f),
		("Assets/_Game/Prefabs/Stations/Workbench.prefab", "Workbench", 0.9f),
		("Assets/_Game/Resources/Prefabs/DeathBag.prefab", "DeathBag", 0.4f)
	};

	[MenuItem("Eidren/V0.2/Stilumbau/Stationen Vorher-Captures")]
	public static void CaptureStationenVorher()
	{
		CaptureStationen("TempReview/StilumbauE2/stationen-vorher");
	}

	[MenuItem("Eidren/V0.2/Stilumbau/Stationen Nachher-Captures")]
	public static void CaptureStationenNachher()
	{
		CaptureStationen("TempReview/StilumbauE2/stationen-nachher");
	}

	public static void CaptureStationen(string ordner)
	{
		Directory.CreateDirectory(ordner);
		foreach ((string pfad, string name, float zielHoehe) station in Stationen)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(station.pfad);
			if (prefab == null)
			{
				Debug.LogError("[StilE2] Prefab fehlt: " + station.pfad);
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
			kameraObjekt.transform.position = new Vector3(2.4f, 2.2f, 3.1f);
			kameraObjekt.transform.LookAt(new Vector3(0f, station.zielHoehe, 0f));
			try
			{
				RenderTexture ziel = new RenderTexture(768, 768, 24);
				kamera.targetTexture = ziel;
				kamera.Render();
				RenderTexture.active = ziel;
				Texture2D bild = new Texture2D(768, 768, TextureFormat.RGB24, mipChain: false);
				bild.ReadPixels(new Rect(0f, 0f, 768f, 768f), 0, 0);
				bild.Apply();
				File.WriteAllBytes(Path.Combine(ordner, station.name + ".png"), bild.EncodeToPNG());
				RenderTexture.active = null;
				kamera.targetTexture = null;
				Object.DestroyImmediate(ziel);
				Object.DestroyImmediate(bild);
				Debug.Log("[StilE2] geschrieben: " + station.name);
			}
			finally
			{
				Object.DestroyImmediate(instanz);
				Object.DestroyImmediate(kameraObjekt);
				Object.DestroyImmediate(lichtObjekt);
			}
		}
	}
```

- [ ] **Step 2: Forge-Szenen-Capture ergänzen** — findet die gebackenen Kisten selbst (robust gegen Positionsänderungen), fotografiert je Instanz `Closed` und `Opened`; Szene wird NICHT gespeichert:

```csharp
	[MenuItem("Eidren/V0.2/Stilumbau/Forge Vorher-Captures")]
	public static void CaptureForgeVorher()
	{
		CaptureForgeSzene("TempReview/StilumbauE2/forge-vorher");
	}

	[MenuItem("Eidren/V0.2/Stilumbau/Forge Nachher-Captures")]
	public static void CaptureForgeNachher()
	{
		CaptureForgeSzene("TempReview/StilumbauE2/forge-nachher");
	}

	public static void CaptureForgeSzene(string ordner)
	{
		Directory.CreateDirectory(ordner);
		UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Game/Scenes/EidraForge.unity");
		WorldChestVisual[] kisten = Object.FindObjectsByType<WorldChestVisual>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
		HashSet<string> vergeben = new HashSet<string>();
		foreach (WorldChestVisual kiste in kisten)
		{
			string name = kiste.gameObject.name.Replace("(Clone)", string.Empty);
			if (!vergeben.Add(name))
			{
				continue; /* je Familie ein Exemplar reicht (SupplyChest steht mehrfach) */
			}
			Vector3 fuss = kiste.transform.position;
			GameObject kameraObjekt = new GameObject("ForgeCaptureKamera");
			Camera kamera = kameraObjekt.AddComponent<Camera>();
			kameraObjekt.transform.position = fuss + new Vector3(2.6f, 3.4f, -3.4f);
			kameraObjekt.transform.LookAt(fuss + Vector3.up * 0.5f);
			try
			{
				foreach (WorldChestVisualState zustand in new[] { WorldChestVisualState.Closed, WorldChestVisualState.Opened })
				{
					kiste.Apply(zustand);
					RenderTexture ziel = new RenderTexture(1280, 720, 24);
					kamera.targetTexture = ziel;
					kamera.Render();
					RenderTexture.active = ziel;
					Texture2D bild = new Texture2D(1280, 720, TextureFormat.RGB24, mipChain: false);
					bild.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
					bild.Apply();
					File.WriteAllBytes(Path.Combine(ordner, $"{name}_{zustand}.png"), bild.EncodeToPNG());
					RenderTexture.active = null;
					kamera.targetTexture = null;
					Object.DestroyImmediate(ziel);
					Object.DestroyImmediate(bild);
					Debug.Log("[StilE2] geschrieben: " + name + "_" + zustand);
				}
				kiste.Apply(WorldChestVisualState.Closed);
			}
			finally
			{
				Object.DestroyImmediate(kameraObjekt);
			}
		}
	}
```

(`using System.Collections.Generic;` ergänzen, falls in der Datei noch nicht vorhanden.)

- [ ] **Step 3: Vorher-Läufe ausführen** (beide OHNE `-nographics`; Logs `stil-e2-t1-stationen.log` / `stil-e2-t1-forge.log`):

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureStationenVorher -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e2-t1-stationen.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.WorldChestStyleCaptureUtility.CaptureForgeVorher -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e2-t1-forge.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\stil-e2-t1-stationen.log","C:\Users\phine\Documents\Projekt Eidren\stil-e2-t1-forge.log" -Pattern "error CS|\[StilE2\]"
```

Erwartet: 3 Stations-PNGs und 16 Forge-PNGs (8 Familien × 2 Zustände). Mindestens 3 Bilder ansehen (nicht leer, Objekt sichtbar).

- [ ] **Step 4: Prüfnachweis** — PNG-Listings beider Ordner, Logauszug, Sichtbeschreibung.

---

### Task 2: ForgeContainerVisualBuilder auf die Fabrik umstellen

**Files:**
- Modify: `Assets/_Game/Editor/ForgeContainerVisualBuilder.cs`
- Test: `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` (Ergänzung)

**Interfaces:**
- Consumes: `EidrenMeshFactory.TaperedBox/Loft` (geschachtelte `EidrenMeshFactory.LoftProfile`/`LoftShape`), `EidrenWorldStyleAssets.EnsureWorldMaterial()`, `WorldChestVisual.Configure(lid, closed, opened, emptied, leftHand, rightHand, lootFull, lootPartial)` (Hände hier `null`).
- Produces: `BuildPrefab(Spec)` erzeugt Vertexfarben-Prefabs `<Name>_2D.prefab` mit `LootFill_Full`/`LootFill_Partial`; `Spec` und `Specs()` bleiben unverändert (Farb-/Größenquelle). Task 3 lässt den Builder laufen.

- [ ] **Step 1: Failing Test ergänzen** — in `EidrenWorldStyleTests.cs` anhängen:

```csharp
		[TestCase("SupplyChest", 1.18f, 0.8f, 0.78f)]
		[TestCase("OptionalChest", 1.25f, 0.8f, 0.82f)]
		[TestCase("EliteChest", 1.42f, 1f, 0.92f)]
		[TestCase("CompletionChest", 1.62f, 1.2f, 1.02f)]
		[TestCase("SmallRewardChest", 0.95f, 0.7f, 0.68f)]
		[TestCase("MediumRewardChest", 1.2f, 0.9f, 0.82f)]
		[TestCase("LargeRewardChest", 1.55f, 1.2f, 1.02f)]
		[TestCase("RecoveryContainer", 1.45f, 0.8f, 0.82f)]
		public void ForgeKiste_IstImVertexfarbenStil(string name, float breite, float hoehe, float tiefe)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Containers/Forge/" + name + "_2D.prefab");
			Assert.That(prefab, Is.Not.Null, name);
			int dreiecke = 0;
			foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true))
			{
				Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
					name + "/" + filter.name + " ohne Vertexfarben");
				dreiecke += filter.sharedMesh.triangles.Length / 3;
			}
			Assert.That(dreiecke, Is.LessThanOrEqualTo(500), name + " ueberschreitet den Dreieckskorridor");
			foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
			{
				Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
					name + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
			}
			Assert.That(prefab.transform.Find("LootFill_Full"), Is.Not.Null, name + " ohne LootFill_Full");
			Assert.That(prefab.transform.Find("LootFill_Partial"), Is.Not.Null, name + " ohne LootFill_Partial");
			BoxCollider collider = prefab.GetComponent<BoxCollider>();
			Assert.That(collider.size, Is.EqualTo(new Vector3(breite, hoehe, tiefe)), name + " Collider veraendert");
		}
```

- [ ] **Step 2: Testlauf — muss scheitern** (Prefabs existieren noch nicht): Filter `Eidren.Tests.EditMode.EidrenWorldStyleTests`, XML `TestResults-stil-e2-t2a.xml`, Log `stil-e2-t2a.log`.

- [ ] **Step 3: Builder umstellen** — in `ForgeContainerVisualBuilder.cs`: private Methoden `TaperedBox`, `Wedge`, `MeshFrom`, `MaterialFor` löschen; `Persist` nach dem E1-Muster einfügen (Pfadformat unverändert `Forge_{NNN}_{Name}`):

```csharp
	private static Mesh Persist(Mesh mesh)
	{
		string path = string.Format("{0}/Forge_{1:000}_{2}.asset", MeshRoot, _meshSequence++, mesh.name);
		Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
		if (existing != null)
		{
			EditorUtility.CopySerialized(mesh, existing);
			Object.DestroyImmediate(mesh);
			EditorUtility.SetDirty(existing);
			return existing;
		}
		AssetDatabase.CreateAsset(mesh, path);
		return mesh;
	}
```

`BuildPrefab(Spec spec)` vollständig ersetzen (Palette abgeleitet wie in E1; Geometrie-Sprache der Weltkisten, Familienunterschiede aus `Spec.Bands`/`Crest`/`Locker`):

```csharp
	private static GameObject BuildPrefab(Spec spec)
	{
		Color plank = Color.Lerp(spec.Body, Color.white, 0.12f);
		Color metall = Color.Lerp(spec.Accent, Color.black, 0.35f);
		Color innen = new Color(0.018f, 0.015f, 0.012f);
		Color futter = Color.Lerp(spec.Body, Color.white, 0.28f);
		Color gold = new Color(0.84f, 0.66f, 0.29f);
		Material material = EidrenWorldStyleAssets.EnsureWorldMaterial();
		GameObject root = new GameObject(spec.Name + "_3D");

		/* Korpus: Sockel + zwei Plankenlagen (Namen neu, Signatur bleibt ueber Bounds + Filterzahl) */
		MeshObject("Sockel", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 1.06f, spec.Size.y * 0.1f, spec.Size.z * 1.1f), 1f, spec.Accent)), material, root.transform);
		GameObject planke1 = MeshObject("Planke_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x, spec.Size.y * 0.28f, spec.Size.z), 1f, spec.Body)), material, root.transform);
		planke1.transform.localPosition = Vector3.up * (spec.Size.y * 0.1f);
		GameObject planke2 = MeshObject("Planke_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x, spec.Size.y * 0.25f, spec.Size.z), 0.98f, plank)), material, root.transform);
		planke2.transform.localPosition = Vector3.up * (spec.Size.y * 0.38f);

		/* Deckel: Bogen-Loft am bestehenden LidPivot-Ort */
		GameObject lidPivot = Child("LidPivot", root.transform);
		lidPivot.transform.localPosition = new Vector3(0f, spec.Size.y * 0.61f, spec.Size.z * 0.45f);
		EidrenMeshFactory.LoftProfile[] bogen =
		{
			new EidrenMeshFactory.LoftProfile(0f, spec.Size.x * 0.98f, spec.Size.z * 0.94f),
			new EidrenMeshFactory.LoftProfile(spec.Size.y * 0.14f, spec.Size.x * 0.9f, spec.Size.z * 0.86f),
			new EidrenMeshFactory.LoftProfile(spec.Size.y * 0.26f, spec.Size.x * 0.72f, spec.Size.z * 0.68f),
			new EidrenMeshFactory.LoftProfile(spec.Size.y * 0.34f, spec.Size.x * 0.48f, spec.Size.z * 0.46f)
		};
		GameObject lid = MeshObject("ForgedLid", Persist(EidrenMeshFactory.Loft(bogen, EidrenMeshFactory.LoftShape.Rect, spec.Body, capBottom: false, capTop: true)), material, lidPivot.transform);
		lid.transform.localPosition = new Vector3(0f, 0f, (0f - spec.Size.z) * 0.45f);

		GameObject closed = Child("ClosedDetails", root.transform);
		GameObject opened = Child("OpenedDetails", root.transform);
		GameObject emptied = Child("EmptiedDetails", root.transform);

		/* Schauseite -z: Schnalle unter ClosedDetails, Baender an der Wurzel (immer sichtbar) */
		GameObject clasp = MeshObject("IdentityClasp", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.16f, spec.Size.y * 0.3f, spec.Size.z * 0.1f), 0.8f, metall)), material, closed.transform);
		clasp.transform.localPosition = new Vector3(0f, spec.Size.y * 0.4f, (0f - spec.Size.z) * 0.5f);
		for (int index = 0; index < spec.Bands; index++)
		{
			float x = ((spec.Bands == 1) ? 0f : Mathf.Lerp((0f - spec.Size.x) * 0.38f, spec.Size.x * 0.38f, (float)index / (float)(spec.Bands - 1)));
			GameObject band = MeshObject($"ForgedBand_{index + 1}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.07f, spec.Size.y * 0.98f, spec.Size.z * 1.06f), 0.98f, metall)), material, root.transform);
			band.transform.localPosition = new Vector3(x, 0f, 0f);
		}

		/* Kamm: Spec.Crest Pyramidenzaehne auf der Deckelfront; Locker (Recovery) traegt sie aufrecht */
		for (int index = 0; index < spec.Crest; index++)
		{
			GameObject zahn = MeshObject($"Crest_{index + 1}", Persist(EidrenMeshFactory.TaperedBox(new Vector3(0.12f + (float)index * 0.018f, 0.2f + (float)index * 0.035f, 0.1f), 0.1f, spec.Accent)), material, closed.transform);
			zahn.transform.localPosition = new Vector3(((float)index - (float)(spec.Crest - 1) * 0.5f) * 0.19f, spec.Size.y * 0.82f, (0f - spec.Size.z) * 0.28f);
			zahn.transform.localRotation = Quaternion.Euler(spec.Locker ? 0f : (-10f), 0f, 0f);
		}

		/* Offen/Leer + Loot-Fuellstand wie bei den Weltkisten */
		GameObject interior = MeshObject("DarkInterior", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.72f, 0.035f, spec.Size.z * 0.62f), 0.97f, innen)), material, opened.transform);
		interior.transform.localPosition = Vector3.up * (spec.Size.y * 0.63f);
		GameObject lining = MeshObject("EmptyInterior", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.72f, 0.025f, spec.Size.z * 0.62f), 0.98f, futter)), material, emptied.transform);
		lining.transform.localPosition = Vector3.up * (spec.Size.y * 0.635f);
		GameObject lootFull = Child("LootFill_Full", root.transform);
		MeshObject("Gold_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.4f, spec.Size.y * 0.16f, spec.Size.z * 0.36f), 0.55f, gold)), material, lootFull.transform).transform.localPosition = new Vector3((0f - spec.Size.x) * 0.07f, spec.Size.y * 0.64f, 0f);
		MeshObject("Gold_2", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.24f, spec.Size.y * 0.1f, spec.Size.z * 0.22f), 0.5f, gold)), material, lootFull.transform).transform.localPosition = new Vector3(spec.Size.x * 0.15f, spec.Size.y * 0.64f, (0f - spec.Size.z) * 0.08f);
		GameObject lootPartial = Child("LootFill_Partial", root.transform);
		MeshObject("GoldRest_1", Persist(EidrenMeshFactory.TaperedBox(new Vector3(spec.Size.x * 0.18f, spec.Size.y * 0.06f, spec.Size.z * 0.16f), 0.55f, gold)), material, lootPartial.transform).transform.localPosition = new Vector3((0f - spec.Size.x) * 0.2f, spec.Size.y * 0.635f, spec.Size.z * 0.12f);

		BoxCollider boxCollider = root.AddComponent<BoxCollider>();
		boxCollider.size = spec.Size;
		boxCollider.center = Vector3.up * spec.Size.y * 0.5f;
		WorldChestVisual worldChestVisual = root.AddComponent<WorldChestVisual>();
		worldChestVisual.Configure(lidPivot.transform, closed, opened, emptied, null, null, lootFull, lootPartial);
		worldChestVisual.Apply(WorldChestVisualState.Closed);
		int dreiecke = 0;
		foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
		{
			dreiecke += filter.sharedMesh.triangles.Length / 3;
		}
		Debug.Log($"[StilE2] {spec.Name}: {dreiecke} Dreiecke");
		GameObject result = PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Containers/Forge/" + spec.Name + "_2D.prefab");
		Object.DestroyImmediate(root);
		return result;
	}
```

Zusätzlich: `AddCrest` löschen (im neuen `BuildPrefab` integriert); `Build()` muss vor dem Speichern den Prefab-Ordner sicherstellen — in `EnsureFolders()` ergänzen: `Folder("Assets/_Game/Prefabs", "Containers");` und `Folder("Assets/_Game/Prefabs/Containers", "Forge");`. `MeshObject`/`Child` bleiben. `BuildPrefab` behält den Rückgabetyp `void` des Bestands — im Codeblock oben deshalb die letzten zwei Zeilen ersetzen durch:

```csharp
		PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Game/Prefabs/Containers/Forge/" + spec.Name + "_2D.prefab");
		Object.DestroyImmediate(root);
	}
```

- [ ] **Step 4: Prüfnachweis** — Datei kompiliert erst mit Task 3 (Builder-Lauf erzeugt die Prefabs für den Test); hier: Quelltext-Diff dokumentieren.

---

### Task 3: Forge-Builder-Lauf, Szene neu backen, Tests grün

**Files:** keine neuen Quelldateien.

**Interfaces:**
- Consumes: Task 2. Produces: 8 Prefabs unter `Prefabs/Containers/Forge/`, neu gebackene `EidraForge.unity`; `ForgeKiste_IstImVertexfarbenStil` 8/8 grün; `V02ContainerVisualTests.ForgeContainers_AreEightUniqueStateful3DFamilies` wird voraussichtlich grün (war Baseline-Fehlschlag → zulässiger `<=`-Eintrag im Namensdiff).

- [ ] **Step 1: Builder laufen lassen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.ForgeContainerVisualBuilder.Build -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e2-t3-build.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\stil-e2-t3-build.log" -Pattern "error CS|\[StilE2\]"
```

Erwartet: acht `[StilE2] <Name>: N Dreiecke` mit N ≤ 500; acht `_2D.prefab`-Dateien existieren.

- [ ] **Step 2: EidraForge-Szene neu backen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.EidraForgeSceneBuilder.Build -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\stil-e2-t3-szene.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\stil-e2-t3-szene.log" -Pattern "error CS|Exception"
```

- [ ] **Step 3: Stil- und Containertests laufen lassen** — Filter `Eidren.Tests.EditMode.EidrenWorldStyleTests`, XML `TestResults-stil-e2-t3.xml`, Log `stil-e2-t3.log`; zusätzlich Filter `Eidren.Tests.V02ContainerVisualTests` (Namespace `Eidren.Tests`!), XML `TestResults-stil-e2-t3b.xml`. Erwartet: Stiltests komplett `Passed`; der Forge-Containertest `Passed`.

- [ ] **Step 4: Prüfnachweis** — Logs, XMLs, Dreieckszahlen, Prefab-Listing.

---

### Task 4: Stationen auf die Fabrik umstellen — **ENTFALLEN (Auftraggeber-Entscheid 07.08.2026)**

> Bei der Umsetzung zeigte sich: Die drei Stations-Prefabs tragen seit dem
> 05.08.2026 handgebaute Geometrie (`Geometry_A14`); der Wächter im Builder
> schützt sie zu Recht. Entscheid: Geometrie bleibt, Stationen sind aus dem
> Stilumbau gestrichen. Die Task-4-Codeänderungen wurden vollständig
> zurückgebaut; die Vorher-Studio-Captures der Stationen bleiben als
> Dokumentation des handgebauten Standes erhalten. Der ursprüngliche
> Taskinhalt bleibt unten als Nachweis stehen, wird aber nicht ausgeführt.

**Files:**
- Modify: `Assets/_Game/Editor/EidrenWorldStyleAssets.cs` (Helfer ergänzen)
- Modify: `Assets/_Game/Editor/StorageContentBuilder.cs` (StorageChest + DeathBag)
- Modify: `Assets/_Game/Editor/CraftingContentBuilder.cs` (Workbench)
- Test: `Assets/_Game/Editor/Tests/EidrenWorldStyleTests.cs` (Ergänzung)

**Interfaces:**
- Consumes: Fabrik + Material (E1). Produces: gemeinsamer Helfer

```csharp
	/// <summary>Ersetzt einen VisualCube: Kastenoptik im Vertexfarben-Stil, mittig auf localPosition wie ein Primitive-Cube.</summary>
	public static GameObject StyledBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color farbe, string meshRoot)
```

(legt ein GameObject an, MeshFilter mit `EidrenMeshFactory.TaperedBox(localScale, 1f, farbe)` persistiert unter `meshRoot` mit Sequenzzähler je Aufrufdatei, MeshRenderer mit `EnsureWorldMaterial()`, `localPosition = localPosition - Vector3.up * localScale.y * 0.5f` — Fabrikbasis liegt auf y=0, der alte Cube war mittig; KEIN Collider, die alten `VisualCube` zerstörten ihre Collider ebenfalls).

- [ ] **Step 1: Failing Test ergänzen** — in `EidrenWorldStyleTests.cs`:

```csharp
		[TestCase("Assets/_Game/Prefabs/Stations/StorageChest.prefab")]
		[TestCase("Assets/_Game/Prefabs/Stations/Workbench.prefab")]
		[TestCase("Assets/_Game/Resources/Prefabs/DeathBag.prefab")]
		public void Station_IstImVertexfarbenStil(string pfad)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
			Assert.That(prefab, Is.Not.Null, pfad);
			int dreiecke = 0;
			MeshFilter[] filterListe = prefab.GetComponentsInChildren<MeshFilter>(includeInactive: true);
			Assert.That(filterListe.Length, Is.GreaterThan(0), pfad + " ohne Meshes");
			foreach (MeshFilter filter in filterListe)
			{
				Assert.That(filter.sharedMesh.colors.Length, Is.EqualTo(filter.sharedMesh.vertexCount),
					pfad + "/" + filter.name + " ohne Vertexfarben");
				dreiecke += filter.sharedMesh.triangles.Length / 3;
			}
			Assert.That(dreiecke, Is.LessThanOrEqualTo(400), pfad + " ueberschreitet den Dreieckskorridor");
			foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true))
			{
				Assert.That(renderer.sharedMaterial.name, Is.EqualTo("M_EidrenWorld_VertexLit"),
					pfad + "/" + renderer.name + " nutzt nicht das gemeinsame Material");
			}
		}
```

- [ ] **Step 2: Testlauf — muss scheitern** (alte Cubes ohne Vertexfarben): XML `TestResults-stil-e2-t4a.xml`, Log `stil-e2-t4a.log`.

- [ ] **Step 3: Helfer implementieren** — in `EidrenWorldStyleAssets`:

```csharp
		private static readonly Dictionary<string, int> Sequenzen = new Dictionary<string, int>();

		public static void ResetSequenz(string meshRoot)
		{
			Sequenzen[meshRoot] = 0;
		}

		public static GameObject StyledBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color farbe, string meshRoot)
		{
			if (!AssetDatabase.IsValidFolder(meshRoot))
			{
				string eltern = System.IO.Path.GetDirectoryName(meshRoot)?.Replace('\\', '/');
				AssetDatabase.CreateFolder(eltern, System.IO.Path.GetFileName(meshRoot));
			}
			if (!Sequenzen.TryGetValue(meshRoot, out int sequenz))
			{
				sequenz = 0;
			}
			Mesh mesh = EidrenMeshFactory.TaperedBox(localScale, 1f, farbe);
			string pfad = string.Format("{0}/Station_{1:000}_{2}.asset", meshRoot, sequenz, name);
			Sequenzen[meshRoot] = sequenz + 1;
			Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(pfad);
			if (existing != null)
			{
				EditorUtility.CopySerialized(mesh, existing);
				Object.DestroyImmediate(mesh);
				EditorUtility.SetDirty(existing);
				mesh = existing;
			}
			else
			{
				AssetDatabase.CreateAsset(mesh, pfad);
			}
			GameObject teil = new GameObject(name);
			teil.transform.SetParent(parent, worldPositionStays: false);
			teil.transform.localPosition = localPosition - Vector3.up * (localScale.y * 0.5f);
			teil.AddComponent<MeshFilter>().sharedMesh = mesh;
			teil.AddComponent<MeshRenderer>().sharedMaterial = EnsureWorldMaterial();
			return teil;
		}
```

(`using System.Collections.Generic;` und `using UnityEngine;` prüfen/ergänzen.)

- [ ] **Step 4: Builder umstellen** — in `StorageContentBuilder.BuildStorageChestPrefab`: vor dem ersten Teil `EidrenWorldStyleAssets.ResetSequenz("Assets/_Game/Art/Stations/Meshes");` dann die vier `VisualCube(...)`-Zeilen ersetzen durch `EidrenWorldStyleAssets.StyledBlock(root.transform, "ChestBody", new Vector3(0f, 0.55f, 0f), new Vector3(2.25f, 1.05f, 1.45f), wood.color, "Assets/_Game/Art/Stations/Meshes");` usw. für `ChestLid`, `FrontBand`, `Latch` (Positionen/Skalen unverändert aus dem Bestand; Farbe = `wood.color` bzw. `copper.color`). In `BuildDeathBag` analog (`Sack`, `Knot` mit `fiber.color`). In `CraftingContentBuilder` analog für `WorkSurface`, `LegLeft`, `LegRight`, `ToolRail` — dort ebenfalls zuerst `ResetSequenz`. Die lokalen `VisualCube`-Methoden löschen, wenn sie danach unbenutzt sind (in beiden Dateien prüfen). Der `Geometry_A14`-Wächter und alle `StorageContainer.Configure`-Aufrufe bleiben unverändert.

- [ ] **Step 5: Builder-Läufe** — beide Builder headless ausführen: `-executeMethod Eidren.Editor.StorageContentBuilder.BuildAll` (Log `stil-e2-t4-storage.log`) und `-executeMethod Eidren.Editor.CraftingContentBuilder.BuildAll` (Log `stil-e2-t4-crafting.log`); Achtung: der `Geometry_A14`-Wächter überspringt den Neubau NICHT (die alten Cube-Prefabs tragen kein `Geometry_A14`-Kind), das ist erwartet. Danach Stiltest-Lauf: Filter `Eidren.Tests.EditMode.EidrenWorldStyleTests`, XML `TestResults-stil-e2-t4.xml`, Log `stil-e2-t4.log`. Erwartet: alle Stiltests `Passed`.

- [ ] **Step 6: Prüfnachweis** — Logs, XML, Prefab-Zeitstempel.

---

### Task 5: Nachher-Captures, Namensdiff, Abnahme

**Files:** keine Quellcode-Änderung; erzeugt `Documentation/Etappen/Stilumbau/STILUMBAU_E2_ABNAHME.md`.

**Interfaces:**
- Consumes: Vorher-PNGs (Task 1), Baseline (Task 0), alle Umstellungen (Task 2–4).

- [ ] **Step 1: Nachher-Captures** — nur `CaptureForgeNachher` (ohne `-nographics`; Log `stil-e2-t5-forge.log`). Erwartet: 16 PNGs. (Stationen entfallen per Task-4-Entscheid; ihre Vorher-Bilder dokumentieren den handgebauten Bestand.)
- [ ] **Step 2: Volle EditMode-Suite + Namensdiff** — wie E1-Task 7 Step 2, gegen `TestResults-stil-e2-baseline.xml`, Ergebnis `TestResults-stil-e2-final.xml`, Log `stil-e2-final.log`. Erwartet: keine `=>`-Einträge; `<=`-Einträge (z. B. `V02ContainerVisualTests`) auflisten.
- [ ] **Step 3: Bildpaare begutachten** — je Gruppe mindestens 6 Paare ansehen. Kriterien: (1) kein leeres Bild; (2) Segmentierung erkennbar (Sockel/Planken/Bogendeckel bei Kisten, Beine/Platte/Werkzeugschiene bei der Werkbank); (3) die acht Forge-Familien bleiben unterscheidbar (Bänder-/Kammzahl, Größe, Farbwelt aus `Specs()`); (4) Forge-Kisten zeigen offen den Loot; (5) Farbwelt entspricht den Vorher-Bildern.
- [ ] **Step 4: Abnahmebericht** — `STILUMBAU_E2_ABNAHME.md` nach dem Muster von E1: Kriterien ✓/✗ mit Bildverweisen, Dreieckszahlen aller 11 Objekte, Namensdiff, Assetliste, offene Punkte. Freigabe liegt beim Auftraggeber.

---

## Selbstreview-Vermerk

- Spec-Abdeckung Etappe 2: Forge-Kisten (Task 2/3), Stationen (Task 4), fehlende Prefabs + Szenen-Rebake (Task 3), Loot-Füllstand Forge (Task 2; alle Familien erhalten die Knoten — ob das Spiel `PartiallyEmptied` für Forge-Kisten je setzt, entscheidet die Laufzeit; es wird KEIN neuer Zustand erfunden, gemäß Entwurf), Captures/Abnahme (Task 1/5), Sicherung/Baseline (Task 0).
- Bestandsverträge stehen wörtlich in den Globalen Vorgaben (V02-Testerwartungen, Collider, LidPivot, `_2D`-Pfadnamen, Stationen-Teilnamen, `Geometry_A14`-Wächter).
- Typkonsistenz: geschachtelte `EidrenMeshFactory.LoftProfile`-Verwendung wie in E1 Task 6; `StyledBlock`-Signatur identisch in Definition (Task 4 Step 3) und Aufrufen (Step 4); `Configure`-8-Arg mit `null`-Händen entspricht der E1-Signatur.
- Bekannte Abweichung, bewusst: Die Forge-Kämme (`Crest_N`) werden Pyramidenzähne (stark getaperte Kästen) statt der alten Mittelfirst-Keile — die Fabrik-`Wedge` hat die Firstkante hinten, nicht mittig; ein Sonderprimitiv dafür wäre YAGNI. Im Abnahmebericht ausweisen.
