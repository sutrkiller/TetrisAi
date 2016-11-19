using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris.Models
{
    public class NextMove : IComparable<NextMove>
    {
        public int Rotation { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public int EstimatedMoves { get; set; }
        public double Rating { get; set; }

        public int CompareTo(NextMove other)
        {
            int moves = EstimatedMoves - other.EstimatedMoves;
            if (moves != 0) return moves;

            if (Rating > other.Rating) return 1;
            if (Rating < other.Rating) return -1;

            return 0;
        }
    }
}
