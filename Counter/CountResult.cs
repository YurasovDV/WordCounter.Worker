using System;

namespace WordCounter.Worker
{
    public class CountResult
    {
        public Guid CorrelationId { get; set; }

        public int WordCount { get; set; }
    }
}
