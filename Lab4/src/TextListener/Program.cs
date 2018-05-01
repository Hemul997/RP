using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using StackExchange.Redis;
using System.Text;

namespace TextListener
{
    public class Program
    {
        private static string GetValueByKeyAndHostName(string id, string hostName)
        {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
            IDatabase database = redisChannel.GetDatabase();
            string value = "";
            value = database.StringGet(id);
            return value;
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
                    string text = GetValueByKeyAndHostName(id, hostName);
                    Console.WriteLine(text);
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