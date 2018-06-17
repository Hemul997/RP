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
    public class RedisDBConnector
    {
        private string hostName;
        public RedisDBConnector(String hostName) {
            this.hostName = hostName;
        }
        private int CountDatabaseID(String value) 
        {
            int id = 0;

            for(int i = 0; i < value.Length; i++)
            {
                if(Char.IsLetter(value[i])) 
                {
                    id++;
                }
            }

            return id % 10;
        }

        public void SetData(String id, String prefix, String value) {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
            int databaseNumber = CountDatabaseID(id);
            Console.WriteLine("Text has sent to database#" + Convert.ToString(databaseNumber));
            IDatabase redisDB = redisChannel.GetDatabase(databaseNumber);
            redisDB.StringSet(id, value);
        }
        public String GetData(String id, String prefix) {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
            int databaseNumber = CountDatabaseID(id);
            Console.WriteLine("Text has got from database#" + Convert.ToString(databaseNumber));
            IDatabase redisDB = redisChannel.GetDatabase(databaseNumber);
            String value = "";
            value = redisDB.StringGet(prefix + id);
            return value;
        }
    }
}