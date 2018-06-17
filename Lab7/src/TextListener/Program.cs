using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using StackExchange.Redis;
using System.Text;
using System.Text.RegularExpressions;

namespace TextListener
{
    public class Program
    {
        private const string HOST_NAME = "localhost";
        private const string EXCHANGE_NAME = "backend-api";

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

        private static string GetValueById(string id)
        {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(HOST_NAME);
            int dbNum = CountHash(id);
            Console.WriteLine("Text has got from database#" + Convert.ToString(dbNum));
            IDatabase redisDB = redisChannel.GetDatabase(dbNum);
            string value = "";
            value = redisDB.StringGet(id);
            return value;
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
                    string receivedMessage = Encoding.UTF8.GetString(body);
                    var args = Regex.Split(receivedMessage, ":");
                    if (args.Length == 2 && args[0].Equals("Text created")) {
                        string id = args[1];
                        string text = GetValueById(id);
                        Console.WriteLine("Text created " + id + ":" + text);
                    }                 
                    
                };
                channel.BasicConsume(queue: queueName,
                                    autoAck: true,
                                    consumer: consumer);
                Console.ReadLine();
        }

        public static void Main(string[] args)
        {
            RabbitListener();
        }
    }
}