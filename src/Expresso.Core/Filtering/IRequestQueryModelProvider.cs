namespace Expresso.Core.Filtering
{
    public interface IRequestQueryModelProvider
    {
        QueryModel GetFilterModel(string context);
        QueryModel GetSortModel(string context);
    }
}
