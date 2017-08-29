using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchStatistics.src
{
	public class SmallGroupSplit
	{
		private static double GetDelta(double[] array, int index)
		{
			if (index - 1 < 0 || index > array.Length)
			{
				return 0;
			}
			else
			{
				return array[index] - array[index - 1];
			}
		}

		public static Tuple<Hero[], Hero[]> SplitSmallGroup(Hero[] heros)
		{
			double[] heroDeltas = new double[heros.Length];
			for (int i = 0; i < heros.Length; ++i)
			{
				heroDeltas[i] = heros[i].Hours;
			}
			Tuple<double[], double[]> SSG_RAW = SplitSmallGroup(heroDeltas);
			Hero[] List1 = new Hero[SSG_RAW.Item1.Length];
			Hero[] List2 = (SSG_RAW.Item2 == null) ? null : new Hero[SSG_RAW.Item2.Length];

			for (int i = 0; i < heros.Length; ++i)
			{
				if (i < List1.Length)
				{
					List1[i] = heros[i];
				}
				else
				{
					List2[i - List1.Length] = heros[i];
				}
			}

			return new Tuple<Hero[], Hero[]>(List1, List2);
		}

		//This function is designed to split small groups in size of 3-10 ish.
		//It's not fancy and definitly won't work on larger sizes but for what I'm doing, it's OK.
		public static Tuple<double[], double[]> SplitSmallGroup(double[] group)
		{
			if (group.Length == 1)
			{
				return new Tuple<double[], double[]>(group, null);
			}

			double avgDelta = 0;
			List<double> Group1 = new List<double>();
			List<double> Group2 = new List<double>();
			bool group1 = true;
			double cGrad = avgDelta;

			for (int i = 1; i < group.Length; ++i)
			{
				avgDelta += GetDelta(group, i);
			}

			avgDelta /= group.Length - 1;

			//In OW, mains with ~5 hour gap of each other are pretty close.
			const int maxCap = 2;
			avgDelta = (avgDelta > maxCap) ? avgDelta : maxCap;

			Group1.Add(group[0]);
			for (int i = 1; i < group.Length; ++i)
			{
				double delta = GetDelta(group, i);
				if (group1)
				{
					if (delta >= avgDelta)
					{
						cGrad = delta;
						group1 = false;
						Group2.Add(group[i]);
					}
					else
					{
						Group1.Add(group[i]);
					}
				}
				else
				{
					if (delta > cGrad)
					{
						cGrad = delta;
						foreach (double item in Group2)
						{
							Group1.Add(item);
						}
						Group2.Clear();
					}
					Group2.Add(group[i]);
				}
			}

			return new Tuple<double[], double[]>(Group1.ToArray(), (Group2.Count == 0) ? null : Group2.ToArray());
		}
	}
}
