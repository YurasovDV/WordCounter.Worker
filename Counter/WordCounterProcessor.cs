using System;
using WordCounter.Common;

namespace WordCounter.Worker
{
    public class WordCounterProcessor : BaseProcessor<CountResult>
    {
        public WordCounterProcessor()
        {

        }

        public override OperationResult<CountResult> Process(BusinessMessage msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }
            var count = GetCount(msg.Content);
            return OperationResult<CountResult>.Success(new CountResult() { CorrelationId = msg.CorrelationId, WordCount = count });
        }

        private int GetCount(string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return 0;
            }

            return content.Split(new[] { ' ' }).Length;
        }
    }
}
