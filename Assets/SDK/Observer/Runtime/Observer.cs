using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LSDK.Core
{
    public class Observer
    {
        private class Handler
        {
            public Delegate Original { get; set; }
            public Action<object> Invoke { get; set; }
        }
        private static Dictionary<string, List<Handler>> _observerDicts = new Dictionary<string, List<Handler>>();
        /// <summary>
        /// Subscribe to an event 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        public static void Subscribe<T>(string eventName, Action<object> action)
        {
            if (action == null) return;
            Add(eventName, action, data =>
            {
                if (data is T type) action(type);
            });
        }
        public static void Subscribe(string eventName, Action action)
        {
            if (action == null) return;
            Add(eventName, action, _ => action());
        }
        public static void Unsubscribe<T>(string eventName, Action<T> action) => Remove(eventName, action);
        public static void Unsubscribe(string eventName, Action action) => Remove(eventName, action);
        public static void Publish(string eventName) => Dispatch(eventName, null);
        public static void Publish<T>(string eventName, T data) => Dispatch(eventName, data);

        /// <summary>
        /// private method
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="original"></param>
        /// <param name="wrapper"></param>
        /// Add a new observer to the event
        private static void Add(string eventName, Delegate original, Action<object> wrapper)
        {
            if (!_observerDicts.TryGetValue(eventName, out var list))
            {
                list = new List<Handler>();
                _observerDicts[eventName] = list;
            }
            list.Add(new Handler() { Original = original, Invoke = wrapper });
        }

        private static void Remove(string eventName, Delegate original)
        {
            if (original == null) return;
            if (!_observerDicts.TryGetValue(eventName, out var list)) return;
            list.RemoveAll(handler => handler.Original.Equals(original));

        }
        private static void Dispatch(string eventName, object data)
        {
            if (!_observerDicts.TryGetValue(eventName, out var list)) return;
            var tempArr = list.ToArray();
            foreach (var handler in tempArr)
            {
                handler.Invoke(data);
            }
        }

    }
}
