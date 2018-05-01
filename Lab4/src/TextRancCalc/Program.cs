using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using StackExchange.Redis;
using System.Text;
using System.Text.RegularExpressions;

namespace TextRankCalc
{
    class Program
    {
        private static string REG_EN_VOWEL = @"[aeyuio]";
        private static string REG_EN_CONSONANT = @"[bcdfghjklmnpqrstvwxz]";
        private static string REG_RUS_VOWEL = @"[аеёиоуыэюя]";
        private static string REG_RUS_CONSONANT = @"[бвгджзклмнпрстфхцчшщ]";

        private static string GetValueByKeyAndHostName(string id, string hostName)
        {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
            IDatabase database = redisChannel.GetDatabase();
            string value = "";
            value = database.StringGet(id);
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

        private static void SetTextRankById(string id, float textRank, string hostName)
        {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
            IDatabase database = redisChannel.GetDatabase();
            database.StringSet(id, textRank);
        }

        private static void RabbitListener()
        {
            string hostName = "localhost";
            string exchangeName = "backend-api";

            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = hostName
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
            string queueName = channel.QueueDeclare().QueueName;
            channel.ExchangeDeclare(exchange: exchangeName, type: "fanout");
            channel.QueueBind(queue: queueName,
                                exchange: exchangeName,
                                routingKey: "");

             var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    string id = Encoding.UTF8.GetString(body);                    
                    string value = GetValueByKeyAndHostName(id, hostName);

                    float textRank = TextRankCalc(value);

                    SetTextRankById(id, textRank, hostName);

                    string rank = GetValueByKeyAndHostName(id, hostName);

                    Console.WriteLine(id + " : " + rank);
                };
                channel.BasicConsume(queue: queueName,
                                    autoAck: true,
                                    consumer: consumer);
                Console.ReadLine();
        }

        static void Main(string[] args)
        {
            RabbitListener();
        }
    }
}
