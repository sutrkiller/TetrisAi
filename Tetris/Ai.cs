using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        //public int PlayedBlocks => _playedBlocks;
        //public int ClearedLines => _clearedLines;
        //public int PlayedGames => _playedGames;
        public int Generation { get; }
        public ConcurrentBag<Tuple<int, int, int>> PreviousGames { get; set; } = new ConcurrentBag<Tuple<int,int, int>>();

        //private int[][] _grid;
        private readonly int _h;
        private readonly int _w;
        //private bool _playing;
        //private int _playedBlocks;
        //private int _clearedLines;
        private int _playedGames;

        private Ai(int height, int width)
        {
            _h = height;
            _w = width;
            foreach (var cube in _cubes)
            {
                cube.Value.GenerateAllPossibilites(_w);
            }
        }

        public Ai(double[] factors, int height = 20, int width = 10) : this(height, width)
        {
            factors.CopyTo(Factors, 0);
        }

        public Ai(int id, double[] factors, int generation = 0, int height = 20, int width = 10) : this(height, width)
        {
            Id = id;
            Generation = generation;
            factors.CopyTo(Factors, 0);
        }

        public Ai(string csvLine, int height = 20, int width = 10) : this(height, width)
        {

            var parts = csvLine.Split(';');
            Id = Convert.ToInt32(parts[0]);
            Generation = Convert.ToInt32(parts[1]);
            PreviousGames = new ConcurrentBag<Tuple<int, int,int>>(parts[2].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x =>
               {
                   var tmp = x.Split('/');
                   return new Tuple<int, int, int>(Convert.ToInt32(tmp[0]), Convert.ToInt32(tmp[1]), tmp.Length > 2 ? Convert.ToInt32(tmp[2]) : 0);
               }));
            //_playedBlocks = PreviousGames.LastOrDefault()?.Item1 ?? 0;
            //_clearedLines = PreviousGames.LastOrDefault()?.Item2 ?? 0;
            _playedGames = PreviousGames.Count;

            //GapsW = Convert.ToDouble(parts[4]);
            //JumpsW = Convert.ToDouble(parts[5]);
            //HeightsW = Convert.ToDouble(parts[6]);
            //FullRowsW = Convert.ToDouble(parts[7]);
            parts.Skip(3).Select(Convert.ToDouble).ToArray().CopyTo(Factors, 0);
            foreach (var cube in _cubes)
            {
                cube.Value.GenerateAllPossibilites(_w);
            }
        }

        public int[][] StartNewGame()
        {
            var grid = new int[_h][];
            for (int i = 0; i < _h; i++)
            {
                grid[i] = new int[_w];
            }
            //_playedBlocks = 0;
            //_clearedLines = 0;

            // _playing = true;
            return grid;
        }

        public void EndRound(int keepGames)
        {
            PreviousGames = new ConcurrentBag<Tuple<int, int, int>>(PreviousGames.Skip(PreviousGames.Count - keepGames));
            //_playedBlocks = 0;
            //_clearedLines = 0;
            //_playing = false;
        }

        public Tuple<int, int> PlayOneMove(string input, ref int[][] grid, ref int playedBlocks, ref int clearedLines, ref int maxHeight, bool writeDebug = false)
        {
            //if (!_playing) throw new InvalidOperationException("Game has to be started first.");
            if (writeDebug) Console.WriteLine($">> {input}");
            var seq = input.Select(x => _cubes[x.ToString().ToUpper()]).ToList();
            var next = DiscoverNextMove(grid, seq);
            if (writeDebug) Console.WriteLine($"<< {next?.Item3.Item1} {next?.Item3.Item2}");

            int lowest = 0;
            var res = next != null && AddToGridColumn(grid, seq[0].Rotations[next.Item3.Item2], next.Item3.Item1, out lowest);

            if (!res)
            {
                //_playing = false;
                //_playedGames++;
                Interlocked.Increment(ref _playedGames);
                PreviousGames.Add(new Tuple<int, int, int>(playedBlocks, clearedLines, maxHeight));

                return null;
            }
            var delRows = DeleteRows(ref grid);
            var mHeight = ColumnHeights(grid).Max();
            if (maxHeight < mHeight) maxHeight = mHeight;
            clearedLines += delRows.Count;
            ++playedBlocks;
            //Console.WriteLine(DrawArray(_grid));
            //Console.WriteLine();

            if (writeDebug)
            {
                var debugGrid = grid.Select(x => x.Select(c => c == 0 ? " " : "#").ToArray()).ToList();
                var item = seq[0].Rotations[next.Item3.Item2];
                for (int k = 0; k < item.Length; k++)
                {
                    item[k].Select(x => x == 0 ? " " : "@").Zip(debugGrid[lowest + k].Skip(next.Item3.Item1), (a, b) => a == "@" ? a : b).ToArray().CopyTo(debugGrid[lowest + k], next.Item3.Item1);
                }
                debugGrid = debugGrid.Where((x, i) => !delRows.Contains(i)).ToList();
                for (int i = 0; i < delRows.Count; i++)
                {
                    debugGrid.Insert(0, Enumerable.Repeat(" ", _w).ToArray());
                }
                Console.WriteLine(string.Join(Environment.NewLine, debugGrid.Select(x => "|" + string.Join("", x) + "|")));
                Console.WriteLine("+" + string.Join("", Enumerable.Repeat("-", _w)) + "+");
                Console.WriteLine();
            }

            return next.Item3;
        }

        public Tuple<int, int,int> PlayOneMove(string input, int[][] grid)
        {
            var seq = input.Select(x => _cubes[x.ToString().ToUpper()]).ToList();
            var next = DiscoverNextMove(grid, seq);
            int lowest = 0;
            var res = next != null && AddToGridColumn(grid, seq[0].Rotations[next.Item3.Item2], next.Item3.Item1, out lowest);

            if (!res)
            {
                return null;
            }
            return new Tuple<int, int,int>(next.Item3.Item1, next.Item3.Item2, lowest);
        }

        public Tuple<int, int,int> PlayMoreMoves(IEnumerable<string> inputs, ref int[][] grid)
        {
            //if (!_playing) throw new InvalidOperationException("Game has to be started first.");
            var playedBlocks = 0;
            var clearedLines = 0;
            var maxHeight = 0;
            var playing = true;
            foreach (var t in inputs)
            {
                if (PlayOneMove(t, ref grid, ref playedBlocks, ref clearedLines, ref maxHeight) != null)
                {
                    Console.WriteLine(playedBlocks);
                    continue;
                }
                playing = false;
                break;
            }
            var result = new Tuple<int, int, int>(playedBlocks, clearedLines, maxHeight);
            if (playing)
            {
                Interlocked.Increment(ref _playedGames);
                //++_playedGames;
                PreviousGames.Add(result);
            }
            return result;
            //return result;
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

        private Tuple<int, double, Tuple<int, int>> DiscoverNextMove(IReadOnlyList<int[]> grid, IReadOnlyList<Cube> seq, int depth = 0)
        {

            if (seq.Count == depth) return null;

            // var agrValues = new ConcurrentBag<Tuple<int, double, Tuple<int, int>>>();
            var agrValues = new Tuple<int, double, Tuple<int, int>>[seq[depth].AllPossibilities.Count];

            //Parallel.ForEach(seq[depth].AllPossibilities, t =>
            Parallel.For(0, seq[depth].AllPossibilities.Count, i =>
            {
                var t = seq[depth].AllPossibilities[i];

                var newGrid = CopyGrid(grid);

                int lowest;
                var success = AddToGridColumn(newGrid, seq[depth].Rotations[t.Item2], t.Item1, out lowest);
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


        private double RateGrid(int[][] grid, IReadOnlyCollection<int[]> item, int column, int row)
        {
            var fullRows = DeleteRows(ref grid).Count;
            var heights = ColumnHeights(grid);

            var jumps = Enumerable.Range(0, heights.Length - 1).Select(x => Math.Abs(heights[x] - heights[x + 1])).Sum();
            //var fullRows = RowsToDelete(grid).Count();
            var heightsSum = heights.Sum();

            var landingRow = grid.Length - (row + item.Count);
            var rowTransitions = RowTransitions(grid);
            var compTransitions = ColumnTransitions(grid);
            var gaps = compTransitions.Item3;
            var columnTrans = compTransitions.Item1;
            var holesDepth = compTransitions.Item2;
            var rowsWithGaps = compTransitions.Item4;
            var fullCells = compTransitions.Item5;


            var tmpArray = new[] { gaps, jumps, heightsSum, /*fullRows,*/ fullCells, landingRow, rowTransitions, columnTrans, holesDepth, rowsWithGaps };
            return tmpArray.Zip(Factors, (i, d) => i * d).Sum();
        }

        private static Tuple<int, int, int, int, int> ColumnTransitions(IReadOnlyList<int[]> grid)
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

        private static int RowTransitions(int[][] grid)
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

        private static int NumberOfGaps(IReadOnlyList<int[]> grid, IReadOnlyList<int> heights)
        {
            int result = 0;
            var h = grid.Count;
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

        private static int[] ColumnHeights(IReadOnlyList<int[]> grid)
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

        private static int FullCells(IEnumerable<int[]> grid)
        {
            return grid.Aggregate(0, (i, ints) => i += ints.Sum());
        }

        private static IEnumerable<int> RowsToDelete(IReadOnlyList<int[]> grid)
            => Enumerable.Range(0, grid.Count).Where(x => !grid[x].Any(a => a == 0));

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

        private static void AddToPosition(IReadOnlyList<int[]> grid, IReadOnlyList<int[]> item, int i, int j)
        {
            for (var k = 0; k < item.Count; k++)
            {
                item[k].Zip(grid[i + k].Skip(j), (a, b) => a + b).ToArray().CopyTo(grid[i + k], j);
            }
        }

        private List<int> DeleteRows(ref int[][] grid)
        {
            var rows = RowsToDelete(grid).ToList();
            if (!rows.Any()) return new List<int>();
            var newGrid = grid.ToList();
            foreach (var row in rows)
            {
                newGrid.RemoveAt(row);
                newGrid.Insert(0, new int[_w]);
            }
            grid = newGrid.ToArray();
            return rows;
        }

        private static int GetLowest(int[][] grid, int[][] item, int j)
        {
            var results = Enumerable.Range(0, grid.Length).ToList().TakeWhile(x => IsPossible(grid, item, x, j)).ToList();
            return results.Any() ? results.Last() : -1;
        }

        private static bool IsPossible(IReadOnlyList<int[]> grid, IReadOnlyList<int[]> item, int i, int j)
        {
            for (var k = 0; k < item.Count; k++)
            {
                for (var l = 0; l < item[0].Length; l++)
                {
                    if (i + k >= grid.Count || j + l >= grid[0].Length) return false;
                    if (item[k][l] == 1 && grid[i + k][j + l] == 1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static int[][] CopyGrid(IReadOnlyList<int[]> grid)
        {
            var newGrid = new int[grid.Count][];
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
                    $"{Id};{Generation};{string.Join(",", PreviousGames.Select(x => $"{x.Item1}/{x.Item2}/{x.Item3}"))};{string.Join(";", Factors.Select(x => $"{x:0.00000000}"))}");
            //{GapsW:0.00000000};{JumpsW:0.00000000};{HeightsW:0.00000000};{FullRowsW:0.00000000}");
        }
    }
}
