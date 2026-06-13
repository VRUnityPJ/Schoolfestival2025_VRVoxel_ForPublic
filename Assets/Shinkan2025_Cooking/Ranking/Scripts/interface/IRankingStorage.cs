namespace Shinkan2025_Cooking.Ranking.Scripts.@interface
{
    public interface IRankingStorage
    {
        public void UpdateData<T>(T data)
            where T : IRankingDataElement<T>;
    }
}
