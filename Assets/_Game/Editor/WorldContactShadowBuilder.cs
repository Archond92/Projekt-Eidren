// G-003: Kontaktabdunklung unter stehenden Weltobjekten.
//
// Abschnitt 4.4 des Entwurfs: ein weiches Bodendecal verankert jedes stehende
// Objekt am Boden. Der Mechanismus ist dieselbe Bauart wie der
// DynamicActorGroundShadow der Figuren (gleicher Shader, gleiche Tonwerte),
// nur ohne Skript - Weltobjekte bewegen sich nicht, also wird Position und
// Versatz einmal beim Bauen bestimmt. G-004 erweitert von hier aus:
// Bewegung, Sprung, Ausweichrolle und Hangneigung gehoeren dorthin.
//
// Zwei Wege zum Ziel:
// - Ressourcen-Visuals (Baeume, Felsen, Adern, Bueshe) entstehen zur Laufzeit
//   aus Prefabs. Das Decal wird deshalb IN die Prefabs gebaut - jede Instanz
//   im Spiel bekommt es ohne Szenen-Rebuild.
// - AuthoredDecoration-Objekte sind gebackene Szeneninstanzen. Sie bekommen
//   das Decal je Instanz ueber AddToDecorations, aufgerufen aus
//   ZoneLightingBuilder.Apply, der die Szenen ohnehin oeffnet.
//
// Der Bodenbewuchs (Instanzen "{Zone}_Cover_*") bekommt BEWUSST keins: er
// benutzt dieselben SP_-Prefabs wie die Dekoration, ist aber Ausstattung -
// flache Bueschel, die im Boden aufgehen, kein stehendes Objekt. Deshalb
// liegt das Decal auch nicht in den SP_-Prefabs selbst.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eidren.Editor
{
public static class WorldContactShadowBuilder
{
	public const string ChildName = "ContactShadow";

	private const string TexturePath = "Assets/_Game/Art/World/world_contact_shadow.png";

	private const string MaterialPath = "Assets/_Game/Art/World/Materials/M_World_ContactShadow.mat";

	// Spitzendeckung der Textur wie bei den Figurenschatten (111/255): mit dem
	// Materialalpha 0,38 ergibt das dieselbe effektive Spitzendeckung von rund
	// 17 Prozent, die auch unter den Figuren liegt. Ein Wert, eine Wirkung.
	private const float PeakAlpha = 111f / 255f;

	// Fussabdruck-Formung: etwas breiter als das Objekt, in der Tiefe gestaucht
	// wie die Figurenschatten (1,22 breit zu 0,48 tief bei ~0,6 breiter Figur).
	private const float WidthScale = 1.1f;
	private const float DepthScale = 0.55f;

	// G-004: Aufnahmeregel. Ein Kontaktdecal bekommt, was hoeher steht als
	// dieser Wert — Bodenplatten, Wege und flacher Bewuchs fallen darunter
	// heraus, ohne dass jemand eine Ausnahmeliste pflegen muss.
	//
	// 0,2 statt der urspruenglich vorgeschlagenen 0,3: Bei 0,3 fielen fuenf
	// Ressourcen im ABGEBAUTEN Zustand unter die Regel, waehrend ihr aktiver
	// Zustand darueber lag (FiberPlant 0,906 -> 0,250; SwampHemp 1,378 ->
	// 0,228; dazu drei Zonenvarianten). Active- und Exhausted-Prefab tauschen
	// zur Laufzeit — der Bodenschatten waere im Moment des Abbauens sichtbar
	// weggesprungen. Messung aller 80 Prefabs: G004_HOEHENTABELLE.md.
	public const float MindestHoehe = 0.2f;

	[MenuItem("Eidren/Art/Lighting/Build World Contact Shadows (G-003)")]
	public static void BuildAll()
	{
		EnsureMaterial();
		int count = 0;
		foreach (string path in PrefabPaths())
		{
			count += AddToPrefab(path) ? 1 : 0;
		}
		AssetDatabase.SaveAssets();
		Debug.Log("WorldContactShadowBuilder: " + count + " Prefabs mit Kontaktschatten versehen.");
	}

	public static void BuildAllForAutomation()
	{
		BuildAll();
	}

	private static IEnumerable<string> PrefabPaths()
	{
		IEnumerable<string> visuals = Directory.GetFiles("Assets/_Game/Prefabs/Resources/Visuals", "*.prefab");
		IEnumerable<string> variants = Directory.GetFiles("Assets/_Game/Prefabs/Environment/AreaArtVariants", "*.prefab");
		return visuals.Concat(variants).Select((string p) => p.Replace('\\', '/'));
	}

	/* Haengt das Decal an ein BESTEHENDES Prefab auf der Platte. Delegiert an
	   Attach, damit Aufnahmeregel und Bounds-Berechnung an genau einer Stelle
	   stehen — vorher rechnete diese Methode eigenstaendig ueber ALLE Renderer
	   und ohne Hoehenregel.

	   Gebraucht fuer Prefabs, die kein Builder mehr erzeugt: StorageChest und
	   Workbench tragen seit dem 05.08. handgebaute Geometry_A14-Geometrie und
	   werden von ihren Buildern per Etappe-2-Entscheid uebersprungen. Bei ihnen
	   ist der direkte Anbau unbedenklich — es gibt keinen Lauf, der ihn wieder
	   wegwerfen koennte, und GroundContactTests bewacht sie trotzdem. */
	public static bool AddToPrefab(string path)
	{
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			if (!Attach(root))
			{
				return false;
			}
			PrefabUtility.SaveAsPrefabAsset(root, path);
			return true;
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	/* G-004: Haengt das Kontaktdecal an ein im Bau befindliches Objekt. Aufruf
	   aus dem Save()-Engpass jedes erzeugenden Builders, unmittelbar vor
	   SaveAsPrefabAsset. Damit ist die Erdung Teil des Bauwegs statt eine
	   nachtraegliche Dekoration — genau der Grund, warum die G-003-Decals im
	   Stilumbau verlorengingen: BuildAll() hatte sie korrekt gesetzt, die
	   Etappen 1 bis 5 haben dieselben Prefabs anschliessend neu geschrieben.
	   Idempotent: ein vorhandenes Kind wird nicht verdoppelt. */
	public static bool Attach(GameObject root)
	{
		if (root == null || root.transform.Find(ChildName) != null)
		{
			return false;
		}
		// NUR MeshRenderer. Die drei Weltkisten tragen zusaetzlich die
		// Sprite-Glyphen OpeningHand_Left/Right (F-005); ueber alle Renderer
		// gemessen ergaeben sie 11,4 x 6,15 x 8,26 statt der tatsaechlichen
		// 1,2 x 0,59 x 0,79 — das Decal waere am Breitendeckel abgeschnitten
		// und laege als Platte um die Kiste.
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

	// Fuer die AuthoredDecoration-Instanzen einer geoeffneten Szene. Cover-
	// Instanzen des Bodenbewuchses werden ueber den Namen ausgeschlossen.
	public static int AddToDecorations(Transform areaRoot, Material material)
	{
		Transform decorationRoot = areaRoot.Find("AuthoredDecoration");
		if (decorationRoot == null)
		{
			return 0;
		}
		int count = 0;
		foreach (Transform decoration in decorationRoot)
		{
			if (decoration.name.Contains("_Cover_") || decoration.Find(ChildName) != null)
			{
				continue;
			}
			Renderer[] renderers = decoration.GetComponentsInChildren<Renderer>(includeInactive: true);
			if (renderers.Length == 0)
			{
				continue;
			}
			Bounds bounds = renderers[0].bounds;
			for (int i = 1; i < renderers.Length; i++)
			{
				bounds.Encapsulate(renderers[i].bounds);
			}
			// bounds liegen im Weltraum der Szene; CreateDecal rechnet lokal
			// zum Objekt, deshalb Mittelpunkt relativ zur Instanz uebergeben.
			CreateDecal(decoration, bounds, material);
			count++;
		}
		return count;
	}

	public static Material EnsureMaterial()
	{
		EnsureTexture();
		Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
		if (material == null)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
			material = new Material(Shader.Find("Eidren/Actors/GroundShadow"));
			AssetDatabase.CreateAsset(material, MaterialPath);
		}
		material.shader = Shader.Find("Eidren/Actors/GroundShadow");
		// Farbton wie die Figurenschatten; das Alpha ist hier die alleinige
		// Deckkraftsteuerung, es gibt keine Komponente daneben.
		material.SetColor("_Color", new Color(0.11f, 0.15f, 0.18f, 0.38f));
		material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
		EditorUtility.SetDirty(material);
		return material;
	}

	private static void CreateDecal(Transform parent, Bounds worldBounds, Material material)
	{
		GameObject decal = new GameObject(ChildName);
		decal.transform.SetParent(parent, worldPositionStays: false);

		MeshFilter filter = decal.AddComponent<MeshFilter>();
		filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
		MeshRenderer renderer = decal.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = material;
		renderer.shadowCastingMode = ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.lightProbeUsage = LightProbeUsage.Off;
		renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

		// Versatz in Schattenrichtung, dieselbe Formel wie
		// DynamicActorGroundShadow.LateUpdate mit lightOffset 0,18: die
		// Horizontalkomponente der Sonnenrichtung, skaliert mit ihrer Flachheit.
		Vector3 toSun = -(Quaternion.Euler(ZoneLightingBuilder.SunEuler) * Vector3.forward);
		Vector3 shadowDir = new Vector3(0f - toSun.x, 0f, 0f - toSun.z);
		float flatness = Mathf.Clamp01(1f - Mathf.Abs(toSun.y));
		if (shadowDir.sqrMagnitude > 0.001f)
		{
			shadowDir.Normalize();
		}
		Vector3 offset = shadowDir * (0.18f * (0.35f + flatness));

		// Fusspunkt: XZ-Mitte der Renderergrenzen auf Bodenhoehe des Objekts,
		// leicht angehoben gegen Z-Streit mit dem Boden (Wert wie
		// DynamicActorGroundShadow.groundLift).
		decal.transform.position = new Vector3(worldBounds.center.x, parent.position.y, worldBounds.center.z) + offset + Vector3.up * 0.018f;
		// Quad zeigt +Z; flach auf den Boden drehen, Laengsachse in
		// Schattenrichtung wie beim Figurenschatten.
		Vector3 up = ((shadowDir.sqrMagnitude > 0.001f) ? shadowDir : Vector3.back);
		decal.transform.rotation = Quaternion.LookRotation(Vector3.down, up);

		float width = Mathf.Clamp(worldBounds.size.x, 0.4f, 4.5f) * WidthScale;
		float depth = Mathf.Clamp(worldBounds.size.z, 0.3f, 3.5f) * DepthScale;
		decal.transform.localScale = new Vector3(width, depth, 1f);
	}

	// G-003: Die sechs Akteur-Schattentexturen aus dem Dekompilat tragen ihre
	// Form als kleine, hochkant stehende Ellipse in einer 384x128-Leinwand -
	// rund ein Sechstel der Breite ist ueberhaupt gedeckt (Spieler: Median-
	// Alpha 0, Maximum 111). Auf die Weltgrundflaeche von baseSize gespannt
	// bleibt davon ein unsichtbarer Fleck von ~0,19 Einheiten Breite. Diese
	// Methode ersetzt den Inhalt durch eine formatfuellende weiche Ellipse
	// derselben Bauart wie das Welt-Decal; die Spitzendeckung 111/255 der
	// Originale bleibt erhalten, damit die Deckkraftrechnung der Komponente
	// unveraendert gilt. Die Originaldateien liegen in der Sicherung vom
	// 05.08.2026.
	[MenuItem("Eidren/Art/Lighting/Regenerate Actor Shadow Textures (G-003)")]
	public static void RegenerateActorShadowTextures()
	{
		string[] paths = Directory.GetFiles("Assets/_Game/Resources/Art/Actors", "*_ground_shadow.png", SearchOption.AllDirectories);
		foreach (string raw in paths)
		{
			string path = raw.Replace('\\', '/');
			WriteSoftEllipse(path, 384, 128, PeakAlpha);
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
			importer.textureType = TextureImporterType.Default;
			importer.alphaIsTransparency = true;
			importer.mipmapEnabled = false;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.SaveAndReimport();
		}
		Debug.Log("WorldContactShadowBuilder: " + paths.Length + " Akteur-Schattentexturen regeneriert.");
	}

	public static void RegenerateActorShadowTexturesForAutomation()
	{
		RegenerateActorShadowTextures();
	}

	private static void WriteSoftEllipse(string path, int width, int height, float peakAlpha)
	{
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
		Color32[] pixels = new Color32[width * height];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float nx = ((float)x + 0.5f) / width * 2f - 1f;
				float ny = ((float)y + 0.5f) / height * 2f - 1f;
				float r = Mathf.Sqrt(nx * nx + ny * ny);
				// Kantengeglaettetes Abfallen von r=0,25 (volle Deckung) bis
				// r=1 (null). Bewusst nicht Mathf.SmoothStep: das ist ein
				// geglaetteter Lerp zwischen zwei WERTEN und liefert bei r=0
				// den Startwert 0,25 statt 0 - die Spitzendeckung laege dann
				// ein Viertel unter dem Sollwert.
				float t = Mathf.Clamp01((r - 0.25f) / 0.75f);
				float falloff = 1f - t * t * (3f - 2f * t);
				byte a = (byte)Mathf.RoundToInt(255f * peakAlpha * falloff);
				pixels[y * width + x] = new Color32(28, 38, 46, a);
			}
		}
		texture.SetPixels32(pixels);
		File.WriteAllBytes(path, texture.EncodeToPNG());
		UnityEngine.Object.DestroyImmediate(texture);
		AssetDatabase.ImportAsset(path);
	}

	private static void EnsureTexture()
	{
		if (File.Exists(TexturePath))
		{
			return;
		}
		Directory.CreateDirectory(Path.GetDirectoryName(TexturePath));

		// Deterministisch erzeugte weiche Ellipse: Alpha faellt vom Zentrum
		// mit smoothstep auf 0 am Rand. Peak wie die Figurenschatten-Texturen.
		WriteSoftEllipse(TexturePath, 256, 128, PeakAlpha);
		TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
		importer.textureType = TextureImporterType.Default;
		importer.alphaIsTransparency = true;
		importer.mipmapEnabled = false;
		importer.wrapMode = TextureWrapMode.Clamp;
		importer.textureCompression = TextureImporterCompression.Uncompressed;
		importer.SaveAndReimport();
	}
}
}
