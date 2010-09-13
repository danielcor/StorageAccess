namespace StorageAccess.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;

	public sealed class MultiTenantStorage : IUpdateStorage
	{
		private const string DefaultPropertyName = "TenantId";
		private static readonly IDictionary<Type, PropertyInfo> PropertyCache =
			new Dictionary<Type, PropertyInfo>();
		private readonly IUpdateStorage storage;
		private readonly Guid tenantId;
		private readonly string propertyName;

		public MultiTenantStorage(IUpdateStorage storage, Guid tenantId, string propertyName)
		{
			this.storage = storage;
			this.tenantId = tenantId;
			this.propertyName = propertyName ?? DefaultPropertyName;
		}
		public void Dispose()
		{
			this.storage.Dispose();
			GC.SuppressFinalize(this);
		}

		public IQueryable<TItem> Items<TItem>() where TItem : class
		{
			var tenantExpression = this.FilterByTenant<TItem>();
			return this.storage.Items<TItem>().Where(tenantExpression);
		}
		private Expression<Func<TItem, bool>> FilterByTenant<TItem>() where TItem : class
		{
			var type = typeof(TItem);
			var property = this.GetProperty(type);
			if (property == null)
				return item => true; // item doesn't have TenantId identifier property

			// TODO: cache this
			// TODO: check out this: http://stefan.rusek.org/Posts/LINQ-Expressions-as-Fast-Reflection-Invoke/3/
			var parameter = Expression.Parameter(type, "item");
			var equals = Expression.Equal(
				Expression.Property(parameter, property),
				Expression.Constant(this.tenantId));

			return Expression.Lambda<Func<TItem, bool>>(equals, new[] { parameter });
		}

		public void Add<TItem>(TItem item) where TItem : class
		{
			this.storage.Add(this.AppendTenantId(item));
		}
		public void Remove<TItem>(TItem item) where TItem : class
		{
			this.storage.Remove(this.AppendTenantId(item));
		}
		public void Update<TItem>(TItem item) where TItem : class
		{
			this.storage.Update(this.AppendTenantId(item));
		}

		private TItem AppendTenantId<TItem>(TItem item) where TItem : class
		{
			if (item == null)
				return null;

			var property = this.GetProperty(item.GetType());
			if (property != null)
				property.SetValue(item, this.tenantId, null);

			return item;
		}
		private PropertyInfo GetProperty(Type type)
		{
			PropertyInfo property;
			if (PropertyCache.TryGetValue(type, out property))
				return property;

			lock (PropertyCache)
			{
				if (PropertyCache.TryGetValue(type, out property))
					return property;

				return PropertyCache[type] = type.GetProperty(this.propertyName, BindingFlags.Instance | BindingFlags.Public);
			}
		}
	}
}