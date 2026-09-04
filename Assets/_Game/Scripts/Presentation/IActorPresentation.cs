using UnityEngine;

namespace Eidren.Presentation
{
	public interface IActorPresentation
	{
		ActorFacing8 Facing { get; }

		ActorVisualState VisualState { get; }

		/// <summary>Weltgroesse der Figur in Metern, fuer Verdeckungs-Raycasts.</summary>
		float WorldHeight { get; }

		void SetFacing(ActorFacing8 facing);

		void SetVisualState(ActorVisualState state, float normalizedTime = 0f);

		void SetTint(Color color);
	}
}
