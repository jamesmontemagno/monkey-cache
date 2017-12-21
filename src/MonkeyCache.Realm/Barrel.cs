using System;
using System.Collections.Generic;
using System.Text;
using Realms;

namespace MonkeyCache.Realm
{
    public class Barrel : IBarrel
    {
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
