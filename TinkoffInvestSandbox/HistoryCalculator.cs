using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi.V1;
using TinkoffInvestLib;

namespace TinkoffInvestSandbox
{
    /// <summary>
    /// Класс созданный для рассчета стратегий по истории
    /// </summary>
    static internal class HistoryCalculator
    {
        static public void FinamCalculator(string historyFilePath)  //Калькулятор работающий по файлам созданным на finam.ru
        {
            using (TextFieldParser parser = new TextFieldParser(historyFilePath))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(";");
                parser.ReadFields();
                int count = 0;
                Dictionary<int, decimal[]> dictionaryFields = new Dictionary<int, decimal[]>();
                while (!parser.EndOfData)
                {
                    //Processing row
                    string[] fields = parser.ReadFields();
                    decimal[] values = 
                    { 
                        Convert.ToDecimal(fields[2]),  //Open
                        Convert.ToDecimal(fields[3]),  //High
                        Convert.ToDecimal(fields[4]),  //Low
                        Convert.ToDecimal(fields[5]),  //Close
                    };
                    dictionaryFields.Add(count, values);
                    count++;
                }

                decimal bestPrice = 0;
                int bestPriceLength1 = 0;
                int bestPriceLength2 = 0;
                for (int length1 = 1; length1 <= 10; length1++)
                {
                    for (int length2 = 1; length2 <= 10; length2++)
                    {
                        int instrumentsCount = 0; //Если инструмент куплен, то 1, если в шорт, то -1, иначе 0
                        decimal lastPrice = dictionaryFields.First().Value[2];   //Последняя цена инструмента
                        decimal result = 0; //Сумма дохода/убытка
                        List<HistoricCandle> candlesList = new List<HistoricCandle>(); //Список свечей для CalculateHeikinAshi
                        for (int i = 0; i < dictionaryFields.Count; i++)
                        {
                            HistoricCandle candle = new HistoricCandle()
                            {
                                Open = dictionaryFields[i][0],
                                High = dictionaryFields[i][1],
                                Low = dictionaryFields[i][2],
                                Close = dictionaryFields[i][3],
                            };
                            candlesList.Add(candle);
                            if (TinkoffInvestSandboxBot.CalculateHeikinAshi(candlesList, length1, length2)) //Если купить                       
                            {
                                if (instrumentsCount == 1)
                                    result += candle.Open - lastPrice;     //Если инструмент куплен то рассчитываем доход
                                else if (instrumentsCount == 0)
                                    instrumentsCount = 1;           //Если инструмент не куплен то покупаем
                                else if (instrumentsCount == -1)
                                {
                                    result += lastPrice - candle.Open;     //Если инструмент куплен в шорт то рассчитываем доход и покупаем инструмент
                                    instrumentsCount = 1;
                                }
                                else throw new Exception();
                            }
                            else    //Если продать                                      
                            {
                                if (instrumentsCount == 1)
                                {
                                    result += candle.Open - lastPrice;     //Если инструмент куплен то рассчитываем доход продаем в шорт
                                    instrumentsCount = -1;
                                }
                                else if (instrumentsCount == 0)
                                    instrumentsCount = -1;           //Если инструмент не куплен то покупаем в шорт
                                else if (instrumentsCount == -1)
                                    result += lastPrice - candle.Open;     //Если инструмент куплен в шорт то рассчитываем доход
                                else throw new Exception();
                            }
                            lastPrice = candle.Open;
                        }
                        if (bestPrice < result)
                        { 
                            bestPrice = result;
                            bestPriceLength1 = length1;
                            bestPriceLength2 = length2;
                        }
                        Console.WriteLine($"{result} - {length1} и {length2}");
                    }
                }
                Console.WriteLine();
                Console.WriteLine($"Лучшие параметры: {bestPrice} - {bestPriceLength1} и {bestPriceLength2}");
            }
        }
    }
}
