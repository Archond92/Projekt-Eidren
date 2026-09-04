using System;
using UnityEngine;

namespace Eidren.Data
{
	[CreateAssetMenu(menuName = "Eidren/World Map/Theme")]
	public sealed class WorldMapThemeData : ScriptableObject
	{
		[Header("Functional colors")]
		[SerializeField]
		private Color backgroundDeep;

		[SerializeField]
		private Color backgroundRaised;

		[SerializeField]
		private Color panelSurface;

		[SerializeField]
		private Color frameNeutral;

		[SerializeField]
		private Color textPrimary;

		[SerializeField]
		private Color textSecondary;

		[SerializeField]
		private Color selection;

		[SerializeField]
		private Color available;

		[SerializeField]
		private Color dangerous;

		[SerializeField]
		private Color locked;

		[SerializeField]
		private Color eventColor;

		[SerializeField]
		private Color boss;

		[SerializeField]
		private Color energyGlow;

		[Header("Secondary region accents")]
		[SerializeField]
		private Color homelandAccent;

		[SerializeField]
		private Color forestAccent;

		[SerializeField]
		private Color quarryAccent;

		[SerializeField]
		private Color marshAccent;

		[SerializeField]
		private Color emberAccent;

		[Header("Typography roles")]
		[SerializeField]
		private WorldMapTypographyStyle mapTitle;

		[SerializeField]
		private WorldMapTypographyStyle regionName;

		[SerializeField]
		private WorldMapTypographyStyle panelHeading;

		[SerializeField]
		private WorldMapTypographyStyle bodyText;

		[SerializeField]
		private WorldMapTypographyStyle smallMetaText;

		[SerializeField]
		private WorldMapTypographyStyle timerText;

		[SerializeField]
		private WorldMapTypographyStyle buttonText;

		[SerializeField]
		private WorldMapTypographyStyle dangerLabel;

		[Header("Unscaled UI motion")]
		[Min(0.01f)]
		[SerializeField]
		private float selectionDuration = 0.12f;

		[Min(0.01f)]
		[SerializeField]
		private float panelDuration = 0.24f;

		[Min(0.1f)]
		[SerializeField]
		private float eventPulseDuration = 1.8f;

		[Min(0.01f)]
		[SerializeField]
		private float travelConfirmationDuration = 0.18f;

		[Min(0.01f)]
		[SerializeField]
		private float transitionFadeDuration = 0.28f;

		[Range(1f, 1.08f)]
		[SerializeField]
		private float selectedScale = 1.04f;

		public Color BackgroundDeep => backgroundDeep;

		public Color BackgroundRaised => backgroundRaised;

		public Color PanelSurface => panelSurface;

		public Color FrameNeutral => frameNeutral;

		public Color TextPrimary => textPrimary;

		public Color TextSecondary => textSecondary;

		public Color Selection => selection;

		public Color Available => available;

		public Color Dangerous => dangerous;

		public Color Locked => locked;

		public Color Event => eventColor;

		public Color Boss => boss;

		public Color EnergyGlow => energyGlow;

		public float SelectionDuration => selectionDuration;

		public float PanelDuration => panelDuration;

		public float EventPulseDuration => eventPulseDuration;

		public float TravelConfirmationDuration => travelConfirmationDuration;

		public float TransitionFadeDuration => transitionFadeDuration;

		public float SelectedScale => selectedScale;

		public Color GetRegionAccent(WorldRegionType type)
		{
			if (1 == 0)
			{
			}
			Color result = type switch
			{
				WorldRegionType.SafeHomeland => homelandAccent, 
				WorldRegionType.Forest => forestAccent, 
				WorldRegionType.Quarry => quarryAccent, 
				WorldRegionType.Marsh => marshAccent, 
				WorldRegionType.EmberRuins => emberAccent, 
				_ => frameNeutral, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		public WorldMapTypographyStyle GetTypography(WorldMapTypographyRole role)
		{
			if (1 == 0)
			{
			}
			WorldMapTypographyStyle result = role switch
			{
				WorldMapTypographyRole.MapTitle => mapTitle, 
				WorldMapTypographyRole.RegionName => regionName, 
				WorldMapTypographyRole.PanelHeading => panelHeading, 
				WorldMapTypographyRole.BodyText => bodyText, 
				WorldMapTypographyRole.SmallMetaText => smallMetaText, 
				WorldMapTypographyRole.TimerText => timerText, 
				WorldMapTypographyRole.ButtonText => buttonText, 
				WorldMapTypographyRole.DangerLabel => dangerLabel, 
				_ => bodyText, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	[Serializable]
	public struct WorldMapTypographyStyle
	{
		[SerializeField]
		private Font font;

		[SerializeField]
		private int size;

		[SerializeField]
		private FontStyle style;

		public Font Font => font;

		public int Size => Mathf.Max(8, size);

		public FontStyle Style => style;
	}
}
