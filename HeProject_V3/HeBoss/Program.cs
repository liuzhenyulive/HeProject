﻿using System;
using System.IO;
using HeProject;
using Newtonsoft.Json;

namespace HeBoss
{
    internal class Program
    {
        private static void Main()
        {
            try
            {
                Console.WriteLine("请勿关闭此窗口,正在处理中...");

                var dataflow = new ProjectDataFlow();
                var pipeline = dataflow.CreatePipeLine();
                dataflow.Process("_Source.xlsx");
                pipeline.Wait();
                var writer = new WriteToExcel(dataflow.ProcessContext);
                writer.Write();
                File.WriteAllText("log.txt", JsonConvert.SerializeObject(dataflow.ProcessContext));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}