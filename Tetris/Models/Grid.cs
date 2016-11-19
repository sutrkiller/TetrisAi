using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris.Models
{
    public class Grid
    {
        public int Width { get; }
        public int Height{ get; }
        public int[][] Cells { get; private set; }

        private int[][] _lastCube;
        private int _lastRow;
        private int _lastColumn;

        public Grid(int h, int w)
        {
            Width = w;
            Height = h;
            Cells = new int[Height][];
            for (int i = 0; i < Height; i++)
            {
                Cells[i] = new int[Width];
            }
        }

        public bool AddToColumn(int[][] item, int j, out int lowest)
        {
            //int lowest;
            if ((lowest = GetLowest(item, j)) == -1) return false;
            AddToPosition(item, lowest, j);
            return true;
        }

        public void AddToPosition(int[][] item, int i, int j)
        {
            for (var k = 0; k < item.Length; k++)
            {
                item[k].Zip(Cells[i + k].Skip(j), (a, b) => a + b).ToArray().CopyTo(Cells[i + k], j);
            }
            _lastCube = item;
            _lastRow = i;
            _lastColumn = j;
        }

        public List<int> DeleteRows()
        {
            var rows = RowsToDelete().ToList();
            if (!rows.Any()) return new List<int>();
            var newGrid = Cells.ToList();
            foreach (var row in rows)
            {
                newGrid.RemoveAt(row);
                newGrid.Insert(0, new int[Width]);
            }
            Cells = newGrid.ToArray();
            return rows;
        }

        public IEnumerable<int> RowsToDelete()
            => Enumerable.Range(0, Cells.Length).Where(x => !Cells[x].Any(a => a == 0));

        public int GetLowest(int[][] item, int j)
        {
            var results = Enumerable.Range(0, Cells.Length).ToList().TakeWhile(x => IsPossible(item, x, j)).ToList();
            return results.Any() ? results.Last() : -1;
        }

        public bool IsPossible(IReadOnlyList<int[]> item, int i, int j)
        {
            if (i < 0 || j < 0) return false;
            for (var k = 0; k < item.Count; k++)
            {
                for (var l = 0; l < item[0].Length; l++)
                {
                    if (i + k >= Cells.Length || j + l >= Cells[0].Length) return false;
                    if (item[k][l] == 1 && Cells[i + k][j + l] == 1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public Grid Clone()
        {
            var newGrid = new int[Cells.Length][];
            for (int i = 0; i < newGrid.Length; i++)
            {
                newGrid[i] = Cells[i].Clone() as int[];
            }

            var grid = new Grid(Height, Width) {Cells = newGrid};
            return grid;
        }

        public List<int> GetPossibleRotations(Cube item, int row, int column)
        {
            return item.Rotations.Where(x => IsPossible(x.Value, row, column)).Select(x => x.Key).ToList();
        }

        public double[] RateGrid()
        {
            var heights = ColumnHeights();

            var jumps = Enumerable.Range(0, heights.Length - 1).Select(x => Math.Abs(heights[x] - heights[x + 1])).Sum();
            var heightsSum = heights.Sum();

            var landingRow = Height - (_lastRow + _lastCube.Length);
            var rowTransitions = RowTransitions();
            var compTransitions = ColumnTransitions();
            double gaps = compTransitions.Item3;
            var columnTrans = compTransitions.Item1;
            var holesDepth = compTransitions.Item2;
            var rowsWithGaps = compTransitions.Item4;
            var fullCells = compTransitions.Item5;


            return new[] { gaps, jumps, heightsSum, fullCells, landingRow, rowTransitions, columnTrans, holesDepth, rowsWithGaps };
        }

        private Tuple<int, int, int, int, int> ColumnTransitions()
        {
            var result = 0;
            var holesD = 0;
            var gaps = 0;
            var set = new HashSet<int>();
            var numCells = 0;
            for (int j = 0; j < Width; j++)
            {
                var last = Cells[0][j];
                var colH = 0;
                var oneAlready = false;
                for (int i = 0; i < Height; i++)
                {
                    int[] c = Cells[i];
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

        private int RowTransitions()
        {
            var result = 0;
            foreach (int[] r in Cells)
            {
                var last = r[0];
                for (int j = 0; j < Width; j++)
                {
                    if (r[j] == last) continue;
                    ++result;
                    last = r[j];
                }
            }
            return result;
        }

        private int[] ColumnHeights()
        {
            int[] heights = new int[Width];
            for (int j = 0; j < Width; j++)
            {
                for (int i = 0; i < Height; i++)
                {
                    if (Cells[i][j] == 1)
                    {
                        heights[j] = Height - i;
                        break;
                    }
                }
            }
            return heights;
        }
    }
}
