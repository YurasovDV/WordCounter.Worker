using System;
using WordCounter.Common;
using WordCounter.Worker.DAL;

namespace WordCounter.Worker
{
    public class WordCounterProcessor : BaseProcessor<CountResultRow>
    {
        public override OperationResult<CountResultRow> Process(BusinessMessage msg)
        {
            if (msg == null)
            {
                throw new ArgumentNullException(nameof(msg));
            }
            var count = GetCount(msg.Content);
            return OperationResult<CountResultRow>.Success(new CountResultRow() { CorrelationId = msg.CorrelationId, WordCount = count });
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
