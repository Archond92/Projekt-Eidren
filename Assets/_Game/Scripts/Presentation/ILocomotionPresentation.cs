using UnityEngine;

namespace Eidren.Presentation
{
	/// <summary>
	/// Darstellungen, die eine Fortbewegungsrichtung visuell umsetzen koennen
	/// (2D: Schrittversatz der Sprite-Wurzel; 3D: Lauf-/Gehanimation).
	/// </summary>
	public interface ILocomotionPresentation
	{
		/// <summary>
		/// Meldet die aktuelle Bewegungsrichtung in Weltkoordinaten sowie, ob
		/// sich der Akteur gerade bewegt.
		/// </summary>
		void SetLocomotion(Vector3 worldDirection, bool moving);
	}
}
