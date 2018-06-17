using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Threading;

namespace Backend.Controllers
{
    public class DataObject {
        public string data {get; set;}
    }

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private const string HOST_NAME = "localhost";
        private const string EXCHANGE_NAME = "backend-api"; 
        private static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(HOST_NAME);
        private static IDatabase redisDB = redisChannel.GetDatabase();


        private IActionResult GetRankFromDbById(string id)
        {
            int tryCount = 5;
            int sleepTime = 100;

            string value = null;
            for(int i = 0; i < tryCount; i++)
            {   
                value = redisDB.StringGet("TextRankGuid_" + id);
                if(value == null) 
                {
                    Thread.Sleep(sleepTime);
                } 
                else 
                {
                    break;
                }                
            }   

            IActionResult result = null;
            if(value != null)          
            {
                result = Ok(value);
            } 
            else 
            {
                result = new NotFoundResult();
            }            
            
            return result;
        }

        // GET api/values/<id>
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {           
            return GetRankFromDbById(id);
        }

        private static void AddIdToExchange(string id) 
        {
            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = HOST_NAME
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: EXCHANGE_NAME, type: "fanout");
            string message = "Text created:" + id;
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: EXCHANGE_NAME,
                        routingKey: "",
                        basicProperties: null,
                        body: body);
        }

        // POST api/values
        [HttpPost]
        public string Post(string value)
        {
            if (value == null) {
                return("Value is null");
            }
            var id = Guid.NewGuid().ToString();
            redisDB.StringSet(id, value);
            AddIdToExchange(id);
            return id;
        }
    }
}
