using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitterStatisticBot.DLL;

namespace TwitterStatisticBot.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                /* Немного инфы:
                 * 
                 * Твиттер-аккаунт, с которым взаимодействуем:
                 * логин: test-tesovich@mail.ru,
                 * пароль: 7753191a
                 * 
                 * Можно постить с любого другого, но перед этим необходимо создать приложение на https://apps.twitter.com/
                 * и сформировать ключи и токены на вкладке Keys and Access Token, нужные ниже.
                 * 
                 * Так как максимальная длина одного твита 140 символов, статистика разбивается на несколько частей и твитится в несколько твитов.
                 */

                StatisticBot bot = new StatisticBot(
                    "lGNvlTq2tFULUcsD3uLjAcdI9",
                    "SgjNnKGpqYhVZ5ST55sBUcGJTiv7VfBKZlRG9LZrPtjVTLl1KG",
                    "900250846331629568-dud0QTDa1ARf0K1P7mWgK73upjCH3sl",
                    "WSZqC6EOsIFsttERkLLlBcbGGhLevImPlJyVTzMr6S3dR");

                Console.Write("Введите твиттер-аккаунт: ");
                string username = Console.ReadLine();

                while (!String.IsNullOrEmpty(username))
                {
                    try
                    {
                        Console.WriteLine($"Пытаемся получить статистику твитов пользователя {username}...");

                        string statistic = await bot.GetStatisticForTweets(username: username);
                        Console.WriteLine(statistic + "\n");
                        Console.WriteLine("Постим статистику в твиттер...");

                        var tweetsCount = await bot.PostStatistic(username, statistic);
                        Console.WriteLine($"Размещено {tweetsCount} твитов, так как максимальная длина твита ограничена 140 символами\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    Console.Write("Введите твиттер-аккаунт (оставьте пустым для выхода): ");
                    username = Console.ReadLine();
                }
            }).GetAwaiter().GetResult();
        }
    }
}
