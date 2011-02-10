namespace StorageAccess.NHibernate
{
	using System.Data;
	using global::NHibernate;

	internal static class SessionExtensions
	{
		public static ITransaction BeginTransaction(this ISession session, TransactionIsolation level)
		{
			return session.BeginTransaction(GetIsolationLevel(level));
		}
		private static IsolationLevel GetIsolationLevel(TransactionIsolation level)
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