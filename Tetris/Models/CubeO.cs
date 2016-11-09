using System.Collections.Generic;

namespace Tetris.Models
{
    public class CubeO : Cube
    {
        public CubeO()
        {
            Rotations = new Dictionary<int, int[][]>()
            {
                {0,new[]
                {
                    new[] {1,1},
                    new[] {1,1},
                } },
            };

        }
    }
}