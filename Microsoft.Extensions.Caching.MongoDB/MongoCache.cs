﻿namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using System.Threading.Tasks;
	using Distributed;
	using global::MongoDB.Driver;
	using Options;

	public class MongoCache : IDistributedCache
	{
		private IMongoDatabase _Database;

		public MongoCache(IOptions<MongoCacheOptions> optionsAccessor)
		{
			if (optionsAccessor == null)
			{
				throw new ArgumentNullException(nameof(optionsAccessor));
			}

			if (optionsAccessor.Value.ConnectionString == null)
			{
				throw new ArgumentException("ConnectionString is missing", nameof(optionsAccessor));
			}

			var url = new MongoUrl(optionsAccessor.Value.ConnectionString);
			if (url.DatabaseName == null)
			{
				throw new ArgumentException("ConnectionString requires a database name", nameof(optionsAccessor));
			}

			var client = new MongoClient(url);
			_Database = client.GetDatabase(url.DatabaseName);
		}

		public byte[] Get(string key)
		{
			throw new NotImplementedException();
		}

		public Task<byte[]> GetAsync(string key)
		{
			throw new NotImplementedException();
		}

		public void Refresh(string key)
		{
			throw new NotImplementedException();
		}

		public Task RefreshAsync(string key)
		{
			throw new NotImplementedException();
		}

		public void Remove(string key)
		{
			throw new NotImplementedException();
		}

		public Task RemoveAsync(string key)
		{
			throw new NotImplementedException();
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			throw new NotImplementedException();
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			throw new NotImplementedException();
		}
	}
}