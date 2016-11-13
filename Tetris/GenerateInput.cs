using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tetris.Models;

namespace Tetris
{
    public static class GenerateInput
    {
        private static readonly List<Cube> Cubes = new List<Cube>()
        {
            {new CubeI()},
            {new CubeJ()},
            {new CubeL()},
            {new CubeO()},
            {new CubeS()},
            {new CubeT()},
            {new CubeZ()}
        };

        private static Random _rand = new Random();
        public static void Generate(int number, int length, string path = "inputsGen.txt")
        {
            List<string> choices = new List<string>() { "I", "J", "T", "S", "Z", "O", "L" };
            
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < number; i++)
            {
                builder.AppendLine(string.Join("", Enumerable.Repeat(0, length).Select(x => choices[_rand.Next(0, choices.Count)])));
            }

            File.WriteAllText(path, builder.ToString());
        }

        public static List<Cube> GenerateNext(int count)
        {
            //List<string> choices = new List<string>() { "I", "J", "T", "S", "Z", "O", "L" };
            return Enumerable.Repeat(0, count).Select(x => Cubes[_rand.Next(0, Cubes.Count)]).ToList();
        }
    }
}
