# Wanderer 3D — Einbau-Implementierungsplan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Ziel:** Die Spielfigur rendert als Low-Poly-3D-Modell aus `Wanderer.glb` (Rüstungs-Slotmeshes, materialneutrale Waffenmeshes, 12 Clips); Gegner bleiben Sprites, laufen aber über dieselben Schnittstellen.

**Architektur:** Schnittstellen-Refactoring löst fünf Konsumenten vom konkreten `SpriteActorPresentation`. Neue `MeshActorPresentation` (Legacy-`Animation`-Komponente, flüssige Drehung, prozedurale Hit/Stagger/Death/Appear) plus `WandererEquipmentVisual` (WeaponData→Stance) ersetzen am Spieler Sprite-Billboard und `PlayerWeaponVisual`. Ein Editor-Builder baut `Player.prefab` um.

**Tech Stack:** Unity 6000.3 (lokal unter `.unity-editor/`), URP 17.3 (Gamma + LDR), glTFast `com.unity.cloud.gltfast`, NUnit (EditMode in `Assets/_Game/Editor/Tests/`, PlayMode in `Assets/_Game/Tests/PlayMode/`).

## Globale Regeln

- Projektpfad: `C:\Users\phine\Documents\Projekt Eidren` (Leerzeichen! Immer quoten, Aufrufoperator `&` verwenden, nie `Start-Process`).
- Unity headless: `& ".\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" …` — **Exit-Code lügt**: Erfolg nur, wenn das `-logFile` frei von `error CS` ist bzw. die Ergebnis-XML existiert und `result="Passed"` meldet. Nach `&` immer `Wait-Process -Name Unity -Timeout 900` plus ein paar Sekunden Puffer.
- Vor JEDEM Unity-Aufruf: `Test-Path "Library\UnityLockfile"` — wenn vorhanden, läuft der parallele codex-Agent oder ein Restlauf. Warten, **niemals löschen**.
- Testläufe im Hintergrund starten; auf `Test run completed` im Log UND Existenz der XML warten, nicht auf die Task-Notification.
- Nach PlayMode-Läufen `Assets/InitTestScene*.unity` löschen.
- **Kein `git commit` für Quelldateien** — das Repo trackt nur die 5 Release-Dateien (`.gitignore` = `*`). Beleg je Task sind Logdatei + Ergebnis-XML mit dem Präfix `w3d-…`.
- Code-Sprache: deutsche Kommentare/Doku-Kommentare, Tabs als Einrückung, `sealed`-Klassen — wie im Bestand.
- EditMode-Tests: `Object.Instantiate` ruft kein `Awake` — Komponenten im Test immer per `Configure(...)` verdrahten oder Getter träge auflösen.
- Namespace-Falle EditMode: `Eidren.Interaction.InteractionMode` kollidiert mit `UnityEditor.InteractionMode` → voll qualifizieren.

---

### Task 0: Sicherung und Vorflug

**Files:** keine Quellcode-Änderung.

**Interfaces:** —

- [ ] **Step 1: Quellstand sichern** (Quellcode ist NICHT versioniert — einzige Absicherung gegen Fehlschläge)

```powershell
$stamp = Get-Date -Format "yyyyMMdd-HHmm"
$ziel = "C:\Users\phine\Documents\Eidren-Sicherungen\vor-wanderer3d-$stamp"
New-Item -ItemType Directory -Force $ziel | Out-Null
Copy-Item "C:\Users\phine\Documents\Projekt Eidren\Assets\_Game" "$ziel\_Game" -Recurse
Copy-Item "C:\Users\phine\Documents\Projekt Eidren\Packages\manifest.json" "$ziel\manifest.json"
Get-ChildItem $ziel | Select-Object Name
```

Erwartet: Ordner `_Game` und `manifest.json` liegen im Sicherungsordner.

- [ ] **Step 2: Lockfile prüfen**

```powershell
Test-Path "C:\Users\phine\Documents\Projekt Eidren\Library\UnityLockfile"
```

Erwartet: `False`. Bei `True`: warten bis frei (parallelen Agent nicht abschießen).

---

### Task 1: glTFast einbinden, GLB-Import verifizieren

**Files:**
- Modify: `Packages/manifest.json`
- Create: `Assets/_Game/Editor/Wanderer3DImportDiagnose.cs`
- Test: `Assets/_Game/Editor/Tests/Wanderer3DAssetTests.cs`

**Interfaces:**
- Produces: importierte GLB unter `Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb` mit 16 Knoten-GameObjects und 12 `AnimationClip`-Subassets (Legacy), Namen exakt wie in `README_Wanderer3D.md`. Task 4/6 verlassen sich auf diese Namen.

- [ ] **Step 1: Paket eintragen** — in `Packages/manifest.json` alphabetisch nach `com.unity.burst` einfügen (Einrückung der Datei übernehmen):

```json
"com.unity.cloud.gltfast":  "6.8.0",
```

- [ ] **Step 2: Diagnose-Editor-Skript anlegen** — `Assets/_Game/Editor/Wanderer3DImportDiagnose.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Protokolliert die Subassets der importierten Wanderer.glb, damit sich
	/// Mesh-/Clip-Namen und der Legacy-Status ohne GUI pruefen lassen.
	/// </summary>
	public static class Wanderer3DImportDiagnose
	{
		public const string GlbPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb";

		[MenuItem("Eidren/V0.2/Wanderer3D Importdiagnose")]
		public static void Run()
		{
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath(GlbPath);
			Debug.Log($"[W3D] Subassets gesamt: {assets.Length}");
			foreach (Object asset in assets)
			{
				string extra = string.Empty;
				if (asset is AnimationClip clip)
				{
					extra = $" legacy={clip.legacy} laenge={clip.length:F2}s loop={clip.isLooping}";
				}
				Debug.Log($"[W3D] {asset.GetType().Name}: {asset.name}{extra}");
			}
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
			Debug.Log("[W3D] Wurzel: " + (root != null ? root.name : "FEHLT"));
			if (root != null)
			{
				Animation animation = root.GetComponentInChildren<Animation>(true);
				Debug.Log("[W3D] Animation-Komponente: " + (animation != null ? "vorhanden" : "FEHLT"));
			}
		}
	}
}
```

- [ ] **Step 3: Import + Diagnose laufen lassen** (Hintergrund; erster Lauf lädt das Paket — Internet nötig):

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.Wanderer3DImportDiagnose.Run -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\w3d-t1-import.log"
Wait-Process -Name Unity -Timeout 1200 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\w3d-t1-import.log" -Pattern "error CS|\[W3D\]|Failed to resolve"
```

Erwartet: keine `error CS`/Resolve-Fehler; 16 Mesh-Knoten und 12 Clips gelistet. Notieren, ob `legacy=True` (erwartet bei glTFast-Standard) und ob die Wurzel eine `Animation`-Komponente trägt — Task 6 richtet sich danach.

- [ ] **Step 4: Fehlenden Test schreiben** — `Assets/_Game/Editor/Tests/Wanderer3DAssetTests.cs`:

```csharp
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
	public sealed class Wanderer3DAssetTests
	{
		private const string GlbPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb";

		private static readonly string[] ExpectedNodes = new string[16]
		{
			"Basis",
			"Helm_Stoff", "Helm_Kupfer", "Helm_Eisen",
			"Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
			"Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
			"Haende_Stoff", "Haende_Kupfer", "Haende_Eisen",
			"Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer"
		};

		private static readonly string[] ExpectedClips = new string[12]
		{
			"Ruhe_Speer", "Ruhe_Dolche", "Ruhe_Hammer",
			"Gehen_Speer", "Gehen_Dolche", "Gehen_Hammer",
			"Laufen_Speer", "Laufen_Dolche", "Laufen_Hammer",
			"Angriff_Speer", "Angriff_Dolche", "Angriff_Hammer"
		};

		[Test]
		public void Glb_EnthaeltAlleSlotUndWaffenKnoten()
		{
			GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
			Assert.That(root, Is.Not.Null, "GLB nicht importiert: " + GlbPath);
			foreach (string node in ExpectedNodes)
			{
				Transform child = FindDeep(root.transform, node);
				Assert.That(child, Is.Not.Null, "Knoten fehlt: " + node);
				Assert.That(child.GetComponentInChildren<SkinnedMeshRenderer>(true), Is.Not.Null,
					"Knoten ohne SkinnedMeshRenderer: " + node);
			}
		}

		[Test]
		public void Glb_EnthaeltAlleZwoelfClips()
		{
			var clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath).OfType<AnimationClip>()
				.Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
			foreach (string expected in ExpectedClips)
			{
				Assert.That(clips.Any(clip => clip.name == expected), Is.True, "Clip fehlt: " + expected);
			}
			Assert.That(clips.Length, Is.EqualTo(12), "Unerwartete Clip-Anzahl");
		}

		private static Transform FindDeep(Transform root, string childName)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
			{
				if (child.name == childName)
				{
					return child;
				}
			}
			return null;
		}
	}
}
```

- [ ] **Step 5: Test gezielt laufen lassen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.Wanderer3DAssetTests" -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t1.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\w3d-t1-tests.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t1.xml" -Pattern 'result="Passed"' | Select-Object -First 1
```

Erwartet: beide Tests `Passed`. Falls Knotennamen abweichen (glTFast kann Namen anpassen): Diagnose-Log aus Step 3 heranziehen, `ExpectedNodes` **nicht** ändern, sondern erst klären, warum die GLB abweicht (Quelle ist `Source~/export_gltf.js`).

---

### Task 2: URP-Shader für Vertexfarben

**Files:**
- Create: `Assets/_Game/Shaders/Actors/WandererVertexLit.shader`
- Test: `Assets/_Game/Editor/Tests/Wanderer3DAssetTests.cs` (Ergänzung)

**Interfaces:**
- Produces: Shader `"Eidren/Actors/WandererVertexLit"` mit Property `_Tint` (Color, Standard weiß). Task 4 setzt `_Tint` per `MaterialPropertyBlock`; Task 6 erzeugt daraus `M_Wanderer_VertexLit.mat`.

- [ ] **Step 1: Failing Test ergänzen** — in `Wanderer3DAssetTests.cs` anhängen:

```csharp
		[Test]
		public void WandererShader_ExistiertUndHatTintProperty()
		{
			Shader shader = Shader.Find("Eidren/Actors/WandererVertexLit");
			Assert.That(shader, Is.Not.Null, "Shader Eidren/Actors/WandererVertexLit fehlt");
			var material = new Material(shader);
			Assert.That(material.HasProperty("_Tint"), Is.True, "_Tint-Property fehlt");
			Object.DestroyImmediate(material);
		}
```

- [ ] **Step 2: Shader schreiben** — `Assets/_Game/Shaders/Actors/WandererVertexLit.shader`. Beleuchtung bewusst schlicht: Hauptlicht (mit Schattenempfang) + Ambient-SH, Gamma+LDR-konform, kein Tonemapping. Schattenwurf/Depth kommen per `UsePass` aus dem URP-Lit:

```hlsl
Shader "Eidren/Actors/WandererVertexLit"
{
	// Vertexfarben-Shader fuer die Wanderer3D-Figur. AO und Augenleuchten sind
	// in den Vertexfarben eingebacken; das Flat Shading kommt aus den Normalen.
	Properties
	{
		_Tint ("Tint", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				half4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				half4 color : COLOR;
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _Tint;
			CBUFFER_END

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.color = input.color;
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half3 albedo = input.color.rgb * _Tint.rgb;
				float3 normalWS = normalize(input.normalWS);
				float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				Light mainLight = GetMainLight(shadowCoord);
				half3 licht = mainLight.color * mainLight.shadowAttenuation
					* saturate(dot(normalWS, mainLight.direction));
				licht += SampleSH(normalWS);
				#if defined(_ADDITIONAL_LIGHTS)
				uint count = GetAdditionalLightsCount();
				for (uint index = 0u; index < count; index++)
				{
					Light zusatz = GetAdditionalLight(index, input.positionWS);
					licht += zusatz.color * zusatz.distanceAttenuation
						* saturate(dot(normalWS, zusatz.direction));
				}
				#endif
				return half4(albedo * licht, 1.0h);
			}
			ENDHLSL
		}

		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
		UsePass "Universal Render Pipeline/Lit/DepthOnly"
	}
	FallBack Off
}
```

- [ ] **Step 3: Kompilieren + Test laufen lassen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.Wanderer3DAssetTests" -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t2.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\w3d-t2-tests.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\w3d-t2-tests.log" -Pattern "error CS|Shader error"
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t2.xml" -Pattern 'result="Passed"' | Select-Object -First 1
```

Erwartet: keine Shader-/Compilerfehler, alle Tests `Passed`. Falls `UsePass` am Namen scheitert (URP-Versionstext), exakten Passnamen via `Shader.Find("Universal Render Pipeline/Lit")` im Diagnose-Log prüfen.

---

### Task 3: Schnittstellen-Refactoring (2D bleibt grün)

**Files:**
- Create: `Assets/_Game/Scripts/Presentation/ILocomotionPresentation.cs`
- Create: `Assets/_Game/Scripts/Presentation/IAuthoredStatePresentation.cs`
- Create: `Assets/_Game/Scripts/Presentation/IArmorPresentation.cs`
- Create: `Assets/_Game/Scripts/Presentation/ActorPresentationRegistry.cs`
- Create: `Assets/_Game/Scripts/Combat/IPlayerWeaponPresentation.cs`
- Modify: `Assets/_Game/Scripts/Presentation/IActorPresentation.cs`
- Modify: `Assets/_Game/Scripts/Presentation/SpriteActorPresentation.Atlas.cs`
- Modify: `Assets/_Game/Scripts/Presentation/StaticSpriteActorPresentation.cs`
- Modify: `Assets/_Game/Scripts/Presentation/ActorOcclusionTransparency.cs`
- Modify: `Assets/_Game/Scripts/Gameplay/Presentation/PlayerVisualAnimator.cs`
- Modify: `Assets/_Game/Scripts/Gameplay/Presentation/SpriteActorAnimator.cs`
- Modify: `Assets/_Game/Scripts/Combat/PlayerWeaponVisual.cs`
- Modify: `Assets/_Game/Scripts/Combat/PlayerCombatController.cs:422-449`
- Modify: `Assets/_Game/Scripts/Composition/PlayerPrefabBindings.cs`
- Modify: `Assets/_Game/Editor/EidrenSceneStructureBuilder.cs` (Aufrufer von `ConfigurePrefabReferences`)

**Interfaces:**
- Produces (Task 4 implementiert sie für 3D, Task 5/6 verdrahten sie):

```csharp
// IActorPresentation erhaelt zusaetzlich:
float WorldHeight { get; }

public interface ILocomotionPresentation
{
	void SetLocomotion(Vector3 worldDirection, bool moving);
}

public interface IAuthoredStatePresentation
{
	void SetAuthoredState(string stem, float normalizedTime = 0f, bool loop = false, bool restart = false);
	/// <summary>Zeitgebundener Zustand: 3D skaliert den Clip auf die Dauer, 2D spielt wie bisher.</summary>
	void SetAuthoredTimedState(string stem, float durationSeconds);
}

public interface IArmorPresentation
{
	void SetAssetVariant(string actorId);
	void SetArmorParts(bool head, bool chest, bool hands, bool legs);
	void SetArmorTiers(int head, int chest, int hands, int legs);
}

// Eidren.Combat:
public interface IPlayerWeaponPresentation
{
	void Show(WeaponData weapon);
	void PlayAttack(float duration, int comboIndex, WeaponFamily family);
}

public static class ActorPresentationRegistry
{
	public static void Register(MonoBehaviour presentation);   // nur MonoBehaviours, die IActorPresentation implementieren
	public static void Unregister(MonoBehaviour presentation);
	public static IReadOnlyList<MonoBehaviour> Active { get; }
}
```

- [ ] **Step 1: Schnittstellendateien anlegen** — vier neue Dateien in `Assets/_Game/Scripts/Presentation/` mit exakt den Signaturen aus dem Interfaces-Block (Namespace `Eidren.Presentation`, deutsche Doku-Kommentare). `ActorPresentationRegistry`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// Aktive Akteur-Darstellungen (Sprite wie Mesh) melden sich hier an,
	/// damit Querschnittssysteme (z. B. Verdeckungs-Transparenz) sie ohne
	/// teure Szenensuche und ohne konkreten Typ finden.
	/// </summary>
	public static class ActorPresentationRegistry
	{
		private static readonly List<MonoBehaviour> Entries = new List<MonoBehaviour>();

		public static IReadOnlyList<MonoBehaviour> Active => Entries;

		public static void Register(MonoBehaviour presentation)
		{
			if (presentation is IActorPresentation && !Entries.Contains(presentation))
			{
				Entries.Add(presentation);
			}
		}

		public static void Unregister(MonoBehaviour presentation)
		{
			Entries.Remove(presentation);
		}
	}
}
```

- [ ] **Step 2: `IActorPresentation` erweitern** — Member `float WorldHeight { get; }` mit Doku-Kommentar (Weltgröße der Figur in Metern, für Verdeckungs-Raycasts) ergänzen.

- [ ] **Step 3: `SpriteActorPresentation` anpassen** — Klassenzeile:

```csharp
public sealed class SpriteActorPresentation : MonoBehaviour, IActorPresentation, ILocomotionPresentation, IAuthoredStatePresentation, IArmorPresentation
```

`WorldHeight`, `SetLocomotion`, `SetAuthoredState`, `SetAssetVariant`, `SetArmorParts`, `SetArmorTiers` existieren bereits — nur die neue Methode ergänzen (bei den anderen `SetAuthoredState`-Überladungen einsortieren):

```csharp
		public void SetAuthoredTimedState(string stem, float durationSeconds)
		{
			// 2D kennt keine Zeitskalierung: Atlas-Timing bleibt massgeblich.
			SetAuthoredState(stem, 0f, loop: false, restart: true);
		}
```

In `OnEnable`/`OnDisable` (falls nicht vorhanden: anlegen) `ActorPresentationRegistry.Register(this)` / `Unregister(this)` aufrufen; bestehenden `OnEnable`-Inhalt beibehalten.

- [ ] **Step 4: `StaticSpriteActorPresentation` anpassen** — `public float WorldHeight => 2f;` ergänzen (bisheriger Occlusion-Standard für statische Akteure) plus Registry-An-/Abmeldung in `OnEnable`/`OnDisable`.

- [ ] **Step 5: `ActorOcclusionTransparency` auf Registry umstellen** — Feld `_actors` wird `MonoBehaviour[]`; `RefreshOccluders` ersetzt die `FindObjectsByType`-Zeile:

```csharp
			_actors = System.Linq.Enumerable.ToArray(ActorPresentationRegistry.Active);
			foreach (MonoBehaviour actor in _actors)
			{
				if (actor != null && actor.isActiveAndEnabled)
				{
					CollectOccluders(actor);
				}
			}
```

`CollectOccluders(MonoBehaviour actor)` nutzt `((IActorPresentation)actor).WorldHeight` statt `actor.WorldHeight`; `IsActorHierarchy` iteriert über `MonoBehaviour`-Einträge (`actor.transform.root` unverändert).

- [ ] **Step 6: `PlayerVisualAnimator` entkoppeln** — Feld `_spritePresentation` ersetzen durch:

```csharp
		private IActorPresentation _presentation;
		private ILocomotionPresentation _locomotion;
		private IAuthoredStatePresentation _authored;
		private IArmorPresentation _armor;
```

`InitializeExploration`: `_presentation = ActorPresentationLocator.Find(base.gameObject); _locomotion = _presentation as ILocomotionPresentation; _authored = _presentation as IAuthoredStatePresentation; _armor = _presentation as IArmorPresentation;` — der Idle-Startaufruf läuft über `_authored?.SetAuthoredState("idle", 0f, loop: true, restart: true)`. Alle übrigen `_spritePresentation.`-Stellen mechanisch ersetzen: `SetLocomotion`→`_locomotion?`, `SetFacing`→`_presentation`, `SetAuthoredState`→`_authored?`, `SetAssetVariant`/`SetArmorParts`/`SetArmorTiers`→`_armor?`. `PlaySpriteLocked` umbenennen in `PlayLockedState` und auf `_authored?.SetAuthoredTimedState(stem, Mathf.Max(0.05f, duration))` umstellen (Lock-Timer bleibt). `BindInteraction`: vierter Parameter wird `_presentation != null`. Die Waffen-Suche `GetComponentInChildren<PlayerWeaponVisual>` wird `GetComponentInChildren<MonoBehaviour>`-frei: `base.transform.root.GetComponentInChildren<IPlayerWeaponPresentation>(true)?.PlayAttack(...)` — dafür `using Eidren.Combat;` ergänzen (besteht bereits).

- [ ] **Step 7: `SpriteActorAnimator` entkoppeln** — die drei `is SpriteActorPresentation spriteActorPresentation`-Muster werden `is IAuthoredStatePresentation authored` mit Aufrufen `authored.SetAuthoredState(...)`; `SetLocomotion` läuft über `_presentation as ILocomotionPresentation`. Verhalten unverändert.

- [ ] **Step 8: Waffen-Schnittstelle** — `Assets/_Game/Scripts/Combat/IPlayerWeaponPresentation.cs` mit der Signatur aus dem Interfaces-Block (Namespace `Eidren.Combat`, `using Eidren.Data;`). `PlayerWeaponVisual` deklariert sie (Methoden existieren). In `PlayerCombatController.RefreshWeaponVisual()` (Zeile 424) den Typ tauschen:

```csharp
			IPlayerWeaponPresentation playerWeaponVisual = ((_weaponVisual != null) ? _weaponVisual.GetComponent<IPlayerWeaponPresentation>() : null);
```

(Der spätere `GetComponent<Renderer>`-Zweig bleibt unverändert — am leeren 3D-WeaponDriver liefert er `null` und wird bereits abgesichert.)

- [ ] **Step 9: `PlayerPrefabBindings` verbreitern** — Feld und Property:

```csharp
		[SerializeField]
		private MonoBehaviour playerVisual;   // muss IActorPresentation implementieren

		public IActorPresentation PlayerVisual => playerVisual as IActorPresentation;
```

`ConfigurePrefabReferences(..., SpriteActorPresentation visual, ...)` → Parametertyp `MonoBehaviour visual`. Aufrufer in `EidrenSceneStructureBuilder` kompiliert dadurch weiter (übergibt konkrete Komponente). Falls `ValidateReferences` `playerVisual` prüft: Prüfung auf `PlayerVisual != null` umstellen.

- [ ] **Step 10: `PlayerPrefabTests` minimal nachziehen** — Zeile 51 (`Is.SameAs(art.GetComponentInChildren<SpriteActorPresentation>(...))`) wird:

```csharp
		Assert.That(component.PlayerVisual, Is.SameAs((object)art.GetComponentInChildren<SpriteActorPresentation>(includeInactive: true)));
```

(Der große 3D-Umbau der Tests folgt in Task 6 — hier nur grün halten.)

- [ ] **Step 11: Volle EditMode-Suite laufen lassen** (Refactoring darf nichts brechen):

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform EditMode -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t3.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\w3d-t3-tests.log"
Wait-Process -Name Unity -Timeout 1200 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\w3d-t3-tests.log" -Pattern "error CS"
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t3.xml" -Pattern 'failed="0"' | Select-Object -First 1
```

Erwartet: keine Compile-Fehler, 0 Fehlschläge.

---

### Task 4: MeshActorPresentation

**Files:**
- Create: `Assets/_Game/Scripts/Presentation/MeshActorPresentation.cs`
- Test: `Assets/_Game/Editor/Tests/MeshActorPresentationTests.cs`

**Interfaces:**
- Consumes: Schnittstellen aus Task 3; Clip-/Knotennamen aus Task 1.
- Produces (Task 5/6 rufen sie):

```csharp
public sealed class MeshActorPresentation : MonoBehaviour, IActorPresentation, ILocomotionPresentation, IAuthoredStatePresentation, IArmorPresentation
{
	public const string StanceSpear = "Speer";
	public const string StanceDaggers = "Dolche";
	public const string StanceHammer = "Hammer";
	public void Configure(Animation configuredAnimation, Transform configuredModelRoot, float configuredWorldHeight = 1.8f);
	public void SetWeaponStance(string stance, bool showWeaponMesh);
	public static string ResolveClipName(string stem, string stance);   // null => prozedural
	public static string ArmorMeshName(int slotIndex, int tier);        // slotIndex: 0 Kopf, 1 Brust, 2 Haende, 3 Beine; tier 0..3, ausserhalb => null
	public static float FacingToYaw(ActorFacing8 facing);               // (int)facing * 45 Grad, N = +Z wie EightDirectionResolver
}
```

- [ ] **Step 1: Failing Tests schreiben** — `Assets/_Game/Editor/Tests/MeshActorPresentationTests.cs` (reine Mapping-Logik, kein Unity-Lebenszyklus nötig):

```csharp
using Eidren.Presentation;
using NUnit.Framework;

namespace Eidren.Tests.EditMode
{
	public sealed class MeshActorPresentationTests
	{
		[TestCase("idle", "Dolche", "Ruhe_Dolche")]
		[TestCase("move", "Speer", "Laufen_Speer")]
		[TestCase("hammer1", "Hammer", "Angriff_Hammer")]
		[TestCase("hammer3", "Hammer", "Angriff_Hammer")]
		[TestCase("dagger4", "Dolche", "Angriff_Dolche")]
		[TestCase("spear2", "Speer", "Angriff_Speer")]
		public void ResolveClipName_LiefertClipFuerBekannteStems(string stem, string stance, string expected)
		{
			Assert.That(MeshActorPresentation.ResolveClipName(stem, stance), Is.EqualTo(expected));
		}

		[TestCase("hit")]
		[TestCase("death")]
		[TestCase("dodge")]
		[TestCase("harvest")]
		[TestCase("appear")]
		[TestCase("")]
		public void ResolveClipName_LiefertNullFuerProzeduraleStems(string stem)
		{
			Assert.That(MeshActorPresentation.ResolveClipName(stem, MeshActorPresentation.StanceDaggers), Is.Null);
		}

		[TestCase(0, 1, "Helm_Stoff")]
		[TestCase(1, 2, "Harnisch_Kupfer")]
		[TestCase(2, 3, "Haende_Eisen")]
		[TestCase(3, 1, "Beine_Stoff")]
		[TestCase(0, 0, null)]
		[TestCase(2, 4, null)]
		public void ArmorMeshName_BildetSlotUndStufeAufKnotennamenAb(int slot, int tier, string expected)
		{
			Assert.That(MeshActorPresentation.ArmorMeshName(slot, tier), Is.EqualTo(expected));
		}

		[Test]
		public void FacingToYaw_NordIstNullGradUndSuedIst180()
		{
			Assert.That(MeshActorPresentation.FacingToYaw(ActorFacing8.N), Is.EqualTo(0f));
			Assert.That(MeshActorPresentation.FacingToYaw(ActorFacing8.E), Is.EqualTo(90f));
			Assert.That(MeshActorPresentation.FacingToYaw(ActorFacing8.S), Is.EqualTo(180f));
			Assert.That(MeshActorPresentation.FacingToYaw(ActorFacing8.NW), Is.EqualTo(315f));
		}
	}
}
```

- [ ] **Step 2: Testlauf — muss scheitern** (Compile-Fehler, Klasse fehlt):

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform EditMode -testFilter "Eidren.Tests.EditMode.MeshActorPresentationTests" -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t4a.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\w3d-t4a-tests.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\w3d-t4a-tests.log" -Pattern "error CS"
```

Erwartet: `error CS0246` (MeshActorPresentation unbekannt).

- [ ] **Step 3: Implementierung** — `Assets/_Game/Scripts/Presentation/MeshActorPresentation.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// 3D-Gegenstueck zu SpriteActorPresentation: spielt die Legacy-Clips der
	/// Wanderer.glb ueber eine Animation-Komponente, dreht das Modell fluessig
	/// zur Blick-/Bewegungsrichtung und stellt Zustaende ohne eigenen Clip
	/// (Treffer, Taumeln, Tod, Erscheinen, Ausweichen, Ernten) prozedural dar.
	/// Schaltet ausserdem die Ruestungs-Slotmeshes und das Waffenmesh.
	/// </summary>
	public sealed class MeshActorPresentation : MonoBehaviour, IActorPresentation, ILocomotionPresentation, IAuthoredStatePresentation, IArmorPresentation
	{
		private enum ProceduralPose
		{
			None,
			Hit,
			Stagger,
			Death,
			Appear,
			Dodge,
			Harvest
		}

		public const string StanceSpear = "Speer";
		public const string StanceDaggers = "Dolche";
		public const string StanceHammer = "Hammer";

		// Reihenfolge entspricht den SetArmorTiers-Parametern: Kopf, Brust, Haende, Beine.
		private static readonly string[] SlotPrefixes = { "Helm_", "Harnisch_", "Haende_", "Beine_" };
		private static readonly string[] TierSuffixes = { null, "Stoff", "Kupfer", "Eisen" };
		private static readonly string[] WeaponMeshNames = { "Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer" };
		private static readonly int TintId = Shader.PropertyToID("_Tint");

		[SerializeField]
		private Animation animationPlayer;

		[SerializeField]
		private Transform modelRoot;

		[SerializeField]
		private float worldHeight = 1.8f;

		[SerializeField]
		private float turnDegreesPerSecond = 720f;

		[SerializeField]
		private float crossfadeSeconds = 0.12f;

		private Renderer[] _renderers = Array.Empty<Renderer>();
		private readonly Dictionary<string, Transform> _namedNodes = new Dictionary<string, Transform>();
		private MaterialPropertyBlock _properties;
		private Color _tint = Color.white;
		private string _stance = StanceDaggers;
		private bool _weaponVisible;
		private string _currentClip;
		private float _targetYaw = 180f;
		private float _currentYaw = 180f;
		private ProceduralPose _procedural;
		private float _proceduralStartedAt;
		private float _proceduralDuration;
		private Vector3 _modelBasePosition;
		private Vector3 _modelBaseScale = Vector3.one;

		public ActorFacing8 Facing { get; private set; } = ActorFacing8.S;

		public ActorVisualState VisualState { get; private set; } = ActorVisualState.Idle;

		public float WorldHeight => worldHeight;

		public void Configure(Animation configuredAnimation, Transform configuredModelRoot, float configuredWorldHeight = 1.8f)
		{
			animationPlayer = configuredAnimation;
			modelRoot = configuredModelRoot;
			worldHeight = configuredWorldHeight;
			CacheHierarchy();
		}

		private void Awake()
		{
			CacheHierarchy();
		}

		private void OnEnable()
		{
			ActorPresentationRegistry.Register(this);
		}

		private void OnDisable()
		{
			ActorPresentationRegistry.Unregister(this);
		}

		private void CacheHierarchy()
		{
			Transform root = (modelRoot != null) ? modelRoot : base.transform;
			_renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
			_namedNodes.Clear();
			foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (!_namedNodes.ContainsKey(child.name))
				{
					_namedNodes.Add(child.name, child);
				}
			}
			if (_properties == null)
			{
				_properties = new MaterialPropertyBlock();
			}
			if (modelRoot != null)
			{
				_modelBasePosition = modelRoot.localPosition;
				_modelBaseScale = modelRoot.localScale;
			}
		}

		// ------------------------------------------------------------------
		// IActorPresentation
		// ------------------------------------------------------------------

		public void SetFacing(ActorFacing8 facing)
		{
			Facing = facing;
			_targetYaw = FacingToYaw(facing);
		}

		public void SetVisualState(ActorVisualState state, float normalizedTime = 0f)
		{
			VisualState = state;
			switch (state)
			{
			case ActorVisualState.Idle:
				PlayLoop("Ruhe_" + _stance);
				break;
			case ActorVisualState.Move:
				PlayLoop("Laufen_" + _stance);
				break;
			case ActorVisualState.Ability1:
			case ActorVisualState.Ability2:
				PlayTimed("Angriff_" + _stance, 0f);
				break;
			case ActorVisualState.Hit:
				StartProcedural(ProceduralPose.Hit, 0.2f);
				break;
			case ActorVisualState.Stagger:
				StartProcedural(ProceduralPose.Stagger, 0.5f);
				break;
			case ActorVisualState.Death:
				PlayLoop("Ruhe_" + _stance);
				StartProcedural(ProceduralPose.Death, 1.2f);
				break;
			case ActorVisualState.Appear:
				StartProcedural(ProceduralPose.Appear, 0.6f);
				break;
			}
		}

		public void SetTint(Color color)
		{
			_tint = color;
			ApplyTint(_tint);
		}

		// ------------------------------------------------------------------
		// ILocomotionPresentation
		// ------------------------------------------------------------------

		public void SetLocomotion(Vector3 worldDirection, bool moving)
		{
			worldDirection.y = 0f;
			if (moving && worldDirection.sqrMagnitude > 0.0001f)
			{
				// Kontinuierliche Drehung aus der echten Bewegungsrichtung;
				// SetFacing liefert nur die 45-Grad-Quantisierung als Rueckfall.
				_targetYaw = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
			}
		}

		// ------------------------------------------------------------------
		// IAuthoredStatePresentation
		// ------------------------------------------------------------------

		public void SetAuthoredState(string stem, float normalizedTime = 0f, bool loop = false, bool restart = false)
		{
			string clip = ResolveClipName(stem, _stance);
			if (clip != null)
			{
				if (loop)
				{
					PlayLoop(clip);
				}
				else
				{
					PlayTimed(clip, 0f);
				}
				return;
			}
			StartProceduralForStem(stem, 0f);
		}

		public void SetAuthoredTimedState(string stem, float durationSeconds)
		{
			string clip = ResolveClipName(stem, _stance);
			if (clip != null)
			{
				PlayTimed(clip, durationSeconds);
				return;
			}
			StartProceduralForStem(stem, durationSeconds);
		}

		// ------------------------------------------------------------------
		// IArmorPresentation
		// ------------------------------------------------------------------

		public void SetAssetVariant(string actorId)
		{
			// Atlas-Konzept der Sprite-Darstellung; fuer das Mesh bedeutungslos.
		}

		public void SetArmorParts(bool head, bool chest, bool hands, bool legs)
		{
			SetArmorTiers(head ? 1 : 0, chest ? 1 : 0, hands ? 1 : 0, legs ? 1 : 0);
		}

		public void SetArmorTiers(int head, int chest, int hands, int legs)
		{
			ApplySlot(0, head);
			ApplySlot(1, chest);
			ApplySlot(2, hands);
			ApplySlot(3, legs);
		}

		// ------------------------------------------------------------------
		// Waffen und Meshes
		// ------------------------------------------------------------------

		/// <summary>
		/// Stellt den Waffen-Clip-Satz und die Sichtbarkeit des Waffenmeshes um.
		/// Unbewaffnet werden die Dolche-Clips ohne Mesh verwendet.
		/// </summary>
		public void SetWeaponStance(string stance, bool showWeaponMesh)
		{
			_stance = string.IsNullOrEmpty(stance) ? StanceDaggers : stance;
			_weaponVisible = showWeaponMesh;
			string active = _weaponVisible ? ("Waffe_" + _stance) : null;
			foreach (string meshName in WeaponMeshNames)
			{
				SetNodeActive(meshName, meshName == active);
			}
			if (VisualState == ActorVisualState.Idle || VisualState == ActorVisualState.Move)
			{
				SetVisualState(VisualState);
			}
		}

		public static string ResolveClipName(string stem, string stance)
		{
			if (string.IsNullOrEmpty(stem))
			{
				return null;
			}
			if (stem == "idle")
			{
				return "Ruhe_" + stance;
			}
			if (stem == "move")
			{
				return "Laufen_" + stance;
			}
			if (stem.StartsWith("hammer", StringComparison.Ordinal))
			{
				return "Angriff_Hammer";
			}
			if (stem.StartsWith("dagger", StringComparison.Ordinal))
			{
				return "Angriff_Dolche";
			}
			if (stem.StartsWith("spear", StringComparison.Ordinal))
			{
				return "Angriff_Speer";
			}
			return null;
		}

		public static string ArmorMeshName(int slotIndex, int tier)
		{
			if (slotIndex < 0 || slotIndex >= SlotPrefixes.Length || tier < 1 || tier > 3)
			{
				return null;
			}
			return SlotPrefixes[slotIndex] + TierSuffixes[tier];
		}

		public static float FacingToYaw(ActorFacing8 facing)
		{
			return (int)facing * 45f;
		}

		private void ApplySlot(int slotIndex, int tier)
		{
			string active = ArmorMeshName(slotIndex, Mathf.Clamp(tier, 0, 3));
			for (int index = 1; index < TierSuffixes.Length; index++)
			{
				string meshName = SlotPrefixes[slotIndex] + TierSuffixes[index];
				SetNodeActive(meshName, meshName == active);
			}
		}

		private void SetNodeActive(string nodeName, bool active)
		{
			if (_namedNodes.TryGetValue(nodeName, out Transform node) && node != null && node.gameObject.activeSelf != active)
			{
				node.gameObject.SetActive(active);
			}
		}

		// ------------------------------------------------------------------
		// Clip-Wiedergabe
		// ------------------------------------------------------------------

		private void PlayLoop(string clipName)
		{
			if (_procedural == ProceduralPose.Death)
			{
				return;
			}
			if (animationPlayer == null || animationPlayer[clipName] == null)
			{
				return;
			}
			AnimationState state = animationPlayer[clipName];
			state.wrapMode = WrapMode.Loop;
			state.speed = 1f;
			if (_currentClip != clipName)
			{
				animationPlayer.CrossFade(clipName, crossfadeSeconds);
				_currentClip = clipName;
			}
		}

		private void PlayTimed(string clipName, float durationSeconds)
		{
			if (_procedural == ProceduralPose.Death)
			{
				return;
			}
			if (animationPlayer == null || animationPlayer[clipName] == null)
			{
				return;
			}
			AnimationState state = animationPlayer[clipName];
			state.wrapMode = WrapMode.ClampForever;
			state.speed = (durationSeconds > 0.01f) ? (state.length / durationSeconds) : 1f;
			state.time = 0f;
			animationPlayer.CrossFade(clipName, crossfadeSeconds * 0.5f);
			_currentClip = clipName;
		}

		// ------------------------------------------------------------------
		// Prozedurale Zustaende (kein Clip in der GLB vorhanden)
		// ------------------------------------------------------------------

		private void StartProceduralForStem(string stem, float durationSeconds)
		{
			switch (stem)
			{
			case "hit":
				StartProcedural(ProceduralPose.Hit, (durationSeconds > 0f) ? durationSeconds : 0.2f);
				break;
			case "death":
				PlayLoop("Ruhe_" + _stance);
				StartProcedural(ProceduralPose.Death, 1.2f);
				break;
			case "dodge":
				StartProcedural(ProceduralPose.Dodge, (durationSeconds > 0f) ? durationSeconds : 0.32f);
				break;
			case "harvest":
				StartProcedural(ProceduralPose.Harvest, float.PositiveInfinity);
				break;
			case "appear":
				StartProcedural(ProceduralPose.Appear, (durationSeconds > 0f) ? durationSeconds : 0.6f);
				break;
			case "stagger":
				StartProcedural(ProceduralPose.Stagger, (durationSeconds > 0f) ? durationSeconds : 0.5f);
				break;
			}
		}

		private void StartProcedural(ProceduralPose pose, float duration)
		{
			if (_procedural == ProceduralPose.Death)
			{
				return;
			}
			_procedural = pose;
			_proceduralStartedAt = Time.time;
			_proceduralDuration = duration;
		}

		private void Update()
		{
			_currentYaw = Mathf.MoveTowardsAngle(_currentYaw, _targetYaw, turnDegreesPerSecond * Time.deltaTime);
		}

		private void LateUpdate()
		{
			if (modelRoot == null)
			{
				return;
			}
			float tilt = 0f;
			float roll = 0f;
			Vector3 offset = Vector3.zero;
			Vector3 scale = _modelBaseScale;
			Color tint = _tint;
			float elapsed = Time.time - _proceduralStartedAt;
			float progress = (_proceduralDuration > 0f && !float.IsPositiveInfinity(_proceduralDuration))
				? Mathf.Clamp01(elapsed / _proceduralDuration)
				: 0f;
			switch (_procedural)
			{
			case ProceduralPose.Hit:
				tint = Color.Lerp(new Color(1f, 0.25f, 0.2f), _tint, progress);
				break;
			case ProceduralPose.Stagger:
				roll = Mathf.Sin(elapsed * 40f) * 8f * (1f - progress);
				break;
			case ProceduralPose.Death:
				tilt = Mathf.SmoothStep(0f, 90f, Mathf.Clamp01(elapsed / 0.7f));
				offset.y = -Mathf.SmoothStep(0f, 0.4f, Mathf.Clamp01((elapsed - 0.7f) / 0.5f));
				break;
			case ProceduralPose.Appear:
				scale = _modelBaseScale * Mathf.SmoothStep(0f, 1f, progress);
				break;
			case ProceduralPose.Dodge:
				tilt = Mathf.Sin(progress * Mathf.PI) * 20f;
				break;
			case ProceduralPose.Harvest:
				offset.y = Mathf.Abs(Mathf.Sin(elapsed * 6f)) * 0.06f;
				tilt = 8f;
				break;
			}
			if (_procedural != ProceduralPose.None && _procedural != ProceduralPose.Death && progress >= 1f)
			{
				_procedural = ProceduralPose.None;
				tint = _tint;
			}
			modelRoot.localPosition = _modelBasePosition + offset;
			modelRoot.localScale = scale;
			modelRoot.rotation = Quaternion.Euler(tilt, _currentYaw, roll);
			ApplyTint(tint);
		}

		private void ApplyTint(Color color)
		{
			if (_properties == null)
			{
				_properties = new MaterialPropertyBlock();
			}
			foreach (Renderer target in _renderers)
			{
				if (target != null)
				{
					target.GetPropertyBlock(_properties);
					_properties.SetColor(TintId, color);
					target.SetPropertyBlock(_properties);
				}
			}
		}
	}
}
```

- [ ] **Step 4: Testlauf — muss bestehen** (gleicher Befehl wie Step 2, Log/XML `w3d-t4b`). Erwartet: alle `MeshActorPresentationTests` `Passed`, keine `error CS`.

---

### Task 5: WandererEquipmentVisual (Kampf-Anbindung)

**Files:**
- Create: `Assets/_Game/Scripts/Combat/WandererEquipmentVisual.cs`
- Test: `Assets/_Game/Editor/Tests/MeshActorPresentationTests.cs` (Ergänzung)

**Interfaces:**
- Consumes: `IPlayerWeaponPresentation` (Task 3), `MeshActorPresentation.SetWeaponStance` (Task 4), `WeaponData.Identity.Family` (Bestand).
- Produces: Komponente für den `WeaponDriver`-Knoten; Task 6 baut sie ins Prefab.

- [ ] **Step 1: Failing Test ergänzen** — in `MeshActorPresentationTests.cs`:

```csharp
		[TestCase(Eidren.Data.WeaponFamily.Hammer, MeshActorPresentation.StanceHammer, true)]
		[TestCase(Eidren.Data.WeaponFamily.Daggers, MeshActorPresentation.StanceDaggers, true)]
		[TestCase(Eidren.Data.WeaponFamily.Spear, MeshActorPresentation.StanceSpear, true)]
		[TestCase(Eidren.Data.WeaponFamily.None, MeshActorPresentation.StanceDaggers, false)]
		public void ResolveStance_BildetWaffenfamilieAufStanceAb(Eidren.Data.WeaponFamily family, string expectedStance, bool expectedVisible)
		{
			var (stance, visible) = Eidren.Combat.WandererEquipmentVisual.ResolveStance(family);
			Assert.That(stance, Is.EqualTo(expectedStance));
			Assert.That(visible, Is.EqualTo(expectedVisible));
		}
```

- [ ] **Step 2: Testlauf — muss scheitern** (Filter `MeshActorPresentationTests`, Log `w3d-t5a`): `error CS0246` (`WandererEquipmentVisual` unbekannt).

- [ ] **Step 3: Implementierung** — `Assets/_Game/Scripts/Combat/WandererEquipmentVisual.cs`:

```csharp
using Eidren.Data;
using Eidren.Presentation;
using UnityEngine;

namespace Eidren.Combat
{
	/// <summary>
	/// 3D-Ersatz fuer PlayerWeaponVisual: Waffen sind materialneutral im Rig
	/// enthalten (eine Optik je Familie, Werte kommen aus WeaponData).
	/// Diese Komponente uebersetzt die ausgeruestete Waffe in Stance und
	/// Mesh-Sichtbarkeit der MeshActorPresentation.
	/// </summary>
	public sealed class WandererEquipmentVisual : MonoBehaviour, IPlayerWeaponPresentation
	{
		private MeshActorPresentation _mesh;

		public void Show(WeaponData weapon)
		{
			MeshActorPresentation mesh = ResolveMesh();
			if (mesh == null)
			{
				return;
			}
			WeaponFamily family = (weapon != null) ? weapon.Identity.Family : WeaponFamily.None;
			var (stance, visible) = ResolveStance(family);
			mesh.SetWeaponStance(stance, visible);
		}

		public void PlayAttack(float duration, int comboIndex, WeaponFamily family)
		{
			// Der Angriffs-Clip laeuft ueber die Stems des PlayerVisualAnimator;
			// hier ist nichts zu tun.
		}

		public static (string stance, bool weaponVisible) ResolveStance(WeaponFamily family)
		{
			switch (family)
			{
			case WeaponFamily.Hammer:
				return (MeshActorPresentation.StanceHammer, true);
			case WeaponFamily.Daggers:
				return (MeshActorPresentation.StanceDaggers, true);
			case WeaponFamily.Spear:
				return (MeshActorPresentation.StanceSpear, true);
			default:
				// Unbewaffnet: Dolche-Haltung ohne Waffenmesh (siehe README_Wanderer3D).
				return (MeshActorPresentation.StanceDaggers, false);
			}
		}

		private MeshActorPresentation ResolveMesh()
		{
			if (_mesh == null)
			{
				_mesh = base.transform.root.GetComponentInChildren<MeshActorPresentation>(includeInactive: true);
			}
			return _mesh;
		}
	}
}
```

- [ ] **Step 4: Testlauf — muss bestehen** (Filter `MeshActorPresentationTests`, Log/XML `w3d-t5b`). Erwartet: `Passed`.

---

### Task 6: Builder — Player.prefab auf 3D umbauen, Prefab-Tests neu

**Files:**
- Create: `Assets/_Game/Editor/Wanderer3DPlayerBuilder.cs`
- Modify: `Assets/_Game/Editor/Tests/PlayerPrefabTests.cs` (ersetzen)
- Erzeugt Assets: `Assets/_Game/Art/Actors/Player/Wanderer3D/M_Wanderer_VertexLit.mat`, `Assets/_Game/Prefabs/Actors/3D/Player_3D.prefab`, geändertes `Assets/_Game/Prefabs/Player/Player.prefab`

**Interfaces:**
- Consumes: alle vorherigen Tasks.
- Produces: `Player.prefab` mit Kind `Player_Visual/Player_3D(Clone-Instanz)` (MeshActorPresentation + Animation + Wanderer-Rig), leerem `WeaponDriver` mit `WandererEquipmentVisual`, Bindings-Feld `playerVisual` → MeshActorPresentation. Kein `SpriteActorPresentation`, kein `DynamicActorGroundShadow`, kein `PlayerWeaponVisual` mehr am Spieler. `HarvestToolPresentation` bleibt (Werkzeug-Billboard, eigener Folgeauftrag).

- [ ] **Step 1: PlayerPrefabTests ersetzen** — Datei-Inhalt komplett durch die 3D-Fassung tauschen (Failing Test zuerst; die zwei Atlas-/2D-Tests entfallen ersatzlos, der CharacterController-Test bleibt wörtlich erhalten):

```csharp
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Eidra;
using Eidren.Gameplay.Presentation;
using Eidren.Player;
using Eidren.Presentation;
using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;

namespace Eidren.Tests.EditMode
{
public sealed class PlayerPrefabTests
{
	private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";

	[Test]
	public void PlayerPrefab_Contains3DPlayerStructureAndReferences()
	{
		GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
		Assert.That(root, Is.Not.Null, "Missing prefab: " + PlayerPrefabPath);
		PlayerPrefabBindings component = root.GetComponent<PlayerPrefabBindings>();
		Assert.That(component, Is.Not.Null);
		Assert.That(component.CharacterController, Is.SameAs((object)root.GetComponent<CharacterController>()));
		Assert.That(component.Damageable, Is.SameAs((object)root.GetComponent<Damageable>()));
		Assert.That(component.Motor, Is.SameAs((object)root.GetComponent<PlayerMotor>()));
		Assert.That(component.EidraTeam, Is.SameAs((object)root.GetComponent<EidraTeamController>()));
		Assert.That(component.Combat, Is.SameAs((object)root.GetComponent<PlayerCombatController>()));
		Assert.That(component.Consumables, Is.SameAs((object)root.GetComponent<ConsumableController>()));
		Assert.That(component.VisualAnimator, Is.SameAs((object)root.GetComponent<PlayerVisualAnimator>()));

		Transform art = root.transform.Find("Player_Visual");
		Assert.That(art, Is.Not.Null);
		MeshActorPresentation mesh = art.GetComponentInChildren<MeshActorPresentation>(true);
		Assert.That(mesh, Is.Not.Null, "Player_Visual traegt keine MeshActorPresentation");
		Assert.That(component.PlayerVisual, Is.SameAs((object)mesh));
		Assert.That(root.GetComponentInChildren<SpriteActorPresentation>(true), Is.Null,
			"Spieler traegt noch die 2D-Darstellung");
		Assert.That(root.GetComponentInChildren<DynamicActorGroundShadow>(true), Is.Null,
			"Blob-Schatten am Spieler ist obsolet (echte URP-Schatten)");

		SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
		Assert.That(renderers, Is.Not.Empty, "Wanderer-Rig fehlt");
		foreach (SkinnedMeshRenderer renderer in renderers)
		{
			Assert.That(renderer.sharedMaterial, Is.Not.Null);
			Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Eidren/Actors/WandererVertexLit"),
				"Falscher Shader an " + renderer.name);
		}

		Animation animationPlayer = art.GetComponentInChildren<Animation>(true);
		Assert.That(animationPlayer, Is.Not.Null, "Animation-Komponente fehlt");
		Assert.That(animationPlayer["Ruhe_Dolche"], Is.Not.Null, "Clip Ruhe_Dolche nicht angebunden");
		Assert.That(animationPlayer["Angriff_Hammer"], Is.Not.Null, "Clip Angriff_Hammer nicht angebunden");

		Transform weapon = root.transform.Find("WeaponDriver");
		Assert.That(weapon, Is.Not.Null);
		Assert.That(component.WeaponVisual, Is.SameAs((object)weapon));
		Assert.That(weapon.GetComponent<WandererEquipmentVisual>(), Is.Not.Null);
		Assert.That(weapon.GetComponent<PlayerWeaponVisual>(), Is.Null, "2D-Waffen-Billboard ist obsolet");
		Assert.That(weapon.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty,
			"WeaponDriver traegt noch Sprite-Waffen");

		Assert.That(root.GetComponent<PlayerHarvestVisual>(), Is.Not.Null);
		Assert.That(component.MaxHealth, Is.EqualTo(100f));
		Assert.DoesNotThrow(new TestDelegate(component.ValidateReferences));
	}

	[Test]
	public void PlayerPrefab_SlotMeshesStartInaktivNurBasisAktiv()
	{
		GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
		Assert.That(root, Is.Not.Null);
		string[] inactive = new string[15]
		{
			"Helm_Stoff", "Helm_Kupfer", "Helm_Eisen",
			"Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
			"Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
			"Haende_Stoff", "Haende_Kupfer", "Haende_Eisen",
			"Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer"
		};
		Assert.That(FindDeep(root.transform, "Basis"), Is.Not.Null);
		Assert.That(FindDeep(root.transform, "Basis").gameObject.activeSelf, Is.True, "Basis muss aktiv sein");
		foreach (string nodeName in inactive)
		{
			Transform node = FindDeep(root.transform, nodeName);
			Assert.That(node, Is.Not.Null, "Knoten fehlt: " + nodeName);
			Assert.That(node.gameObject.activeSelf, Is.False, "Muss anfangs inaktiv sein: " + nodeName);
		}
	}

	[Test]
	public void PlayerPrefab_PreservesCharacterControllerValues()
	{
		GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
		Assert.That(gameObject, Is.Not.Null, "Missing prefab: " + PlayerPrefabPath);
		CharacterController component = gameObject.GetComponent<CharacterController>();
		Assert.That(component, Is.Not.Null);
		Assert.That(component.height, Is.EqualTo(2f));
		Assert.That(component.radius, Is.EqualTo(0.48f));
		Assert.That(component.center, Is.EqualTo(new Vector3(0f, 1f, 0f)));
		Assert.That(gameObject.GetComponents<Collider>(), Has.Length.EqualTo(1));
		Assert.That(gameObject.GetComponents<CharacterController>(), Has.Length.EqualTo(1));
	}

	private static Transform FindDeep(Transform root, string childName)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			if (child.name == childName)
			{
				return child;
			}
		}
		return null;
	}
}
}
```

Hinweis: `InteractionController`-/`MeleeWeaponHitbox`-Asserts aus der Altfassung übernehmen, falls `ValidateReferences` sie verlangt — dabei `Eidren.Interaction.InteractionController` voll qualifizieren (CS0104).

- [ ] **Step 2: Testlauf — muss scheitern** (Filter `PlayerPrefabTests`, Log `w3d-t6a`): Fehlschläge, weil das Prefab noch 2D ist.

- [ ] **Step 3: Builder schreiben** — `Assets/_Game/Editor/Wanderer3DPlayerBuilder.cs`:

```csharp
using Eidren.Combat;
using Eidren.Composition;
using Eidren.Presentation;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Baut das Spieler-Prefab von der Sprite- auf die Wanderer3D-Darstellung um.
	/// Idempotent: mehrfache Laeufe erzeugen denselben Endzustand.
	/// </summary>
	public static class Wanderer3DPlayerBuilder
	{
		private const string GlbPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/Wanderer.glb";
		private const string MaterialPath = "Assets/_Game/Art/Actors/Player/Wanderer3D/M_Wanderer_VertexLit.mat";
		private const string VisualPrefabPath = "Assets/_Game/Prefabs/Actors/3D/Player_3D.prefab";
		private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";
		private const string ShaderName = "Eidren/Actors/WandererVertexLit";

		private static readonly string[] InitiallyInactiveNodes = new string[15]
		{
			"Helm_Stoff", "Helm_Kupfer", "Helm_Eisen",
			"Harnisch_Stoff", "Harnisch_Kupfer", "Harnisch_Eisen",
			"Beine_Stoff", "Beine_Kupfer", "Beine_Eisen",
			"Haende_Stoff", "Haende_Kupfer", "Haende_Eisen",
			"Waffe_Speer", "Waffe_Dolche", "Waffe_Hammer"
		};

		[MenuItem("Eidren/V0.2/Build Wanderer3D Player")]
		public static void Build()
		{
			Material material = EnsureMaterial();
			BuildVisualPrefab(material);
			RewirePlayerPrefab();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			Debug.Log("[W3D] Player.prefab auf Wanderer3D umgebaut.");
		}

		private static Material EnsureMaterial()
		{
			Shader shader = Shader.Find(ShaderName);
			if (shader == null)
			{
				throw new InvalidOperationException("Shader fehlt: " + ShaderName);
			}
			Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
			if (material == null)
			{
				material = new Material(shader);
				AssetDatabase.CreateAsset(material, MaterialPath);
			}
			else
			{
				material.shader = shader;
			}
			return material;
		}

		private static void BuildVisualPrefab(Material material)
		{
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
			if (model == null)
			{
				throw new InvalidOperationException("GLB nicht importiert: " + GlbPath);
			}
			System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs/Actors/3D");
			var root = new GameObject("Player_3D");
			try
			{
				GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(model);
				rig.name = "Wanderer";
				rig.transform.SetParent(root.transform, worldPositionStays: false);

				foreach (SkinnedMeshRenderer renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
				{
					renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
					renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
					renderer.receiveShadows = true;
				}

				foreach (string nodeName in InitiallyInactiveNodes)
				{
					Transform node = FindDeep(rig.transform, nodeName);
					if (node == null)
					{
						throw new InvalidOperationException("Knoten fehlt im Rig: " + nodeName);
					}
					node.gameObject.SetActive(false);
				}

				Animation animationPlayer = rig.GetComponentInChildren<Animation>(true);
				if (animationPlayer == null)
				{
					animationPlayer = rig.AddComponent<Animation>();
				}
				animationPlayer.playAutomatically = false;
				var clips = AssetDatabase.LoadAllAssetsAtPath(GlbPath).OfType<AnimationClip>()
					.Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
				foreach (AnimationClip clip in clips)
				{
					if (animationPlayer.GetClip(clip.name) == null)
					{
						animationPlayer.AddClip(clip, clip.name);
					}
				}

				MeshActorPresentation presentation = root.AddComponent<MeshActorPresentation>();
				presentation.Configure(animationPlayer, rig.transform, 1.8f);

				PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		private static void RewirePlayerPrefab()
		{
			GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
			try
			{
				Transform art = root.transform.Find("Player_Visual");
				if (art == null)
				{
					throw new InvalidOperationException("Player prefab has no Player_Visual.");
				}
				for (int index = art.childCount - 1; index >= 0; index--)
				{
					UnityEngine.Object.DestroyImmediate(art.GetChild(index).gameObject);
				}
				GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
				var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
				visual.transform.SetParent(art, worldPositionStays: false);

				Transform driver = root.transform.Find("WeaponDriver");
				if (driver == null)
				{
					throw new InvalidOperationException("Player prefab has no WeaponDriver.");
				}
				for (int index = driver.childCount - 1; index >= 0; index--)
				{
					UnityEngine.Object.DestroyImmediate(driver.GetChild(index).gameObject);
				}
				PlayerWeaponVisual oldWeapon = driver.GetComponent<PlayerWeaponVisual>();
				if (oldWeapon != null)
				{
					UnityEngine.Object.DestroyImmediate(oldWeapon);
				}
				if (driver.GetComponent<WandererEquipmentVisual>() == null)
				{
					driver.gameObject.AddComponent<WandererEquipmentVisual>();
				}

				MeshActorPresentation mesh = visual.GetComponentInChildren<MeshActorPresentation>(true);
				PlayerPrefabBindings bindings = root.GetComponent<PlayerPrefabBindings>();
				var serialized = new SerializedObject(bindings);
				serialized.FindProperty("playerVisual").objectReferenceValue = mesh;
				serialized.ApplyModifiedPropertiesWithoutUndo();

				EditorUtility.SetDirty(root);
				PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private static Transform FindDeep(Transform root, string childName)
		{
			foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
			{
				if (child.name == childName)
				{
					return child;
				}
			}
			return null;
		}
	}
}
```

- [ ] **Step 4: Builder ausführen**

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -executeMethod Eidren.Editor.Wanderer3DPlayerBuilder.Build -quit -logFile "C:\Users\phine\Documents\Projekt Eidren\w3d-t6-build.log"
Wait-Process -Name Unity -Timeout 900 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\w3d-t6-build.log" -Pattern "error CS|Exception|\[W3D\]"
```

Erwartet: `[W3D] Player.prefab auf Wanderer3D umgebaut.`, keine Exceptions. Falls die GLB-Instanz keine `Animation`-Komponente mitbringt und `animationPlayer[clip]` später `null` liefert: Clips wurden evtl. nicht als Legacy importiert — dann in den Importeinstellungen der GLB (`AssetImporter.GetAtPath` + `SerializedObject`, Property `importSettings.animationMethod` = 1/Legacy) nachsteuern und neu importieren.

- [ ] **Step 5: Prefab-Tests laufen lassen** (Filter `PlayerPrefabTests`, Log/XML `w3d-t6b`). Erwartet: alle drei Tests `Passed`.

---

### Task 7: PlayMode-Tests nachziehen

**Files:**
- Modify: `Assets/_Game/Tests/PlayMode/PlayerPresentationPlayModeTests.cs`
- ggf. Modify: `Assets/_Game/Tests/PlayMode/GroundShadowPlayModeTests.cs` (nur spielerbezogene Asserts)

**Interfaces:**
- Consumes: fertiges `Player.prefab` (Task 6), `MeshActorPresentation`-API (Task 4).

- [ ] **Step 1: Bestand lesen** — beide Dateien öffnen und jeden Assert markieren, der `SpriteActorPresentation`, Atlas-Texturen, `PlayerWeaponVisual` oder den Blob-Schatten des **Spielers** betrifft. Gegner-Asserts unverändert lassen.

- [ ] **Step 2: Spieler-Asserts ersetzen** — in `PlayerPresentationPlayModeTests.cs` die sprite-spezifischen Tests durch diese 3D-Fassung ersetzen (Namespace/using-Stil der Datei übernehmen):

```csharp
		[UnityEngine.TestTools.UnityTest]
		public System.Collections.IEnumerator Player3D_RuestungswechselSchaltetGenauEinSlotmesh()
		{
			GameObject prefab = Resources.Load<GameObject>("Player") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player/Player.prefab");
			GameObject player = UnityEngine.Object.Instantiate(prefab);
			yield return null;
			MeshActorPresentation mesh = player.GetComponentInChildren<MeshActorPresentation>(true);
			Assert.That(mesh, Is.Not.Null);
			mesh.SetArmorTiers(2, 1, 0, 3);
			yield return null;
			Assert.That(FindDeep(player.transform, "Helm_Kupfer").gameObject.activeSelf, Is.True);
			Assert.That(FindDeep(player.transform, "Helm_Stoff").gameObject.activeSelf, Is.False);
			Assert.That(FindDeep(player.transform, "Harnisch_Stoff").gameObject.activeSelf, Is.True);
			Assert.That(FindDeep(player.transform, "Haende_Stoff").gameObject.activeSelf, Is.False);
			Assert.That(FindDeep(player.transform, "Haende_Kupfer").gameObject.activeSelf, Is.False);
			Assert.That(FindDeep(player.transform, "Haende_Eisen").gameObject.activeSelf, Is.False);
			Assert.That(FindDeep(player.transform, "Beine_Eisen").gameObject.activeSelf, Is.True);
			UnityEngine.Object.Destroy(player);
		}

		[UnityEngine.TestTools.UnityTest]
		public System.Collections.IEnumerator Player3D_AngriffSpieltAngriffsClip()
		{
			GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Player/Player.prefab");
			GameObject player = UnityEngine.Object.Instantiate(prefab);
			yield return null;
			MeshActorPresentation mesh = player.GetComponentInChildren<MeshActorPresentation>(true);
			Animation animationPlayer = player.GetComponentInChildren<Animation>(true);
			Assert.That(mesh, Is.Not.Null);
			Assert.That(animationPlayer, Is.Not.Null);
			mesh.SetWeaponStance(MeshActorPresentation.StanceHammer, showWeaponMesh: true);
			mesh.SetAuthoredTimedState("hammer1", 0.8f);
			yield return null;
			Assert.That(animationPlayer.IsPlaying("Angriff_Hammer"), Is.True, "Angriff_Hammer laeuft nicht");
			Assert.That(FindDeep(player.transform, "Waffe_Hammer").gameObject.activeSelf, Is.True);
			UnityEngine.Object.Destroy(player);
		}
```

(Hilfsmethode `FindDeep` wie in `PlayerPrefabTests` in die Klasse aufnehmen. `UnityEditor.AssetDatabase` funktioniert in PlayMode-Tests im Editor-Testlauf; das Muster existiert bereits in anderen PlayMode-Tests des Projekts — falls nicht, Prefab über `Resources` bzw. die im Projekt übliche Ladehilfe beziehen.)

- [ ] **Step 3: PlayMode-Lauf** (Hintergrund, volle Plattform — Filter auf die zwei geänderten Klassen):

```powershell
& "C:\Users\phine\Documents\Projekt Eidren\.unity-editor\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\Users\phine\Documents\Projekt Eidren" -runTests -testPlatform PlayMode -testFilter "Eidren.PlayMode.Tests.PlayerPresentationPlayModeTests;Eidren.PlayMode.Tests.GroundShadowPlayModeTests" -testResults "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t7.xml" -logFile "C:\Users\phine\Documents\Projekt Eidren\w3d-t7-tests.log"
Wait-Process -Name Unity -Timeout 1800 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5
Select-String -Path "C:\Users\phine\Documents\Projekt Eidren\TestResults-w3d-t7.xml" -Pattern 'failed="0"' | Select-Object -First 1
Remove-Item "C:\Users\phine\Documents\Projekt Eidren\Assets\InitTestScene*.unity" -Force -ErrorAction SilentlyContinue
```

(Exakte Testklassen-Namespaces vorher aus den Dateien ablesen — Filter entsprechend setzen.) Erwartet: 0 Fehlschläge.

---

### Task 8: Volle Suiten, Fallout beheben

**Files:** nach Befund (Kandidaten: `CompositionCompletenessTests`, `G001VisualAbnahmeRunner`, `ActorPresentationFollowUpTests`, Interaktions-/Kampf-Integrationstests, `PlayerHarvestVisual`).

**Interfaces:** —

- [ ] **Step 1: Volle EditMode-Suite** (Log/XML `w3d-t8-editmode`, Befehl wie Task 3 Step 11). Erwartet: `failed="0"`.
- [ ] **Step 2: Volle PlayMode-Suite** (Log/XML `w3d-t8-playmode`, `-testPlatform PlayMode`, Timeout 2400, danach `InitTestScene*` löschen). Erwartet: `failed="0"`.
- [ ] **Step 3: Fallout einzeln beheben** — je Fehlschlag: Ursache lesen, minimal fixen (Regel: Verhalten der Gegner-Sprite-Pfade nicht ändern; Spieler-Asserts auf 3D umstellen), betroffene Klasse gezielt nachtesten (`-testFilter`), dann Suite wiederholen. Bei Ordnungsabhängigkeits-Verdacht: Klasse isoliert laufen lassen (Muster `g003-floatingtext-isoliert`).

---

### Task 9: Build, Sichtprüfung, G-001-Captures

**Files:** keine Quellcode-Änderung (nur bei Befund).

**Interfaces:** —

- [ ] **Step 1: Windows-Build** — denselben Build-Weg wie `g001-build.log` verwenden (Aufruf aus dem Log übernehmen; Log `w3d-t9-build.log`). Erwartet: Build erfolgreich, keine `error CS`.
- [ ] **Step 2: G-001-Capture-Lauf** — den vorhandenen Capture-Runner (`G001VisualAbnahmeRunner`, Aufruf aus `G001_ABNAHME.md` bzw. den `auftrag5-capture-player-*.log`s übernehmen) ausführen; neue Bilder nach `G001-Captures/` schreiben lassen.
- [ ] **Step 3: Sichtprüfung durch den Auftraggeber** — neue Captures dem Nutzer zeigen (Vertexfarben korrekt in Gamma+LDR? Echte Schatten plausibel bei Sonnentiefe −0,678? Drehung flüssig?). Erst nach dessen Freigabe gilt der Einbau als abgenommen; `G001_ABNAHME.md` um einen Wanderer3D-Absatz ergänzen.
- [ ] **Step 4: Aufräumen** — `w3d-*`-Logs/XMLs ins Sicherungsverzeichnis verschieben oder löschen; `Documentation/Etappen/Wanderer3D/WANDERER3D_EINBAU_ENTWURF.md` Status auf „umgesetzt" setzen.

---

## Selbstreview-Ergebnis (bereits eingearbeitet)

- Spec-Abdeckung: glTFast→T1, Shader→T2, Schnittstellen→T3, MeshActorPresentation (Clips, Drehung, prozedural, Slots)→T4, Waffen→T5, Prefab/Schatten→T6, Tests→T1/T4/T6/T7/T8, G-001→T9. Der im Entwurf genannte `WandererStyleCatalog` entfällt: `PlayerPrefabBindings.ArmorTier()` liefert bereits validierte Stufen 0–3 aus `ItemDefinition.Tier`, die Waffenfamilie kommt aus `WeaponIdentityData` — ein zusätzliches Mapping-Asset wäre reine Doppelung (YAGNI). Abweichung im Entwurf nachtragen (T9 Step 4).
- Typkonsistenz: `SetAuthoredTimedState(string, float)` einheitlich in T3 (Interface + Sprite), T4 (Mesh), T7 (Testaufruf); `SetWeaponStance(string, bool)` in T4/T5/T7; `playerVisual` als `MonoBehaviour` in T3/T6.
- Platzhalter: keine. Der einzige bewusst offene Punkt (Legacy-Status der Clips) hat in T1 einen Diagnose-Schritt und in T6 eine konkrete Rückfallebene.
