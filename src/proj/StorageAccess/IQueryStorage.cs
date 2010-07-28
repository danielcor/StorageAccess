namespace StorageAccess
{
	using System.Linq;

	public interface IQueryStorage
	{
		IQueryable<TItem> Items<TItem>() where TItem : class;
	}
}