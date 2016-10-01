﻿namespace Microsoft.Extensions.Caching.MongoDB
{
	using System;
	using System.Threading.Tasks;
	using Distributed;
	using global::MongoDB.Driver;
	using Options;

	public class MongoCache : IDistributedCache
	{
		private readonly ISystemClock _Clock;
		private readonly IMongoCollection<CacheEntry> _Collection;
		private readonly MongoCacheOptions _Options;

		// todo extension method to register services, should validate config?

		public MongoCache(ISystemClock clock, IOptions<MongoCacheOptions> optionsAccessor)
		{
			if (clock == null)
			{
				throw new ArgumentNullException(nameof(clock));
			}
			_Clock = clock;

			if (optionsAccessor == null)
			{
				throw new ArgumentNullException(nameof(optionsAccessor));
			}

			var options = optionsAccessor.Value;
			if (options.ConnectionString == null)
			{
				throw new ArgumentException("ConnectionString is missing", nameof(optionsAccessor));
			}

			var url = new MongoUrl(options.ConnectionString);
			if (url.DatabaseName == null)
			{
				throw new ArgumentException("ConnectionString requires a database name", nameof(optionsAccessor));
			}

			var client = new MongoClient(url);
			_Collection = client.GetDatabase(url.DatabaseName)
				.GetCollection<CacheEntry>(options.CollectionName);
			_Options = options;
		}

		public byte[] Get(string key)
		{
			return GetAndRefresh(key, _Options.WaitForRefreshOnGet);
		}

		/// <summary>
		///     note as ugly as it is, we need separate implementations of Sync & Async
		///     if we wrap either way async over sync, or sync over async, we take away decisions from consumers
		///     and take away flexibility
		///     MongoDB has separate APIs for sync & async so we're exposing that via these different implementations
		///     tests are parameterized so not a big deal, just need twice the impl
		/// </summary>
		/// <param name="key"></param>
		/// <param name="waitForRefresh">
		///     If false, refresh is fire and forget. If true, the value isn't returned until refresh
		///     completes
		/// </param>
		/// <returns></returns>
		private byte[] GetAndRefresh(string key, bool waitForRefresh)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			var entry = _Collection.Find(e => e.Key == key).FirstOrDefault();
			if (entry == null)
			{
				return null;
			}
			if (entry.IsExpired(_Clock))
			{
				TriggerCleanup(entry);
				return null;
			}
			entry.Refresh(_Clock);
			var updateLastAccesssed = Builders<CacheEntry>.Update
				.Set(e => e.LastAccessedAt, entry.LastAccessedAt);
			if (waitForRefresh)
			{
				_Collection.UpdateOne(e => e.Key == key, updateLastAccesssed);
			}
			else
			{
				_Collection.UpdateOneAsync(e => e.Key == key, updateLastAccesssed);
			}
			return entry.Value;
		}

		private void TriggerCleanup(CacheEntry entry)
		{
			// todo remove old entries
		}

		public async Task<byte[]> GetAsync(string key)
		{
			return await GetAndRefreshAsync(key, _Options.WaitForRefreshOnGet);
		}

		private async Task<byte[]> GetAndRefreshAsync(string key, bool waitForRefresh)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			var entry = await _Collection.Find(e => e.Key == key).FirstOrDefaultAsync();
			if (entry == null)
			{
				return null;
			}
			if (entry.IsExpired(_Clock))
			{
				TriggerCleanup(entry);
				return null;
			}
			entry.Refresh(_Clock);
			var updateLastAccesssed = Builders<CacheEntry>.Update
				.Set(e => e.LastAccessedAt, entry.LastAccessedAt);
			if (waitForRefresh)
			{
				await _Collection.UpdateOneAsync(e => e.Key == entry.Key, updateLastAccesssed);
			}
			else
			{
#pragma warning disable 4014
				_Collection.UpdateOneAsync(e => e.Key == entry.Key, updateLastAccesssed);
#pragma warning restore 4014
			}
			return entry.Value;
		}

		/// <summary>
		///     Referesh always waits for save to DB, use this if you want to be sure that the update on a refresh is persisted.
		/// </summary>
		/// <param name="key"></param>
		public void Refresh(string key) => GetAndRefresh(key, waitForRefresh: true);

		/// <summary>
		///     Refresh always awaits the save to DB, use this if you want to be sure that the update on a refresh is persisted.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public Task RefreshAsync(string key) => GetAndRefreshAsync(key, waitForRefresh: true);

		public void Remove(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			_Collection.DeleteOne(e => e.Key == key);
			// todo confirm?
		}

		public Task RemoveAsync(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			return _Collection.DeleteOneAsync(e => e.Key == key);
			// todo confirm?
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (value == null)
			{
				// todo call Remove?
				throw new ArgumentNullException(nameof(key));
			}
			options = options ?? new DistributedCacheEntryOptions();

			var entry = CacheEntry.Create(_Clock, key, value, options);
			if (entry.IsExpired(_Clock))
			{
				return;
			}

			_Collection.ReplaceOne(e => e.Key == key, entry, new UpdateOptions {IsUpsert = true});
		}

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}
			if (value == null)
			{
				// todo call Remove?
				throw new ArgumentNullException(nameof(key));
			}
			options = options ?? new DistributedCacheEntryOptions();

			var entry = CacheEntry.Create(_Clock, key, value, options);
			if (entry.IsExpired(_Clock))
			{
				return Task.FromResult(0);
			}
			return _Collection.ReplaceOneAsync(e => e.Key == key, entry, new UpdateOptions {IsUpsert = true});
		}
	}
}