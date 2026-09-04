using Eidren.Data;
using System;
using UnityEngine.UI;
using UnityEngine;

namespace Eidren.Composition
{
	public sealed class WorldMapResourceEntry : MonoBehaviour
	{
		[SerializeField]
		private Image icon;

		[SerializeField]
		private Text label;

		public string DisplayedResourceId { get; private set; }

		public void ConfigureUi(Image resourceIcon, Text resourceLabel)
		{
			icon = resourceIcon;
			label = resourceLabel;
		}

		public void Bind(WorldMapResourcePreview preview, WorldMapThemeData theme)
		{
			if (icon == null || label == null)
			{
				throw new InvalidOperationException("Resource entry '" + base.name + "' is not configured.");
			}
			DisplayedResourceId = preview.StableId;
			icon.sprite = preview.Icon;
			icon.color = ((preview.Icon != null) ? theme.TextPrimary : theme.FrameNeutral);
			label.text = (preview.IsFuturePlaceholder ? (preview.DisplayName + " · SPÄTER") : preview.DisplayName);
			theme.ApplyTypography(label, WorldMapTypographyRole.BodyText, theme.TextPrimary);
			base.gameObject.SetActive(value: true);
		}

		public void Clear()
		{
			DisplayedResourceId = string.Empty;
			base.gameObject.SetActive(value: false);
		}
	}
}
