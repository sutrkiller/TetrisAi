using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tetris.Models;

namespace Tetris
{
    public class AiFinal
    {
        private readonly double[] _fact =
            "-0.25246807;-0.37864260;-0.56586424;-0.01309536;0.21249134;-0.33714135;-0.50522643;-0.08181478;-0.27294390" //3M+
            .Split(';').Select(Convert.ToDouble).ToArray();

        public NextMove DiscoverNextMove(Grid grid, IReadOnlyList<Cube> cubes, int depth = 0)
        {
            if (cubes.Count == depth) return null;

            var allPossibilities = GenerateAllPossibilities(cubes[depth], grid.Width);
            var agrValues = new NextMove[allPossibilities.Count];

            Parallel.For(0, allPossibilities.Count, i =>
            {
                var pos = allPossibilities[i];
                var cube = cubes[depth].Rotations[pos.Item2];
                var gridLocal = grid.Clone();

                int lowest;
                var success = gridLocal.AddToColumn(cube,pos.Item1,out lowest);
                if (!success) return;

                var next = DiscoverNextMove(gridLocal, cubes, depth + 1);
//                double rating;
//                int estMoves;
                if (next != null)
                {
                    agrValues[i] = new NextMove() { Column = pos.Item1, Rotation = pos.Item2, EstimatedMoves = next.EstimatedMoves, Rating = next.Rating, Row = lowest};
//                    rating = next.Rating;
//                    estMoves = next.EstimatedMoves;
                }
                else
                {
                    gridLocal.DeleteRows();
                    var rating = gridLocal.RateGrid().Zip(_fact,(d,f) => d * f).Sum();
                    agrValues[i] = new NextMove() { Column = pos.Item1, Rotation = pos.Item2, EstimatedMoves = depth, Rating = rating, Row = lowest};
                }
                
            });
            return agrValues.Max();
        }

        private List<Tuple<int, int>> GenerateAllPossibilities(Cube cube, int w)
        {
            var allPossibilities = new List<Tuple<int, int>>();
            foreach (var rotation in cube.Rotations)
            {
                for (int j = 0; j < w; j++)
                {
                    allPossibilities.Add(new Tuple<int, int>(j, rotation.Key));
                }
            }
            return allPossibilities;
        }

    }
}
