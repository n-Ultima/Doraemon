using System;

namespace Doraemon.Services
{
    /// <summary>
    /// Declares the class as a service to inject to our service pool.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DoraemonService : Attribute
    {
        
    }
}