using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.RegularExpressions;

namespace TextSuccessMarker
{
    class Program
    {
        const string INPUT_EXCHANGE = "text-rank-calc";
        const string OUTPUT_EXCHANGE = "text-success-marker";
        const float MIN_SUCCESS_VALUE = 0.5f;
        
        private static void AddMessageToExchange(string id, string status, string exchange, IModel channel)
        {            
            channel.ExchangeDeclare(exchange, "fanout");
            string message = "TextSuccessMarked:" + id + ":" + status;
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: exchange,
                                    routingKey: "",
                                    basicProperties: null,
                                    body: body);            
        }
       
        private static void RabbitListener() 
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using(var connection = factory.CreateConnection())
            using(var channel = connection.CreateModel())
            {  
                channel.ExchangeDeclare(exchange: INPUT_EXCHANGE, type: "fanout");

                var queueName = channel.QueueDeclare().QueueName;
                channel.QueueBind(queue: queueName,
                                exchange: INPUT_EXCHANGE,
                                routingKey: "");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);                    

                    var msgArgs = Regex.Split(message, ":");                    
                    if (msgArgs.Length == 3 && msgArgs[0] == "TextRankCalculated")
                    {
                        Console.WriteLine("RECEIVED: " + message);
                        string id = msgArgs[1];
                        float rank = float.Parse(msgArgs[2]);                        
                        if (rank > MIN_SUCCESS_VALUE)
                        {
                            AddMessageToExchange(msgArgs[1], "true", OUTPUT_EXCHANGE, channel);
                        } 
                        else
                        {
                            AddMessageToExchange(msgArgs[1], "false", OUTPUT_EXCHANGE, channel);
                        }
                    } 
                };
                channel.BasicConsume(queue: queueName,
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
