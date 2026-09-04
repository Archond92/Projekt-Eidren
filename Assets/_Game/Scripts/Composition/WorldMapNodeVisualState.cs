namespace Eidren.Composition
{
	public readonly struct WorldMapNodeVisualState
	{
		public bool Available { get; }

		public bool Selected { get; }

		public bool Visited { get; }

		public bool EventActive { get; }

		public bool Boss { get; }

		public bool BossDefeated { get; }

		public bool Completed { get; }

		public WorldMapNodeVisualState(bool available, bool selected, bool visited, bool eventActive, bool boss, bool bossDefeated = false, bool completed = false)
		{
			Available = available;
			Selected = selected;
			Visited = visited;
			EventActive = eventActive;
			Boss = boss;
			BossDefeated = bossDefeated;
			Completed = completed;
		}
	}
}
