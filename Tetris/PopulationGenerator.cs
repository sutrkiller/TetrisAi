using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tetris
{
    public class PopulationGenerator
    {
        private readonly int _population;
        private readonly int _inputSize;
        private readonly int _inputsCount;
        private readonly int _h;
        private readonly int _w;
        private readonly int _aheadRead;
        private readonly string _outputFolder;
        private readonly int _generations;
        private static readonly Random Rand = new Random();

        public PopulationGenerator(int generations, int population, int inputSize, int inputsCount, int h = 20, int w = 10, int aheadRead = 2, string outputFolder = "generations")
        {
            _population = population;
            _inputSize = inputSize;
            _inputsCount = inputsCount;
            _h = h;
            _w = w;
            _aheadRead = aheadRead;
            _outputFolder = outputFolder;
            _generations = generations;
        }


        private readonly int _take = 10;
        private static int _nextId = 0;
        public void RunGen(StreamWriter str)
        {
            Func<int,string> outputFile = (gen) => Path.Combine(_outputFolder,$"output_{gen}.txt");
            Func<int,string> inputFile = (gen) => Path.Combine(_outputFolder,$"input_{gen}.txt");
            Func<int,string> linesFile = (gen) => Path.Combine(_outputFolder,$"lines_{gen}.txt");
            Func<int,string> tmpFile = (gen) => Path.Combine(_outputFolder,$"tmp_{gen}.txt");
            Func<int,string> tmpFileC = (gen) => Path.Combine(_outputFolder,$"tmpC_{gen}.txt");
            var lastGen = Enumerable.Range(0, _generations).Select(x => (int?)x).LastOrDefault(x => File.Exists(inputFile(x.GetValueOrDefault())));
            var parents = lastGen != null ? File.ReadAllLines(inputFile(lastGen.Value)).Select(x => new Ai(x)).ToList()
                : Enumerable.Range(0, _population).Select((x, i) => GeneratePlayer(i, 0)).ToList();
            File.WriteAllLines(inputFile(lastGen.GetValueOrDefault()),parents.Select(x=>x.ToString()));

            

            var tmpParents = new List<Ai>();
            if (File.Exists(tmpFile(lastGen.GetValueOrDefault())))
            {
                tmpParents = File.ReadAllLines(tmpFile(lastGen.GetValueOrDefault())).Select(x => new Ai(x)).ToList();
            }
            var tmpChildren = new List<Ai>();
            if (File.Exists(tmpFileC(lastGen.GetValueOrDefault())))
            {
                tmpChildren = File.ReadAllLines(tmpFileC(lastGen.GetValueOrDefault())).Select(x => new Ai(x)).ToList();
            }

            _nextId = Math.Max(parents.Max(x => x.Id) + 1,tmpChildren.Max(x=>x.Id) +1);

            for (int generation = lastGen.GetValueOrDefault(); generation < _generations; generation++)
            {
                Func<Ai, int> clearedLinesSelector = ai => ai.PreviousGames.Select(x => x.Item2).Sum();
                Func<Ai, int> playedBlocksSelector = ai => ai.PreviousGames.Select(x => x.Item1).Sum();
                var generation1 = generation;
                Func<Ai, double> selector = x => clearedLinesSelector(x) + (generation1 - x.Generation) / (double)_generations;

                if (!File.Exists(linesFile(generation)))
                {
                    GenerateInput.Generate(_inputsCount, (_inputSize /* * (generation + 1)*/), linesFile(generation));
                }
                var games = File.ReadAllLines(linesFile(generation));
                var inputs = games.Select(x => Enumerable.Range(0, x.Length)
                                .Select(i => x.Substring(i, i + _aheadRead >= x.Length ? x.Length - i : _aheadRead))
                                .ToList()).ToList();


                int counted = 0;
                if (!File.Exists(tmpFile(generation)))
                {
                    File.Create(tmpFile(generation)).Dispose();
                }
                foreach (var parent in parents)
                { 
                    if (counted >= tmpParents.Count)
                    {
                        parent.PreviousGames = new ConcurrentBag<Tuple<int, int>>();
                        foreach (var input in inputs)
                        //Parallel.ForEach(inputs, input =>
                        {
                            var grid = parent.StartNewGame();
                            parent.PlayMoreMoves(input, ref grid);
                        }
                        //);
                        File.AppendAllLines(tmpFile(generation), new[] { parent.ToString() });
                        tmpParents.Add(parent);
                    }
                    var pl = tmpParents[counted];
                    Console.WriteLine($"{generation,5}_{pl.Id,5}: {playedBlocksSelector(pl),6}/{clearedLinesSelector(pl),6} [{pl.Generation,4}]    ({++counted})");
                }
                parents = tmpParents.OrderByDescending(selector).ToList();

                LogString(str, "Parents:");
                LogString(str, $"CL: {generation,5}: Avg: {parents.Select(x => clearedLinesSelector(x)).Average(),6}, Min: {parents.Select(x => clearedLinesSelector(x)).Min(),6}, Max: {parents.Select(x => clearedLinesSelector(x)).Max(),6}");
                LogString(str, $"PB: {generation,5}: Avg: {parents.Select(x => playedBlocksSelector(x)).Average(),6}, Min: {parents.Select(x => playedBlocksSelector(x)).Min(),6}, Max: {parents.Select(x => playedBlocksSelector(x)).Max(),6}");
                LogString(str, "\tRatio: " + parents.Select(x => playedBlocksSelector(x)).Max() / (double)parents.Select(x => clearedLinesSelector(x)).Max());
                //var ordered = players.OrderByDescending(selector).ToList();
                LogString(str,
                    $"\t{parents[0].Id}: {playedBlocksSelector(parents[0])}/{clearedLinesSelector(parents[0])} [{parents[0].Generation}] - {string.Join(" ", parents[0].Factors.Select(x => $"{x:0.00000000}"))}");//{ordered[0].GapsW:0.00000000} {ordered[0].JumpsW:0.00000000} {ordered[0].HeightsW:0.00000000} {ordered[0].FullRowsW:0.00000000}");
                LogString(str,
                    $"\t{parents[1].Id}: {playedBlocksSelector(parents[1])}/{clearedLinesSelector(parents[1])} [{parents[1].Generation}] - {string.Join(" ", parents[1].Factors.Select(x => $"{x:0.00000000}"))}");//{ordered[1].GapsW:0.00000000} {ordered[1].JumpsW:0.00000000} {ordered[1].HeightsW:0.00000000} {ordered[1].FullRowsW:0.00000000}");
                LogString(str, "");

                File.WriteAllText(outputFile(generation),
                    string.Join(Environment.NewLine,
                        parents.Select(x => x.ToString())));



                List<Ai> children = new List<Ai>();
                for (int i = 0; i < parents.Count; i++)
                {
                    for (int j = i+1; j < parents.Count; j++)
                    {
                        children.Add(GenerateOffspring2(parents[i], parents[j], _nextId++ , generation + 1, selector));
                    }
                }

                counted = 0;
                if (!File.Exists(tmpFileC(generation)))
                {
                    File.Create(tmpFileC(generation)).Dispose();
                }
                foreach (var child in children)
                {
                    if (counted >= tmpChildren.Count)
                    {
                        child.PreviousGames = new ConcurrentBag<Tuple<int, int>>();
                        foreach (var input in inputs)
                        //Parallel.ForEach(inputs, input =>
                        {
                            var grid = child.StartNewGame();
                            child.PlayMoreMoves(input, ref grid);
                        }
                        //);
                        File.AppendAllLines(tmpFileC(generation), new[] { child.ToString() });
                        tmpChildren.Add(child);
                    }
                    var pl = tmpChildren[counted];
                    Console.WriteLine($"{generation,5}_{pl.Id,5}: {playedBlocksSelector(pl),6}/{clearedLinesSelector(pl),6} [{pl.Generation,4}]    ({++counted})");
                }
                children = tmpChildren.OrderByDescending(selector).ToList();

                LogString(str, "");
                LogString(str, $"CL: {generation,5}: Avg: {children.Select(x => clearedLinesSelector(x)).Average(),6}, Min: {children.Select(x => clearedLinesSelector(x)).Min(),6}, Max: {children.Select(x => clearedLinesSelector(x)).Max(),6}");
                LogString(str, $"PB: {generation,5}: Avg: {children.Select(x => playedBlocksSelector(x)).Average(),6}, Min: {children.Select(x => playedBlocksSelector(x)).Min(),6}, Max: {children.Select(x => playedBlocksSelector(x)).Max(),6}");
                LogString(str, "\tRatio: " + children.Select(x => playedBlocksSelector(x)).Max() / (double)children.Select(x => clearedLinesSelector(x)).Max());
                //var ordered = players.OrderByDescending(selector).ToList();
                LogString(str,
                    $"\t{children[0].Id}: {playedBlocksSelector(children[0])}/{clearedLinesSelector(children[0])} [{children[0].Generation}] - {string.Join(" ", children[0].Factors.Select(x => $"{x:0.00000000}"))}");//{ordered[0].GapsW:0.00000000} {ordered[0].JumpsW:0.00000000} {ordered[0].HeightsW:0.00000000} {ordered[0].FullRowsW:0.00000000}");
                LogString(str,
                    $"\t{children[1].Id}: {playedBlocksSelector(children[1])}/{clearedLinesSelector(children[1])} [{children[1].Generation}] - {string.Join(" ", children[1].Factors.Select(x => $"{x:0.00000000}"))}");//{ordered[1].GapsW:0.00000000} {ordered[1].JumpsW:0.00000000} {ordered[1].HeightsW:0.00000000} {ordered[1].FullRowsW:0.00000000}");
                LogString(str, "");

                File.AppendAllLines(outputFile(generation), new List<string> { "" });
                File.AppendAllLines(outputFile(generation), children.Select(x => x.ToString()));


                parents = parents.Concat(children).OrderByDescending(selector).Take(_population).ToList();

                

                //players = PrepareNextGeneration(players, generation + 1, selector);
                tmpParents.Clear();
                tmpChildren.Clear();
                File.WriteAllLines(inputFile(generation+1), parents.Select(x => x.ToString()));

            }
        }

        private Ai GenerateOffspring2(Ai ai1, Ai ai2, int id, int nextGeneration, Func<Ai, double> selector)
        {
            Func<int, int> errorSelector = index => Rand.Next(100) >= 5 ? -1 : index;
            var vector =
                NormalizeVector(
                    //ai1.Factors.Zip(ai2.Factors,(a1, a2) => CrossingFunction(a1,selector(ai1),a2,selector(ai2)))
                    ai1.Factors.Select((x, i) => CrossingFunction(x, selector(ai1), ai2.Factors[i], selector(ai2), i== errorSelector(i))).ToArray()

                    //CrossingFunction(ai1.GapsW, selector(ai1), ai2.GapsW, selector(ai2), iError == 0),
                    //CrossingFunction(ai1.JumpsW, selector(ai1), ai2.JumpsW, selector(ai2), iError == 1),
                    //CrossingFunction(ai1.HeightsW, selector(ai1), ai2.HeightsW, selector(ai2), iError == 2),
                    //CrossingFunction(ai1.FullRowsW, selector(ai1), ai2.FullRowsW, selector(ai2), iError == 3)
                    );
            return new Ai(id, vector, nextGeneration);
        }

        public void RunGeneration(StreamWriter str)
        {
            Func<Ai, int> playedBlocksSelector = ai => ai.PreviousGames.Select(x => x.Item1).Sum(); //ai.PreviousGames.Skip(ai.PreviousGames.Count - _inputsCount).Select(x => x.Item1).Sum();
            Func<Ai, int> clearedLinesSelector = ai => ai.PreviousGames.Select(x => x.Item2).Sum();//ai.PreviousGames.Skip(ai.PreviousGames.Count - _inputsCount).Select(x => x.Item2).Sum();

            //var players = Enumerable.Range(0, _population).Select((x, i) => GeneratePlayer(i, 0)).ToList();
            var lastGen = Enumerable.Range(0, _generations).Select(x => (int?)x).LastOrDefault(x=>File.Exists(Path.Combine(_outputFolder,$"input_{x}.txt")));
            var inputFileName = Path.Combine(_outputFolder, $"input_{lastGen ?? 0}.txt");
            var players = lastGen != null ? File.ReadAllLines(inputFileName).Select(x => new Ai(x)).ToList() 
                : Enumerable.Range(0, _population).Select((x, i) => GeneratePlayer(i, 0)).ToList();
            File.WriteAllLines(inputFileName,players.Select(x=>x.ToString()));

            List<Ai> tmpPlayers = new List<Ai>();
            string tmpFileName = Path.Combine(_outputFolder, $"tmp_{lastGen}.txt");
            if (File.Exists(tmpFileName))
            {
                tmpPlayers = File.ReadAllLines(tmpFileName).Select(x => new Ai(x)).ToList();
            }

            for (int generation = lastGen ?? 0; generation < _generations; generation++)
            {
                var generation1 = generation;
                Func<Ai, double> selector = x => /*playedBlocksSelector(x) * 2 +*/ clearedLinesSelector(x) + (generation1 - x.Generation) / (double)_generations;

                //if (File.Exists(GetGenerationFilePath(generation)))
                //{
                //    players =
                //        PrepareNextGeneration(
                //            File.ReadAllLines(GetGenerationFilePath(generation)).Select(x => new Ai(x)).ToList(),
                //            generation + 1, selector);
                //    continue;
                //}


                //if (File.Exists(Path.Combine(_outputFolder,$"input_{generation}.txt")))
                //{
                //    players =
                //        //PrepareNextGeneration(
                //            File.ReadAllLines(GetGenerationFilePath(generation)).Select(x => new Ai(x)).ToList();
                //    //continue;
                //}

                string fileName = Path.Combine(_outputFolder, $"inputsGen_{generation}.txt");
                
                if (!File.Exists(fileName))
                {
                    GenerateInput.Generate(_inputsCount, (_inputSize /* * (generation + 1)*/), fileName);
                }
                var games = File.ReadAllLines(fileName);
                var inputs =
                    games.Select(
                        x =>
                            Enumerable.Range(0, x.Length)
                                .Select(i => x.Substring(i, i + _aheadRead >= x.Length ? x.Length - i : _aheadRead))
                                .ToList()).ToList();


                //string tmpFileName = Path.Combine(_outputFolder, "progress.tmp");
                //if (File.Exists(tmpFileName))
                //{
                //    var lines = File.ReadAllLines("progress.tmp");
                //}

                
                int counted = 0;
                tmpFileName = Path.Combine(_outputFolder, $"tmp_{generation}.txt");
                if (!File.Exists(tmpFileName))
                {
                    File.Create(tmpFileName).Dispose();
                }
                foreach (var player in players)
                //Parallel.ForEach(players, player =>
                {
                    if (counted >= tmpPlayers.Count)
                    {
                        player.PreviousGames = new ConcurrentBag<Tuple<int, int>>();
                        foreach (var input in inputs)
                        //Parallel.ForEach(inputs, input =>
                            {
                                var grid = player.StartNewGame();
                                player.PlayMoreMoves(input, ref grid);
                            }
                        //);
                        //player.EndRound(_inputsCount);
                        File.AppendAllLines(tmpFileName, new[] { player.ToString() });
                        tmpPlayers.Add(player);
                    }
                    var pl = tmpPlayers[counted];
                    //File.AppendAllLines("progress.tmp",new []{player.ToString()});
                    Console.WriteLine(
                        $"{generation,5}_{pl.Id,5}: {playedBlocksSelector(pl),6}/{clearedLinesSelector(pl),6} [{pl.Generation,4}]    ({++counted})");
                }
                players = tmpPlayers;
                
                //);


                LogString(str,"");
                LogString(str, $"CL: {generation,5}: Avg: {players.Select(x => clearedLinesSelector(x)).Average(),6}, Min: {players.Select(x => clearedLinesSelector(x)).Min(),6}, Max: {players.Select(x => clearedLinesSelector(x)).Max(),6}");
                LogString(str, $"PB: {generation,5}: Avg: {players.Select(x => playedBlocksSelector(x)).Average(),6}, Min: {players.Select(x => playedBlocksSelector(x)).Min(),6}, Max: {players.Select(x => playedBlocksSelector(x)).Max(),6}");
                LogString(str,"\tRatio: "+ players.Select(x => playedBlocksSelector(x)).Max() / (double)players.Select(x => clearedLinesSelector(x)).Max());
                var ordered = players.OrderByDescending(selector).ToList();
                LogString(str,
                    $"\t{ordered[0].Id}: {playedBlocksSelector(ordered[0])}/{clearedLinesSelector(ordered[0])} [{ordered[0].Generation}] - {string.Join(" ", ordered[0].Factors.Select(x => $"{x:0.00000000}"))}");//{ordered[0].GapsW:0.00000000} {ordered[0].JumpsW:0.00000000} {ordered[0].HeightsW:0.00000000} {ordered[0].FullRowsW:0.00000000}");
                LogString(str,
                    $"\t{ordered[1].Id}: {playedBlocksSelector(ordered[1])}/{clearedLinesSelector(ordered[1])} [{ordered[1].Generation}] - {string.Join(" ", ordered[1].Factors.Select(x => $"{x:0.00000000}"))}");//{ordered[1].GapsW:0.00000000} {ordered[1].JumpsW:0.00000000} {ordered[1].HeightsW:0.00000000} {ordered[1].FullRowsW:0.00000000}");
                LogString(str,"");

                File.WriteAllText(GetGenerationFilePath(generation),
                    string.Join(Environment.NewLine,
                        ordered.Select(x => x.ToString())));

                players = PrepareNextGeneration(players, generation + 1, selector);
                tmpPlayers.Clear();
                File.WriteAllLines(Path.Combine(_outputFolder, $"input_{generation+1}.txt"),players.Select(x=>x.ToString()));
            }
        }

        private static void LogString(StreamWriter writer, string text, bool logToConsole = true, bool logToWriter = true)
        {
            if (logToWriter)
            {
                writer?.WriteLine(text);
            }
            if (logToConsole)
            {
                Console.WriteLine(text);
            }
        }

        private Ai GeneratePlayer(int id, int generation)
        {
            var facts = Enumerable.Range(0, 9).Select(x => NextParam()).ToArray();
            var vector = NormalizeVector(facts);
            //return new Ai(id, vector[0], vector[1], vector[2], vector[3]);
            return new Ai(id, vector, generation);
        }

        private List<Ai> PrepareNextGeneration(List<Ai> current, int nextGeneration, Func<Ai, double> selector)
        {
            List<Ai> offsprings = new List<Ai>();
            for (int i = 0; i < _population * 0.3; i++)
            {
                var selectedAis =
                    RandomSet((int)(0.1 * _population), _population)
                        .Select(x => current[x])
                        .OrderByDescending(selector)
                        .Take(2)
                        .ToList();
                offsprings.Add(GenerateOffspring2(selectedAis[0], selectedAis[1], nextGeneration * _population + i, nextGeneration, selector));
                //offsprings.Add(GenerateOffspring(selectedAis[0], selectedAis[1], nextGeneration * _population + i, nextGeneration, selector));
            }

            return current.OrderByDescending(selector).Take((int)(_population * 0.7)).Concat(offsprings).ToList();

        }

        private Ai GenerateOffspring(Ai ai1, Ai ai2, int id, int nextGeneration, Func<Ai, double> selector)
        {
            int iError = Rand.Next(100) >= 5 ? -1 : Rand.Next(ai1.Factors.Length);
            var vector =
                NormalizeVector(
                    //ai1.Factors.Zip(ai2.Factors,(a1, a2) => CrossingFunction(a1,selector(ai1),a2,selector(ai2)))
                    ai1.Factors.Select((x, i) => CrossingFunction(x, selector(ai1), ai2.Factors[i], selector(ai2), iError == i)).ToArray()

                    //CrossingFunction(ai1.GapsW, selector(ai1), ai2.GapsW, selector(ai2), iError == 0),
                    //CrossingFunction(ai1.JumpsW, selector(ai1), ai2.JumpsW, selector(ai2), iError == 1),
                    //CrossingFunction(ai1.HeightsW, selector(ai1), ai2.HeightsW, selector(ai2), iError == 2),
                    //CrossingFunction(ai1.FullRowsW, selector(ai1), ai2.FullRowsW, selector(ai2), iError == 3)
                    );
            return new Ai(id, vector, nextGeneration);
        }

        private double CrossingFunction(double d1, double w1, double d2, double w2, bool error)
        {
            return (d1 * w1 + d2 * w2) / (w1 + w2) + (error ? RandomlyNegate(Rand.Next(21) / 100.0) : 0);
        }

        private static IEnumerable<int> RandomSet(int count, int max)
        {
            HashSet<int> set = new HashSet<int>();

            while (set.Count != count)
            {
                set.Add(Rand.Next(max));
            }
            return set;
        }

        private string GetGenerationFilePath(int generation) => Path.Combine(_outputFolder, generation + ".txt");

        private static double RandomlyNegate(double param)
        {
            return Rand.Next(2) == 1 ? -param : param;
        }

        private static double NextParam()
        {
            //return RandomlyNegate(Rand.NextDouble());
            return -(Rand.NextDouble());
        }

        private static double[] NormalizeVector(params double[] items)
        {
            var div = Math.Sqrt(items.Sum(x => x * x));
            return Math.Abs(div) < 0.0000000001 ? items : items.Select(x => x / div).ToArray();
        }
    }
}