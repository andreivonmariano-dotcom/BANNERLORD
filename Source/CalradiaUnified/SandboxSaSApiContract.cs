using System;
using System.Reflection;

namespace CalradiaUnified
{
    /// <summary>Pure reflection contract for the supported read-only SaS v1 API.</summary>
    internal static class SandboxSaSApiContract
    {
        internal static bool IsSupported(Type type)
        {
            if(type==null)return false;
            try
            {
                const BindingFlags flags=BindingFlags.Public|BindingFlags.Static;
                var version=type.GetField("ApiVersion",flags);
                if(version==null || !version.IsLiteral ||
                    version.FieldType!=typeof(int) ||
                    (int)version.GetRawConstantValue()!=1)return false;
                var enlisted=type.GetProperty("IsPlayerEnlisted",flags);
                if(enlisted==null || enlisted.PropertyType!=typeof(bool) ||
                    enlisted.GetIndexParameters().Length!=0)return false;
                var getter=enlisted.GetGetMethod(false);
                if(getter==null || !getter.IsStatic)return false;
                var snapshot=type.GetMethod("GetSnapshot",flags,null,
                    Type.EmptyTypes,null);
                return snapshot!=null && !snapshot.ContainsGenericParameters &&
                    snapshot.ReturnType!=typeof(void);
            }
            catch(Exception){return false;}
        }
    }
}
