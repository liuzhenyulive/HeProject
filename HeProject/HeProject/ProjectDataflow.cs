﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HeProject.Model;
using HeProject.Part2;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace HeProject
{
    public class ProjectDataFlow
    {
        private readonly ExecutionDataflowBlockOptions _executionDataflowBlockOptions;
        public ProcessContext _processContext;
        private int _totalRow;
        private ITargetBlock<string> _startBlock;
        private object _lock = new object();

        public ProjectDataFlow()
        {

            _executionDataflowBlockOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
        }

        public void Process(string filePath)
        {
            _startBlock.Post(filePath);
            _startBlock.Complete();
        }

        public Task CreatePipeLine()
        {
            var p1S2Block = CreateReadFileBlock(CreateP1Block(2));

            var p1S3Block = CreateP1Block(3);
            var currentP1Block = p1S3Block;
            for (int i = 4; i < 10; i++)
            {
                var block = CreateP1Block(i);
                currentP1Block.LinkTo(block, new DataflowLinkOptions() { PropagateCompletion = true });
                currentP1Block = block;
            }

            IPropagatorBlock<int, int> p2S2Block = CreateP2Block(2);
            var currentP2Block = p2S2Block;
            for (int i = 3; i < 10; i++)
            {
                var block = CreateP2Block(i);
                currentP2Block.LinkTo(block, new DataflowLinkOptions() { PropagateCompletion = true });
                currentP2Block = block;
            }

            var p1BroadcastBlock = new BroadcastBlock<int>(i =>
              {
                  //Console.WriteLine($"第一,二部分第{i}行开始处理;");
                  return i;
              }, _executionDataflowBlockOptions);
            p1S2Block.LinkTo(p1BroadcastBlock, new DataflowLinkOptions() { PropagateCompletion = true });
            p1BroadcastBlock.LinkTo(p1S3Block, new DataflowLinkOptions() { PropagateCompletion = true });
            //   p1BroadcastBlock.LinkTo(p2S2Block, new DataflowLinkOptions() {PropagateCompletion = true});


            var finallyP1Block = new ActionBlock<int>(x =>
            {
                Console.WriteLine($"第一部分{x}行处理完成!");
               // Thread.Sleep(2000);
            });
            currentP1Block.LinkTo(finallyP1Block, new DataflowLinkOptions() { PropagateCompletion = true });

            var finallyP2Block = new ActionBlock<int>(x =>
            {
                Console.WriteLine($"第二部分{x}行处理完成!");
              //  Thread.Sleep(2000);
            });
            currentP2Block.LinkTo(finallyP2Block, new DataflowLinkOptions() { PropagateCompletion = true });

            return Task.WhenAll(new Task[] { finallyP1Block.Completion, finallyP2Block.Completion });
        }

        private void PrintState(ProgressState state)
        {
            //Task.Run(() =>
            //{
            //    lock (_lock)
            //    {
            //        Console.SetCursorPosition(0, state.Step);
            //        if (state.Row == -1)
            //        {
            //            Console.WriteLine($"第{state.Step}步执行失败:{state.ErrorMessage}!");
            //        }
            //        //else if (state.Row == -2)
            //        //{
            //        //    Console.WriteLine($"第{state.Step}步执行成功!");
            //        //}
            //    }
            //});
        }

        private IPropagatorBlock<int, int> CreateReadFileBlock(IPropagatorBlock<int, int> p2Block)
        {

            _startBlock = new ActionBlock<string>(x =>
                {
                    try
                    {
                        if (x == null)
                        {
                            PrintState(new ProgressState(1, -1) { ErrorMessage = "路径不允许为空!" });
                            return;
                        }

                        if (!File.Exists(x))
                        {
                            PrintState(new ProgressState(1, -1) { ErrorMessage = "文件不存在!" });
                            return;
                        }
                        XSSFWorkbook hssfwb;
                        using (FileStream file = new FileStream(x, FileMode.Open, FileAccess.Read))
                        {
                            hssfwb = new XSSFWorkbook(file);
                        }
                        ISheet sheet = hssfwb.GetSheetAt(0);
                        _totalRow = sheet.LastRowNum;
                        _processContext = new ProcessContext(sheet.LastRowNum + 1);
                        for (int row = 0; row <= sheet.LastRowNum; row++)
                        {
                            if (sheet.GetRow(row) != null) //null is when the row only contains empty cells 
                            {
                                if (!CheckSourceData(sheet.GetRow(row)))
                                {
                                    Console.WriteLine($"检查到第{row}行数据格式有误,请关闭此程序并检查导入数据或清空表格重新导入!");
                                    return;
                                }

                                for (int column = 0; column < StepLength.P1; column++)
                                {
                                    _processContext.SetP1Value(1, row, column, (int)sheet.GetRow(row).GetCell(column).NumericCellValue);
                                }
                                _processContext.SetP1StepState(1, row, true);
                                p2Block.Post(row);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }



                }, _executionDataflowBlockOptions);
            _startBlock.Completion.ContinueWith(x =>
            {
                PrintState(new ProgressState(1, -2));
                p2Block.Complete();
            });
            return p2Block;
        }

        private bool CheckSourceData(IRow row)
        {
            try
            {
                int countOfZero = 0;
                for (int j = 0; j < StepLength.P1; j++)
                {
                    var cellData = row.GetCell(j).ToString();
                    if (string.IsNullOrEmpty(cellData))
                        return false;
                    if (int.Parse(cellData) == 0)
                        countOfZero++;
                    if (countOfZero > 6)
                        return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private IPropagatorBlock<int, int> CreateP1Block(int step)
        {
            var progressBlock = new TransformBlock<int, int>(x =>
            {
                try
                {
                    var handler = (IP1Handler)Activator.CreateInstance(Type.GetType($"HeProject.S{step}Handler") ?? throw new InvalidOperationException());
                    var result = handler.Hnalder(x, _processContext);
                    if (result != null)
                        PrintState(new ProgressState(step, -1) { ErrorMessage = result });
                    _processContext.SetP1StepState(step, x, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return x;
            }, _executionDataflowBlockOptions);
            progressBlock.Completion.ContinueWith(t => { PrintState(new ProgressState(step, -2)); });
            return progressBlock;
        }

        private IPropagatorBlock<int, int> CreateP2Block(int step)
        {
            var progressBlock = new TransformBlock<int, int>(x =>
            {
                try
                {
                    var handler = (IP2Handler)Activator.CreateInstance(Type.GetType($"HeProject.ProgressHandler.P2.S{step}P2Handler") ?? throw new InvalidOperationException());
                    var result = handler.Hnalder(x, _processContext);
                    if (result != null)
                        PrintState(new ProgressState(step, -1) { ErrorMessage = result });
                    _processContext.SetP2StepState(step, x, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return x;
            }, _executionDataflowBlockOptions);
            progressBlock.Completion.ContinueWith(t => { PrintState(new ProgressState(step, -2)); });
            return progressBlock;
        }
    }
}