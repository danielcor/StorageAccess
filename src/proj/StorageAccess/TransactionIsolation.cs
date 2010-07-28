namespace StorageAccess
{
	public enum TransactionIsolation
	{
		Default,
		ReadUncommitted,
		ReadCommitted,
		RepeatableRead,
		Serializable,
		Snapshot
	}
}