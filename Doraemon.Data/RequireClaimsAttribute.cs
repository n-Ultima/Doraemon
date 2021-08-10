using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Doraemon.Data.Models.Core;
using Qmmands;

namespace Doraemon.Data
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RequireClaims : Attribute
    {
        public ClaimMapType[] _claims;
        public RequireClaims(params ClaimMapType[] claims)
        {
            _claims = claims;
        }
        
    }
}