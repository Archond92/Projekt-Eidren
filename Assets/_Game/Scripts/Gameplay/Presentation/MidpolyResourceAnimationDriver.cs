using Eidren.Interaction;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Eidren.Gameplay.Presentation
{
	/// <summary>
	/// Spielt die in den freigegebenen Mid-Poly-Ressourcen enthaltenen Clips
	/// ohne zusaetzlichen AnimatorController. Idle laeuft nur auf LOD0; beim
	/// Sammelfortschritt wird der authorisierte Harvest_Recoil eingeblendet.
	/// Gameplay-Timing und ResourceNode-Zustaende bleiben unveraendert.
	/// </summary>
	public sealed class MidpolyResourceAnimationDriver : MonoBehaviour
	{
		private static readonly float[] HarvestTriggers = { 0.08f, 0.56f };

		[SerializeField]
		private Animator targetAnimator;

		[SerializeField]
		private AnimationClip idleClip;

		[SerializeField]
		private AnimationClip harvestClip;

		private ResourceNode _node;
		private PlayableGraph _graph;
		private AnimationMixerPlayable _mixer;
		private AnimationClipPlayable _idlePlayable;
		private AnimationClipPlayable _harvestPlayable;
		private bool _graphCreated;
		private bool _harvestPlaying;
		private float _harvestStartedAt;
		private int _nextTrigger;

		public bool IsConfigured => targetAnimator != null && harvestClip != null;

		public void Configure(Animator animator, AnimationClip idle, AnimationClip harvest)
		{
			targetAnimator = animator;
			idleClip = idle;
			harvestClip = harvest;
		}

		private void OnEnable()
		{
			if (!IsConfigured)
			{
				return;
			}
			CreateGraph();
			_node = GetComponentInParent<ResourceNode>();
			if (_node != null)
			{
				_node.ProgressChanged += HandleProgress;
			}
		}

		private void OnDisable()
		{
			if (_node != null)
			{
				_node.ProgressChanged -= HandleProgress;
			}
			_node = null;
			if (_graphCreated)
			{
				_graph.Destroy();
				_graphCreated = false;
			}
		}

		private void Update()
		{
			if (!_graphCreated || !_harvestPlaying)
			{
				return;
			}

			float elapsed = Time.time - _harvestStartedAt;
			float length = Mathf.Max(0.01f, harvestClip.length);
			float blendIn = Mathf.Clamp01(elapsed / 0.08f);
			float blendOut = Mathf.Clamp01((length - elapsed) / 0.14f);
			float weight = Mathf.Min(blendIn, blendOut);
			_mixer.SetInputWeight(0, 1f - weight);
			_mixer.SetInputWeight(1, weight);

			if (elapsed >= length)
			{
				_harvestPlaying = false;
				_harvestPlayable.SetSpeed(0d);
				_mixer.SetInputWeight(0, 1f);
				_mixer.SetInputWeight(1, 0f);
			}
		}

		private void HandleProgress(float normalizedProgress)
		{
			float progress = Mathf.Clamp01(normalizedProgress);
			if (progress <= 0.001f)
			{
				_nextTrigger = 0;
				return;
			}
			while (_nextTrigger < HarvestTriggers.Length && progress >= HarvestTriggers[_nextTrigger])
			{
				PlayHarvest();
				_nextTrigger++;
			}
		}

		private void CreateGraph()
		{
			_graph = PlayableGraph.Create(name + "_MidPolyResourceAnimation");
			_graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
			_mixer = AnimationMixerPlayable.Create(_graph, 2);

			AnimationClip restClip = idleClip != null ? idleClip : harvestClip;
			_idlePlayable = AnimationClipPlayable.Create(_graph, restClip);
			_idlePlayable.SetApplyFootIK(false);
			_idlePlayable.SetApplyPlayableIK(false);
			_idlePlayable.SetSpeed(idleClip != null ? 1d : 0d);
			_harvestPlayable = AnimationClipPlayable.Create(_graph, harvestClip);
			_harvestPlayable.SetApplyFootIK(false);
			_harvestPlayable.SetApplyPlayableIK(false);
			_harvestPlayable.SetSpeed(0d);

			_graph.Connect(_idlePlayable, 0, _mixer, 0);
			_graph.Connect(_harvestPlayable, 0, _mixer, 1);
			_mixer.SetInputWeight(0, 1f);
			_mixer.SetInputWeight(1, 0f);
			AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Resource", targetAnimator);
			output.SetSourcePlayable(_mixer);
			_graph.Play();
			_graphCreated = true;
		}

		private void PlayHarvest()
		{
			if (!_graphCreated)
			{
				return;
			}
			_harvestPlayable.SetTime(0d);
			_harvestPlayable.SetDone(false);
			_harvestPlayable.SetSpeed(1d);
			_harvestStartedAt = Time.time;
			_harvestPlaying = true;
		}
	}
}
