using System;

namespace Doraemon.Data.Repositories
{
    /// <summary>
    /// Marks a repository to be added as a scoped service when gathering them inside our service collection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DoraemonRepository : Attribute
    {
        
    }
}