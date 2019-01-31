using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using SentinelData;

namespace SentinelApp
{
    class MessageHandler : IMessage
    {
        public string Message(string value)
        {
            MessageQueue.AddMessage(value);
            Console.WriteLine("message received : " + value);
            return value;
        }
    }
}
