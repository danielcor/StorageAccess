namespace StorageAccess
{
	using System;

	public interface IAmTransactional : IDisposable
	{
		void BeginTransaction();
		void BeginTransaction(TransactionIsolation level);
		void CommitTransaction();
	}
}