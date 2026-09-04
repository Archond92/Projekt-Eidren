using UnityEngine;

namespace Eidren.Editor
{
	/// <summary>
	/// F34-001/F34-002/F34-003: LOD-Schwellen fuer die orthografische Spielkamera.
	/// Die Kamera zoomt nur zwischen 5,5 und 7,4 Weltmetern Halbhoehe; die
	/// Standard-Qualitaetsstufe "Medium" traegt einen LOD-Bias von 0,7. Die alten
	/// Perspektiv-Schwellen (0,55/0,24/0,06) liessen kleine Props bei diesem
	/// Bildausschnitt dauerhaft auf der groebsten Stufe oder blendeten sie ganz
	/// aus (Faserpflanze, Truhen, Bodenloot). Die Schwellen werden deshalb aus
	/// der Bildschirmhoehe des Objekts bei der normalen Spielkamera abgeleitet,
	/// sodass LOD0 im gesamten Spielzoom sichtbar bleibt und die letzte Stufe
	/// wie in F33-001 erst bei 0,01 ausgeblendet wird.
	/// </summary>
	public static class MidpolyLodThresholds
	{
		public const float GameCameraOrthographicSize = 7.4f;

		public const float MediumLodBias = 0.7f;

		public const float CullHeight = 0.01f;

		/// <summary>Bildschirmhoehe eines LODGroups bei der normalen Spielkamera in der Stufe Medium.</summary>
		public static float ScreenHeightAtGameCamera(float groupSize)
		{
			return groupSize / (2f * GameCameraOrthographicSize) * MediumLodBias;
		}

		/// <summary>Setzt die Schwellen relativ zur eigenen Bildschirmhoehe: LOD0 ab 60 %, LOD1 ab 35 %, Ausblenden bei 0,01.</summary>
		public static void ApplyOrthographicThresholds(LODGroup group)
		{
			group.RecalculateBounds();
			float height = ScreenHeightAtGameCamera(group.size);
			LOD[] lods = group.GetLODs();
			for (int i = 0; i < lods.Length; i++)
			{
				lods[i].screenRelativeTransitionHeight = ThresholdFor(i, lods.Length, height);
			}
			group.SetLODs(lods);
		}

		public static float ThresholdFor(int index, int count, float heightAtGameCamera)
		{
			if (index == count - 1)
			{
				return Mathf.Min(CullHeight, heightAtGameCamera * 0.2f);
			}
			return heightAtGameCamera * ((index == 0) ? 0.6f : 0.35f);
		}
	}
}
