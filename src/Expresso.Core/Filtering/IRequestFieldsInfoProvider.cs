namespace Expresso.Core.Filtering
{
    public interface IRequestFieldsInfoProvider
    {
        (string, Type)[] GetValidFilterFields(string context);
        (string, Type)[] GetValidSortFields(string context);
    }
}
