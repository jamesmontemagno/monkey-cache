﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MonkeyCache
{
	/// <summary>
	/// Interface for a barrel of cache
	/// </summary>
	public interface IBarrel
	{
		/// <summary>
		/// Enable / Disable auto expiring of items in the barrel
		/// </summary>
		bool AutoExpire { get; set; }

		/// <summary>
		/// Add an item to the barrel
		/// </summary>
		/// <typeparam name="T">Type of item</typeparam>
		/// <param name="key">Key to use</param>
		/// <param name="data">Data to store of type T</param>
		/// <param name="expireIn">How long in the future the item should expire</param>
		/// <param name="options">Specific json serializer options to use</param>
		/// <param name="eTag">eTag to use if needed</param>
		[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")] 
		void Add<T>(string key, T data, TimeSpan expireIn, JsonSerializerOptions options = null, string eTag = null);

		/// <summary>
		/// Add an item to the barrel
		/// </summary>
		/// <typeparam name="T">Type of item</typeparam>
		/// <param name="key">Key to use</param>
		/// <param name="data">Data to store of type T</param>
		/// <param name="expireIn">How long in the future the item should expire</param>
		/// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
		/// <param name="eTag">eTag to use if needed</param>
		void Add<T>(string key, T data, TimeSpan expireIn, JsonTypeInfo<T> jsonTypeInfo, string eTag = null);

		/// <summary>
		/// Empty a set of keys
		/// </summary>
		/// <param name="key">Keys to empty</param>
		void Empty(params string[] key);

		/// <summary>
		/// Empty all items from the barrel
		/// </summary>
		void EmptyAll();

		/// <summary>
		/// Empty only expired items from the barrel
		/// </summary>
		void EmptyExpired();

		/// <summary>
		/// Checks to see if a key exists in the barrel
		/// </summary>
		/// <param name="key">Key to check</param>
		/// <returns>True if the key exists, else false</returns>
		bool Exists(string key);

		/// <summary>
		/// Gets keys with specified state
		/// </summary>
		/// <param name="state">State to get: Multiple with flags: CacheState.Active | CacheState.Expired</param>
		/// <returns>The keys</returns>
		IEnumerable<string> GetKeys(CacheState state = CacheState.Active);

		/// <summary>
		/// Get an object for the key
		/// </summary>
		/// <typeparam name="T">Type of object to get</typeparam>
		/// <param name="key">Key to use</param>
		/// <param name="options">Specific json serializer options to use</param>
		/// <returns>The object back if it exists, else null</returns>
		/// <remarks>
		/// When AutoExpire is set to true, Get{T} will return NULL if the item is expired
		/// </remarks>
		[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")] 
		T Get<T>(string key, JsonSerializerOptions options = null);

		/// <summary>
		/// Get an object for the key
		/// </summary>
		/// <typeparam name="T">Type of object to get</typeparam>
		/// <param name="key">Key to use</param>
		/// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
		/// <returns>The object back if it exists, else null</returns>
		/// <remarks>
		/// When AutoExpire is set to true, Get{T} will return NULL if the item is expired
		/// </remarks>
		T Get<T>(string key, JsonTypeInfo<T> jsonTypeInfo);

		/// <summary>
		/// Get the eTag for a key
		/// </summary>
		/// <param name="key">Key to use</param>
		/// <returns>eTag for key, else null</returns>
		string GetETag(string key);

		/// <summary>
		/// Checks if key is expired
		/// </summary>
		/// <param name="key">The key to check</param>
		/// <returns>True if expired, else false</returns>
		bool IsExpired(string key);

		/// <summary>
		/// Gets the expiration date for a key
		/// </summary>
		/// <param name="key">The key to check</param>
		/// <returns>The date if exists, else null</returns>
		DateTime? GetExpiration(string key);
	}
}