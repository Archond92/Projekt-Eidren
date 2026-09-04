namespace Eidren.Interaction
{
	public enum InteractionCancelReason
	{
		None = 0,
		InputReleased = 1,
		OutOfRange = 2,
		TargetChanged = 3,
		AttackStarted = 4,
		DodgeStarted = 5,
		PlayerDamaged = 6,
		PlayerDied = 7,
		TargetUnavailable = 8,
		SceneTransition = 9,
		InputDisabled = 10,
		ControllerDisabled = 11
	}
}
