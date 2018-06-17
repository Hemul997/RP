using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.RegularExpressions;

namespace TextProcessingLimiter
{
    class Program
    {
        private const string HOST_NAME = "localhost";
        private const string INPUT_EXCHANGE = "backend-api";
        private const string SUCCESS_EXCHANGE = "text-success-marker";
        private const string OUTPUT_EXCHANGE = "processing-limiter";

        private static void AddMessageToExchange(string id, string status, string exchange, IModel channel)
        {            
            channel.ExchangeDeclare(exchange, "direct");
            string message = "ProcessingAccepted:" + id + ":" + status;
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: exchange,
                                    routingKey: "",
                                    basicProperties: null,
                                    body: body);            
        }

        private static void RabbitListener() 
        {
            int maxRequestsCount = 2;

            var factory = new ConnectionFactory() { HostName = HOST_NAME };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {  
                channel.ExchangeDeclare(exchange: INPUT_EXCHANGE, type: "fanout");
                channel.ExchangeDeclare(exchange: SUCCESS_EXCHANGE, type: "fanout");

                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: queueName,
                                exchange: INPUT_EXCHANGE,
                                routingKey: "");

                var successQueue = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: successQueue,
                                exchange: SUCCESS_EXCHANGE,
                                routingKey: "");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);                    
                    
                    var msgArgs = Regex.Split(message, ":");

                    if(msgArgs.Length == 3 && msgArgs[0] == "TextSuccessMarked" && msgArgs[2] == "false")
                    {
                        Console.WriteLine("Sent message was not success marked. INC max count of requests ");
                        maxRequestsCount++;
                    }
                    
                    if(msgArgs.Length == 2 && msgArgs[0] == "Text created")
                    {
                        if( maxRequestsCount > 0)
                        {
                            maxRequestsCount--;
                            Console.WriteLine("Permission for text processing issued. DEC max count of requests");
                            Console.WriteLine("RECEIVED: " + message);
                            AddMessageToExchange(msgArgs[1], "true", OUTPUT_EXCHANGE, channel);
                        }
                        else
                        {
                            Console.WriteLine("Message limit");
                            AddMessageToExchange(msgArgs[1], "false", OUTPUT_EXCHANGE, channel);
                        }
                    } 
                    
                };
                channel.BasicConsume(queue: queueName,
                                    autoAck: true,
                                    consumer: consumer);
                channel.BasicConsume(queue: successQueue,
                                    autoAck: true,
                                    consumer: consumer);

                Console.WriteLine("Listening messages.");
                Console.ReadLine();
            }
        }

        static void Main(string[] args)
        {
            RabbitListener();
        }
    }
}
