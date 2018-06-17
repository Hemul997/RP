﻿using System;
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
        private const string INPUT_EXCHANGE_NAME = "backend-api";
        private const string OUTPUT_EXCHANGE_NAME = "text-rank-tasks";
        private const string ROUTING_KEY = "text-rank-task";

        private static void SendIdToQueue(string id, IModel channel) {
            channel.ExchangeDeclare(OUTPUT_EXCHANGE_NAME, "direct");
            string message = "TextRankTask:" + id;
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: OUTPUT_EXCHANGE_NAME,
                                    routingKey: ROUTING_KEY,
                                    basicProperties: null,
                                    body: body); 
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
            channel.ExchangeDeclare(exchange: INPUT_EXCHANGE_NAME, type: "fanout");
            channel.QueueBind(queue: queueName,
                                exchange: INPUT_EXCHANGE_NAME,
                                routingKey: "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                string receivedMessage = Encoding.UTF8.GetString(body);
                var args = Regex.Split(receivedMessage, ":");
                if (args.Length == 2 && args[0].Equals("Text created")) {
                    string id = args[1];
                    SendIdToQueue(id, channel);
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