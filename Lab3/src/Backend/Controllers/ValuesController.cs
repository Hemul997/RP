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

namespace Backend.Controllers
{
    
    public class DataObject {
        public string data {get; set;}
    }

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        const string hostName = "localhost";
        const string exchangeName = "backend-api"; 
        //static readonly ConcurrentDictionary<string, string> _data = new ConcurrentDictionary<string, string>();
        private static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
        IDatabase database = redisChannel.GetDatabase();


        // GET api/values/<id>
        [HttpGet("{id}")]
        public string Get(string id)
        {
            string value = null;
            //_data.TryGetValue(id, out value);
            database.StringGet(id);
            return value;
        }

        private static void AddIdToExchange(string id) 
        {
            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = hostName
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
            channel.QueueDeclare(exchangeName, false, false, false, null);

            string message = id;
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                        routingKey: exchangeName,
                        basicProperties: null,
                        body: body);
        }

        // POST api/values
        [HttpPost]
        public string Post(string value)
        {
            if (value != null) {
                Console.WriteLine(value);
            }
            var id = Guid.NewGuid().ToString();
            database.StringSet(id, value);
            AddIdToExchange(id);
            //_data[id] = value;
            return id;
        }
    }
}
