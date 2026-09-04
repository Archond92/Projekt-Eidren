using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// Zweiter Teil von MeshActorPresentation (partial): benannte, autorierte
	/// Zustaende (IAuthoredStatePresentation), die Clip-Wiedergabe (PlayLoop/
	/// PlayTimed) sowie die prozeduralen Zustaende ohne eigenen Clip in der
	/// Wanderer.glb (Treffer, Taumeln, Tod, Erscheinen, Ausweichen, Ernten)
	/// samt ihrer Anwendung je Frame (Update/LateUpdate/ApplyTint).
	/// </summary>
	public sealed partial class MeshActorPresentation
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

		public const float MaxTimedPlaybackSpeed = 1.5f;

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
			// F34-007: Die Angriffsclips sind 1,7- bis 3,5-mal laenger als die
			// Kampffenster der Waffen (Speer 1,167 s gegen 0,48 s). Ungedeckelt
			// lief der Stoss als Zuckung ab. Gedeckelt zeigt das Fenster den
			// Anlauf samt Stoss, der Rest wird vom Folgezustand ueberblendet.
			state.speed = (durationSeconds > 0.01f) ? Mathf.Min(state.length / durationSeconds, MaxTimedPlaybackSpeed) : 1f;
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
				// Wiederbeleben/Erscheinen loest die Todes-Sperre wieder auf - noetig fuer Revive und Gegner-Pooling.
				_procedural = ProceduralPose.None;
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
