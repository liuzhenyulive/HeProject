﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HeProject.Model;

namespace HeProject.ProgressHandler.P4
{
    public class P4MutilValueOrderHandle
    {
        private int _step;
        private int _offset;
        private int _columnWidth = 6;
        public string GetOrder(int step, int row, ProcessContext context, int offset = 1)
        {
            try
            {
                _step = step;
                _offset = offset;
                List<int> order = new List<int>();
                bool[] handled = new bool[_columnWidth];
                if (row == 0)
                {
                    for (int i = 0; i < _columnWidth; i++)
                    {
                        if (handled[i])
                            continue;
                        order.Add(i);
                    }
                }
                else
                {
                    Handle(row, context, order, handled);
                }

                var beforePaiXu = context.GetP4RowResult(step - offset, row);
                if (beforePaiXu == null || beforePaiXu.Count == 0)
                    return null;

                var indexs = beforePaiXu.Where(u => (bool)u.Value).Select(u => u.Key).ToArray();
                for (int i = 0; i < _columnWidth; i++)
                {
                    if (indexs.Contains(order[i]))
                    {
                        context.SetP4Value(step, row, i, true);
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void Handle(int row, ProcessContext context, List<int> order, bool[] handled, int[] columns = null)
        {
            var orderKeyValuePair = GetOrder(row, context, columns);
            if (orderKeyValuePair.Count == 0)
            {
                foreach (var column in columns)
                {
                    if (handled[column])
                        continue;
                    order.Add(column);
                    handled[column] = true;
                }
                return;
            }

            foreach (var valuePair in orderKeyValuePair)
            {
                if (handled[valuePair.Key])
                    continue;

                var sameCondition = orderKeyValuePair.Where(u => u.Value.Key == valuePair.Value.Key)
                    .Where(u => u.Value.Value == valuePair.Value.Value).ToArray();
                if (sameCondition.Length > 1)
                {
                    if (row < 1)
                        foreach (var column in columns)
                        {
                            if (handled[column])
                                continue;
                            order.Add(column);
                            handled[column] = true;
                        }
                    Handle(row - 1, context, order, handled, sameCondition.Select(u => u.Key).ToArray());
                }

                if (handled[valuePair.Key])
                    continue;
                order.Add(valuePair.Key);
                handled[valuePair.Key] = true;
            }
        }

        private List<KeyValuePair<int, KeyValuePair<int, int>>> GetOrder(int row, ProcessContext context, int[] columns = null)
        {
            try
            {
                Dictionary<int, KeyValuePair<int, int>> distance = new Dictionary<int, KeyValuePair<int, int>>();
                for (int i = 0; i < _columnWidth; i++)
                {
                    if (columns != null && !columns.Contains(i))
                        continue;

                    int distanceLength = 0;
                    int valueLength = 0;
                    bool inValue = false;
                    for (int j = row - 1; j >= 0; j--)
                    {
                        if (inValue)
                        {
                            if (context.GetP4Value<bool>(_step - _offset, j, i))
                                valueLength++;
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (context.GetP4Value<bool>(_step - _offset, j, i))
                                inValue = true;
                            else
                            {
                                distanceLength++;
                            }
                        }

                        if (j == 0)
                            distanceLength = row - 1;

                    }

                    distance.Add(i, new KeyValuePair<int, int>(distanceLength, valueLength));
                }

                return distance.OrderBy(u => u.Value.Key).ThenByDescending(u => u.Value.Value).ThenBy(u => u.Key)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
