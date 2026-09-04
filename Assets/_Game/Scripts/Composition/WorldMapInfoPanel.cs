using Eidren.Data;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapInfoPanel : MonoBehaviour
	{
		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private RectTransform animatedRoot;

		[SerializeField]
		private Image panelSurface;

		[SerializeField]
		private Text title;

		[SerializeField]
		private Text description;

		[SerializeField]
		private WorldMapDangerIndicator dangerIndicator;

		[SerializeField]
		private Text progress;

		[SerializeField]
		private Text resourcesHeading;

		[SerializeField]
		private Text enemies;

		[SerializeField]
		private Text places;

		[SerializeField]
		private Text boss;

		[SerializeField]
		private WorldMapResourceEntry[] resourceEntries = Array.Empty<WorldMapResourceEntry>();

		private WorldMapThemeData _theme;

		private Coroutine _animationRoutine;

		public string DisplayedNodeId { get; private set; }

		public bool IsOpen { get; private set; }

		public int OpenInvocationCount { get; private set; }

		public void ConfigureUi(CanvasGroup group, RectTransform motionRoot, Image surface, Text titleLabel, Text descriptionLabel, WorldMapDangerIndicator danger, Text progressLabel, Text resourceHeadingLabel, Text enemyLabel, Text placesLabel, Text bossLabel, WorldMapResourceEntry[] entries)
		{
			canvasGroup = group;
			animatedRoot = motionRoot;
			panelSurface = surface;
			title = titleLabel;
			description = descriptionLabel;
			dangerIndicator = danger;
			progress = progressLabel;
			resourcesHeading = resourceHeadingLabel;
			enemies = enemyLabel;
			places = placesLabel;
			boss = bossLabel;
			resourceEntries = entries ?? Array.Empty<WorldMapResourceEntry>();
		}

		public void ApplyTheme(WorldMapThemeData theme)
		{
			_theme = theme ?? throw new ArgumentNullException("theme");
			ValidateReferences();
			panelSurface.color = theme.PanelSurface;
			theme.ApplyTypography(title, WorldMapTypographyRole.PanelHeading, theme.Selection);
			theme.ApplyTypography(description, WorldMapTypographyRole.BodyText, theme.TextPrimary);
			theme.ApplyTypography(progress, WorldMapTypographyRole.SmallMetaText, theme.TextSecondary);
			theme.ApplyTypography(resourcesHeading, WorldMapTypographyRole.SmallMetaText, theme.TextSecondary);
			theme.ApplyTypography(enemies, WorldMapTypographyRole.BodyText, theme.TextPrimary);
			theme.ApplyTypography(places, WorldMapTypographyRole.BodyText, theme.TextPrimary);
			theme.ApplyTypography(boss, WorldMapTypographyRole.SmallMetaText, theme.Boss);
		}

		public void Bind(WorldMapNodeDefinition node, bool visited, bool bossDefeated = false)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}
			if (_theme == null)
			{
				throw new InvalidOperationException("Apply a theme before binding node data.");
			}
			DisplayedNodeId = node.Id;
			title.text = node.DisplayName;
			description.text = node.Description;
			dangerIndicator.SetDanger(node.DangerLevel, _theme);
			progress.text = "EMPFOHLEN · " + node.RecommendedProgress;
			enemies.text = "MÖGLICHE GEGNER\n" + JoinOrFallback(node.PossibleEnemies, "Keine");
			places.text = "BESONDERE ORTE\n" + JoinOrFallback(node.SpecialPlaces, "Keine");
			boss.text = ((node.HasBoss && bossDefeated) ? "ABGESCHLOSSEN · BOSS BESIEGT" : (node.HasBoss ? "BOSSGEBIET · MÄCHTIGE PRÄSENZ" : ((node.RegionType == WorldRegionType.SafeHomeland) ? "SICHERES GEBIET" : (visited ? "BEREITS BESUCHT" : "NOCH NICHT BESUCHT"))));
			for (int i = 0; i < resourceEntries.Length; i++)
			{
				if (i < node.Resources.Count)
				{
					resourceEntries[i].Bind(node.Resources[i], _theme);
				}
				else
				{
					resourceEntries[i].Clear();
				}
			}
		}

		public void Open(bool immediate = false)
		{
			OpenInvocationCount++;
			StartAnimation(opening: true, immediate);
		}

		public void Close(bool immediate = false)
		{
			StartAnimation(opening: false, immediate);
		}

		public void ValidateReferences()
		{
			if (canvasGroup == null || animatedRoot == null || panelSurface == null || title == null || description == null || dangerIndicator == null || progress == null || resourcesHeading == null || enemies == null || places == null || boss == null || resourceEntries == null || resourceEntries.Any((WorldMapResourceEntry entry) => entry == null))
			{
				throw new InvalidOperationException("WorldMapInfoPanel '" + base.name + "' has missing serialized references.");
			}
		}

		private void StartAnimation(bool opening, bool immediate)
		{
			if (_animationRoutine != null)
			{
				StopCoroutine(_animationRoutine);
			}
			if (opening && !base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(value: true);
			}
			if (immediate || !Application.isPlaying || !base.isActiveAndEnabled)
			{
				SetAnimationState(opening ? 1f : 0f, opening);
				return;
			}
			base.gameObject.SetActive(value: true);
			_animationRoutine = StartCoroutine(Animate(opening));
		}

		private IEnumerator Animate(bool opening)
		{
			float from = canvasGroup.alpha;
			float to = (opening ? 1f : 0f);
			float duration = Mathf.Max(0.01f, _theme.PanelDuration);
			for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
			{
				float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
				float value = Mathf.Lerp(from, to, t);
				SetAnimationState(value, active: true);
				yield return null;
			}
			SetAnimationState(to, opening);
			_animationRoutine = null;
		}

		private void SetAnimationState(float value, bool active)
		{
			canvasGroup.alpha = value;
			canvasGroup.interactable = active;
			canvasGroup.blocksRaycasts = active;
			animatedRoot.localScale = Vector3.one * Mathf.Lerp(0.985f, 1f, value);
			IsOpen = active && value >= 0.999f;
			if (!active)
			{
				base.gameObject.SetActive(value: false);
			}
		}

		private static string JoinOrFallback(IReadOnlyList<string> values, string fallback)
		{
			if (values == null || values.Count == 0)
			{
				return fallback;
			}
			string text = string.Join(" · ", values.Where((string value) => !string.IsNullOrWhiteSpace(value)));
			return string.IsNullOrWhiteSpace(text) ? fallback : text;
		}

		private void OnDisable()
		{
			if (_animationRoutine != null)
			{
				StopCoroutine(_animationRoutine);
			}
			_animationRoutine = null;
		}
	}
}
