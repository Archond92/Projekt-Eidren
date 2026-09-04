using UnityEditor;

namespace Eidren.Editor
{
// G-002: Importvorgaben fuer die beiden geteilten Graustufenlagen des Bodens.
//
// Die Standardvorgaben von Unity sind fuer Farbtexturen gedacht und beschaedigen
// bei diesen beiden Bildern genau das, wofuer sie da sind. Gemessen wurde ein
// Nachbarpixel-Abstand von 6,012 auf der Erdflaeche, waehrend die Textur selbst
// 0,0408 (in 0..1) traegt - rechnerisch waeren daraus rund 2,6 Punkte Zugewinn
// zu erwarten gewesen, angekommen sind 0,55. Drei Vorgaben erklaeren die
// Luecke:
//
//   sRGBTexture      Die Lagen sind Masken, keine Farben. Als sRGB gelesen
//                    wird 0,5 zu 0,21 linear: die Flaeche wird dunkler und die
//                    Schwankung um die Mitte flacher, weil die sRGB-Kurve dort
//                    weniger steil ist.
//   textureCompression DXT quantisiert in 4x4-Bloecken. Genau die Koernung von
//                    einem bis zwei Pixeln, auf die es hier ankommt, faellt
//                    darin zusammen. 1024x1024 unkomprimiert kostet 1 MiB je
//                    Lage - fuer zwei geteilte Texturen vertretbar.
//   mipmapEnabled    Ein Texel deckt bei der Spielkamera etwa einen
//                    Bildschirmpixel ab; die Tiefenachse liegt durch die
//                    52-Grad-Neigung knapp darueber, weshalb die GPU auf
//                    Mip 1 ausweicht und die Haelfte der Frequenz mittelt.
//                    Die Kamera ist auf orthografische Groesse 6 festgelegt,
//                    ein Herauszoomen gibt es nicht - Mipstufen bringen hier
//                    also keinen Nutzen, den sie kosten duerften.
//
// Als AssetPostprocessor statt als einmaliger Lauf: Tools\G002-Textures.ps1
// erzeugt die Bilder neu, und Unity importiert sie danach wieder mit den
// Standardvorgaben. Eine einmalige Korrektur waere beim naechsten Lauf weg.
public sealed class GroundMapImportSettings : AssetPostprocessor
{
	private static readonly string[] Maps =
	{
		"Assets/_Game/Art/Zones/Shared/ground_detail_grain_v01.png",
		"Assets/_Game/Art/Zones/Shared/ground_macro_variation_v01.png"
	};

	private void OnPreprocessTexture()
	{
		if (System.Array.IndexOf(Maps, assetPath) < 0) { return; }

		TextureImporter importer = (TextureImporter)assetImporter;
		importer.sRGBTexture = false;
		importer.textureCompression = TextureImporterCompression.Uncompressed;
		importer.mipmapEnabled = false;
		importer.filterMode = UnityEngine.FilterMode.Bilinear;
		importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
	}

	// Der Postprozessor greift beim Import. Bereits importierte Bilder behalten
	// ihre alten Vorgaben, bis sie erneut eingelesen werden - deshalb dieser
	// Einstiegspunkt:
	//   Unity.exe -batchmode -quit -projectPath . \
	//     -executeMethod Eidren.Editor.GroundMapImportSettings.ReimportAll
	public static void ReimportAll()
	{
		foreach (string path in Maps)
		{
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			UnityEngine.Debug.Log($"G-002: {path} neu eingelesen.");
		}
		AssetDatabase.Refresh();
		UnityEngine.Debug.Log("G-002 IMPORTVORGABEN: fertig.");
	}
}
}
