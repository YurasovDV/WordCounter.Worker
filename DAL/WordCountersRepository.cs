using Microsoft.Extensions.Logging;
using System;
using WordCounter.Common;

namespace WordCounter.Worker.DAL
{
    public class WordCountersRepository : IWordCountersRepository
    {
        private readonly Connector _connector;
        private readonly IEnvironmentFacade _environmentFacade;
        private readonly ILogger _logger;
        private readonly CountResultsContext _countResultsContext;

        public WordCountersRepository(ILogger<WordCountersRepository> logger, CountResultsContext countResultsContext, Connector connector, IEnvironmentFacade environmentFacade)
        {
            _logger = logger;
            _countResultsContext = countResultsContext;
            _connector = connector;
            _environmentFacade = environmentFacade;
        }

        public void WaitForDb()
        {
            Func<bool> ping = () =>
            {
                _countResultsContext.Database.EnsureCreated();
                return true;
            };

            _connector.EnsureIsUp(_logger, _environmentFacade.BuildDbSettings(), ping);
        }

        public int Create(CountResultRow countResultRow)
        {
            _countResultsContext.CountResults.Add(countResultRow);
            _countResultsContext.SaveChanges();
            return countResultRow.Id;
        }
    }
}
