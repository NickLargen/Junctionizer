namespace Junctionizer.Model
{
    public interface IMonitorsAccess
    {
        bool IsBeingAccessed { get; }
        bool IsBeingDeleted { get; }
    }
}
