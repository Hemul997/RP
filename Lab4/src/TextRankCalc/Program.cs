using System;
using System.Text;
using System.Text.RegularExpressions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using StackExchange.Redis;


namespace TextRankCalc
{
    class Program
    {
        private static string REG_EN_VOWEL = @"[aeyuio]";
        private static string REG_EN_CONSONANT = @"[bcdfghjklmnpqrstvwxz]";
        private static string REG_RUS_VOWEL = @"[аеёиоуыэюя]";
        private static string REG_RUS_CONSONANT = @"[бвгджзклмнпрстфхцчшщ]";

        private const string HOST_NAME = "localhost";
        private const string EXCHANGE_NAME = "backend-api";

        private static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(HOST_NAME);
        private static IDatabase redisDB = redisChannel.GetDatabase();
        
        private static string GetValueById(string id)
        {
            string value = "";
            value = redisDB.StringGet(id);
            return value;
        }


        private static float TextRankCalc(string text)
        {
            float vowel = 0;
            float consonant = 0;

            vowel += Regex.Matches(text, REG_EN_VOWEL, RegexOptions.IgnoreCase).Count;
            vowel += Regex.Matches(text, REG_RUS_VOWEL, RegexOptions.IgnoreCase).Count;

            consonant += Regex.Matches(text, REG_EN_CONSONANT, RegexOptions.IgnoreCase).Count;
            consonant += Regex.Matches(text, REG_RUS_CONSONANT, RegexOptions.IgnoreCase).Count;

            if (consonant == 0)
            {
                return vowel;
            }

            return vowel / consonant;
        }

        private static void SetTextRankById(string id, float textRank)
        {
            redisDB.StringSet("TextRankGuid_" + id, textRank);
        }

        private static void RabbitListener()
        {
            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = HOST_NAME
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
            string queueName = channel.QueueDeclare().QueueName;
            channel.ExchangeDeclare(exchange: EXCHANGE_NAME, type: "fanout");
            channel.QueueBind(queue: queueName,
                                exchange: EXCHANGE_NAME,
                                routingKey: "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                
                string message = Encoding.UTF8.GetString(body); 
                string id = Regex.Split(message, ":")[1];                   
                string value = GetValueById(id);

                float textRank = TextRankCalc(value);

                SetTextRankById(id, textRank);

                string rank = GetValueById("TextRankGuid_" + id);

                Console.WriteLine(id + " : " + rank);
            };
            channel.BasicConsume(queue: queueName,
                                autoAck: true,
                                consumer: consumer);
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("TextRankCalc");
            RabbitListener();
        }
    }
}
