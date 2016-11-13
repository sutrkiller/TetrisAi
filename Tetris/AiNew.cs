using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tetris.Models;

namespace Tetris
{
    public class AiNew
    {
        private readonly Dictionary<Type, Cube> _cubes = new Dictionary<Type, Cube>()
        {
            {typeof(CubeI), new CubeI()},
            {typeof(CubeJ), new CubeJ()},
            {typeof(CubeL), new CubeL()},
            {typeof(CubeO), new CubeO()},
            {typeof(CubeS), new CubeS()},
            {typeof(CubeT), new CubeT()},
            {typeof(CubeZ), new CubeZ()}
        };

        public double[] Factors { get; } = new double[9];
        public int Id { get; }
        public List<Tuple<int, int>> PreviousGames { get; set; } = new List<Tuple<int, int>>();

        public AiNew(int id, double[] factors)
        {
            Id = id;
            factors.CopyTo(Factors, 0);
        }

        public Tuple<int, double, Tuple<int, int>> DiscoverNextMove(Grid grid, IReadOnlyList<Cube> seq, int depth = 0 )
        {
            if (seq.Count == depth) return null;

            // var agrValues = new ConcurrentBag<Tuple<int, double, Tuple<int, int>>>();
            var agrValues = new Tuple<int, double, Tuple<int, int>>[seq[depth].AllPossibilities.Count];

            //Parallel.ForEach(seq[depth].AllPossibilities, t =>
            Parallel.For(0, seq[depth].AllPossibilities.Count, i =>
            {
                var t = seq[depth].AllPossibilities[i];

                var newGrid =grid.Clone();

                int lowest;
                var success = newGrid.AddToColumn(seq[depth].Rotations[t.Item2], t.Item1, out lowest);
                if (!success) return; //return;

                var next = DiscoverNextMove(newGrid, seq, depth + 1);
                var value = next?.Item2 ?? RateGrid(newGrid, seq[depth].Rotations[t.Item2], t.Item1, lowest);

                agrValues[i] = new Tuple<int, double, Tuple<int, int>>(next?.Item1 ?? depth, value, t);

            }
            );
            agrValues = agrValues.Where(x => x != null).ToArray();

            if (agrValues.Length == 0) return null;
            //if (agrValues.Count == 0) return null;
            var maxCubes2 = agrValues.Max(a => a.Item1);
            var bestKey2 = agrValues.Where(x => x.Item1 == maxCubes2).Aggregate((seed, n) => n.Item2 > seed.Item2 ? n : seed);
            //File.AppendAllText("test2.txt",$"{bestKey?.Item3?.Item1} {bestKey?.Item3?.Item2}\n");
            return bestKey2;
        }

        private double RateGrid(Grid grid, IReadOnlyCollection<int[]> item, int column, int row)
        {
            var fullRows = grid.DeleteRows().Count;
            var heights = ColumnHeights(grid.Cells);

            var jumps = Enumerable.Range(0, heights.Length - 1).Select(x => Math.Abs(heights[x] - heights[x + 1])).Sum();
            //var fullRows = RowsToDelete(grid).Count();
            var heightsSum = heights.Sum();

            var landingRow = grid.Height - (row + item.Count);
            var rowTransitions = RowTransitions(grid.Cells);
            var compTransitions = ColumnTransitions(grid.Cells);
            var gaps = compTransitions.Item3;
            var columnTrans = compTransitions.Item1;
            var holesDepth = compTransitions.Item2;
            var rowsWithGaps = compTransitions.Item4;
            var fullCells = compTransitions.Item5;


            var tmpArray = new[] { gaps, jumps, heightsSum, /*fullRows,*/ fullCells, landingRow, rowTransitions, columnTrans, holesDepth, rowsWithGaps };
            return tmpArray.Zip(Factors, (i, d) => i * d).Sum();
        }

        private Tuple<int, int, int, int, int> ColumnTransitions(IReadOnlyList<int[]> grid)
        {
            var result = 0;
            var holesD = 0;
            var gaps = 0;
            var set = new HashSet<int>();
            var numCells = 0;
            for (int j = 0; j < grid[0].Length; j++)
            {
                var last = grid[0][j];
                var colH = 0;
                var oneAlready = false;
                for (int i = 0; i < grid.Count; i++)
                {
                    int[] c = grid[i];
                    if (c[j] == 1)
                    {
                        oneAlready = true;
                        ++colH;
                        ++numCells;
                    }
                    else
                    {
                        if (oneAlready)
                        {
                            ++gaps;
                            set.Add(i);
                        }
                        holesD += colH;
                        colH = 0;
                    }
                    if (c[j] == last) continue;
                    ++result;
                    last = c[j];
                }
            }
            return new Tuple<int, int, int, int, int>(result, holesD, gaps, set.Count, numCells);
        }

        private int RowTransitions(int[][] grid)
        {
            var result = 0;
            foreach (int[] r in grid)
            {
                var last = r[0];
                for (int j = 0; j < grid[0].Length; j++)
                {
                    if (r[j] == last) continue;
                    ++result;
                    last = r[j];
                }
            }
            return result;
        }

        private int[] ColumnHeights(IReadOnlyList<int[]> grid)
        {
            int[] heights = new int[grid[0].Length];
            for (int j = 0; j < grid[0].Length; j++)
            {
                for (int i = 0; i < grid.Count; i++)
                {
                    if (grid[i][j] == 1)
                    {
                        heights[j] = grid.Count - i;
                        break;
                    }
                }
            }
            return heights;
        }

    }
}
