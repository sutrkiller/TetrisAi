using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tetris.Models;

namespace Tetris
{
    public class Ai
    {
        private readonly Dictionary<string, Cube> _cubes = new Dictionary<string, Cube>()
        {
            {"I", new CubeI()},
            {"J", new CubeJ()},
            {"L", new CubeL()},
            {"O", new CubeO()},
            {"S", new CubeS()},
            {"T", new CubeT()},
            {"Z", new CubeZ()}
        };

        public double[] Factors { get; } = new double[9];

        //public double GapsW { get; }
        //public double JumpsW { get; }
        //public double HeightsW { get; }
        //public double FullRowsW { get; }
        public int Id { get; }

        public int PlayedBlocks => _playedBlocks;
        public int ClearedLines => _clearedLines;
        public int PlayedGames => _playedGames;
        public int Generation { get; }
        public List<Tuple<int, int>> PreviousGames { get; set; } = new List<Tuple<int, int>>();

        private int[][] _grid;
        private int _h;
        private int _w;
        private bool _playing;
        private int _playedBlocks;
        private int _clearedLines;
        private int _playedGames;

        //public Ai(int id, double gapsW, double jumpsW, double heightsW, double fullRowsW)
        //{
        //    Id = id;
        //    GapsW = gapsW;
        //    JumpsW = jumpsW;
        //    HeightsW = heightsW;
        //    FullRowsW = fullRowsW;
        //}

        public Ai(int id, double[] factors, int generation = 0)
        {
            Id = id;
            Generation = generation;
            factors.CopyTo(Factors, 0);
        }

        public Ai(string csvLine)
        {

            var parts = csvLine.Split(';');
            Id = Convert.ToInt32(parts[0]);
            Generation = Convert.ToInt32(parts[1]);
            PreviousGames = parts[2].Split(new[] {','},StringSplitOptions.RemoveEmptyEntries).Select(x =>
            {
                var tmp = x.Split('/');
                return new Tuple<int, int>(Convert.ToInt32(tmp[0]), Convert.ToInt32(tmp[1]));
            }).ToList();
            _playedBlocks = PreviousGames.LastOrDefault()?.Item1 ?? 0;
            _clearedLines = PreviousGames.LastOrDefault()?.Item2 ?? 0;
            _playedGames = PreviousGames.Count;

            //GapsW = Convert.ToDouble(parts[4]);
            //JumpsW = Convert.ToDouble(parts[5]);
            //HeightsW = Convert.ToDouble(parts[6]);
            //FullRowsW = Convert.ToDouble(parts[7]);
            parts.Skip(3).Select(Convert.ToDouble).ToArray().CopyTo(Factors, 0);
        }

        public void StartNewGame(int h, int w)
        {
            _h = h;
            _w = w;
            _grid = new int[h][];
            for (int i = 0; i < h; i++)
            {
                _grid[i] = new int[w];
            }
            _playedBlocks = 0;
            _clearedLines = 0;
            foreach (var cube in _cubes)
            {
                cube.Value.GenerateAllPossibilites(_w);
            }
            _playing = true;
        }

        public void EndRound(int keepGames)
        {
            PreviousGames = PreviousGames.Skip(PreviousGames.Count - keepGames).ToList();
            _playedBlocks = 0;
            _clearedLines = 0;
            _playing = false;
        }

        public bool PlayOneMove(string input)
        {
            //if (!_playing) throw new InvalidOperationException("Game has to be started first.");
            //Console.WriteLine($">> {input}");
            var seq = input.Select(x => _cubes[x.ToString().ToUpper()]).ToList();
            var next = DiscoverNextMove(_grid, seq);
            //Console.WriteLine($"<< {next?.Item3.Item1} {next?.Item3.Item2}");
            int lowest;
            var res = next != null && AddToGridColumn(_grid, seq[0].Rotations[next.Item3.Item2], next.Item3.Item1, out lowest);
            if (!res)
            {
                _playing = false;
                _playedGames++;
                PreviousGames.Add(new Tuple<int, int>(_playedBlocks, _clearedLines));
                return false;
            }
            _clearedLines += DeleteRows();
            ++_playedBlocks;
            //Console.WriteLine(DrawArray(_grid));
            //Console.WriteLine();
            return true;
        }

        public Tuple<int, int> PlayMoreMoves(List<string> inputs)
        {
            if (!_playing) throw new InvalidOperationException("Game has to be started first.");
            foreach (string t in inputs)
            {
                if (!PlayOneMove(t))
                {
                    break;
                }
            }
            if (_playing)
            {
                _playing = false;
                ++_playedGames;
                PreviousGames.Add(new Tuple<int, int>(_playedBlocks, _clearedLines));
            }
            return new Tuple<int, int>(_playedBlocks, _clearedLines);
        }

        private string DrawArray(int[][] grid)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var t in grid)
            {
                builder.AppendLine(string.Join("", t));
            }
            builder.AppendLine();
            return builder.ToString();
        }

        private Tuple<int, double, Tuple<int, int>> DiscoverNextMove(int[][] grid, List<Cube> seq, int depth = 0)
        {

            if (seq.Count == depth) return null;

            ConcurrentBag<Tuple<int, double, Tuple<int, int>>> agrValues = new ConcurrentBag<Tuple<int, double, Tuple<int, int>>>();

            Parallel.ForEach(seq[depth].AllPossibilities, t =>
            {
                var newGrid = CopyGrid(grid);

                int lowest;
                var success = AddToGridColumn(newGrid, seq[depth].Rotations[t.Item2], t.Item1, out lowest);
                if (!success) return;

                var next = DiscoverNextMove(newGrid, seq, depth + 1);

                var value = RateGrid(newGrid, seq[depth].Rotations[t.Item2], t.Item1, lowest);

                agrValues.Add(new Tuple<int, double, Tuple<int, int>>(next?.Item1 ?? depth, next?.Item2 + value ?? value, t));

            }
            );

            if (agrValues.Count == 0) return null;
            var maxCubes2 = agrValues.Max(a => a.Item1);
            var bestKey2 = agrValues.Where(x => x.Item1 == maxCubes2).Aggregate((seed, n) => n.Item2 > seed.Item2 ? n : seed);
            //File.AppendAllText("test2.txt",$"{bestKey?.Item3?.Item1} {bestKey?.Item3?.Item2}\n");
            return bestKey2;
        }

        private double RateGrid(int[][] grid, int[][] item, int column, int row)
        {
            var heights = ColumnHeights(grid);

            var jumps = Enumerable.Range(0, heights.Length - 1).Select(x => Math.Abs(heights[x] - heights[x + 1])).Sum();
            var fullRows = RowsToDelete(grid).Count();
            var heightsSum = heights.Sum();

            var landingRow = grid.Length - (row + item.Length);
            var rowTransitions = RowTransitions(grid);
            var compTransitions = ColumnTransitions(grid);
            var gaps = compTransitions.Item3;
            var columnTrans = compTransitions.Item1;
            var holesDepth = compTransitions.Item2;
            var rowsWithGaps = compTransitions.Item4;
            var fullCells = compTransitions.Item5;


            var tmpArray = new[] { gaps, jumps, heightsSum, fullRows, fullCells, landingRow, rowTransitions, columnTrans, holesDepth, rowsWithGaps };
            return tmpArray.Zip(Factors, (i, d) => i * d).Sum();


        }

        private Tuple<int, int, int, int, int> ColumnTransitions(int[][] grid)
        {
            var result = 0;
            var holesD = 0;
            var gaps = 0;
            var set = new HashSet<int>();
            var numCells = 0;
            for (int j = 0; j < grid[0].Length; j++)
            {
                var last = 0;
                var colH = 0;
                var oneAlready = false;
                for (int i = 0; i < grid.Length; i++)
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
                        if (oneAlready) ++gaps;
                        set.Add(i);
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
                var last = 1;
                for (int j = 0; j < grid[0].Length; j++)
                {
                    if (r[j] == last) continue;
                    ++result;
                    last = r[j];
                }
            }
            return result;
        }

        private static int NumberOfGaps(int[][] grid, int[] heights)
        {
            int result = 0;
            var h = grid.Length;
            var w = grid[0].Length;
            var set = new HashSet<int>();
            for (int j = 0; j < w; j++)
            {
                for (int i = h - heights[j] + 1; i < h; i++)
                {
                    result += 1 - grid[i][j];
                }
            }

            return result;
        }

        private static int[] ColumnHeights(int[][] grid)
        {
            int[] heights = new int[grid[0].Length];
            for (int j = 0; j < grid[0].Length; j++)
            {
                for (int i = 0; i < grid.Length; i++)
                {
                    if (grid[i][j] == 1)
                    {
                        heights[j] = grid.Length - i;
                        break;
                    }
                }
            }
            return heights;
        }

        private static int FullCells(int[][] grid)
        {
            return grid.Aggregate(0, (i, ints) => i += ints.Sum());
        }

        private static IEnumerable<int> RowsToDelete(int[][] grid)
            => Enumerable.Range(0, grid.Length).Where(x => !grid[x].Any(a => a == 0));

        private static bool AddToGridColumn(int[][] grid, int[][] item, int j, out int lowest)
        {
            //int lowest;
            if ((lowest = GetLowest(grid, item, j)) == -1) return false;
            AddToPosition(grid, item, lowest, j);
            return true;
        }

        //private int[][] AddToGridColumnCopy(int[][] grid, int[][] item, int j)
        //{
        //    var newGrid = CopyGrid(grid);
        //    if (!AddToGridColumn(newGrid, item, j)) return null;
        //    return newGrid;
        //}

        private static void AddToPosition(int[][] grid, int[][] item, int i, int j)
        {
            for (int k = 0; k < item.Length; k++)
            {
                item[k].Zip(grid[i + k].Skip(j), (a, b) => a + b).ToArray().CopyTo(grid[i + k], j);
            }
        }

        private int DeleteRows()
        {
            var rows = RowsToDelete(_grid).ToList();
            if (!rows.Any()) return 0;
            var newGrid = _grid.ToList();
            foreach (var row in rows)
            {
                newGrid.RemoveAt(row);
                newGrid.Insert(0, new int[_w]);
            }
            _grid = newGrid.ToArray();
            return rows.Count();
        }

        private static int GetLowest(int[][] grid, int[][] item, int j)
        {
            var results = Enumerable.Range(0, grid.Length).ToList().TakeWhile(x => IsPossible(grid, item, x, j)).ToList();
            return results.Any() ? results.Last() : -1;
        }

        private static bool IsPossible(int[][] grid, int[][] item, int i, int j)
        {
            for (var k = 0; k < item.Length; k++)
            {
                for (var l = 0; l < item[0].Length; l++)
                {
                    if (i + k >= grid.Length || j + l >= grid[0].Length) return false;
                    if (item[k][l] == 1 && grid[i + k][j + l] == 1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static int[][] CopyGrid(int[][] grid)
        {
            int[][] newGrid = new int[grid.Length][];
            for (int i = 0; i < newGrid.Length; i++)
            {
                newGrid[i] = grid[i].Clone() as int[];
            }
            return newGrid;
        }

        public override string ToString()
        {
            return
                string.Format(
                    $"{Id};{Generation};{string.Join(",", PreviousGames.Select(x => $"{x.Item1}/{x.Item2}"))};{string.Join(";", Factors.Select(x => $"{x:0.00000000}"))}");
            //{GapsW:0.00000000};{JumpsW:0.00000000};{HeightsW:0.00000000};{FullRowsW:0.00000000}");
        }
    }
}
