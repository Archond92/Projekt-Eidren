namespace Eidren.Combat
{
	public interface IStaggerable
	{
		float CurrentStagger { get; }

		float MaxStagger { get; }

		void AddStagger(float amount);
	}
}
