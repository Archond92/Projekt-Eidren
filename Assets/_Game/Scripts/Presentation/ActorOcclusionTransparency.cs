using System.Collections.Generic;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Eidren.Presentation
{
	[DefaultExecutionOrder(900)]
	public sealed class ActorOcclusionTransparency : MonoBehaviour
	{
		private const float RefreshInterval = 0.08f;

		private const float MinimumOccluderHeight = 1.35f;

		private const float MinimumOccluderSpan = 1.6f;

		private const float FadedAlpha = 0.24f;

		private const float FadeSpeed = 5.5f;

		private const float CastRadius = 0.18f;

		private readonly RaycastHit[] _hits = new RaycastHit[96];

		private readonly HashSet<Renderer> _requested = new HashSet<Renderer>();

		private readonly Dictionary<Renderer, OcclusionFadeState> _states = new Dictionary<Renderer, OcclusionFadeState>();

		private readonly List<Renderer> _cleanup = new List<Renderer>();

		/// <summary>Hoehe, auf die bei Weltobjekten gezielt wird (Kistenmitte).</summary>
		private const float FokusHoehe = 0.6f;

		/// <summary>Nur Weltobjekte in dieser Kameraentfernung werden freigehalten.</summary>
		private const float FokusReichweite = 28f;

		private MonoBehaviour[] _actors = Array.Empty<MonoBehaviour>();

		private Camera _camera;

		private float _nextRefreshAt;

		private static bool _twinMissingLogged;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Install()
		{
			if (!(UnityEngine.Object.FindFirstObjectByType<ActorOcclusionTransparency>() != null))
			{
				GameObject gameObject = new GameObject("Actor Occlusion Transparency");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<ActorOcclusionTransparency>();
			}
			// Startbeweis in jedem Player.log: Ist der Fade-Zwilling im
			// Paket? Er wurde bereits einmal still aus dem Build gestrippt.
			Shader twin = Resources.Load<Material>("Materials/OcclusionFadeTwin")?.shader ?? Shader.Find("Eidren/World/VertexLitFade");
			if (twin == null)
			{
				Debug.LogError("[Eidren] Fade-Zwilling fehlt im Build — Sichtlinien-Ausblendung ist wirkungslos.");
			}
			else
			{
				Debug.Log("[Eidren] Fade-Zwilling geladen: " + twin.name);
			}
		}

		private void LateUpdate()
		{
			if (Time.unscaledTime >= _nextRefreshAt)
			{
				_nextRefreshAt = Time.unscaledTime + 0.08f;
				RefreshOccluders();
			}
			AnimateFades();
		}

		private void RefreshOccluders()
		{
			if (_camera == null || !_camera.isActiveAndEnabled)
			{
				_camera = Camera.main;
			}
			_requested.Clear();
			if (_camera == null)
			{
				return;
			}
			_actors = System.Linq.Enumerable.ToArray(ActorPresentationRegistry.Active);
			foreach (MonoBehaviour actor in _actors)
			{
				if (actor != null && actor.isActiveAndEnabled)
				{
					CollectOccluders(actor);
				}
			}
			CollectFocusOccluders();
		}

		/// <summary>
		/// F32-007: Weltobjekte aus der Sichtliste genauso freihalten wie
		/// Akteure — eine Kiste hinter einem Fels war sonst unsichtbar,
		/// waehrend ihr Preisschild frei im Raum schwebte.
		///
		/// Nur in Kameranaehe: Jeder Eintrag kostet einen Strahl pro Abtastung,
		/// und was ausserhalb des Bildes liegt, muss niemand freihalten.
		/// </summary>
		private void CollectFocusOccluders()
		{
			Vector3 kamera = _camera.transform.position;
			IReadOnlyList<Component> fokus = OcclusionFocusRegistry.Active;
			for (int i = 0; i < fokus.Count; i++)
			{
				Component eintrag = fokus[i];
				if (eintrag == null || !eintrag.gameObject.activeInHierarchy)
				{
					continue;
				}
				Vector3 mitte = eintrag.transform.position + Vector3.up * FokusHoehe;
				if ((mitte - kamera).sqrMagnitude > FokusReichweite * FokusReichweite)
				{
					continue;
				}
				CollectOccludersTowardsPoint(mitte, mitte.y);
			}
		}

		private void CollectOccluders(MonoBehaviour actor)
		{
			// F31-001: Zwei Strahlen je Akteur — Mitte UND Fusspunkt. Bei der
			// steilen Kamera (52 Grad) laeuft der Mittelstrahl ueber niedrige
			// Stammkapseln hinweg, obwohl die breite Krone die Figur visuell
			// verdeckt; der Fusspunkt-Strahl schneidet den Stamm.
			float actorTopY = actor.transform.position.y + ((IActorPresentation)actor).WorldHeight;
			CollectOccludersTowards(actor, ((IActorPresentation)actor).WorldHeight * 0.48f, actorTopY);
			CollectOccludersTowards(actor, 0.12f, actorTopY);
		}

		private void CollectOccludersTowards(MonoBehaviour actor, float targetHeight, float actorTopY)
		{
			CollectOccludersTowardsPoint(actor.transform.position + Vector3.up * targetHeight, actorTopY);
		}

		private void CollectOccludersTowardsPoint(Vector3 vector, float actorTopY)
		{
			Vector3 position = _camera.transform.position;
			Vector3 direction = vector - position;
			float magnitude = direction.magnitude;
			if (magnitude <= 0.05f)
			{
				return;
			}
			direction /= magnitude;
			int num = Physics.SphereCastNonAlloc(position, 0.18f, direction, _hits, magnitude - 0.05f, -5, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < num; i++)
			{
				Collider collider = _hits[i].collider;
				if (collider == null || IsActorHierarchy(collider.transform) || !IsOccluder(collider, actorTopY))
				{
					continue;
				}
				Renderer[] componentsInChildren = collider.GetComponentsInChildren<Renderer>(includeInactive: true);
				Renderer[] array = componentsInChildren;
				foreach (Renderer renderer in array)
				{
					if (renderer != null && renderer.enabled)
					{
						_requested.Add(renderer);
					}
				}
			}
		}

		/// <summary>
		/// F31-001: Fade-Material fuer einen Verdecker. Der opake Weltshader
		/// kennt weder Blend noch Alphakanal — Materialien an ihm wechseln
		/// auf den Transparenz-Zwilling (Eidren/World/VertexLitFade), alle
		/// uebrigen behalten den URP-Transparenzumbau.
		/// </summary>
		public static Material CreateFadeMaterial(Material source)
		{
			Material clone = new Material(source)
			{
				name = source.name + " (Occlusion Fade)",
				hideFlags = HideFlags.DontSave,
				renderQueue = 3000
			};
			if (source.shader != null && source.shader.name == "Eidren/World/VertexLit")
			{
				// Nach-Release-Fix (18.08.2026): Shader.Find allein reicht im
				// PLAYER nicht — der Zwilling war nirgends referenziert und
				// wurde aus jedem Build gestrippt (im Editor unsichtbar, weil
				// dort alle Shader geladen sind). Das Resources-Material haelt
				// ihn verlaesslich im Paket.
				Shader fadeTwin = Resources.Load<Material>("Materials/OcclusionFadeTwin")?.shader ?? Shader.Find("Eidren/World/VertexLitFade");
				if (fadeTwin == null && !_twinMissingLogged)
				{
					// Lauter Waechter im Build: Ohne diese Zeile scheitert der
					// Fade STILL, wenn der Shader erneut gestrippt wird.
					_twinMissingLogged = true;
					Debug.LogError("[Eidren] Fade-Zwilling fehlt im Build — Sichtlinien-Ausblendung ist wirkungslos.");
				}
				if (fadeTwin != null)
				{
					clone.shader = fadeTwin;
					if (source.HasProperty("_Tint"))
					{
						clone.SetColor("_Tint", source.GetColor("_Tint"));
					}
					clone.SetColor("_Color", Color.white);
					clone.renderQueue = 3000;
					return clone;
				}
			}
			OcclusionFadeState.ApplyTransparencySetup(clone);
			return clone;
		}

		/// <summary>
		/// Nach-Release-Fix (18.08.2026): Neben hohen Verdeckern zaehlt auch,
		/// wessen UNTERKANTE ueber Kopfhoehe des Akteurs haengt — Balken,
		/// Galerien und Rohre im Verlies sind flach, verdecken aber von oben
		/// und fielen durch die reine Hoehenregel.
		/// </summary>
		public static bool IsOccluderFor(Bounds bounds, float actorTopY)
		{
			return IsLargeOccluder(bounds) || bounds.min.y >= actorTopY;
		}

		public static bool IsLargeOccluder(Bounds bounds)
		{
			// F31-001: Hoehe allein entscheidet — ein 2,4 m hoher Baum
			// verdeckt die Figur vollstaendig, egal wie schlank er ist. Die
			// alte Zusatzbedingung (Spanne >= 1,6) liess jeden Baum (1,2)
			// durchfallen und machte die Ausblendung im Freien wirkungslos.
			return bounds.size.y >= 1.35f;
		}

		private static bool IsOccluder(Collider collider, float actorTopY)
		{
			return IsOccluderFor(collider.bounds, actorTopY) || collider.GetComponentInParent<OcclusionFadeTarget>() != null;
		}

		private bool IsActorHierarchy(Transform candidate)
		{
			// F31-001: Verwandtschaft zum Akteur pruefen, NICHT Wurzelgleichheit.
			// Der alte Vergleich gegen transform.root erklaerte in Zonen, deren
			// Inhalt unter einer gemeinsamen Szenenwurzel haengt, ALLES zur
			// Akteurshierarchie — Baeume, Boden, Gegner — und der Dienst hat
			// deshalb nie einen einzigen Verdecker angefragt.
			MonoBehaviour[] actors = _actors;
			foreach (MonoBehaviour actor in actors)
			{
				if (!(actor == null))
				{
					Transform actorTransform = actor.transform;
					if (candidate == actorTransform || candidate.IsChildOf(actorTransform) || actorTransform.IsChildOf(candidate))
					{
						return true;
					}
				}
			}
			// F32-007: Ein Fokusobjekt darf sich nicht selbst ausblenden — der
			// Strahl endet knapp davor, die Kugel streift es aber.
			IReadOnlyList<Component> fokus = OcclusionFocusRegistry.Active;
			for (int i = 0; i < fokus.Count; i++)
			{
				Component eintrag = fokus[i];
				if (eintrag != null)
				{
					Transform fokusTransform = eintrag.transform;
					if (candidate == fokusTransform || candidate.IsChildOf(fokusTransform) || fokusTransform.IsChildOf(candidate))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void AnimateFades()
		{
			foreach (Renderer item in _requested)
			{
				if (!(item == null))
				{
					if (!_states.TryGetValue(item, out var value))
					{
						value = new OcclusionFadeState(item);
						_states.Add(item, value);
					}
					value.Requested = true;
				}
			}
			_cleanup.Clear();
			foreach (KeyValuePair<Renderer, OcclusionFadeState> state in _states)
			{
				Renderer key = state.Key;
				OcclusionFadeState value2 = state.Value;
				if (key == null)
				{
					value2.Dispose();
					_cleanup.Add(key);
					continue;
				}
				float target = (value2.Requested ? 0.24f : 1f);
				value2.Alpha = Mathf.MoveTowards(value2.Alpha, target, 5.5f * Time.unscaledDeltaTime);
				value2.Apply();
				value2.Requested = false;
				if (value2.Alpha >= 0.999f && !_requested.Contains(key))
				{
					value2.Restore();
					value2.Dispose();
					_cleanup.Add(key);
				}
			}
			foreach (Renderer item2 in _cleanup)
			{
				_states.Remove(item2);
			}
		}

		private void OnDestroy()
		{
			foreach (OcclusionFadeState value in _states.Values)
			{
				value.Restore();
				value.Dispose();
			}
			_states.Clear();
		}
	}
}
