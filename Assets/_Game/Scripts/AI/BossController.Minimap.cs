using Eidren.Core.Services;

namespace Eidren.AI
{
	/// <summary>
	/// N04-001: Der Boss trägt auf der Karte sein eigenes Symbol statt des roten
	/// Punkts der übrigen Gegner. Eigene Teildatei wegen §7.
	/// </summary>
	public sealed partial class BossController
	{
		protected override MinimapMarkerKind MinimapKind => MinimapMarkerKind.Boss;
	}
}
