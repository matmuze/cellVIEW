using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ConsoleApplication1
{
    class Instance
    {
        public int type;
        public int compartment;
        public float[] data = new float[19];
    }


    class Program
    {

        private const int InstanceDataSize = 7;

        static void Main(string[] args)
        {
            bool beginInstanceBatch = false;
            bool beginGroupInstanceBatch = false;

            var instances = new List<Instance>();
            var instanceData = new float[InstanceDataSize];
            var instanceDataList = new List<float>();

            int totalInstanceCount = 0;
            int instanceDataCounter = 0;
            int instanceTypeCounter = 0;
            int ingredientPdbNameIndex = 0;

            string ingredientPdbName;

            List<string> ingredientPdbNames = new List<string>();

            JsonTextReader reader = new JsonTextReader(new StringReader(File.ReadAllText(@"D:\Projects\Unity5\CellPackViewer\Data\HIV\HIV-1_0.1.6-7_mixed_pdb.json")));
            BinaryWriter writer = new BinaryWriter(File.Open(@"D:\Projects\Unity5\CellPackViewer\Data\HIV\output.bin", FileMode.Create));
            
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    //Console.WriteLine(String.Format("Token: {0}, Value type: {1}, Value: {2}", reader.TokenType, reader.ValueType, reader.Value));

                    if (reader.Value.ToString() == "ingredients")
                    {
                        reader.Read();
                        reader.Read();
                        var compartement = reader.Value.ToString();
                        Console.WriteLine("*****");
                        Console.WriteLine("compartement: " + compartement);
                    }

                    if (reader.Value.ToString() == "pdb")
                    {
                        ingredientPdbName = reader.ReadAsString();
                        ingredientPdbName= ingredientPdbName.Replace(".pdb", "");

                        if (!ingredientPdbNames.Contains(ingredientPdbName)) ingredientPdbNames.Add(ingredientPdbName);
                        ingredientPdbNameIndex = ingredientPdbNames.IndexOf(ingredientPdbName);

                        Console.WriteLine("pdb name: " + ingredientPdbName);
                        Console.WriteLine("pdb name index: " + ingredientPdbNameIndex); 
                        
                        beginGroupInstanceBatch = true;
                    }
                    else if (beginGroupInstanceBatch && (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer))
                    {
                        instanceData[instanceDataCounter] = float.Parse(reader.Value.ToString());

                        instanceDataCounter++;
                        if (instanceDataCounter >= InstanceDataSize)
                        {
                            totalInstanceCount ++;
                            instanceDataList.AddRange(instanceData.ToList());
                            instanceDataCounter = 0;
                        }
                    }

                    if (beginGroupInstanceBatch && reader.Value.ToString() == "name")
                    {
                        string name =  reader.ReadAsString();
                        Console.WriteLine("name " + name);
                        Console.WriteLine("num instances " + instanceDataList.Count() / 7);

                        var aa = instanceDataList.ToArray();

                        for (int i = 0; i < instanceDataList.Count; i += InstanceDataSize)
                        {
                            var outData = new float[InstanceDataSize + 1];
                            outData[0] = ingredientPdbNameIndex;
                            Buffer.BlockCopy(aa, i * sizeof(float), outData, 1 * sizeof(float), InstanceDataSize * sizeof(float));
                            
                            var byteArray = new byte[outData.Length * sizeof(float)];
                            Buffer.BlockCopy(outData, 0, byteArray, 0, byteArray.Length);
                            writer.Write(byteArray);
                        }
                        instanceDataList.Clear();

                        beginGroupInstanceBatch = false;
                    }
                }
            }

            File.WriteAllLines(@"D:\Projects\Unity5\CellPackViewer\Data\HIV\output.txt", ingredientPdbNames);
        }
    }
}
