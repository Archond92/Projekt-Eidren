using Eidren.Composition;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eidren.Editor
{
	/// <summary>
	/// Setzt die Verliesbeleuchtung aus den Daten in EidraForgeLicht: Punktlichter,
	/// Umgebungswerte, Nebel und das eine schattenfaehige Richtungslicht.
	/// </summary>
	public static class EidraForgeLichtBuilder
	{
		public static void BauePunktlichter(Transform wurzel)
		{
			foreach (ForgeLicht daten in EidraForgeLicht.Punktlichter)
			{
				EidraForgeLayout.IstAufBoden(daten.X, daten.Z, out float boden);
				GameObject objekt = new GameObject(daten.Id);
				objekt.transform.SetParent(wurzel, worldPositionStays: false);
				objekt.transform.position = new Vector3(daten.X, boden + daten.Hoehe, daten.Z);
				Light licht = objekt.AddComponent<Light>();
				licht.type = LightType.Point;
				licht.range = daten.Reichweite;
				licht.intensity = daten.Intensitaet;
				licht.color = daten.Farbe;
				/* Ausdruecklich gesetzt, nicht vergessen: Zusatzlichter koennen im
				   Projekt ohnehin keine Schatten werfen
				   (m_AdditionalLightShadowsSupported: 0). Ein aktivierter Schalter
				   wuerde nur suggerieren, dass sie es taeten. */
				licht.shadows = LightShadows.None;
			}
		}

		/// <summary>
		/// Umgebungslicht, Nebel und das geerbte Sonnenobjekt auf Verlieswerte.
		/// Die Sonne wird umgestellt, nicht geloescht: sie ist die einzige
		/// schattenfaehige Quelle und traegt die Wandschatten, die seit dem
		/// Verzicht auf Decken die Enge erzeugen.
		/// </summary>
		public static void SetzeSzenenlicht(ZoneController zone)
		{
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = EidraForgeLicht.Umgebung;
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Linear;
			RenderSettings.fogColor = new Color(0.04f, 0.04f, 0.05f);
			RenderSettings.fogStartDistance = 30f;
			RenderSettings.fogEndDistance = 90f;

			Light sonne = null;
			if (zone.SystemsRoot != null)
			{
				Transform gefunden = zone.SystemsRoot.Find("Sun");
				if (gefunden != null)
				{
					sonne = gefunden.GetComponent<Light>();
				}
			}
			if (sonne == null)
			{
				foreach (Light kandidat in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
				{
					if (kandidat.type == LightType.Directional)
					{
						sonne = kandidat;
						break;
					}
				}
			}
			if (sonne == null)
			{
				Debug.LogWarning("[SchmiedeLicht] Kein Richtungslicht gefunden - Wandschatten fehlen.");
				return;
			}
			sonne.intensity = EidraForgeLicht.Richtungsintensitaet;
			sonne.color = EidraForgeLicht.Richtungsfarbe;
			sonne.shadows = LightShadows.Soft;
			sonne.transform.rotation = Quaternion.Euler(EidraForgeLicht.Richtungswinkel);
		}
	}
}
