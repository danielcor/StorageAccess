namespace StorageAccess
{
	using System;

	public interface IModifyStorage : IQueryStorage, IDisposable
	{
		void Add<TItem>(TItem item) where TItem : class;
		void Remove<TItem>(TItem item) where TItem : class;
		void Update<TItem>(TItem item) where TItem : class;
	}
}