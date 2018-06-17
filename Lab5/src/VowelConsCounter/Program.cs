using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using StackExchange.Redis;
using System.Text;
using System.Text.RegularExpressions;

namespace VowelConsCounter
{
    class Program
    {
        private static string REG_EN_VOWEL = @"[aeyuio]";
        private static string REG_EN_CONSONANT = @"[bcdfghjklmnpqrstvwxz]";
        private static string REG_RUS_VOWEL = @"[аеёиоуыэюя]";
        private static string REG_RUS_CONSONANT = @"[бвгджзклмнпрстфхцчшщ]";
        
        private const string HOST_NAME = "localhost";
        private const string INPUT_EXCHANGE_NAME = "text-rank-tasks";
        private const string OUTPUT_EXCHANGE_NAME = "vowel-cons-counter";
        private const string QUEUE_NAME = "count-task";
        private const string INPUT_ROUTING_KEY = "text-rank-task";
        private const string OUTPUT_ROUTING_KEY = "vowel-cons-task";

        private static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(HOST_NAME);
        private static IDatabase redisDB = redisChannel.GetDatabase();

        private static string GetValueById(string id)
        {
            string value = "";
            value = redisDB.StringGet(id);
            return value;
        }

        private static VowelConsCounted CountVowelCons(string id, string text)
        {
            float vowel = 0;
            float consonant = 0;

            vowel += Regex.Matches(text, REG_EN_VOWEL, RegexOptions.IgnoreCase).Count;
            vowel += Regex.Matches(text, REG_RUS_VOWEL, RegexOptions.IgnoreCase).Count;

            consonant += Regex.Matches(text, REG_EN_CONSONANT, RegexOptions.IgnoreCase).Count;
            consonant += Regex.Matches(text, REG_RUS_CONSONANT, RegexOptions.IgnoreCase).Count;

            return new VowelConsCounted(id, vowel.ToString(), consonant.ToString());
        }

        private static void SendDataToQueue(VowelConsCounted data, IModel channel)
        {        
            channel.ExchangeDeclare(OUTPUT_EXCHANGE_NAME, "direct");
            string message = "VowelConsCounted:" + data.Id + ":" + data.Vowels + ":" + data.Cons;
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: OUTPUT_EXCHANGE_NAME,
                                    routingKey: OUTPUT_ROUTING_KEY,
                                    basicProperties: null,
                                    body: body);            
        }        

        private static void RabbitListener()
        {
            var factory = new ConnectionFactory() { HostName = HOST_NAME };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: INPUT_EXCHANGE_NAME, 
                                        type: "direct");                
                channel.QueueDeclare(queue: QUEUE_NAME,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.QueueBind(queue: QUEUE_NAME,
                                exchange: INPUT_EXCHANGE_NAME,
                                routingKey: INPUT_ROUTING_KEY);
            
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var msgArgs = Regex.Split(message, ":");                    
                    if (msgArgs.Length == 2 && msgArgs[0] == "TextRankTask")
                    {                        
                        string id = msgArgs[1];
                        string text = GetValueById(id);
                        Console.WriteLine("ID: " + id + " text: " + text);
                        VowelConsCounted result = CountVowelCons(id, text);
                        SendDataToQueue(result, channel);
                    }

                };
                channel.BasicConsume(queue: QUEUE_NAME,
                                    autoAck: true,
                                    consumer: consumer);                
                Console.ReadLine();
            }
        }

        static void Main(string[] args)
        {
            RabbitListener();
        }
    }
}
