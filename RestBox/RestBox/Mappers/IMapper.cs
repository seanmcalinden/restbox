namespace RestBox.Mappers
{
    public interface IMapper<in TSource, in TDestination>
    {
        void Map(TSource source, TDestination destination);
    }
}