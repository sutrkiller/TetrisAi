using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris.Models
{
    public class CubeI : Cube
    {
        public CubeI()
        {
            Rotations = new Dictionary<int, int[][]>()
            {
                {0,new[] { new[] {1,1,1,1} } },
                {1,new[]
                {
                    new[] {1},
                    new[] {1},
                    new[] {1},
                    new[] {1}
                } },
            };
        }
    }
}
