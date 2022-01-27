using System;
using System.Reflection;
using System.Linq;

namespace MassTransitRMQExtensions.Helpers
{
    internal static class Helpers
    {
        public static string GetControllerName(this Type type)
        {
            var typeName = type.Name;
            return typeName.Substring(0, typeName.Length - "Controller".Length);
        }
        public static string GetQueueName(this MethodInfo method)
        {
            return $"{GetControllerName(method.DeclaringType)}_{method.Name}";
        }
        public static bool CheckMethodHasAttribute<A>(this MethodInfo method) where A : Attribute
        {
            return method.GetCustomAttributes<A>().Any();
        }
    }
}
