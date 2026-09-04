namespace Eidren.Presentation
{
	/// <summary>
	/// Darstellungen, die Ausruestungs-Varianten (Asset-Set) und Ruestungsteile
	/// samt Tier-Stufen visuell anzeigen koennen.
	/// </summary>
	public interface IArmorPresentation
	{
		/// <summary>Wechselt die Asset-Variante (z. B. Basis- vs. Wanderer-Set).</summary>
		void SetAssetVariant(string actorId);

		/// <summary>Blendet einzelne Ruestungsteile ein oder aus.</summary>
		void SetArmorParts(bool head, bool chest, bool hands, bool legs);

		/// <summary>Setzt die Tier-Stufe je Ruestungsteil (0 = kein Teil).</summary>
		void SetArmorTiers(int head, int chest, int hands, int legs);
	}
}
