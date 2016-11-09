using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tetris
{
    public class Program
    {
        private const int AheadRead = 2;

        //private static double[] _fact = { -0.35663, -0.184483, -0.510066, 0.760666 };
        private static double[] _fact = "-0.18546955;0.15218882;-0.08896772;0.46421439;0.28656953;-0.67705236;-0.32819135;-0.21207249;-0.16069557".Split(';').Select(Convert.ToDouble).ToArray();

        static void Main(string[] args)
        {
            var str = new StreamWriter(File.Create($"outputs_{DateTime.Now:yyyyMMddhhmmss}.txt"));
            str.AutoFlush = true;

            var games = File.ReadAllLines("inputs2.txt");
            var inputs = games.Select(x => Enumerable.Range(0, x.Length).Select(i => x.Substring(i, i + AheadRead >= x.Length ? x.Length - i : AheadRead)).ToList()).ToList();

            PopulationGenerator generator = new PopulationGenerator(1000, 500, 1000, 10);
            generator.RunGeneration(str);

            //PlayGame(inputs, str);

            str.Dispose();
        }

        private static void PlayGame(List<List<string>> inputs, StreamWriter str)
        {
            var player = new Ai(0,_fact);
            foreach (var input in inputs)
            {
                player.StartNewGame(20, 10);
                var result = player.PlayMoreMoves(input);
                Console.WriteLine($"{result.Item1}/{result.Item2}");
            }
        }

        
    }
}