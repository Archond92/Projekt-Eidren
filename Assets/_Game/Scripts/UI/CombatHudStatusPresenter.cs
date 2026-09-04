using Eidren.AI;
using Eidren.Combat;
using Eidren.Core.Services;
using Eidren.Data;
using Eidren.Eidra;
using Eidren.Gameplay.Flow;
using Eidren.Player;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	public sealed class CombatHudStatusPresenter : MonoBehaviour
	{
		[SerializeField]
		private GameObject playerPanel;

		[SerializeField]
		private Image healthFill;

		[SerializeField]
		private Image staminaFill;

		[SerializeField]
		private Image resonanceFill;

		[SerializeField]
		private Text healthValue;

		[SerializeField]
		private Image experienceFill;

		[SerializeField]
		private Text levelValue;

		[SerializeField]
		private GameObject objectivePanel;

		[SerializeField]
		private Text objectiveText;

		[SerializeField]
		private GameObject bossPanel;

		[SerializeField]
		private Image bossHealthFill;

		[SerializeField]
		private Image bossStaggerFill;

		[SerializeField]
		private Text bossState;

		private Damageable _health;

		private PlayerMotor _motor;

		private EidraTeamController _eidra;

		private BossController _boss;

		private GameFlowController _flow;

		private PlayerProgressionService _progression;

		public Image ExperienceFill => experienceFill;

		public Text LevelValue => levelValue;

		public void ConfigureProgressionReferences(Image authoredExperienceFill, Text authoredLevelValue)
		{
			experienceFill = authoredExperienceFill;
			levelValue = authoredLevelValue;
		}

		public void ConfigureReferences(GameObject authoredPlayerPanel, Image authoredHealth, Image authoredStamina, Image authoredResonance, Text authoredHealthValue, GameObject authoredObjective, Text authoredObjectiveText, GameObject authoredBoss, Image authoredBossHealth, Image authoredBossStagger, Text authoredBossState)
		{
			playerPanel = authoredPlayerPanel;
			healthFill = authoredHealth;
			staminaFill = authoredStamina;
			resonanceFill = authoredResonance;
			healthValue = authoredHealthValue;
			objectivePanel = authoredObjective;
			objectiveText = authoredObjectiveText;
			bossPanel = authoredBoss;
			bossHealthFill = authoredBossHealth;
			bossStaggerFill = authoredBossStagger;
			bossState = authoredBossState;
		}

		public void Bind(Damageable health, PlayerMotor motor, EidraTeamController eidra, BossController boss, GameFlowController flow, PlayerProgressionService progression)
		{
			Unbind();
			_health = health;
			_motor = motor;
			_eidra = eidra;
			_boss = boss;
			_flow = flow;
			_progression = progression;
			_health.HealthChanged += RefreshHealth;
			_motor.StaminaChanged += RefreshStamina;
			if (_eidra != null)
			{
				_eidra.ActiveEidraChanged += RefreshEidra;
			}
			if (_boss != null)
			{
				_boss.HealthChanged += RefreshBossHealth;
				_boss.StaggerChanged += RefreshBossStagger;
				_boss.EnemyStateChanged += RefreshBossState;
				_boss.BattleActivityChanged += RefreshBossActivity;
			}
			if (_flow != null)
			{
				_flow.BattleStarted += HandleBattleStarted;
				_flow.ResourceChanged += HandleResourceChanged;
			}
			if (_progression != null)
			{
				_progression.Changed += RefreshProgression;
			}
			RefreshHealth(_health.CurrentHealth, _health.MaxHealth);
			RefreshStamina(_motor.CurrentStamina, _motor.MaxStamina);
			RefreshEidra((_eidra != null) ? _eidra.ActiveData : null);
			if (_boss != null)
			{
				RefreshBossHealth(_boss.CurrentHealth, _boss.MaxHealth);
				RefreshBossStagger(_boss.CurrentStagger, _boss.MaxStagger);
			}
			SetBossVisible(_boss != null && _boss.BattleActive);
			if (_progression != null)
			{
				RefreshProgression(_progression.State);
			}
		}

		private void RefreshProgression(PlayerProgressionSnapshot state)
		{
			if (experienceFill != null)
			{
				experienceFill.fillAmount = ((state.Level >= state.MaximumLevel) ? 1f : Ratio(state.Experience, state.ExperienceToNextLevel));
			}
			if (levelValue != null)
			{
				levelValue.text = $"LV {state.Level}";
			}
		}

		private void RefreshEidra(EidraData active)
		{
			resonanceFill.fillAmount = ((active != null) ? 1f : 0f);
		}

		private void RefreshHealth(float current, float maximum)
		{
			healthFill.fillAmount = Ratio(current, maximum);
			healthValue.text = $"{current:0}/{maximum:0}";
		}

		private void RefreshStamina(float current, float maximum)
		{
			staminaFill.fillAmount = Ratio(current, maximum);
		}

		private void RefreshBossHealth(float current, float maximum)
		{
			bossHealthFill.fillAmount = Ratio(current, maximum);
		}

		private void RefreshBossStagger(float current, float maximum)
		{
			bossStaggerFill.fillAmount = Ratio(current, maximum);
		}

		private void RefreshBossState(EnemyState previous, EnemyState current)
		{
			bossState.text = current.ToString().ToUpperInvariant();
		}

		private void RefreshBossActivity(bool active)
		{
			SetBossVisible(active);
		}

		private void HandleBattleStarted()
		{
			SetBossVisible(visible: true);
		}

		private void HandleResourceChanged(int amount)
		{
			if (amount > 0)
			{
				objectiveText.text = "ARENA BETRETEN";
			}
		}

		private void SetBossVisible(bool visible)
		{
			bossPanel.SetActive(visible);
			objectivePanel.SetActive(!visible);
		}

		private static float Ratio(float current, float maximum)
		{
			return (maximum <= 0f) ? 0f : Mathf.Clamp01(current / maximum);
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_health != null)
			{
				_health.HealthChanged -= RefreshHealth;
			}
			if (_motor != null)
			{
				_motor.StaminaChanged -= RefreshStamina;
			}
			if (_eidra != null)
			{
				_eidra.ActiveEidraChanged -= RefreshEidra;
			}
			if (_boss != null)
			{
				_boss.HealthChanged -= RefreshBossHealth;
				_boss.StaggerChanged -= RefreshBossStagger;
				_boss.EnemyStateChanged -= RefreshBossState;
				_boss.BattleActivityChanged -= RefreshBossActivity;
			}
			if (_flow != null)
			{
				_flow.BattleStarted -= HandleBattleStarted;
				_flow.ResourceChanged -= HandleResourceChanged;
			}
			if (_progression != null)
			{
				_progression.Changed -= RefreshProgression;
			}
			_health = null;
			_motor = null;
			_eidra = null;
			_boss = null;
			_flow = null;
			_progression = null;
		}
	}
}
