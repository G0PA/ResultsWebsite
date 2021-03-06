﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.TransportObject;

namespace RabbitMQ.RabbitMQ
{
    public static class RabbitMQMessageSender
    {
        static ConnectionFactory Factory;
        static IConnection Connection;
        static IModel Channel;
        static IBasicProperties Properties;
        static RabbitMQMessageSender()
        {
            Factory = new ConnectionFactory() { HostName = "localhost" };
            Connection = Factory.CreateConnection();
            Channel = Connection.CreateModel();
            Channel.QueueDeclare(queue: "task_queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            //var consumer = new EventingBasicConsumer(Channel);
            //consumer.Received += (model, ea) =>
            //{
            //    var body = ea.Body;
            //    var message = Encoding.UTF8.GetString(body);                
            //};
            //Channel.BasicConsume(queue: "task_queue",
            //                     autoAck: true,
            //                     consumer: consumer);
            Properties = Channel.CreateBasicProperties();
        }
        public static void Send(LiveEvent liveEvent)
        {         
            var body = ObjectToByteArray(liveEvent);
            Properties.Persistent = true;

            Channel.BasicPublish(exchange: "",
                                 routingKey: "task_queue",
                                 basicProperties: Properties,
                                 body: body);
        }
        private static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        
    }   
}
