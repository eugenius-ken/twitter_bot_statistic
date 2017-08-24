using LinqToTwitter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TwitterStatisticBot.DLL
{
    public class StatisticBot
    {
        private TwitterContext twc;

        /// <summary>
        /// Необходимо создать приложение на apps.twitter.com и сформировать необходимые ключи и токены на вкладке Keys and Access Tokens
        /// </summary>
        /// <param name="consumerKey"></param>
        /// <param name="consumerSecret"></param>
        /// <param name="token"></param>
        /// <param name="tokenSecret"></param>
        public StatisticBot(string consumerKey, string consumerSecret, string token, string tokenSecret)
        {
            var auth = new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore()
                {
                    ConsumerKey = consumerKey,
                    ConsumerSecret = consumerSecret,
                    OAuthToken = token,
                    OAuthTokenSecret = tokenSecret
                }
            };

            twc = new TwitterContext(auth);
        }

        /// <summary>
        /// Рассчитывает частотность букв и цифр твитов
        /// </summary>
        /// <param name="username">твиттер-аккаунт, с которого необходимо брать твиты</param>
        /// <param name="tweetsCount">количество твитов</param>
        /// <param name="caseSensitive">учитывать ли регистр</param>
        /// <param name="withDigits">нужна ли статистика по цифрам</param>
        /// <returns>возвращает json-объект в строке</returns>
        public async Task<String> GetStatisticForTweets(string username, int tweetsCount = 5, bool caseSensitive = false, bool withDigits = false)
        {
            string result = String.Empty;
            List<String> tweets = new List<String>();
            try
            {
                tweets =
                await
                (from tweet in twc.Status
                 where tweet.Type == StatusType.User &&
                       tweet.ScreenName == username &&
                       tweet.Count == 5
                 select tweet.Text)
                .ToListAsync();
            }
            catch(TwitterQueryException ex)
            {
                throw new Exception(ex.Message);
            }
            

            tweets = ClearTweets(tweets);

            return GetStatistic(tweets, caseSensitive, withDigits);
        }

        /// <summary>
        /// Делает твиты со статистикой. Так как максимальная длина твита - 140 символов, разбивает статистику по частям.
        /// </summary>
        /// <param name="username">твиттер-аккаунт, с которого брали твиты</param>
        /// <param name="statistic">json-объект в строке со статистикой</param>
        /// <returns>возвращает количество сделанных твитов</returns>
        public async Task<Int32> PostStatistic(string username, string statistic)
        {
            if (!username.StartsWith("@"))
                username = "@" + username;

            //divide statistic to parts because maximum tweet length is 140
            var statisticDictionary = JsonConvert.DeserializeObject<Dictionary<String, String>>(statistic);

            int takeCount = 7;
            int skipCount = 0;
            int iterateCount = statisticDictionary.Count / takeCount + 1;

            for (int i = 0; i < iterateCount; i++)
            {
                var statisticPortion = statisticDictionary.Skip(skipCount).Take(takeCount).ToDictionary(kv => kv.Key, kv => kv.Value);
                string json = JsonConvert.SerializeObject(statisticPortion);
                string message = $"{username}, статистика последних 5 твитов:\n {json}";

                try
                {
                    await twc.TweetAsync(message);
                }
                catch (TwitterQueryException ex)
                {
                    switch (ex.ErrorCode)
                    {
                        case 187: throw new Exception("Такой твит уже был. Скорее всего вы уже постили статистику для данного аккаунта, твиттер не дает постить одно и то же :(");
                        case 261: throw new Exception("Из-за большой частоты твитов, твиттер ограничил права на постинг :(");
                        case 226: throw new Exception("Твиттер заподозрил, что твиты делает не человек и на время запретил твитить. Попробуте позже :)");
                        case 326: throw new Exception("Из-за большой частоты твитов, твиттер ограничил права на постинг :(");
                        default: throw new Exception(ex.Message);
                    }
                }

                skipCount += takeCount;
            }

            return iterateCount;
        }

        private List<String> ClearTweets(IList<String> tweets)
        {
            var tweetsResult = new List<String>(tweets.Count);

            foreach (var tweet in tweets)
            {
                string result = tweet;
                int startIndex = tweet.IndexOf("https://");

                //remove urls
                while (startIndex != -1)
                {
                    int finishIndex = result.IndexOf(' ', startIndex);
                    result = result.Substring(0, startIndex);

                    if (finishIndex != -1)
                        result += tweet.Substring(finishIndex + 1, tweet.Length - finishIndex - 1);

                    startIndex = result.IndexOf("https://");
                }

                //remove useless symbols
                result = Regex.Replace(result, @"\t|\n|\r", "");
                //result = Regex.Replace(result, @"[^a-zA-Zа-яА-Я0-9]", ""); //не знаю надо ли убирать запятые, точки, пробелы и прочее

                tweetsResult.Add(result);
            }

            return tweetsResult;
        }

        private string GetStatistic(IEnumerable<String> tweets, bool caseSensitive, bool withDigits)
        {
            const string charsNonCaseSensitive = "абвгдеёжзийклмнопрсуфхцчшщъыьэюя";
            const string charsCaseSensitive = "аАбБвВгГдДеЕёЁжЖзЗиИйЙкКлЛмМнНоОпПрРсСуУфФхХцЦчЧшШщЩъЪыЫьЬэЭюЮяЯ";

            string chars = caseSensitive ? charsCaseSensitive : charsNonCaseSensitive;
            if (withDigits) chars += "0123456789";

            string summaryText = String.Empty;
            foreach (var tweet in tweets)
            {
                summaryText += tweet;
            }

            if (!caseSensitive)
                summaryText = summaryText.ToLower();

            JObject jsonObj = new JObject();
            foreach (char ch in chars)
            {
                var count = 0;
                foreach (char item in summaryText)
                {
                    if (item == ch)
                        count++;
                }

                jsonObj[ch.ToString()] = Math.Round((double)count / (double)summaryText.Length, 3);

            }

            return jsonObj.ToString();
        }


    }
}
