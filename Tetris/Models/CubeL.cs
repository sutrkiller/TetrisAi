using System.Collections.Generic;

namespace Tetris.Models
{
    public class CubeL : Cube
    {
        public CubeL()
        {
            Rotations = new Dictionary<int, int[][]>()
            {
                {0,new[]
                {
                    new[] {0,0,1},
                    new[] {1,1,1},
                } },
                {1,new[]
                {
                    new[] {1,0},
                    new[] {1,0},
                    new[] {1,1}
                } },
                {2,new[]
                {
                    new[] {1,1,1},
                    new[] {1,0,0},
                } },
                {3,new[]
                {
                    new[] {1,1},
                    new[] {0,1},
                    new[] {0,1},
                } }
            };

        }
    }
}