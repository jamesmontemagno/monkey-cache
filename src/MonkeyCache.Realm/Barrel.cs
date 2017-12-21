using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Realms;

namespace MonkeyCache.Realm
{
    public class Barrel : IBarrel
    {
        protected RealmConfiguration configuration;

        protected Realm realm;

        public Barrel(){
            var cacheDirectory = Path.Combine(Utils.GetBasePath(ApplicationId), "MonkeyCache");
            var database = Path.Combine(cacheDirectory, "barrel.realm");
            configuration = new RealmConfiguration(database);
            realm = new Realm(configuration);
        }

        public void Add(string key, string data, TimeSpan expireIn, string eTag = null){
            
        }

        public void Add<T>(string key, T data, TimeSpan expireIn, string eTag = null){
            
        }

        public void Empty(params string[] key){

        }

        public void EmptyAll(){

        }

        public void EmptyExpired(){

        }

        public bool Exists(string key){

        }

        public string Get(string key){

        }

        public T Get<T>(string key){

        }

        public string GetETag(string key){

        }

        public bool IsExpired(string key){

        }
    }
}
