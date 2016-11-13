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

        public void AddToPosition(IReadOnlyList<int[]> item, int i, int j)
        {
            for (var k = 0; k < item.Count; k++)
            {
                item[k].Zip(Cells[i + k].Skip(j), (a, b) => a + b).ToArray().CopyTo(Cells[i + k], j);
            }
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
    }
}
