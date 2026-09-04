using Eidren.AI;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class BossEncounterView : MonoBehaviour
	{
		[SerializeField]
		private CanvasGroup bossGroup;

		[SerializeField]
		private Text bossName;

		[SerializeField]
		private Image healthFill;

		[SerializeField]
		private Image staggerFill;

		[SerializeField]
		private Text stateLabel;

		[SerializeField]
		private CanvasGroup resultGroup;

		[SerializeField]
		private Text resultTitle;

		[SerializeField]
		private Text rewardLabel;

		[SerializeField]
		private Button closeResultButton;

		private BossController _boss;

		public bool BossVisible => bossGroup != null && bossGroup.alpha > 0.99f;

		public bool ResultVisible => resultGroup != null && resultGroup.alpha > 0.99f;

		public void ConfigureUi(CanvasGroup configuredBossGroup, Text configuredBossName, Image configuredHealthFill, Image configuredStaggerFill, Text configuredStateLabel, CanvasGroup configuredResultGroup, Text configuredResultTitle, Text configuredRewardLabel, Button configuredCloseResultButton)
		{
			bossGroup = configuredBossGroup;
			bossName = configuredBossName;
			healthFill = configuredHealthFill;
			staggerFill = configuredStaggerFill;
			stateLabel = configuredStateLabel;
			resultGroup = configuredResultGroup;
			resultTitle = configuredResultTitle;
			rewardLabel = configuredRewardLabel;
			closeResultButton = configuredCloseResultButton;
		}

		public void Bind(BossController boss, string displayName)
		{
			ValidateReferences();
			Unbind();
			_boss = boss ?? throw new ArgumentNullException("boss");
			bossName.text = (string.IsNullOrWhiteSpace(displayName) ? "GARON" : displayName.ToUpperInvariant());
			_boss.HealthChanged += RefreshHealth;
			_boss.StaggerChanged += RefreshStagger;
			_boss.EnemyStateChanged += HandleStateChanged;
			closeResultButton.onClick.RemoveListener(HideResult);
			closeResultButton.onClick.AddListener(HideResult);
			RefreshHealth(_boss.CurrentHealth, _boss.MaxHealth);
			RefreshStagger(_boss.CurrentStagger, _boss.MaxStagger);
			stateLabel.text = _boss.EnemyState.ToString().ToUpperInvariant();
			SetVisible(bossGroup, visible: false);
			SetVisible(resultGroup, visible: false);
		}

		public void ShowBoss()
		{
			SetVisible(bossGroup, visible: true);
		}

		public void HideBoss()
		{
			SetVisible(bossGroup, visible: false);
		}

		public void ShowVictory(bool inventoryRewarded)
		{
			HideBoss();
			resultTitle.text = "GARON BESIEGT";
			rewardLabel.text = (inventoryRewarded ? "BELOHNUNG\n3 KUPFERBARREN · 2 HEILTRÄNKE" : "INVENTAR VOLL\nBELOHNUNG LIEGT AM KAMPFORT");
			SetVisible(resultGroup, visible: true);
		}

		public void HideResult()
		{
			SetVisible(resultGroup, visible: false);
		}

		public void ValidateReferences()
		{
			if (bossGroup == null || bossName == null || healthFill == null || staggerFill == null || stateLabel == null || resultGroup == null || resultTitle == null || rewardLabel == null || closeResultButton == null)
			{
				throw new InvalidOperationException("BossEncounterView '" + base.name + "' has missing references.");
			}
		}

		private void HandleStateChanged(EnemyState previous, EnemyState current)
		{
			stateLabel.text = current.ToString().ToUpperInvariant();
			if (current == EnemyState.Return || current == EnemyState.Idle || current == EnemyState.Patrol || current == EnemyState.Dead)
			{
				HideBoss();
			}
			else
			{
				ShowBoss();
			}
		}

		private void RefreshHealth(float current, float maximum)
		{
			healthFill.fillAmount = SafeRatio(current, maximum);
		}

		private void RefreshStagger(float current, float maximum)
		{
			staggerFill.fillAmount = SafeRatio(current, maximum);
		}

		private static float SafeRatio(float current, float maximum)
		{
			return (maximum <= 0f) ? 0f : Mathf.Clamp01(current / maximum);
		}

		private static void SetVisible(CanvasGroup group, bool visible)
		{
			group.alpha = (visible ? 1f : 0f);
			group.interactable = visible;
			group.blocksRaycasts = visible;
		}

		private void Unbind()
		{
			if (!(_boss == null))
			{
				_boss.HealthChanged -= RefreshHealth;
				_boss.StaggerChanged -= RefreshStagger;
				_boss.EnemyStateChanged -= HandleStateChanged;
				_boss = null;
			}
		}

		private void OnDestroy()
		{
			Unbind();
			if (closeResultButton != null)
			{
				closeResultButton.onClick.RemoveListener(HideResult);
			}
		}
	}
}
