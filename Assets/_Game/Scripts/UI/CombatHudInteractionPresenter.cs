using Eidren.Input;
using Eidren.Interaction;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class CombatHudInteractionPresenter : MonoBehaviour
	{
		[SerializeField]
		private GameObject root;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private InteractionButton control;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Image progressRing;

		[SerializeField]
		private Image pressedSignal;

		[SerializeField]
		private Text verb;

		[SerializeField]
		private Text blockedReason;

		[SerializeField]
		private Sprite fallbackIcon;

		private InteractionController _interaction;

		public InteractionSnapshot Snapshot { get; private set; }

		public Image ProgressRing => progressRing;

		public InteractionButton Control => control;

		public void ConfigureReferences(GameObject authoredRoot, CanvasGroup authoredCanvasGroup, InteractionButton authoredControl, Image authoredIcon, Image authoredProgress, Image authoredPressedSignal, Text authoredVerb, Text authoredBlockedReason, Sprite authoredFallbackIcon = null)
		{
			root = authoredRoot;
			canvasGroup = authoredCanvasGroup;
			control = authoredControl;
			icon = authoredIcon;
			progressRing = authoredProgress;
			pressedSignal = authoredPressedSignal;
			verb = authoredVerb;
			blockedReason = authoredBlockedReason;
			fallbackIcon = authoredFallbackIcon;
		}

		public Sprite ResolveIcon(in InteractionSnapshot snapshot)
		{
			return (snapshot.Icon != null) ? snapshot.Icon : fallbackIcon;
		}

		public void Bind(PlayerInputReader input, InteractionController interaction)
		{
			Unbind();
			_interaction = interaction;
			control.Initialize(input);
			_interaction.SnapshotChanged += Refresh;
			Refresh(InteractionSnapshot.Empty);
		}

		private void Refresh(InteractionSnapshot snapshot)
		{
			Snapshot = snapshot;
			bool hasTarget = snapshot.HasTarget;
			root.SetActive(hasTarget);
			canvasGroup.blocksRaycasts = hasTarget && snapshot.CanInteract;
			canvasGroup.interactable = hasTarget && snapshot.CanInteract;
			control.SetAvailable(hasTarget && snapshot.CanInteract);
			if (!hasTarget)
			{
				progressRing.fillAmount = 0f;
				return;
			}
			Sprite sprite = ResolveIcon(in snapshot);
			icon.sprite = sprite;
			icon.enabled = sprite != null;
			bool flag = snapshot.Mode != InteractionMode.Instant && (snapshot.State == InteractionState.Starting || snapshot.State == InteractionState.Holding || snapshot.Progress > 0f);
			progressRing.enabled = snapshot.Mode != InteractionMode.Instant;
			progressRing.fillAmount = (flag ? Mathf.Clamp01(snapshot.Progress) : 0f);
			pressedSignal.enabled = flag;
			verb.text = snapshot.DisplayText;
			blockedReason.text = (snapshot.CanInteract ? string.Empty : snapshot.BlockedReason);
			canvasGroup.alpha = (snapshot.CanInteract ? 1f : 0.48f);
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_interaction != null)
			{
				_interaction.SnapshotChanged -= Refresh;
			}
			_interaction = null;
		}
	}
}
