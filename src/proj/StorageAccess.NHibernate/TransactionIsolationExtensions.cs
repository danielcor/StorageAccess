namespace StorageAccess.NHibernate
{
	using System.Data;

	internal static class TransactionIsolationExtensions
	{
		public static IsolationLevel GetLevel(this TransactionIsolation level)
		{
			switch (level)
			{
				case TransactionIsolation.ReadUncommitted:
					return IsolationLevel.ReadUncommitted;

				case TransactionIsolation.RepeatableRead:
					return IsolationLevel.RepeatableRead;

				case TransactionIsolation.Serializable:
					return IsolationLevel.Serializable;

				case TransactionIsolation.Snapshot:
					return IsolationLevel.Snapshot;

				default:
					return IsolationLevel.ReadCommitted;
			}
		}
	}
}