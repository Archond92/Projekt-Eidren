using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// Macht byte-identische Platzhalter-Icons der Item-Familien
	/// unterscheidbar (VisualAssetTests.V01ItemsHaveOneUniqueConsistentlyImportedIcon):
	/// die TMP-Platzhalter teilten sich in zwoelf Gruppen exakt dieselben
	/// Pixel (Kupfer==Eisen, Holz==Hartholz, Stein==Granit, ...), was im
	/// Inventar echte Verwechslungen erlaubt. Pro Familie bleibt das
	/// semantische Basis-Icon unveraendert; die Partner erhalten eine
	/// deterministische HSV-Toenung (Eisen kuehles Stahlblau, Hartholz
	/// dunkler und satter, Stein heller, ...). Alpha bleibt unangetastet.
	/// Wachter: getoent wird NUR, wenn das Icon aktuell byte-identisch zu
	/// einem anderen Icon ist — dadurch ist der Lauf idempotent.
	/// </summary>
	public static class ItemIconFamilienToenung
	{
		private const string IconOrdner = "Assets/_Game/Art/Items";

		private struct Ton
		{
			public float HueOverride;

			public bool HatHue;

			public float SatFaktor;

			public float ValFaktor;

			// Mindest-Saettigung: multiplikative Faktoren verpuffen bei grauen
			// (s=0) und weissen (v=1, Clamp) Platzhaltern — genau daran
			// scheiterte die erste Toenungsrunde bei Pickaxe und den
			// Stein-Icons. MinSat hebt Grau erst auf einen sichtbaren Ton,
			// damit der Hue-Override greifen kann.
			public float MinSat;

			public Ton(float sat, float val)
			{
				HueOverride = 0f;
				HatHue = false;
				SatFaktor = sat;
				ValFaktor = val;
				MinSat = 0f;
			}

			public Ton(float hue, float sat, float val)
			{
				HueOverride = hue;
				HatHue = true;
				SatFaktor = sat;
				ValFaktor = val;
				MinSat = 0f;
			}

			public Ton(float hue, float sat, float val, float minSat)
			{
				HueOverride = hue;
				HatHue = true;
				SatFaktor = sat;
				ValFaktor = val;
				MinSat = minSat;
			}
		}

		private static readonly Dictionary<string, Ton> Regeln = new Dictionary<string, Ton>(System.StringComparer.Ordinal)
		{
			// Eisen-Familie: kuehles Stahlblau statt Kupferton.
			{ "ITEM_TMP_IronBar", new Ton(210f / 360f, 0.45f, 0.92f) },
			{ "ITEM_TMP_IronDaggers", new Ton(210f / 360f, 0.45f, 0.92f) },
			{ "ITEM_TMP_IronHammer", new Ton(210f / 360f, 0.45f, 0.92f) },
			{ "ITEM_TMP_IronOre", new Ton(210f / 360f, 0.45f, 0.92f) },
			{ "ITEM_TMP_IronSpear", new Ton(210f / 360f, 0.45f, 0.92f) },
			// Schmiedebeschlag: dunkles Altmessing.
			{ "ITEM_TMP_SmithingFitting", new Ton(0.9f, 0.68f) },
			// Stufenlose Basiswaffen: entsaettigt, damit Kupfer/Eisen herausstechen.
			{ "ITEM_TMP_Daggers", new Ton(0.35f, 1f) },
			{ "ITEM_TMP_Hammer", new Ton(0.35f, 1f) },
			// Werkzeug: warmer Holzstiel-Braunton mit erzwungener Saettigung.
			{ "ITEM_TMP_Pickaxe", new Ton(0.08f, 1f, 0.88f, 0.22f) },
			// Stein: kuehles Hellgrau, per MinSat auch auf Grauwerten sichtbar.
			{ "ITEM_TMP_Stone", new Ton(0.58f, 1f, 1.05f, 0.10f) },
			{ "ITEM_TMP_StoneBlock", new Ton(0.58f, 1f, 1.05f, 0.10f) },
			// Hartholz dunkler und satter als Holz.
			{ "ITEM_TMP_Hardwood", new Ton(1.2f, 0.7f) },
			{ "ITEM_TMP_HardwoodPlank", new Ton(1.2f, 0.7f) },
			// Sumpfhanf ins Moorgruen.
			{ "ITEM_TMP_SwampHemp", new Ton(0.28f, 1.1f, 0.85f) },
			// Robuster Stoff hell und stofflich statt Seil-Ton.
			{ "ITEM_TMP_RobustCloth", new Ton(0.45f, 1.12f) }
		};

		[MenuItem("Eidren/Art/Item-Icon-Familien toenen")]
		public static void Toenen()
		{
			string[] alle = Directory.GetFiles(IconOrdner, "ITEM_TMP_*.png", SearchOption.TopDirectoryOnly);
			Dictionary<string, byte[]> bytes = alle.ToDictionary((string p) => p, File.ReadAllBytes);
			int getoent = 0;
			foreach (string pfad in alle)
			{
				string name = Path.GetFileNameWithoutExtension(pfad);
				if (!Regeln.TryGetValue(name, out Ton ton))
				{
					continue;
				}
				bool duplikat = alle.Any((string anderer) => anderer != pfad && bytes[anderer].Length == bytes[pfad].Length && bytes[anderer].SequenceEqual(bytes[pfad]));
				if (!duplikat)
				{
					continue;
				}
				Texture2D textur = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
				try
				{
					ImageConversion.LoadImage(textur, bytes[pfad]);
					Color[] pixel = textur.GetPixels();
					for (int i = 0; i < pixel.Length; i++)
					{
						Color.RGBToHSV(pixel[i], out float h, out float s, out float v);
						s = Mathf.Clamp01(Mathf.Max(s * ton.SatFaktor, ton.MinSat));
						if (ton.HatHue && s > 0.01f)
						{
							h = ton.HueOverride;
						}
						v = Mathf.Clamp01(v * ton.ValFaktor);
						Color neu = Color.HSVToRGB(h, s, v);
						neu.a = pixel[i].a;
						pixel[i] = neu;
					}
					textur.SetPixels(pixel);
					byte[] ergebnis = textur.EncodeToPNG();
					if (ergebnis.SequenceEqual(bytes[pfad]))
					{
						// Regel hat keine Pixel bewegt — das Duplikat besteht
						// weiter. Laut melden statt still mitzaehlen.
						Debug.LogWarning("[TOENUNG] Regel wirkungslos (Pixel unveraendert): " + name);
						continue;
					}
					File.WriteAllBytes(pfad, ergebnis);
					AssetDatabase.ImportAsset(pfad);
					getoent++;
				}
				finally
				{
					Object.DestroyImmediate(textur);
				}
			}
			Debug.Log($"[TOENUNG] {getoent} Familien-Icons unterscheidbar getoent.");
		}
	}
}
