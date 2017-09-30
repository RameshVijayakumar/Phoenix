using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Unity
{
    /// <summary>
    /// Custom life time manager class that implements Microsoft.Practices.Unity.LifetimeManager class
    /// Objects are stored in HttpContext object
    /// </summary>
    /// <typeparam name="T">Type of the object whose lifetime is to be managed</typeparam>
    public class HttpContextLifetimeManager<T> : LifetimeManager, IDisposable
    {
        public override object GetValue()
        {
            return HttpContext.Current.Items[typeof(T).AssemblyQualifiedName];
        }
        public override void RemoveValue()
        {
            HttpContext.Current.Items.Remove(typeof(T).AssemblyQualifiedName);
        }
        public override void SetValue(object newValue)
        {
            HttpContext.Current.Items[typeof(T).AssemblyQualifiedName]
                = newValue;
        }
        public void Dispose()
        {
            RemoveValue();
        }
    }
}