namespace StorageAccess.NHibernate
{
	using System.Linq;
	using global::NHibernate;
	using global::NHibernate.Linq;

	public class NHibernateStorage : IModifyStorage, IAmTransactional
	{
		private readonly ISession session;
		private ITransaction transaction;

		public NHibernateStorage(ISession session)
		{
			this.session = session;
		}

		public IQueryable<TItem> Items<TItem>() where TItem : class
		{
			return this.session.Linq<TItem>();
		}

		public void Add<TItem>(TItem item) where TItem : class
		{
			this.session.Save(item);
		}
		public void Update<TItem>(TItem item) where TItem : class
		{
			this.session.SaveOrUpdate(item);
		}
		public void Remove<TItem>(TItem item) where TItem : class
		{
			this.session.Delete(item);
		}

		public void BeginTransaction()
		{
			this.BeginTransaction(TransactionIsolation.Default);
		}
		public void BeginTransaction(TransactionIsolation level)
		{
			this.transaction = this.transaction ?? this.session.BeginTransaction(level.GetLevel());
		}
		public void CommitTransaction()
		{
			this.transaction.Commit();
			this.transaction.Dispose();
			this.transaction = null;
		}
		public void Dispose()
		{
			if (this.transaction != null)
				this.transaction.Dispose();

			this.transaction = null;
		}
	}
}