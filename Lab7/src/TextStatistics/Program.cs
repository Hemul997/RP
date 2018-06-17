using System;
using StackExchange.Redis;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.RegularExpressions;

namespace TextStatistics
{
    class Program
    {
        private const string HOST_NAME = "localhost";
        private const string QUEUE_NAME = "text-rank-calc";
        private const string EXCHANGE_NAME = "text-rank-calc";
        private static void UpdateStatistics(int textCount, float avgRank, int highRankPart, float ranksSum)
        {            
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(HOST_NAME);
            string statistics = textCount + ":" + avgRank + ":" + highRankPart + ":" + ranksSum;
            Console.WriteLine(statistics);
            for(int i = 0; i < 16; i++)
            {
                IDatabase db = redis.GetDatabase(i);
                db.StringSet("statistics", statistics);
            }
        }



        // Инициализация из Redis
        private static void InitStartData(ref int textCount, ref int highRankPart, ref float avgRank, ref float ranksSum)
        {
 
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(HOST_NAME);                        
            IDatabase db = redis.GetDatabase();
            try
            {
                string msg = db.StringGet("statistics");
                var data = Regex.Split(msg, ":");

                textCount = int.Parse(data[0]);
                avgRank = float.Parse(data[1]);
                highRankPart = int.Parse(data[2]);            
                ranksSum = float.Parse(data[3]);
            }
            catch(Exception ex)
            {

            }
        }

        private static void RecalcStatistics(float receivedRank, ref int textCount, 
                                        ref int highRankPart, ref float avgRank, ref float ranksSum) 
        {
            textCount++;
            if(receivedRank > 0.5) 
            {
                highRankPart++;
            }
            ranksSum += receivedRank;
            avgRank = ranksSum / textCount;
        }
        private static void RabbitListener()
        {
            int textCount = 0;
            int highRankPart = 0;
            float avgRank = 0;
            float ranksSum = 0;

            InitStartData(ref textCount, ref highRankPart, ref avgRank, ref ranksSum);

            Console.WriteLine("TextStatistics: {0} {1} {2}", textCount, highRankPart, avgRank);

            var factory = new ConnectionFactory() { HostName = HOST_NAME };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: EXCHANGE_NAME, 
                                        type: "fanout");
                channel.QueueDeclare(queue: QUEUE_NAME,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.QueueBind(queue: QUEUE_NAME,
                                exchange: EXCHANGE_NAME,
                                routingKey: "");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);                    
                    
                    var msgArgs = Regex.Split(message, ":");                    
                    if(msgArgs.Length == 3 && msgArgs[0] == "TextRankCalculated")
                    {                                        
                        Console.WriteLine("Received: " + message);
                        float rank = float.Parse(msgArgs[2]);
                        RecalcStatistics(rank, ref textCount, ref highRankPart, ref avgRank, ref ranksSum);
                        UpdateStatistics(textCount, avgRank, highRankPart, ranksSum);
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