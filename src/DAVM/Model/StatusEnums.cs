namespace DAVM.Model
{
    public enum ResourceStatus : int
    {
        Running,
        Off,
        Deallocated, //not paying (FREE)
        Error,
        Updating,
        Stopping,
        Starting,
        Unknown
    }
}