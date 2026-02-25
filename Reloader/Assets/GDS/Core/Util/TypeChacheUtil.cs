using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GDS.Core {
    public static class TypeCacheUtil {
        public static List<Type> GetConcreteSubclasses<TBase>() {
            var baseType = typeof(TBase);
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => baseType.IsAssignableFrom(t)
                         && !t.IsAbstract
                         && !t.IsInterface)
                .ToList();
        }
    }
}