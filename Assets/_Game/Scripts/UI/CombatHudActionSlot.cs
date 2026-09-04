using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.UI
{
	[Serializable]
	public sealed class CombatHudActionSlot
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private CanvasGroup canvasGroup;

		[SerializeField]
		private Image frame;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private Image cooldown;

		[SerializeField]
		private GameObject lockMark;

		[SerializeField]
		private Text value;

		public Button Button => button;

		public Image Frame => frame;

		public Image Icon => icon;

		public Image Cooldown => cooldown;

		public Text Value => value;

		public CombatHudActionSlot(Button authoredButton, CanvasGroup authoredCanvasGroup, Image authoredFrame, Image authoredIcon, Image authoredCooldown, GameObject authoredLock, Text authoredValue)
		{
			button = authoredButton;
			canvasGroup = authoredCanvasGroup;
			frame = authoredFrame;
			icon = authoredIcon;
			cooldown = authoredCooldown;
			lockMark = authoredLock;
			value = authoredValue;
		}

		public void SetLocked(bool locked)
		{
			lockMark.SetActive(locked);
			canvasGroup.alpha = (locked ? 0.4f : 1f);
		}

		public void SetCooldown(float ratio, float seconds)
		{
			cooldown.fillAmount = Mathf.Clamp01(ratio);
			value.text = ((seconds >= 1f) ? Mathf.CeilToInt(seconds).ToString() : string.Empty);
		}
	}
}
