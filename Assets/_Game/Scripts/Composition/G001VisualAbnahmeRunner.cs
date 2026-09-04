using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Input;
using Eidren.Interaction;
using Eidren.Presentation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Eidren.Composition
{
    // Aktiviert durch Kommandozeilen-Flag -eidren-g001-capture.
    // Erzeugt vier benannte PNG-Captures aus Zone_Greenwood und schreibt
    // einen JSON-Report mit Helligkeitsstreuung und Einfarbigkeitspruefung.
    public sealed class G001VisualAbnahmeRunner : MonoBehaviour
    {
        [Serializable]
        private sealed class AbnahmeReport
        {
            public string positionSetVersion;
            public string generatedUtc;
            public string scene;
            public string buildVersion;
            public int width;
            public int height;
            public AbnahmeRecord[] captures;
            public FrameTimeRecord[] frameTimes;
        }

        [Serializable]
        private sealed class AbnahmeRecord
        {
            public string positionId;
            public string description;
            public string file;
            // Die tatsaechliche Kamerageometrie gehoert in den Report, nicht nur
            // in das Log: G-001 verlangt, dass der Report jede Kameraposition
            // nennt und dass die Positionen ueber Laeufe hinweg identisch sind.
            // Beides laesst sich damit durch einen Vergleich der beiden
            // Report-Dateien pruefen statt durch Zusicherung.
            public string cameraPosition;
            public string cameraRotationEuler;
            public float orthographicSize;
            public string capturedUtc;
            public float brightnessStdDev;
            public bool isMonochrome;
            public bool viewStable;
        }

        // G-002, Abnahmekriterium 8: die Bildrate darf im Windows-x64-Build
        // nicht messbar abfallen. Der Vergleich laeuft bewusst innerhalb eines
        // einzigen Builds: ein frueherer Build steht nicht mehr zur Verfuegung,
        // und zwei Builds auf derselben Maschine zu verschiedenen Zeitpunkten
        // waeren ohnehin schwer vergleichbar. Stattdessen werden die beiden
        // Dinge, die G-002 hinzufuegt, im laufenden Bild einzeln ab- und
        // zugeschaltet.
        [Serializable]
        private sealed class FrameTimeRecord
        {
            public string configurationId;
            public string description;
            public int measuredFrames;
            public float meanMilliseconds;
            public float medianMilliseconds;
            public float percentile95Milliseconds;
            public int visibleRenderers;
        }

        private const string CaptureFlag = "-eidren-g001-capture";
        private const string OutputArgument = "-eidren-g001-output";

        // Versionskennung des Positionssatzes – Aenderung erzwingt neue Baseline
        //
        // g002-v1: Position 05_boden_bewuchs ergaenzt. Die Positionen 01 bis 04
        // sind unveraendert; das laesst sich am Feld cameraPosition der beiden
        // Reports nachrechnen, statt es glauben zu muessen.
        private const string PositionSetVersion = "g002-v1";

        // Name des Knotens, unter dem AreaArtGroundCoverBuilder den Bewuchs
        // ablegt. Er wird fuer Position 04 ausgeblendet und fuer 05 wieder
        // eingeschaltet.
        private const string GroundCoverRootName = "GroundCover";
        private const string TargetScene = "Zone_Greenwood";

        // Fester Weltsaat. GameSession.StartNewGame ruft ZoneStates.Reset()
        // ohne Argument auf, und ZoneStateService benutzt dann new Random() -
        // also die Uhr. Jeder Lauf bekaeme sonst eine anders bestueckte Zone;
        // genau das war der Grund, warum zwei Laeufe an identischer
        // Kameraposition verschiedene Baeume und Felsen zeigten.
        private const int WorldSeed = 20491;

        // Feste Simulationsschrittweite. Ohne sie haengt Time.time von der
        // tatsaechlichen Bildrate ab, und die Sprite-Animation (die durchweg
        // ueber Time.time laeuft, siehe SpriteActorPresentation.Atlas) steht bei
        // der Aufnahme in jedem Lauf in einer anderen Phase.
        private const int CaptureFramerate = 60;

        // Mittlere Kanaldifferenz, unter der zwei Stichproben als gleich gelten
        private const float StabilityTolerance = 0.5f;

        // So viele gleiche Stichproben in Folge gelten als "fertig gezeichnet".
        // Bewusst hoch, damit ein kurzzeitig ruhiges Bild (etwa zwischen zwei
        // Animationsphasen) nicht als Stabilitaet durchgeht.
        private const int StabilityStreak = 30;

        // So viele Frames werden mindestens gerendert, bevor Stabilitaet
        // ueberhaupt gelten kann
        private const int StabilityMinimumFrames = 30;

        // Untergrenze nach dem Szenenwechsel, damit asynchrone Texturuploads
        // landen koennen. In Frames statt Sekunden, damit der Anlauf in beiden
        // Laeufen dieselbe Menge Spielzeit verbraucht.
        private const int WarmupFrames = 300;

        // Zeitlimit, damit ein haengender Ladevorgang den Lauf nicht blockiert
        private const float StabilityTimeoutSeconds = 45f;

        // Hartes Gesamtzeitlimit des Laufs (siehe Watchdog)
        private const float WatchdogTimeoutSeconds = 300f;

        // Bilder mit Helligkeitsstreuung unter diesem Schwellwert gelten als einfarbig
        private const float MonochromeThreshold = 3f;

        // Isometrische Kameraausrichtung (identisch mit IsometricCamera._fixedRotation)
        private static readonly Quaternion CaptureRotation = Quaternion.Euler(52f, 45f, 0f);

        // Kameraversatz relativ zum Fokuspunkt fuer Naehaufnahmen (orthoSize 2.8)
        // Entspricht -forward * ~12 fuer Euler(52, 45, 0):
        // forward = (sin45*cos52, -sin52, cos45*cos52) ≈ (0.435, -0.788, 0.435)
        // offset = -forward * 12 ≈ (-5.22, 9.46, -5.22)
        private static readonly Vector3 CloseOffset = new Vector3(-5.22f, 9.46f, -5.22f);

        // Weitwinkelversatz fuer die Bodenflaechen-Aufnahme (orthoSize 6)
        private static readonly Vector3 GroundOffset = new Vector3(-13.0f, 23.6f, -13.0f);

        // Feste Standorte des Spielers je Position. Vorher wurde der Fokus aus
        // player.transform.position gelesen; der CharacterController setzt sich
        // nach dem Teleport aber noch, sodass die Kamera in zwei Laeufen an
        // verschiedenen Orten stand (gemessen: 0,16 Weltenheiten Unterschied an
        // Position 02). Feste Werte machen die Kamera per Konstruktion gleich.
        private static readonly Vector3 Stand01 = new Vector3(-5f, 0.05f, 12f);
        private static readonly Vector3 Stand02 = new Vector3(-5f, 0.05f, 12f);

        // Blickhoehe ueber dem Standpunkt
        private static readonly Vector3 FocusLift = Vector3.up * 0.5f;

        // Abstand des Spielers zum Baum bei der Ernte-Pose, entgegen der
        // Kamerablickrichtung. Konstant statt aus cam.transform.position
        // abgeleitet - die Kamera driftet, der Baum nicht.
        private const float HarvestApproachDistance = 1.4f;

        private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

        private string _output;
        private bool _lastViewStable;
        private Texture2D _lastCapture;
        private int _captureWidth;
        private int _captureHeight;
        private readonly List<AbnahmeRecord> _records = new List<AbnahmeRecord>();
        private readonly List<FrameTimeRecord> _frameTimes = new List<FrameTimeRecord>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void TryCreate()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, CaptureFlag) < 0)
                return;
            if (Object.FindFirstObjectByType<G001VisualAbnahmeRunner>() != null)
                return;
            var go = new GameObject("G001VisualAbnahmeRunner");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<G001VisualAbnahmeRunner>();
        }

        // Unity faengt Ausnahmen aus Awake ab und protokolliert sie nur. Ohne
        // eigenes try/catch startet Run() dann nie, Application.Quit wird nie
        // erreicht, und der headless Player laeuft endlos weiter und sperrt
        // die EXE. Genau dieser Haenger ist beim ersten Doppellauf passiert.
        private void Awake()
        {
            try
            {
                _output = ReadArgument(Environment.GetCommandLineArgs(), OutputArgument,
                    Path.Combine(Application.persistentDataPath, "G001"));
                Directory.CreateDirectory(_output);
                Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
                Time.captureFramerate = CaptureFramerate;
                StartCoroutine(Watchdog());
                StartCoroutine(Run());
            }
            catch (Exception ex)
            {
                Debug.LogError("G001 VISUAL ABNAHME: FAIL - Start fehlgeschlagen: " + ex);
                Application.Quit(1);
            }
        }

        // Letzte Sicherung: auch ein unerwarteter Abbruch mitten in Run() darf
        // den Prozess nicht am Leben lassen.
        private IEnumerator Watchdog()
        {
            float deadline = Time.realtimeSinceStartup + WatchdogTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
                yield return null;

            Debug.LogError($"G001 VISUAL ABNAHME: FAIL - Zeitlimit von " +
                           $"{WatchdogTimeoutSeconds:F0} s ueberschritten, Lauf abgebrochen.");
            Application.Quit(1);
        }

        private IEnumerator Run()
        {
            // Ein Frame warten damit Awake aller anderen Objekte abgeschlossen ist
            yield return null;

            var services = EidrenServiceRoot.FindOrCreate();
            services.GameSession.StartNewGame();

            // Muss nach StartNewGame kommen: das ruft selbst Reset() ohne Saat.
            services.GameSession.ZoneStates.Reset(WorldSeed);

            // WorldMap als Warmup laden (identisch mit PackageACaptureRunner)
            yield return LoadScene("WorldMap");
            yield return LoadScene(TargetScene);

            // Auf Spieler warten (max. 15 s)
            PlayerPrefabBindings player = null;
            PlayerInputReader input = null;
            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline)
            {
                player = Object.FindFirstObjectByType<PlayerPrefabBindings>();
                input = Object.FindFirstObjectByType<PlayerInputReader>();
                if (player != null && input != null)
                    break;
                yield return null;
            }
            if (player == null || input == null)
                Fail("Spieler oder Input nicht initialisiert in " + TargetScene);

            // Alle Gegner deaktivieren
            foreach (var enemy in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))
                enemy.gameObject.SetActive(false);

            // Anlauf geben, damit asynchrone Texturuploads abgeschlossen sind,
            // bevor die erste Position aufgenommen wird.
            yield return Frames(WarmupFrames);

            Camera cam = Camera.main;

            // =============================================================
            // Position 01: Kupfersatz mit Speer (entspricht SmokeFinal 01)
            // =============================================================
            if (!TryEquipSet(services, "copper_spear",
                    "armor_copper_helmet", "armor_copper_chest",
                    "armor_copper_gloves", "armor_copper_legs"))
                Fail("Kupfersatz konnte nicht ausgeruestet werden.");
            player.Combat.TrySelectWeapon("copper_spear");
            TeleportPlayer(player, Stand01);
            yield return FaceDown(input);
            yield return Frames(24);
            yield return CaptureToFile(cam, Stand01 + FocusLift, CloseOffset, 2.8f,
                "01_copper_set_spear",
                "Kupfersatz plus Speer, Blickrichtung sued, Startbereich",
                player, Stand01);

            // =============================================================
            // Position 02: Gemischter Satz mit Ember Thorn (SmokeFinal 02)
            // =============================================================
            if (!TryEquipSet(services, "ember_thorn",
                    "armor_iron_helmet", "armor_copper_chest",
                    "armor_iron_gloves", "armor_copper_legs"))
                Fail("Gemischter Satz konnte nicht ausgeruestet werden.");
            player.Combat.TrySelectWeapon("ember_thorn");
            TeleportPlayer(player, Stand02);
            yield return FaceDown(input);
            yield return Frames(20);
            yield return CaptureToFile(cam, Stand02 + FocusLift, CloseOffset, 2.8f,
                "02_mixed_ember_thorn",
                "Gemischter Satz plus Ember Thorn, Blickrichtung sued, Startbereich",
                player, Stand02);

            // =============================================================
            // Position 03: Eisenaxt-Ernte-Pose (SmokeFinal 03)
            // =============================================================
            var inventory = services.GameSession.PlayerInventory;
            inventory.Clear();
            if (inventory.Add("iron_axe", 1) != 0)
                Fail("Eisenaxt konnte nicht zum Inventar hinzugefuegt werden.");
            // Nach Namen sortieren, bevor der erste Baum genommen wird.
            // FindObjectsByType liefert keine zugesicherte Reihenfolge; ohne
            // Sortierung waehlten zwei Laeufe verschiedene Baeume (gemessen:
            // Kamera bei (-28,66 | 11,43) gegen (-5,30 | -22,25)). Der Name ist
            // die NodeInstanceId aus dem Zonengenerator und damit bei fester
            // Weltsaat stabil.
            var tree = Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(n => n.Definition != null && n.Definition.Id == "resource.tree")
                .OrderBy(n => n.name, StringComparer.Ordinal)
                .FirstOrDefault();
            if (tree == null)
                Fail("Kein Baumresource in " + TargetScene + " gefunden.");
            // Richtung vom Baum zur Kamera, aus der festen Kameraausrichtung
            // abgeleitet statt aus cam.transform.position: die Kamera driftet
            // bis zur Aufnahme noch nach, der Baum steht fest.
            var approachDir = -(CaptureRotation * Vector3.forward);
            approachDir.y = 0f;
            approachDir = approachDir.normalized;
            var stand03 = tree.transform.position + approachDir * HarvestApproachDistance
                          + Vector3.up * 0.05f;
            TeleportPlayer(player, stand03);
            Physics.SyncTransforms();
            yield return FaceDown(input);
            player.Interaction.RefreshTargetsNow();
            input.SetVirtualInteract(held: true);
            deadline = Time.realtimeSinceStartup + 5f;
            while (!(player.VisualAnimator.HarvestVisual?.ToolVisible ?? false)
                   && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return CaptureToFile(cam, stand03 + FocusLift, CloseOffset, 2.8f,
                "03_iron_axe_harvest",
                "Eisenaxt-Ernte-Pose am Baum, Startbereich",
                player, stand03);
            input.SetVirtualInteract(held: false);

            // =============================================================
            // Position 04: Leere Bodenflaeche ohne Objekte (neu fuer G-002)
            // =============================================================
            player.gameObject.SetActive(false);
            var resources = Object.FindObjectsByType<ResourceNode>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var r in resources)
                r.gameObject.SetActive(false);
            // G-002: auch den Bodenbewuchs ausblenden. Position 04 misst das
            // Bodenmaterial selbst (Abnahmekriterien 1 und 2); Bewuchs im Bild
            // wuerde Nachbardelta und Streuung erhoehen, ohne dass der Boden
            // besser geworden waere. Ohne Bewuchs ist die Messung strenger.
            var cover = FindGroundCoverRoots();
            foreach (var c in cover)
                c.SetActive(false);
            yield return Frames(4);
            // Fester Weltpunkt im offenen Bereich der Zone
            var groundFocus = new Vector3(0f, 0.05f, 6f);
            yield return CaptureToFile(cam, groundFocus, GroundOffset, 6f,
                "04_boden_leer",
                "Leere Bodenflaeche ohne Objekte, offener Bereich Zone_Greenwood");

            // =============================================================
            // Position 05: Gleiche Kamera, Bodenbewuchs sichtbar (neu fuer G-002)
            // =============================================================
            // Bewusst identische Kamerageometrie wie 04. Die beiden Bilder
            // unterscheiden sich in genau einer Sache, und damit belegt der
            // Vergleich Abnahmekriterium 5 (keine bildschirmgrossen leeren
            // Flaechen) ohne weitere Annahmen.
            foreach (var c in cover)
                if (c != null) c.SetActive(true);
            yield return Frames(4);
            yield return CaptureToFile(cam, groundFocus, GroundOffset, 6f,
                "05_boden_bewuchs",
                "Gleiche Bodenflaeche mit Bodenbewuchs, offener Bereich Zone_Greenwood");

            // =============================================================
            // Bildratenmessung (neu fuer G-002, Abnahmekriterium 8)
            // =============================================================
            yield return MeasureFrameTimes(cam, groundFocus, cover);

            // Zustand wiederherstellen
            foreach (var r in resources)
                if (r != null) r.gameObject.SetActive(true);
            player.gameObject.SetActive(true);

            // Report schreiben und beenden
            WriteReport();
            Debug.Log($"G001 VISUAL ABNAHME: PASS ({_records.Count} Positionen)");
            Application.Quit(0);
        }

        // Misst die reine Renderzeit je Frame in drei Konfigurationen und legt
        // das Ergebnis in den Report.
        //
        // Gemessen wird an der Bodenansicht (Position 04/05): dort fuellt der
        // Boden das Bild vollstaendig aus, die zusaetzlichen Texturabtastungen
        // des neuen Shaders schlagen also so stark durch wie ueberhaupt
        // moeglich. Ein guenstigerer Ausschnitt haette den Nachweis wertlos
        // gemacht.
        //
        // Die drei Konfigurationen:
        //   boden_vorher  Basisboden auf URP/Unlit, kein Bewuchs - der Stand
        //                 vor G-002
        //   boden_nachher Basisboden auf Eidren/Ground Detail, kein Bewuchs -
        //                 misst allein die beiden zusaetzlichen Abtastungen
        //   boden_bewuchs zusaetzlich der gestreute Bodenbewuchs - misst die
        //                 zusaetzlichen Renderer
        private IEnumerator MeasureFrameTimes(Camera cam, Vector3 focus, List<GameObject> cover)
        {
            const int WarmFrames = 60;
            const int SampleFrames = 240;

            if (cam == null)
                yield break;

            // Zeit- und Bildratensteuerung fuer die Messung freigeben. Mit
            // captureFramerate laeuft die Uhr in festen Schritten und die
            // gemessene Realzeit je Frame waere die Renderzeit; mit vSync
            // waere sie an die Bildwiederholrate des Monitors geklemmt. Beides
            // muss weg, sonst misst die Messung den Monitor.
            int previousCapture = Time.captureFramerate;
            int previousVSync = QualitySettings.vSyncCount;
            int previousTarget = Application.targetFrameRate;
            float previousScale = Time.timeScale;
            Time.captureFramerate = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Time.timeScale = 0f;

            cam.transform.SetPositionAndRotation(focus + GroundOffset, CaptureRotation);
            cam.orthographic = true;
            cam.orthographicSize = 6f;

            // Die Basisboden-Renderer und ihr aktueller Shader. Fuer die
            // Vorher-Messung wird auf URP/Unlit zurueckgeschaltet und danach
            // wieder zurueck - der Ausgangszustand bleibt erhalten.
            var groundMaterials = new List<Material>();
            var detailShader = Shader.Find("Eidren/Ground Detail");
            var plainShader = Shader.Find("Universal Render Pipeline/Unlit");
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var mat = mr.sharedMaterial;
                if (mat != null && mat.shader == detailShader && !groundMaterials.Contains(mat))
                    groundMaterials.Add(mat);
            }

            foreach (var c in cover)
                if (c != null) c.SetActive(false);

            if (plainShader != null && detailShader != null && groundMaterials.Count > 0)
            {
                foreach (var m in groundMaterials) m.shader = plainShader;
                yield return MeasureOne(cam, "boden_vorher",
                    "Basisboden auf URP/Unlit, ohne Bodenbewuchs (Stand vor G-002)",
                    WarmFrames, SampleFrames);
                foreach (var m in groundMaterials) m.shader = detailShader;
            }
            else
            {
                Debug.LogWarning("G-002: Vorher-Konfiguration nicht messbar - "
                    + $"URP/Unlit gefunden: {plainShader != null}, "
                    + $"Ground-Detail-Materialien: {groundMaterials.Count}");
            }

            yield return MeasureOne(cam, "boden_nachher",
                "Basisboden auf Eidren/Ground Detail, ohne Bodenbewuchs",
                WarmFrames, SampleFrames);

            foreach (var c in cover)
                if (c != null) c.SetActive(true);
            yield return MeasureOne(cam, "boden_bewuchs",
                "Basisboden auf Eidren/Ground Detail, mit Bodenbewuchs",
                WarmFrames, SampleFrames);

            Time.timeScale = previousScale;
            Application.targetFrameRate = previousTarget;
            QualitySettings.vSyncCount = previousVSync;
            Time.captureFramerate = previousCapture;
        }

        private IEnumerator MeasureOne(Camera cam, string id, string description,
            int warmFrames, int sampleFrames)
        {
            for (int i = 0; i < warmFrames; i++)
                yield return EndOfFrame;

            var samples = new double[sampleFrames];
            double last = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < sampleFrames; i++)
            {
                yield return EndOfFrame;
                double now = Time.realtimeSinceStartupAsDouble;
                samples[i] = (now - last) * 1000.0;
                last = now;
            }

            Array.Sort(samples);
            double sum = 0;
            for (int i = 0; i < samples.Length; i++) sum += samples[i];

            int visible = 0;
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            foreach (var r in Object.FindObjectsByType<Renderer>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r.enabled && GeometryUtility.TestPlanesAABB(planes, r.bounds))
                    visible++;
            }

            var record = new FrameTimeRecord
            {
                configurationId = id,
                description = description,
                measuredFrames = sampleFrames,
                meanMilliseconds = (float)(sum / samples.Length),
                medianMilliseconds = (float)samples[samples.Length / 2],
                percentile95Milliseconds = (float)samples[(int)(samples.Length * 0.95f)],
                visibleRenderers = visible
            };
            _frameTimes.Add(record);
            Debug.Log($"G-002 BILDRATE [{id}] Median {record.medianMilliseconds:F3} ms "
                + $"({1000f / Mathf.Max(0.001f, record.medianMilliseconds):F0} FPS), "
                + $"Mittel {record.meanMilliseconds:F3} ms, "
                + $"95. Perzentil {record.percentile95Milliseconds:F3} ms, "
                + $"sichtbare Renderer {record.visibleRenderers}");
        }

        // Sucht die Bewuchs-Knoten aller geladenen Szenen.
        //
        // Ueber den Namen statt ueber einen Typ: der Bewuchs besteht aus
        // Prefab-Instanzen ohne eigenes Skript, es gibt also keine Komponente,
        // nach der sich suchen liesse. Ein leeres Ergebnis ist zulaessig - dann
        // ist in dieser Zone kein Bewuchs gebaut, und Position 04 misst wie
        // bisher.
        private static List<GameObject> FindGroundCoverRoots()
        {
            var found = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
                    {
                        if (t.name == GroundCoverRootName)
                            found.Add(t.gameObject);
                    }
                }
            }
            return found;
        }

        // Rendert die Szene mit explizit gesetzter Kamera in eine PNG-Datei.
        // Ausloesung erfolgt nach yield return EndOfFrame, also nach einem
        // vollstaendig gerenderten Frame, und erst wenn sich die Ansicht
        // nicht mehr veraendert (siehe WaitForStableView).
        private IEnumerator CaptureToFile(Camera cam, Vector3 focus, Vector3 offset,
            float orthoSize, string positionId, string description,
            PlayerPrefabBindings player = null, Vector3? playerStand = null)
        {
            // Sicherstellen, dass der aktuelle Frame vollstaendig gerendert wurde
            yield return EndOfFrame;

            if (cam == null)
                Fail("Keine Kamera fuer Position " + positionId);

            // Spielzeit anhalten. Die Sprite-Darstellung laeuft durchweg ueber
            // Time.time (SpriteActorPresentation.Atlas), die Ansicht aendert
            // sich also ohne Stopp in jedem Frame weiter - deshalb liefen die
            // Positionen 01 und 02 zuvor in das Stabilitaets-Zeitlimit.
            float previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Nachsetzen: der CharacterController rueckt nach dem Teleport noch
            // nach. Bei angehaltener Zeit bleibt der Spieler jetzt, wo er soll.
            if (player != null && playerStand.HasValue)
            {
                TeleportPlayer(player, playerStand.Value);
                Physics.SyncTransforms();
            }

            // Kamerasteuerung stilllegen. IsometricCamera.LateUpdate schiebt die
            // Kamera per Vector3.SmoothDamp und zieht orthographicSize per
            // Mathf.Lerp nach. Beides konvergiert nur asymptotisch, erreicht den
            // Zielwert also nie exakt - mit laufender Steuerung koennen zwei
            // Laeufe grundsaetzlich nicht dasselbe Bild liefern.
            var rig = cam.GetComponent<IsometricCamera>();
            bool rigWasEnabled = rig != null && rig.enabled;
            if (rig != null)
                rig.enabled = false;

            var previousPos = cam.transform.position;
            var previousRot = cam.transform.rotation;
            var previousOrthoSize = cam.orthographicSize;
            var previousCullingMask = cam.cullingMask;
            var previousRect = cam.rect;

            // UI-Canvases ausblenden
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var canvasStates = new bool[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null)
                {
                    canvasStates[i] = canvases[i].enabled;
                    canvases[i].enabled = false;
                }
            }

            Texture2D image;
            // Vor dem finally-Block festhalten: dort wird der vorherige
            // Kamerazustand wiederhergestellt, danach sind die Aufnahmewerte weg.
            Vector3 usedCamPos = focus + offset;
            Vector3 usedCamEuler = CaptureRotation.eulerAngles;
            try
            {
                cam.transform.position = focus + offset;
                cam.transform.rotation = CaptureRotation;
                cam.orthographicSize = orthoSize;
                cam.cullingMask = -1;
                cam.rect = new Rect(0f, 0f, 1f, 1f);

                // Das Projekt benutzt URP 17.3. Dort ist weder ein manuelles
                // cam.Render() unterstuetzt noch liefert ein zur Laufzeit
                // gesetztes targetTexture ein brauchbares Ergebnis: der erste
                // Weg lieferte Teilbilder ohne Boden, der zweite eine gar nicht
                // beschriebene, reinweisse Textur. Aufgenommen wird deshalb der
                // fertige Frame, also genau das, was URP tatsaechlich darstellt.
                yield return WaitForStableView(positionId);
                LogGroundDiagnostics(positionId, cam);
                image = _lastCapture;
                _lastCapture = null;
            }
            finally
            {
                cam.transform.position = previousPos;
                cam.transform.rotation = previousRot;
                cam.orthographicSize = previousOrthoSize;
                cam.cullingMask = previousCullingMask;
                cam.rect = previousRect;
                for (int i = 0; i < canvases.Length; i++)
                    if (canvases[i] != null)
                        canvases[i].enabled = canvasStates[i];
                if (rig != null)
                    rig.enabled = rigWasEnabled;
                Time.timeScale = previousTimeScale;
            }

            if (image == null)
                Fail("Keine Aufnahme fuer Position " + positionId + " erhalten.");

            _captureWidth = image.width;
            _captureHeight = image.height;

            // Helligkeitsstreuung pruefen
            float stdDev = ComputeBrightnessStdDev(image);
            bool isMonochrome = stdDev < MonochromeThreshold;

            // PNG schreiben
            string filename = positionId + ".png";
            string path = Path.Combine(_output, filename);
            byte[] pngBytes = image.EncodeToPNG();
            Object.Destroy(image);
            File.WriteAllBytes(path, pngBytes);

            if (!File.Exists(path) || new FileInfo(path).Length < 1000)
                Fail("PNG wurde nicht geschrieben: " + path);

            if (isMonochrome)
                Debug.LogWarning($"G001 WARNUNG: Position '{positionId}' ist einfarbig (stdDev={stdDev:F2}). Ursache pruefen.");
            else
                Debug.Log($"G001: Position '{positionId}' OK (stdDev={stdDev:F2})");

            _records.Add(new AbnahmeRecord
            {
                positionId = positionId,
                description = description,
                file = filename,
                cameraPosition = FormatVector(usedCamPos),
                cameraRotationEuler = FormatVector(usedCamEuler),
                orthographicSize = orthoSize,
                capturedUtc = DateTime.UtcNow.ToString("O"),
                brightnessStdDev = stdDev,
                isMonochrome = isMonochrome,
                viewStable = _lastViewStable
            });
        }

        // Terrain, Texture-Streaming und Renderer werden asynchron fertig. Eine
        // feste Frame-Zahl abzuwarten ist deshalb ein Ratespiel: im ersten
        // Doppellauf lieferte genau das halb gerenderte Boeden mit weissen
        // Flaechen, und damit zwei nicht deckungsgleiche Laeufe.
        // Stattdessen wird dieselbe Ansicht wiederholt niedrig aufgeloest
        // gerendert, bis sie sich mehrfach hintereinander nicht mehr aendert.
        // Feste Kultur und feste Nachkommastellen. Vector3.ToString() haengt an
        // der Kultur des Systems (Komma statt Punkt) und rundet auf eine Stelle;
        // beides taugt nicht fuer einen Report, der zwischen Laeufen und
        // Maschinen textuell vergleichbar sein soll.
        private static string FormatVector(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F4}, {1:F4}, {2:F4}", v.x, v.y, v.z);
        }

        // Diagnose: klaert, ob fehlende Bodenflaechen an Sichtbarkeit,
        // Material oder Shader liegen. Ohne diese Angaben laesst sich der
        // weisse Hintergrund nicht von einem Renderproblem unterscheiden.
        private static void LogGroundDiagnostics(string positionId, Camera cam)
        {
            if (cam != null)
            {
                Debug.Log($"G001 DIAG [{positionId}] Kamera: clearFlags={cam.clearFlags} " +
                          $"background={cam.backgroundColor} ortho={cam.orthographic} " +
                          $"size={cam.orthographicSize:F2} pos={cam.transform.position} " +
                          $"target={(cam.targetTexture == null ? "<screen>" : "RT")} " +
                          $"enabled={cam.enabled} aktiv={cam.gameObject.activeInHierarchy}");
            }

            int cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
            Debug.Log($"G001 DIAG [{positionId}] Kameras in der Szene: {cameras}");

            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                // Nur die grossflaechigen Bodenflaechen; die vielen kleinen
                // GroundChips sagen nichts ueber das Gesamtbild aus.
                if (r == null || (r.name != "WalkableGround" &&
                                  r.name != "GroundBlend_1" && r.name != "GroundBlend_2"))
                    continue;
                var mat = r.sharedMaterial;
                string matName = mat == null ? "<null>" : mat.name;
                string shaderName = (mat == null || mat.shader == null) ? "<null>" : mat.shader.name;
                bool shaderOk = mat != null && mat.shader != null && mat.shader.isSupported;
                Debug.Log($"G001 DIAG [{positionId}] {r.name}: enabled={r.enabled} " +
                          $"sichtbar={r.isVisible} aktiv={r.gameObject.activeInHierarchy} " +
                          $"layer={r.gameObject.layer} material={matName} shader={shaderName} " +
                          $"shaderUnterstuetzt={shaderOk}");
            }
        }

        private IEnumerator WaitForStableView(string positionId)
        {
            Color32[] previous = null;
            int streak = 0;
            int frames = 0;
            float started = Time.realtimeSinceStartup;
            float deadline = started + StabilityTimeoutSeconds;
            _lastViewStable = false;

            if (_lastCapture != null)
            {
                Object.Destroy(_lastCapture);
                _lastCapture = null;
            }

            while (Time.realtimeSinceStartup < deadline)
            {
                // CaptureScreenshotAsTexture ist nur direkt nach EndOfFrame
                // gueltig und liefert den fertig dargestellten Frame.
                yield return EndOfFrame;
                frames++;

                Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
                Color32[] sample = shot.GetPixels32();
                if (_lastCapture != null)
                    Object.Destroy(_lastCapture);
                _lastCapture = shot;

                // Mindestens ein paar Frames abwarten, damit ein zufaellig
                // gleicher Anfangszustand nicht als "fertig" durchgeht.
                if (frames >= StabilityMinimumFrames && previous != null &&
                    MeanChannelDifference(previous, sample) <= StabilityTolerance)
                {
                    streak++;
                    if (streak >= StabilityStreak)
                    {
                        _lastViewStable = true;
                        Debug.Log($"G001: Ansicht '{positionId}' stabil nach " +
                                  $"{Time.realtimeSinceStartup - started:F2} s ({frames} Frames)");
                        yield break;
                    }
                }
                else
                {
                    streak = 0;
                }

                previous = sample;
            }

            // Kein Abbruch: ein instabiles Bild soll im Vergleich der beiden
            // Laeufe und im Report sichtbar werden, nicht hier stillschweigend
            // verschwinden.
            Debug.LogWarning($"G001 WARNUNG: Ansicht '{positionId}' war nach " +
                             $"{StabilityTimeoutSeconds:F0} s nicht stabil. Aufnahme erfolgt trotzdem.");
        }

        // Mittlere absolute Kanaldifferenz zweier Stichproben, 0..255.
        private static float MeanChannelDifference(Color32[] a, Color32[] b)
        {
            if (a == null || b == null || a.Length != b.Length || a.Length == 0)
                return float.MaxValue;

            // Jeden 7. Pixel abtasten: bei 1920x1080 bleiben ueber 290.000
            // Stichproben, genug fuer die Frage "hat sich etwas veraendert".
            const int step = 7;
            long sum = 0;
            long count = 0;
            for (int i = 0; i < a.Length; i += step)
            {
                sum += Math.Abs(a[i].r - b[i].r);
                sum += Math.Abs(a[i].g - b[i].g);
                sum += Math.Abs(a[i].b - b[i].b);
                count++;
            }
            if (count == 0)
                return float.MaxValue;
            return (float)(sum / (count * 3.0));
        }

        // Mittlere Helligkeitsstreuung (Standardabweichung) ueber alle Pixel.
        // Verwendet ITU-R-Wichtung (0.299 R, 0.587 G, 0.114 B).
        private static float ComputeBrightnessStdDev(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            // Jeden vierten Pixel sampeln (reicht fuer Einfarbigkeitspruefung)
            const int step = 4;
            double sum = 0.0;
            double sumSq = 0.0;
            int count = 0;
            for (int i = 0; i < pixels.Length; i += step)
            {
                var c = pixels[i];
                double brightness = c.r * 0.299 + c.g * 0.587 + c.b * 0.114;
                sum += brightness;
                sumSq += brightness * brightness;
                count++;
            }
            if (count < 2)
                return 0f;
            double mean = sum / count;
            double variance = sumSq / count - mean * mean;
            return (float)Math.Sqrt(Math.Max(0.0, variance));
        }

        private void WriteReport()
        {
            var report = new AbnahmeReport
            {
                positionSetVersion = PositionSetVersion,
                generatedUtc = DateTime.UtcNow.ToString("O"),
                scene = TargetScene,
                buildVersion = Application.version,
                // Tatsaechliche Aufnahmegroesse, nicht die angeforderte
                width = _captureWidth,
                height = _captureHeight,
                captures = _records.ToArray(),
                frameTimes = _frameTimes.ToArray()
            };
            string json = JsonUtility.ToJson(report, prettyPrint: true);
            File.WriteAllText(Path.Combine(_output, "g001-capture-report.json"), json);
        }

        // Gibt jedem Slot des Spielers eine Ausruestung aus der ContentDatabase.
        // Gibt false zurueck wenn ein Item fehlt oder nicht ausgeruestet werden kann.
        private static bool TryEquipSet(EidrenServiceRoot services,
            string weapon, string head, string chest, string hands, string legs)
        {
            var equipment = services.GameSession.PlayerEquipment;
            return TryEquip(services, equipment, EquipmentSlot.Weapon1, weapon)
                && TryEquip(services, equipment, EquipmentSlot.Head, head)
                && TryEquip(services, equipment, EquipmentSlot.Chest, chest)
                && TryEquip(services, equipment, EquipmentSlot.Hands, hands)
                && TryEquip(services, equipment, EquipmentSlot.Legs, legs);
        }

        private static bool TryEquip(EidrenServiceRoot services, PlayerEquipment equipment,
            EquipmentSlot slot, string itemId)
        {
            // ContentDatabase.GetItem wirft KeyNotFoundException statt null zurueckzugeben.
            // G-000 dokumentiert 86 fehlende .asset-Dateien, deshalb ist das ein realer Fall.
            ItemDefinition item;
            try
            {
                item = services.ContentDatabase.GetItem(itemId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"G001: Item '{itemId}' nicht in der ContentDatabase: {ex.Message}");
                return false;
            }
            if (item == null)
            {
                Debug.LogError("G001: Item nicht gefunden: " + itemId);
                return false;
            }
            var stack = ItemStack.Create(item, 1);
            if (!equipment.TryEquip(slot, stack, out string error))
            {
                Debug.LogError($"G001: Ausruesten fehlgeschlagen ({itemId}): {error}");
                return false;
            }
            return true;
        }

        private static void TeleportPlayer(PlayerPrefabBindings player, Vector3 position)
        {
            var cc = player.CharacterController;
            if (cc != null) cc.enabled = false;
            player.transform.position = position;
            if (cc != null) cc.enabled = true;
        }

        private static IEnumerator FaceDown(PlayerInputReader input)
        {
            input.SetGameplayEnabled(enabled: true);
            input.SetVirtualMove(new Vector2(0f, -1f));
            yield return Frames(2);
            input.SetVirtualMove(Vector2.zero);
            yield return Frames(4);
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (op != null && !op.isDone)
                yield return null;
            yield return Frames(16);
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        private static string ReadArgument(string[] args, string key, string fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(args[i + 1]);
            return fallback;
        }

        private static void Fail(string message)
        {
            Debug.LogError("G001 VISUAL ABNAHME: FAIL - " + message);
            Application.Quit(1);
            throw new InvalidOperationException(message);
        }
    }
}
