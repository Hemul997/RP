using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using StackExchange.Redis;
using System.Text;
using System.Text.RegularExpressions;

namespace VowelConsRater
{
    class Program
    {
        private const string HOST_NAME = "localhost";
        private const string EXCHANGE_NAME = "vowel-cons-counter";
        private const string QUEUE_NAME = "rank-task";
        private const string ROUTING_KEY = "vowel-cons-task";

        private static int CountHash(String value) 
        {
            int hash = 0;

            for(int i = 0; i < value.Length; i++)
            {
                if(Char.IsLetter(value[i])) 
                {
                    hash++;
                }
            }

            return hash % 10;
        }
        private static void SetRankInDbById(string id, float value) 
        {
            int dbNum = CountHash(id);
            Console.WriteLine("Text has sent to database#" + (dbNum));
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(HOST_NAME);
            IDatabase db = redis.GetDatabase(dbNum);
                                    
            db.StringSet("TextRankGuid_" + id, value);            
        }

        private static float CalculateRank(string vowels, string cons)
        {
            float vowelsCount = float.Parse(vowels);
            float consCount = float.Parse(cons);
            float result = vowelsCount;
            if(consCount != 0)
            {
                result = vowelsCount / consCount;
            }
            return result;
        }

        private static void RabbitListener() {
            var factory = new ConnectionFactory() { HostName = HOST_NAME };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: EXCHANGE_NAME, 
                                        type: "direct");                
                channel.QueueDeclare(queue: QUEUE_NAME,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);


                channel.QueueBind(queue: QUEUE_NAME,
                                exchange: EXCHANGE_NAME,
                                routingKey: ROUTING_KEY);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);                    
                    var msgArgs = Regex.Split(message, ":");                    
                    if(msgArgs.Length == 4 && msgArgs[0] == "VowelConsCounted")
                    {                
                        VowelConsCounted data = new VowelConsCounted(msgArgs[1], msgArgs[2], msgArgs[3]);
                        float rank = CalculateRank(data.Vowels, data.Cons);
                        SetRankInDbById(data.Id, rank);
                        Console.WriteLine("ID: " + data.Id + " Rank: " + rank.ToString());
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
