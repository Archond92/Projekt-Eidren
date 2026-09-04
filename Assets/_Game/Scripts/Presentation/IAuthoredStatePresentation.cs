namespace Eidren.Presentation
{
	/// <summary>
	/// Darstellungen, die benannte, autorierte Zustaende (Atlas-Stems bzw.
	/// Animationsclips) abspielen koennen.
	/// </summary>
	public interface IAuthoredStatePresentation
	{
		/// <summary>
		/// Spielt den benannten Zustand ab. <paramref name="normalizedTime"/> setzt die
		/// Wiedergabeposition (0..1), <paramref name="loop"/> steuert Endlosschleife,
		/// <paramref name="restart"/> erzwingt einen Neustart auch bei gleichem Stem.
		/// </summary>
		void SetAuthoredState(string stem, float normalizedTime = 0f, bool loop = false, bool restart = false);

		/// <summary>Zeitgebundener Zustand: 3D skaliert den Clip auf die Dauer, 2D spielt wie bisher.</summary>
		void SetAuthoredTimedState(string stem, float durationSeconds);
	}
}
