using System;
using WordCounter.Common;

namespace WordCountWorker
{
    public class Processor
    {
        public void Process(BusinessMessage msg)
        {
            Console.WriteLine($"processor got: {msg.CorrelationId} with content'{msg.Content}'");
        }
    }
}
