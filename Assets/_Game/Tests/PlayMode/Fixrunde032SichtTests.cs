using Eidren.Presentation;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;
using UnityEngine;

namespace Eidren.Tests.PlayMode
{
	/// <summary>
	/// F32-007: Die Sichtlinien-Ausblendung arbeitete nur für Akteure. Eine
	/// Kiste hinter einem Fels blieb unsichtbar — im Verlies schwebte nur
	/// noch ihr Markenpreis frei im Raum. Weltobjekte, die sich in die
	/// Sichtliste eintragen, müssen genauso freigehalten werden.
	/// </summary>
	public sealed class Fixrunde032SichtTests
	{
		[UnityTest]
		public IEnumerator VerdeckteKiste_BlendetDenVerdeckerAus()
		{
			GameObject kameraObjekt = new GameObject("F32007_Kamera");
			GameObject kiste = new GameObject("F32007_Kiste");
			GameObject fels = GameObject.CreatePrimitive(PrimitiveType.Cube);
			GameObject ausblender = new GameObject("F32007_Ausblender");
			// Die Ausblendung nimmt Camera.main — im Voll-Lauf lebt oft noch
			// eine Kamera aus einer früher geladenen Szene. Der gemeinsame
			// Testaufbau macht unsere eindeutig und gibt die anderen zurück.
			Testumgebung.Sitzung umgebung = Testumgebung.Bereitstellen(eigeneHauptkamera: kameraObjekt);
			try
			{
				Camera kamera = kameraObjekt.AddComponent<Camera>();
				kameraObjekt.tag = "MainCamera";
				kameraObjekt.transform.position = new Vector3(0f, 8f, -8f);
				kameraObjekt.transform.LookAt(Vector3.zero);

				kiste.transform.position = Vector3.zero;
				kiste.AddComponent<BoxCollider>();

				// Verdecker zwischen Kamera und Kiste. Groß genug für die
				// Verdeckerregel (Höhe >= 1,35 und Breite >= 1,6).
				fels.name = "F32007_Fels";
				fels.transform.position = new Vector3(0f, 2f, -4f);
				fels.transform.localScale = new Vector3(4f, 4f, 1f);
				Renderer felsRenderer = fels.GetComponent<Renderer>();
				Physics.SyncTransforms();

				OcclusionFocusRegistry.Register(kiste.transform);
				ausblender.AddComponent<ActorOcclusionTransparency>();

				// In BILDERN warten, nicht in Spielzeit: Die Ausblendung blendet
				// mit Time.unscaledDeltaTime, und im Batchmodus laufen
				// deltaTime (gedeckelt) und unscaledDeltaTime (sehr klein) weit
				// auseinander. Ein Budget in Spielzeit war nach wenigen Bildern
				// aufgebraucht, während real kaum Zeit vergangen war.
				for (int bild = 0; bild < 3000 && felsRenderer.material.color.a > 0.95f; bild++)
				{
					yield return null;
				}

				Assert.That(felsRenderer.material.color.a, Is.LessThan(0.95f),
					$"Der Fels vor der Kiste muss ausblenden, steht aber auf Alpha {felsRenderer.material.color.a}.");
			}
			finally
			{
				OcclusionFocusRegistry.Unregister(kiste != null ? kiste.transform : null);
				umgebung.Dispose();
				Object.Destroy(ausblender);
				Object.Destroy(fels);
				Object.Destroy(kiste);
				Object.Destroy(kameraObjekt);
			}
			yield return null;
		}
	}
}
