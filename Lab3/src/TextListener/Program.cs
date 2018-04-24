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

        private static IModel GetRabbitChannel(string hostName, string queueName)
        {
            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = hostName
            };
            IConnection connection = factory.CreateConnection();
            IModel model = connection.CreateModel();
            model.QueueDeclare(queueName, false, false, false, null);
            return model;
        }

        private static void RabbitListener()
        {
            string hostName = "localhost";
            string exchangeName = "backend-api";
            IModel channel = GetRabbitChannel(hostName, exchangeName);
            var subscription = new Subscription(channel, exchangeName, false);
            while (true)
            {
                BasicDeliverEventArgs basicDeliveryEventArgs = subscription.Next();
                string msg = Encoding.UTF8.GetString(basicDeliveryEventArgs.Body);
                string value = GetValueByKeyAndHostName(msg, hostName);
                Console.WriteLine(msg + " : " + value);
                subscription.Ack(basicDeliveryEventArgs);
            }

            if (channel != null)
            {
                channel.Close();
            }
        }

        public static void Main(string[] args)
        {
            RabbitListener();
        }
    }
}