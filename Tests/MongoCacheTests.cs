﻿namespace Tests
{
	using System.Threading.Tasks;
	using Microsoft.Extensions.Caching.Distributed;
	using Microsoft.Extensions.Caching.MongoDB;
	using MongoDB.Driver;
	using NUnit.Framework;
	using static NUnit.Framework.AssertionHelper;

	public class MongoCacheTests
	{
		private const string TestConnectionString = "mongodb://localhost/caching-tests";

		[SetUp]
		public void BeforeEachTest()
		{
			var client = new MongoClient();
			client.DropDatabase(new MongoUrl(TestConnectionString).DatabaseName);
		}

		[Test]
		public async Task GetAndGetAsync_NoCachedValues_ReturnsNull()
		{
			var cache = CreateMongoCache();

			var value = cache.Get("key");
			Expect(value, Is.Null, "Get");

			var asyncValue = await cache.GetAsync("key");
			Expect(asyncValue, Is.Null, "GetAsync");
		}

		private static MongoCache CreateMongoCache()
		{
			var options = new MongoCacheOptions
			{
				ConnectionString = TestConnectionString,
				CollectionName = "cache"
			};
			return new MongoCache(options);
		}

		[Test]
		public async Task SetAndSetAsync_WithoutExpiration_StoresCacheEntry()
		{
			var cache = CreateMongoCache();

			cache.SetString("key", "value");
			await cache.SetStringAsync("keyAsync", "value");

			Expect(cache.GetString("key"), Is.EqualTo("value"));
			Expect(cache.GetString("keyAsync"), Is.EqualTo("value"));
		}

		[Test]
		public async Task RemoveAndRemoveAsync_WithoutEntry_DoesNothing()
		{
			var cache = CreateMongoCache();

			cache.Remove("key");
			await cache.RemoveAsync("keyAsync");
		}

		[Test]
		public async Task RemoveAndRemoveAsync_WithEntry_Removes()
		{
			var cache = CreateMongoCache();
			cache.SetString("key", "value");
			cache.SetString("keyAsync", "value");

			cache.Remove("key");
			await cache.RemoveAsync("keyAsync");

			Expect(cache.GetString("key"), Is.Null);
			Expect(cache.GetString("keyAsync"), Is.Null);
		}
	}
}