using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdbConverter
{
    class Program
    {
        public static string[] AtomSymbols = { "C", "H", "N", "O", "P", "S" }; 

        static void Main(string[] args)
        {
            // Read the file and display it line by line.
            var file = new System.IO.StreamReader(@"D:\Projects\Unity5\CellPackViewer\Data\HIV_mb.pdb");
            var writer = new BinaryWriter(File.Open(@"D:\Projects\Unity5\CellPackViewer\Data\output_m.bin", FileMode.Create));

            int counter = 0;

            string line;
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("ATOM"))
                {
                    //var split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var xStr = line.Substring(30, 8);
                    var yStr = line.Substring(38, 8);
                    var zStr = line.Substring(46, 8);

                    //var atomSymbolId = Array.IndexOf(AtomSymbols, split[2][0].ToString());
                    //if (atomSymbolId < 0) throw new Exception("Atom symbol not found");

                    var floatArray = new[]
                    {
                        0,
                        float.Parse(xStr),
                        float.Parse(yStr),
                        float.Parse(zStr),
                    };

                    var byteArray = new byte[floatArray.Length * sizeof(float)];
                    Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

                    writer.Write(byteArray);

                    counter ++;
                }

                if(counter % 1000 == 0)
                    Console.WriteLine(counter);
            }

            file.Close();

        }
    }
}
