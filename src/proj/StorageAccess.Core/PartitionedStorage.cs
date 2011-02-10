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

		private const string DefaultTenantKeyName = "TenantId";
		private readonly IUpdateStorage storage;
		private readonly Guid tenantId;
		private readonly string tenantKeyName;

		public PartitionedStorage(IUpdateStorage storage, Guid tenantId)
			: this(storage, tenantId, null)
		{
		}
		public PartitionedStorage(IUpdateStorage storage, Guid tenantId, string tenantKeyName)
		{
			this.storage = storage;
			this.tenantId = tenantId;
			this.tenantKeyName = tenantKeyName ?? DefaultTenantKeyName;
		}
		public void Dispose()
		{
			this.storage.Dispose();
			GC.SuppressFinalize(this);
		}

		public IQueryable<TItem> Items<TItem>() where TItem : class
		{
			var items = this.storage.Items<TItem>();

			items = items.Where(this.FilterByPartition<TItem>());

			return items;
		}
		private Expression<Func<TItem, bool>> FilterByPartition<TItem>() where TItem : class
		{
			var type = typeof(TItem);
			var property = GetProperty(type, this.tenantKeyName);
			if (property == null)
				return item => true; // item doesn't have the partition property

			// TODO: cache; also take a look at: http://stefan.rusek.org/Posts/LINQ-Expressions-as-Fast-Reflection-Invoke/3/
			var parameter = Expression.Parameter(type, "item");
			var equals = Expression.Equal(
				Expression.Property(parameter, property),
				Expression.Constant(this.tenantId));

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
			var property = GetProperty(itemType, this.tenantKeyName);
			if (property != null)
				property.SetValue(item, this.tenantId, null);

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