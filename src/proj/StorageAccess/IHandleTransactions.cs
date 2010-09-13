namespace StorageAccess
{
	using System;

	public interface IHandleTransactions : IDisposable
	{
		void BeginTransaction();
		void BeginTransaction(TransactionIsolation level);
		void CommitTransaction();
	}
}