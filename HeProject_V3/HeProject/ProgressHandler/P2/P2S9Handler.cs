﻿using System.Linq;
using HeProject.Model;

namespace HeProject.ProgressHandler.P2
{
    public class P2S9Handler:IP2Handler
    {
        private static readonly int[] Ying = new[] { 1, 2, 5, 8, 9 };
        public string Handler(int stage, int row, ProcessContext context)
        {
            var source = context.GetP1RowResult(stage + 7, row).Select(u => (int)u.Value).ToArray();
            if (Ying.Contains(source[0]))
            {
                if (Ying.Contains(source[2]))
                {
                    context.SetP2Value(stage, 9, row, 0, true);
                }
                else
                {
                    context.SetP2Value(stage, 9, row, 1, true);
                }
            }
            else
            {
                if (Ying.Contains(source[2]))
                {
                    context.SetP2Value(stage, 9, row, 2, true);
                }
                else
                {
                    context.SetP2Value(stage, 9, row, 3, true);
                }
            }

            return null;
        }
    }
}
