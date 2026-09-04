using Eidren.Data;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapNodeView : MonoBehaviour
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private RectTransform animatedRoot;

		[SerializeField]
		private Image frame;

		[SerializeField]
		private Image regionAccent;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Text nodeName;

		[SerializeField]
		private WorldMapDangerIndicator dangerIndicator;

		[SerializeField]
		private GameObject selectedMarker;

		[SerializeField]
		private GameObject visitedMarker;

		[SerializeField]
		private GameObject newMarker;

		[SerializeField]
		private GameObject lockedMarker;

		[SerializeField]
		private Image eventMarker;

		[SerializeField]
		private GameObject bossMarker;

		[SerializeField]
		private GameObject notificationMarker;

		private Action<WorldMapNodeDefinition> _selected;

		private WorldMapThemeData _theme;

		private Coroutine _selectionRoutine;

		private Coroutine _eventRoutine;

		private Vector3 _baseScale = Vector3.one;

		public WorldMapNodeDefinition Definition { get; private set; }

		public WorldMapNodeVisualState State { get; private set; }

		public bool MissingIconFallbackActive { get; private set; }

		public Button Button => button;

		public string DisplayedName => (nodeName != null) ? nodeName.text : string.Empty;

		public int DisplayedDanger => (dangerIndicator != null) ? dangerIndicator.DisplayedDanger : 0;

		public bool SelectedMarkerVisible => selectedMarker != null && selectedMarker.activeSelf;

		public bool LockedMarkerVisible => lockedMarker != null && lockedMarker.activeSelf;

		public bool EventMarkerVisible => eventMarker != null && eventMarker.gameObject.activeSelf;

		public bool BossMarkerVisible => bossMarker != null && bossMarker.activeSelf;

		public bool VisitedMarkerVisible => visitedMarker != null && visitedMarker.activeSelf;

		public bool NewMarkerVisible => newMarker != null && newMarker.activeSelf;

		public Color FrameColor => (frame != null) ? frame.color : Color.clear;

		public void ConfigureUi(Button nodeButton, CanvasGroup group, RectTransform motionRoot, Image nodeFrame, Image accent, Image nodeIcon, Text nameLabel, WorldMapDangerIndicator danger, GameObject selected, GameObject visited, GameObject unvisited, GameObject locked, Image activeEvent, GameObject boss, GameObject notification)
		{
			button = nodeButton;
			canvasGroup = group;
			animatedRoot = motionRoot;
			frame = nodeFrame;
			regionAccent = accent;
			icon = nodeIcon;
			nodeName = nameLabel;
			dangerIndicator = danger;
			selectedMarker = selected;
			visitedMarker = visited;
			newMarker = unvisited;
			lockedMarker = locked;
			eventMarker = activeEvent;
			bossMarker = boss;
			notificationMarker = notification;
			_baseScale = ((animatedRoot != null) ? animatedRoot.localScale : Vector3.one);
		}

		public void Bind(WorldMapNodeDefinition definition, Sprite fallbackIcon, WorldMapThemeData theme, Action<WorldMapNodeDefinition> selected)
		{
			if (definition == null)
			{
				throw new ArgumentNullException("definition");
			}
			if (theme == null)
			{
				throw new ArgumentNullException("theme");
			}
			ValidateReferences();
			if (_selected != null)
			{
				button.onClick.RemoveListener(NotifySelected);
			}
			Definition = definition;
			_theme = theme;
			_selected = selected;
			RectTransform rectTransform = base.transform as RectTransform;
			if (rectTransform != null)
			{
				rectTransform.anchorMin = definition.MapPosition;
				rectTransform.anchorMax = definition.MapPosition;
				rectTransform.anchoredPosition = Vector2.zero;
			}
			nodeName.text = definition.DisplayName;
			dangerIndicator.SetDanger(definition.DangerLevel, theme);
			Sprite iconOrFallback = definition.GetIconOrFallback(fallbackIcon);
			MissingIconFallbackActive = definition.Icon == null;
			icon.sprite = iconOrFallback;
			icon.color = ((iconOrFallback != null) ? theme.TextPrimary : theme.FrameNeutral);
			regionAccent.color = theme.GetRegionAccent(definition.RegionType);
			theme.ApplyTypography(nodeName, WorldMapTypographyRole.RegionName, theme.TextPrimary);
			button.onClick.AddListener(NotifySelected);
		}

		public void ApplyState(WorldMapNodeVisualState state)
		{
			if (Definition == null)
			{
				throw new InvalidOperationException("Bind a node definition before applying state.");
			}
			WorldMapNodeVisualState state2 = State;
			State = state;
			button.interactable = state.Available;
			canvasGroup.alpha = (state.Available ? 1f : 0.62f);
			selectedMarker.SetActive(state.Selected);
			visitedMarker.SetActive(state.Available && (state.Visited || state.Completed));
			newMarker.SetActive(state.Available && !state.Visited && !state.Completed);
			lockedMarker.SetActive(!state.Available);
			eventMarker.gameObject.SetActive(state.EventActive);
			bossMarker.SetActive(state.Boss && !state.BossDefeated);
			notificationMarker.SetActive(state.EventActive);
			frame.color = (state.Selected ? _theme.Selection : (state.Available ? _theme.FrameNeutral : _theme.Locked));
			if (state2.Selected != state.Selected)
			{
				StartSelectionAnimation(state.Selected);
			}
			if (state2.EventActive != state.EventActive)
			{
				SetEventPulse(state.EventActive);
			}
		}

		public void ValidateReferences()
		{
			if (button == null || canvasGroup == null || animatedRoot == null || frame == null || regionAccent == null || icon == null || nodeName == null || dangerIndicator == null || selectedMarker == null || visitedMarker == null || newMarker == null || lockedMarker == null || eventMarker == null || bossMarker == null || notificationMarker == null)
			{
				throw new InvalidOperationException("WorldMapNodeView '" + base.name + "' has missing serialized references.");
			}
		}

		private void StartSelectionAnimation(bool selected)
		{
			if (_selectionRoutine != null)
			{
				StopCoroutine(_selectionRoutine);
			}
			if (!Application.isPlaying)
			{
				animatedRoot.localScale = _baseScale * (selected ? _theme.SelectedScale : 1f);
				_selectionRoutine = null;
			}
			else
			{
				_selectionRoutine = StartCoroutine(AnimateSelection(selected));
			}
		}

		private IEnumerator AnimateSelection(bool selected)
		{
			Vector3 from = animatedRoot.localScale;
			Vector3 to = _baseScale * (selected ? _theme.SelectedScale : 1f);
			float duration = Mathf.Max(0.01f, _theme.SelectionDuration);
			for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
			{
				float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
				animatedRoot.localScale = Vector3.LerpUnclamped(from, to, t);
				yield return null;
			}
			animatedRoot.localScale = to;
			_selectionRoutine = null;
		}

		private void SetEventPulse(bool active)
		{
			if (_eventRoutine != null)
			{
				StopCoroutine(_eventRoutine);
			}
			if (!Application.isPlaying)
			{
				eventMarker.color = _theme.Event;
				_eventRoutine = null;
				return;
			}
			_eventRoutine = (active ? StartCoroutine(PulseEvent()) : null);
			if (!active)
			{
				eventMarker.color = _theme.Event;
			}
		}

		private IEnumerator PulseEvent()
		{
			float duration = Mathf.Max(0.1f, _theme.EventPulseDuration);
			while (State.EventActive)
			{
				float phase = Mathf.PingPong(Time.unscaledTime / duration * 2f, 1f);
				Color color = _theme.Event;
				color.a = Mathf.Lerp(0.48f, 1f, phase);
				eventMarker.color = color;
				yield return null;
			}
			eventMarker.color = _theme.Event;
			_eventRoutine = null;
		}

		private void NotifySelected()
		{
			if (Definition != null)
			{
				_selected?.Invoke(Definition);
			}
		}

		private void OnDisable()
		{
			if (_selectionRoutine != null)
			{
				StopCoroutine(_selectionRoutine);
			}
			if (_eventRoutine != null)
			{
				StopCoroutine(_eventRoutine);
			}
			_selectionRoutine = null;
			_eventRoutine = null;
		}

		private void OnDestroy()
		{
			if (button != null)
			{
				button.onClick.RemoveListener(NotifySelected);
			}
		}
	}
}
