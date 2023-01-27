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
            var controllerSuffix = "Controller";
            if (typeName.EndsWith(controllerSuffix))
            { 
                return typeName.Substring(0, typeName.Length - controllerSuffix.Length);
            }
            return typeName;
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
