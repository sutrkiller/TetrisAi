using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris.Models
{
    class CubeS : Cube
    {
        public CubeS()
        {
            Rotations = new Dictionary<int, int[][]>()
            {
                {0,new[]
                {
                    new[] {0,1,1},
                    new[] {1,1,0},
                } },
                {1,new[]
                {
                    new[] {1,0},
                    new[] {1,1},
                    new[] {0,1},
                } }
            };
        }
    }
}
