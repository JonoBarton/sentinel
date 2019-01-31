using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentinelData
{
    public static class MessageQueue
    {
        static string fileName = @"c:\temp\mq.data";

        public static List<string> GetMessages()
        {
            var messages = File.Exists(fileName) ? File.ReadLines(fileName).ToList() : new List<string>();
            File.WriteAllText(fileName,"");
            return messages;
        }

        public static void AddMessage(string message)
        {
           File.AppendAllText(fileName,Environment.NewLine + message);
        }
    }
}
