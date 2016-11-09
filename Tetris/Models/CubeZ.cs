using System.Collections.Generic;

namespace Tetris.Models
{
    public class CubeZ : Cube
    {
        public CubeZ()
        {
            Rotations = new Dictionary<int, int[][]>()
            {
                {0,new[]
                {
                    new[] {1,1,0},
                    new[] {0,1,1},
                } },
                {1,new[]
                {
                    new[] {0,1},
                    new[] {1,1},
                    new[] {1,0},
                } }
            };
        }
    }
}