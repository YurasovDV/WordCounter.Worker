namespace WordCounter.Worker.DAL
{
    public interface IWordCountersRepository
    {
        int Create(CountResultRow countResultRow);
        void WaitForDb();
    }
}