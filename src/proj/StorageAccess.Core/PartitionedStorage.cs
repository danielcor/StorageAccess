namespace StorageAccess.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;

	public sealed class PartitionedStorage : IUpdateStorage
	{
		private static readonly IDictionary<IReflect, IDictionary<string, PropertyInfo>> Cache =
			new Dictionary<IReflect, IDictionary<string, PropertyInfo>>();

		private const string TenantPartition = "TenantId";
		private const string AccountPartition = "AccountId";
		private readonly IUpdateStorage storage;
		private readonly IEnumerable<KeyValuePair<string, Guid>> partitions;

		public PartitionedStorage(IUpdateStorage storage, Guid tenantId, Guid accountId)
			: this(storage, GetPartitions(tenantId, accountId))
		{
		}
		public PartitionedStorage(IUpdateStorage storage, IEnumerable<KeyValuePair<string, Guid>> partitions)
		{
			this.storage = storage;
			this.partitions = partitions;
		}
		public void Dispose()
		{
			this.storage.Dispose();
			GC.SuppressFinalize(this);
		}
		private static IEnumerable<KeyValuePair<string, Guid>> GetPartitions(Guid tenantId, Guid accountId)
		{
			yield return new KeyValuePair<string, Guid>(TenantPartition, tenantId);
			yield return new KeyValuePair<string, Guid>(AccountPartition, accountId);
		}

		public IQueryable<TItem> Items<TItem>() where TItem : class
		{
			var items = this.storage.Items<TItem>();

			foreach (var partition in this.partitions)
			{
				var partitionExpression = FilterByPartition<TItem>(partition);
				items = items.Where(partitionExpression);
			}

			return items;
		}
		private static Expression<Func<TItem, bool>> FilterByPartition<TItem>(
			KeyValuePair<string, Guid> partition) where TItem : class
		{
			var type = typeof(TItem);
			var property = GetProperty(type, partition.Key);
			if (property == null)
				return item => true; // item doesn't have the partition property

			// TODO: cache; also take a look at: http://stefan.rusek.org/Posts/LINQ-Expressions-as-Fast-Reflection-Invoke/3/
			var parameter = Expression.Parameter(type, "item");
			var equals = Expression.Equal(
				Expression.Property(parameter, property),
				Expression.Constant(partition.Value));

			return Expression.Lambda<Func<TItem, bool>>(equals, new[] { parameter });
		}

		public void Add<TItem>(TItem item) where TItem : class
		{
			this.storage.Add(this.AppendPartitions(item));
		}
		public void Remove<TItem>(TItem item) where TItem : class
		{
			this.storage.Remove(this.AppendPartitions(item));
		}
		public void Update<TItem>(TItem item) where TItem : class
		{
			this.storage.Update(this.AppendPartitions(item));
		}

		private TItem AppendPartitions<TItem>(TItem item) where TItem : class
		{
			if (item == null)
				return null;

			var itemType = item.GetType();
			foreach (var partition in this.partitions)
			{
				var property = GetProperty(itemType, partition.Key);
				if (property != null)
					property.SetValue(item, partition.Value, null);
			}

			return item;
		}
		private static PropertyInfo GetProperty(IReflect type, string propertyName)
		{
			IDictionary<string, PropertyInfo> properties;
			if (!Cache.TryGetValue(type, out properties))
				lock (Cache)
					if (!Cache.TryGetValue(type, out properties))
						Cache[type] = properties = new Dictionary<string, PropertyInfo>();

			PropertyInfo propertyInfo;
			if (!properties.TryGetValue(propertyName, out propertyInfo))
				lock (properties)
					if (!properties.TryGetValue(propertyName, out propertyInfo))
						properties[propertyName] = propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			return propertyInfo;
		}
	}
}