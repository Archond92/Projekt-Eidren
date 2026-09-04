using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// 3D-Darstellung fuer die Kreaturen der Serie. Schwesterklasse zu
	/// MeshActorPresentation, die fuer den WANDERER gebaut ist und deshalb hier
	/// nicht traegt:
	///
	/// - MeshActorPresentation loest Clips ueber Haltungen auf
	///   ("Ruhe_Dolche", "Angriff_Hammer"). Kreaturen haben keine Haltungen.
	/// - Wo dort ein Clip fehlt, faellt die Schicht auf prozedurale Posen
	///   zurueck, weil die Wanderer.glb Treffer, Taumeln, Tod und Erscheinen
	///   nicht als Clips hat. Jede Kreatur hat sie als echte Clips; ein
	///   Rueckfall waere bei ihnen ein Rueckschritt.
	///
	/// Diese Schicht loest deshalb DIREKT auf: ein Zustand, ein Clipname, kein
	/// Ersatz. Fehlt ein Clip, bleibt der laufende stehen — sichtbar falsch ist
	/// besser als unsichtbar erfunden.
	///
	/// Die Kreaturen der Serie tragen unterschiedlich viele Zustaende:
	/// acht bei den elf V02-Kreaturen, zehn bei den Eidra (Noctarion, Terrock),
	/// neun beim Boss (Garon). ActorVisualState fasst nur acht. Die
	/// darueberhinausgehenden laufen ueber SetAuthoredState(stem), das schon
	/// frei benannte Zustaende kennt; StemToClip uebersetzt den Sprite-Stamm in
	/// den Clipnamen der GLB.
	/// </summary>
	public sealed class CreatureMeshPresentation : MonoBehaviour,
		IActorPresentation, ILocomotionPresentation, IAuthoredStatePresentation
	{
		public const string ClipIdle = "Ruhe";
		public const string ClipMove = "Gehen";
		public const string ClipHit = "Treffer";
		public const string ClipStagger = "Taumeln";
		public const string ClipDeath = "Tod";
		public const string ClipAppear = "Erscheinen";

		/// <summary>
		/// Sprite-Stamm zu Clipname. Deckt alle 16 Figuren der Serie ab.
		/// Die Eidra und der Boss benennen ihre Zustaende anders als die
		/// V02-Kreaturen; ohne diese Tabelle liefe der Einbau bei ihnen ins
		/// Leere (siehe README der jeweiligen Figur).
		/// </summary>
		private static readonly Dictionary<string, string> StemClips =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				// gemeinsame Zustaende
				{ "idle", ClipIdle },
				{ "move", ClipMove },
				{ "hit", ClipHit },
				{ "stagger", ClipStagger },
				{ "death", ClipDeath },
				{ "appear", ClipAppear },
				{ "telegraph", "Telegraph" },
				{ "attack", "Angriff" },
				// Eidra: Noctarion und Terrock
				{ "wildattack", "Wildangriff" },
				{ "flee", "Flucht" },
				{ "shadowstep", "Schattenschritt" },
				{ "backmark", "Schattenmal" },
				{ "rockbreaker", "Felsbrecher" },
				{ "stonehide", "Steinhaut" },
				// Boss: Garon
				{ "front", "Frontschlag" },
				{ "spin", "Wirbel" },
				{ "charge", "Ansturm" },
				{ "return", "Rueckkehr" }
			};

		[SerializeField]
		private Animation animationPlayer;

		[SerializeField]
		private Transform modelRoot;

		[SerializeField]
		private float worldHeight = 1.7f;

		[SerializeField]
		private float turnDegreesPerSecond = 540f;

		[SerializeField]
		private float crossfadeSeconds = 0.12f;

		private static readonly int TintId = Shader.PropertyToID("_Tint");

		private Renderer[] _renderers = Array.Empty<Renderer>();
		private MaterialPropertyBlock _properties;
		private Color _tint = Color.white;
		private string _currentClip;
		private float _targetYaw = 180f;
		private float _currentYaw = 180f;
		private bool _dead;

		public ActorFacing8 Facing { get; private set; } = ActorFacing8.S;

		public ActorVisualState VisualState { get; private set; } = ActorVisualState.Idle;

		public float WorldHeight => worldHeight;

		/// <summary>Zuletzt gespielter Clip — fuer Tests und Diagnose.</summary>
		public string CurrentClip => _currentClip;

		public void Configure(Animation configuredAnimation, Transform configuredModelRoot,
			float configuredWorldHeight)
		{
			animationPlayer = configuredAnimation;
			modelRoot = configuredModelRoot;
			worldHeight = configuredWorldHeight;
			CacheHierarchy();
		}

		// ------------------------------------------------------------------
		// Aufloesung — statisch und damit ohne Szene pruefbar
		// ------------------------------------------------------------------

		/// <summary>Clipname zu einem Zustand des gemeinsamen Enums.</summary>
		public static string ResolveClipName(ActorVisualState state)
		{
			switch (state)
			{
			case ActorVisualState.Idle: return ClipIdle;
			case ActorVisualState.Move: return ClipMove;
			case ActorVisualState.Ability1: return "Telegraph";
			case ActorVisualState.Ability2: return "Angriff";
			case ActorVisualState.Hit: return ClipHit;
			case ActorVisualState.Stagger: return ClipStagger;
			case ActorVisualState.Death: return ClipDeath;
			case ActorVisualState.Appear: return ClipAppear;
			default: return null;
			}
		}

		/// <summary>Clipname zu einem Sprite-Stamm. Null, wenn unbekannt.</summary>
		public static string StemToClip(string stem)
		{
			if (string.IsNullOrEmpty(stem))
			{
				return null;
			}
			return StemClips.TryGetValue(stem, out string clip) ? clip : null;
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
			if (state == ActorVisualState.Appear)
			{
				// Erscheinen loest die Todessperre wieder auf — noetig fuer
				// Wiederbelebung und Gegner-Pooling.
				_dead = false;
			}
			string clip = ResolveClipName(state);
			bool schleife = state == ActorVisualState.Idle || state == ActorVisualState.Move;
			if (schleife)
			{
				PlayLoop(clip);
			}
			else
			{
				PlayTimed(clip, 0f);
			}
			if (state == ActorVisualState.Death)
			{
				_dead = true;
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
			if (moving && worldDirection.sqrMagnitude > 0.0001f)
			{
				_targetYaw = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
			}
		}

		// ------------------------------------------------------------------
		// IAuthoredStatePresentation — der Weg fuer die Zusatzzustaende
		// ------------------------------------------------------------------

		public void SetAuthoredState(string stem, float normalizedTime = 0f,
			bool loop = false, bool restart = false)
		{
			string clip = StemToClip(stem);
			if (clip == null)
			{
				return;
			}
			if (loop)
			{
				PlayLoop(clip);
			}
			else
			{
				PlayTimed(clip, 0f);
			}
		}

		public void SetAuthoredTimedState(string stem, float durationSeconds)
		{
			string clip = StemToClip(stem);
			if (clip != null)
			{
				PlayTimed(clip, durationSeconds);
			}
		}

		// ------------------------------------------------------------------
		// Wiedergabe
		// ------------------------------------------------------------------

		private void PlayLoop(string clipName)
		{
			if (_dead || clipName == null || animationPlayer == null)
			{
				return;
			}
			AnimationState state = animationPlayer[clipName];
			if (state == null)
			{
				// Kein Ersatz: die Figur behaelt den laufenden Clip. Ein fehlender
				// Clip ist ein Datenfehler und soll auffallen, nicht kaschiert
				// werden.
				return;
			}
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
			if (_dead || clipName == null || animationPlayer == null)
			{
				return;
			}
			AnimationState state = animationPlayer[clipName];
			if (state == null)
			{
				return;
			}
			state.wrapMode = WrapMode.ClampForever;
			state.speed = (durationSeconds > 0.01f) ? (state.length / durationSeconds) : 1f;
			state.time = 0f;
			animationPlayer.CrossFade(clipName, crossfadeSeconds * 0.5f);
			_currentClip = clipName;
		}

		// ------------------------------------------------------------------
		// Lebenszyklus und Darstellung
		// ------------------------------------------------------------------

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
			if (_properties == null)
			{
				_properties = new MaterialPropertyBlock();
			}
		}

		private void Update()
		{
			if (modelRoot == null)
			{
				return;
			}
			_currentYaw = Mathf.MoveTowardsAngle(_currentYaw, _targetYaw,
				turnDegreesPerSecond * Time.deltaTime);
			modelRoot.localRotation = Quaternion.Euler(0f, ModelLocalYaw(_currentYaw, base.transform.eulerAngles.y), 0f);
		}

		private void ApplyTint(Color color)
		{
			if (_properties == null)
			{
				_properties = new MaterialPropertyBlock();
			}
			foreach (Renderer renderer in _renderers)
			{
				if (renderer == null)
				{
					continue;
				}
				renderer.GetPropertyBlock(_properties);
				_properties.SetColor(TintId, color);
				renderer.SetPropertyBlock(_properties);
			}
		}

		/// <summary>
		/// Lokaler Modell-Gierwinkel aus Welt-Blickwinkel und Wurzeldrehung.
		/// F31-015: Der Facing-Winkel ist ein WELT-Winkel, modelRoot hängt aber
		/// unter der vom Gegner-Controller gedrehten Wurzel — ohne Abzug der
		/// Wurzeldrehung addieren sich beide und das Modell schaut vom Ziel
		/// weg (nach Süden exakt entgegengesetzt zum Telegraphen). Beim
		/// Wanderer fällt der Unterschied weg, weil seine Wurzel nie rotiert.
		/// </summary>
		public static float ModelLocalYaw(float worldYaw, float rootYaw)
		{
			return Mathf.DeltaAngle(rootYaw, worldYaw);
		}

		/// <summary>Blickrichtung zu Gierwinkel. Gleiche Achse wie beim Wanderer.</summary>
		public static float FacingToYaw(ActorFacing8 facing)
		{
			switch (facing)
			{
			case ActorFacing8.N: return 0f;
			case ActorFacing8.NE: return 45f;
			case ActorFacing8.E: return 90f;
			case ActorFacing8.SE: return 135f;
			case ActorFacing8.S: return 180f;
			case ActorFacing8.SW: return 225f;
			case ActorFacing8.W: return 270f;
			default: return 315f;
			}
		}
	}
}
