using System;
using System.Collections.Generic;

namespace Tetris.Models
{
    public abstract class Cube
    {
        public Dictionary<int, int[][]> Rotations { get; protected set; }
        public List<Tuple<int, int>> AllPossibilities { get; } = new List<Tuple<int, int>>();

        public void GenerateAllPossibilites(int w)
        {
            AllPossibilities.Clear();
            foreach (var rotation in Rotations)
            {
                for (int j = 0; j < w; j++)
                {
                    AllPossibilities.Add(new Tuple<int, int>(j, rotation.Key));
                }
            }
        }
    }
}
