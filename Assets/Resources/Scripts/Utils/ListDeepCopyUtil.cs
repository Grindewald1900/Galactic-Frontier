using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils
{
    /// <summary>
    /// Utility class for deep copying lists of objects.
    /// </summary>
    public static class ListDeepCopyUtil
    {
        /// <summary>
        /// Deep copies a list of objects using a custom copy constructor.
        /// </summary>
        public static List<T> DeepCopyWithConstructor<T>(List<T> originalList, Func<T, T> copyConstructor)
        {
            return originalList.Select(item => copyConstructor(item)).ToList();
        }

        /// <summary>
        /// Deep copies a list using JsonUtility (Unity specific). Use only for simple, serializable types.
        /// </summary>
        public static List<T> DeepCopyViaJson<T>(List<T> originalList)
        {
            Wrapper<T> wrapper = new Wrapper<T> { list = originalList };
            string json = JsonUtility.ToJson(wrapper);
            Wrapper<T> clone = JsonUtility.FromJson<Wrapper<T>>(json);
            return clone.list;
        }

        /// <summary>
        /// Deep copies a list where items implement ICloneable.
        /// </summary>
        public static List<T> DeepCopyWithClone<T>(List<T> originalList) where T : ICloneable
        {
            return originalList.Select(item => (T)item.Clone()).ToList();
        }

        [Serializable]
        private class Wrapper<T>
        {
            public List<T> list;
        }
    }
}