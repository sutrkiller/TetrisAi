using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tetris.Models;

namespace Tetris
{
    public static class Program
    {
        private const int AheadRead = 2;
        private const int OneInputLength = 2;
        private const int Height = 20;
        private const int Width = 10;
        private static readonly Dictionary<char, Cube> Cubes = new Dictionary<char, Cube>()
        {
            {'I', new CubeI()},
            {'J', new CubeJ()},
            {'L', new CubeL()},
            {'O', new CubeO()},
            {'S', new CubeS()},
            {'T', new CubeT()},
            {'Z', new CubeZ()}
        };


        public static void Main(string[] args)
        {
            var inStream = Console.In;
            if (args.Length > 0)
            {
                try
                {
                    inStream = new StreamReader(File.OpenRead(args[0]));
                }
                catch (Exception)
                {
                    return;
                }
            }

            AiFinal ai = new AiFinal();
            Grid grid = new Grid(Height,Width);
            string line;
            long count = 1;
            while (!string.IsNullOrEmpty(line = inStream.ReadLine()?.ToUpper()))
            {
                IEnumerable<string> inputs = new List<string> {line};

                if (line.Length > OneInputLength)
                {
                    inputs = Enumerable.Range(0, line.Length).Select(x => string.Join("",line.Skip(x).Take(AheadRead)));
                }
                foreach (var input in inputs)
                {
                    var cubes = input.Select(x => Cubes[x]).ToList();
                    var next = ai.DiscoverNextMove(grid, cubes);
                    if (next == null) return;
                    grid.AddToPosition(cubes[0].Rotations[next.Rotation],next.Row,next.Column);
                    grid.DeleteRows();
                    Console.Out.WriteLine($"{next.Rotation} {next.Column} {count++}"); //TODO: delet count
                }
            }

        }

    }
}