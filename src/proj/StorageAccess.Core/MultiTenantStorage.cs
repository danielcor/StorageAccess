namespace StorageAccess.Core
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;

	public sealed class MultiTenantStorage : IUpdateStorage
	{
		private static readonly IDictionary<Type, PropertyInfo> Cache = new Dictionary<Type, PropertyInfo>();
		private readonly IUpdateStorage storage;
		private readonly Guid tenantId;
		private readonly string propertyName;

		public MultiTenantStorage(IUpdateStorage storage, Func<Guid> tenantId, string propertyName)
		{
			this.storage = storage;
			this.tenantId = tenantId;

			if (string.IsNullOrEmpty(this.propertyName))
				throw new ArgumentException("Argument cannot be a null or empty string.", "propertyName");

			this.propertyName = propertyName;
		}

		public void Dispose()
		{
			this.storage.Dispose();
			GC.SuppressFinalize(this);
		}

		public IQueryable<TItem> Items<TItem>() where TItem : class
		{
			var queryable = this.storage.Items<TItem>();

			var type = typeof(TItem);
			var property = this.GetProperty(typeof(TItem));
			if (property == null) // item doesn't have tenant identifier property
				return queryable;

			// TODO: cache this and make it more readable

			// http://msdn.microsoft.com/en-us/library/bb882637.aspx
			var parameter = Expression.Parameter(type, this.propertyName);
			var left = Expression.Property(parameter, property);
			var right = Expression.Constant(this.tenantId, typeof(Guid));
			var expression = Expression.Equal(left, right);

			var where = Expression.Call(
				typeof(Queryable),
				"Where",
				new[] { queryable.ElementType },
				expression,
				Expression.Lambda<Func<Guid, bool>>(expression, new[] { parameter }));

			return queryable.Provider.CreateQuery<TItem>(where);
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
			if (Cache.TryGetValue(type, out property))
				return property;

			lock (Cache)
			{
				if (Cache.TryGetValue(type, out property))
					return property;

				return Cache[type] = type.GetProperty(this.propertyName, BindingFlags.Instance | BindingFlags.Public);
			}
		}
	}
}