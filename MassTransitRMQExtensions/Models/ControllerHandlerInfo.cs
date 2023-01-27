using System.Reflection;
using System;

namespace MassTransitRMQExtensions.Models
{
    public class ControllerHandlerInfo
    {
        public Type ControllerType { get; }
        public MethodInfo Method { get; }

        public ControllerHandlerInfo(Type controllerType, MethodInfo actionMethod)
        {
            ControllerType = controllerType;
            Method = actionMethod;
        }
    }
}
