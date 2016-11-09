using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris
{
    public static class GenerateInput
    {
        public static void Generate(int number, int length, string path = "inputsGen.txt")
        {
            List<string> choices = new List<string>() { "I", "J", "T", "S", "Z", "O", "L" };
            Random rand = new Random();
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < number; i++)
            {
                builder.AppendLine(string.Join("", Enumerable.Repeat(0, length).Select(x => choices[rand.Next(0, choices.Count)])));
            }

            File.WriteAllText(path, builder.ToString());
        }
    }
}
