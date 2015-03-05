namespace DAVM.Model
{
	public enum VMStatus : int
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