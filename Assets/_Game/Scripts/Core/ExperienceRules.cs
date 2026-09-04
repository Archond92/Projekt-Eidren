using Eidren.Data;
using System;

namespace Eidren.Core.Services
{
	public static class ExperienceRules
	{
		public static int ExperienceToNextLevel(int currentLevel)
		{
			return ProgressionFormula.ExperienceToNextLevel(currentLevel);
		}

		public static int Award(ExperienceSource source, int tier, bool firstCompletion = false)
		{
			if (tier < 0 || tier > 2)
			{
				throw new ArgumentOutOfRangeException("tier");
			}
			if (1 == 0)
			{
			}
			int result;
			switch (source)
			{
			case ExperienceSource.ResourceNode:
			{
				if (1 == 0)
				{
				}
				int num = tier switch
				{
					0 => 8, 
					1 => 12, 
					_ => 18, 
				};
				if (1 == 0)
				{
				}
				result = num;
				break;
			}
			case ExperienceSource.Recipe:
			{
				int num3;
				if (firstCompletion)
				{
					if (1 == 0)
					{
					}
					int num = tier switch
					{
						0 => 25, 
						1 => 50, 
						_ => 90, 
					};
					if (1 == 0)
					{
					}
					num3 = num;
				}
				else
				{
					if (1 == 0)
					{
					}
					int num = tier switch
					{
						0 => 1, 
						1 => 2, 
						_ => 4, 
					};
					if (1 == 0)
					{
					}
					num3 = num;
				}
				result = num3;
				break;
			}
			case ExperienceSource.Building:
			{
				int num2;
				if (firstCompletion)
				{
					if (1 == 0)
					{
					}
					int num = tier switch
					{
						0 => 40, 
						1 => 75, 
						_ => 125, 
					};
					if (1 == 0)
					{
					}
					num2 = num;
				}
				else
				{
					if (1 == 0)
					{
					}
					int num = tier switch
					{
						0 => 1, 
						1 => 2, 
						_ => 4, 
					};
					if (1 == 0)
					{
					}
					num2 = num;
				}
				result = num2;
				break;
			}
			default:
				throw new ArgumentOutOfRangeException("source");
			}
			if (1 == 0)
			{
			}
			return result;
		}
	}
}
