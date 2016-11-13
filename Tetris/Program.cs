using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tetris
{
    public static class Program
    {
        private const int AheadRead = 2;

        //private static readonly double[] Fact = { -0.35663, -0.184483, -0.510066 /*, 0.760666 */};
        private static readonly double[] Fact =
           // "-0.62446979;-0.09672783;-0.01521569;0.09046457;0.47922994;-0.35749483;-0.28163465;-0.32577063;-0.22216521"
           //"-0.39727176;0.03739808;0.19663960;0.29286713;0.17238192;-0.52541337;-0.43137751;-0.42518959;-0.20902336" //37000
           // "0.34274436;0.01345615;-0.57152788;0.49026472;0.06213048;-0.38109708;-0.26206303;-0.30597272;0.06284988" //54000
           //"-0.48301579;-0.07205022;-0.43100421;-0.07430441;0.23473489;-0.35875137;-0.37667421;-0.38145489;-0.31468149"
            "-0.35663;-0.184483;-0.510066"
                .Split(';').Select(Convert.ToDouble).ToArray();

        public static void Main(string[] args)
        {
            string logFile = $"outputs_{DateTime.Now:yyyyMMddhhmmss}.txt";
            var str = new StreamWriter(File.Create(logFile)) { AutoFlush = true };

            try
            {
                switch (args[0] ?? "-p")
                {
                    case "-g":
                        if (args.Length != 5)
                        {
                            Console.WriteLine(
                                "Paramater -g requires four numbers to follow [1. number of generations, 2. population, 3. size of one input, 4. repeat input count]");
                            return;
                        }
                        PopulationGenerator generator = new PopulationGenerator(Convert.ToInt32(args[1]),
                            Convert.ToInt32(args[2]), Convert.ToInt32(args[3]), Convert.ToInt32(args[4]));
                        generator.RunGeneration(str);
                        //generator.RunGen(str);
                        break;
                    case "-c":
                        if (args.Length < 3 || (args[1] == "true" && args.Length < 4))
                        {
                            Console.WriteLine(
                                "Parameter -c requires [1. true/false parameter if input should be generated,[2.] if false, path to input file.]");
                            return;
                        }
                        string fileName;

                        if (args[1] == "true")
                        {
                            fileName = "inputs_autogen.txt";
                            GenerateInput.Generate(Convert.ToInt32(args[2]), Convert.ToInt32(args[3]), fileName);
                        }
                        else
                        {
                            fileName = args[2];
                        }

                        var games = File.ReadAllLines(fileName);
                        var inputs =
                            games.Select(
                                x =>
                                    Enumerable.Range(0, x.Length)
                                        .Select(
                                            i => x.Substring(i, i + AheadRead >= x.Length ? x.Length - i : AheadRead))
                                        .ToList()).ToList();


                        PlayGame(inputs, str);
                        break;
                    case "-p":
                        PlayAgainstPlayer(args.Length > 1 && args[1] == "-d");
                        break;
                    default:
                        ShowHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                str.WriteLine(ex.Message);
                Console.WriteLine($"Sorry, something went wrong...See log file [{logFile}] for more info.");
                Console.WriteLine();
                ShowHelp();
                throw;
            }
            finally
            {
                str.Dispose();
            }





            //PlayGame(inputs, str);


        }

        private static void PlayAgainstPlayer(bool debug = false)
        {
            string input;
            var player = new Ai(0,Fact);
            var grid = player.StartNewGame();
            int playedBlocks = 0;
            int clearedLines = 0;
            while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
            {
                //Console.WriteLine($">> {input}");
                var result = player.PlayOneMove(input, ref grid, ref playedBlocks, ref clearedLines,debug);
                if (result == null) return;
               if (!debug) Console.WriteLine($"{result.Item2} {result.Item1}");

            }
        }

      

        private static void PlayGame(IEnumerable<List<string>> inputs, StreamWriter str)
        {
            var player = new Ai(0, Fact);
            foreach (var input in inputs)
            //Parallel.ForEach(inputs, input =>
                    {
                        var grid = player.StartNewGame();
                        var result = player.PlayMoreMoves(input, ref grid);
                        Console.WriteLine($"{result.Item1}/{result.Item2}");
                    }
                //);

        }

        private static string DrawArray(IEnumerable<int[]> grid)
        {
            var builder = new StringBuilder();

            foreach (var t in grid)
            {
                builder.AppendLine(string.Join("", t));
            }
            builder.AppendLine();
            return builder.ToString();
        }

        private static void ShowHelp()
        {
            var builder = new StringBuilder();
            builder.AppendLine("This is an artificial intelligence for Tetris. It includes a genetic algorithm to learn play tetris.");
            builder.AppendLine("There are 3 modes to try (defined by paramters):");
            builder.AppendLine("Paramater \"-g\" starts genetic algorithm.");
            builder.AppendLine("\tIt requires four numbers to follow (1. number of generations [int], 2. population [int], 3. size of one input [int], 4. repeat input count [int])");
            builder.AppendLine("Parameter -c to play against computer. ");
            builder.AppendLine("It requires  (1. if input should be generated [true/false]) ");
            builder.AppendLine("\t(1==true ? 2. inputs count [int] 3. length of inputs [int])");
            builder.AppendLine("\t(1!=true ? 2. path to input file [string]");
            builder.AppendLine("Parameter -p to play against human player (or others).");
            builder.AppendLine("\tPossible argument [-d] to show debug info (== play grid after every move).");
            builder.AppendLine();
            Console.WriteLine(builder.ToString());
        }

    }
}