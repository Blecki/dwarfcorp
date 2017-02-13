using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;
using System.Runtime.InteropServices;
using System.Reflection.Emit;

namespace NativeAssemblerInjection
{
    public static class MethodUtil
    {

        public static readonly Version Net40 = new Version(4, 0, 30319, 1);
        public static readonly Version Net35 = new Version(3, 5, 21022, 8);
        public static readonly Version Net35SP1 = new Version(3, 5, 30729, 1);
        public static readonly Version Net30 = new Version(3, 0, 4506, 30);
        public static readonly Version Net30SP1 = new Version(3, 0, 4506, 648);
        public static readonly Version Net30SP2 = new Version(3, 0, 4506, 2152);
        public static readonly Version Net20 = new Version(2, 0, 50727, 42);
        public static readonly Version Net20SP1 = new Version(2, 0, 50727, 1433);
        public static readonly Version Net20SP2 = new Version(2, 0, 50727, 3053);

        /// <summary>
        /// Replaces the method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="dest">The dest.</param>
        public static void ReplaceMethod(MethodBase source, MethodBase dest)
        {
            if (!MethodSignaturesEqual(source, dest))
            {
                throw new ArgumentException("The method signatures are not the same.", "source");
            }

            ReplaceMethod(GetMethodAddress(source), dest);
        }

        /// <summary>
        /// Replaces the method.
        /// </summary>
        /// <param name="srcAdr">The SRC adr.</param>
        /// <param name="dest">The dest.</param>
        public static void ReplaceMethod(IntPtr srcAdr, MethodBase dest)
        {
            IntPtr destAdr = GetMethodAddressRef(dest);

            unsafe
            {
                if (IntPtr.Size == 8)
                {
                    ulong* d = (ulong*)destAdr.ToPointer();
                    *d = (ulong)srcAdr.ToInt64();
                }
                else
                {
                    uint* d = (uint*)destAdr.ToPointer();
                    *d = (uint)srcAdr.ToInt32();
                }
            }
        }

        private static IntPtr GetMethodAddressRef(MethodBase srcMethod)
        {
            if ((srcMethod is DynamicMethod))
            {
                return GetDynamicMethodAddress(srcMethod);
            }

            // Prepare the method so it gets jited
            RuntimeHelpers.PrepareMethod(srcMethod.MethodHandle);

            // If 3.5 sp1 or greater than we have a different layout in memory.
            if (IsNet20Sp2OrGreater())
            {
                return GetMethodAddress20SP2(srcMethod);
            }

            unsafe
            {
                // Skip these
                const int skip = 10;

                // Read the method index.
                UInt64* location = (UInt64*)(srcMethod.MethodHandle.Value.ToPointer());
                int index = (int)(((*location) >> 32) & 0xFF);

                if (IntPtr.Size == 8)
                {
                    // Get the method table
                    ulong* classStart = (ulong*)srcMethod.DeclaringType.TypeHandle.Value.ToPointer();
                    ulong* address = classStart + index + skip;
                    return new IntPtr(address);
                }
                else
                {
                    // Get the method table
                    uint* classStart = (uint*)srcMethod.DeclaringType.TypeHandle.Value.ToPointer();
                    uint* address = classStart + index + skip;
                    return new IntPtr(address);
                }
            }
        }

        /// <summary>
        /// Gets the address of the method stub
        /// </summary>
        /// <param name="methodHandle">The method handle.</param>
        /// <returns></returns>
        public static IntPtr GetMethodAddress(MethodBase method)
        {
            if ((method is DynamicMethod))
            {
                return GetDynamicMethodAddress(method);
            }

            // Prepare the method so it gets jited
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            return method.MethodHandle.GetFunctionPointer();
        }

        private static IntPtr GetDynamicMethodAddress(MethodBase method)
        {
            unsafe
            {
                RuntimeMethodHandle handle = GetDynamicMethodRuntimeHandle(method);
                byte* ptr = (byte*)handle.Value.ToPointer();

                if (IsNet20Sp2OrGreater())
                {
                    RuntimeHelpers.PrepareMethod(handle);
                    return handle.GetFunctionPointer();

                    //if (IntPtr.Size == 8)
                    //{
                    //    ulong* address = (ulong*)ptr;
                    //    address = (ulong*)*(address + 5);
                    //    return new IntPtr(address + 12);
                    //}
                    //else
                    //{
                    //    uint* address = (uint*)ptr;
                    //    address = (uint*)*(address + 5);
                    //    return new IntPtr(address + 12);
                    //}

                }
                else
                {


                    if (IntPtr.Size == 8)
                    {
                        ulong* address = (ulong*)ptr;
                        address += 6;
                        return new IntPtr(address);
                    }
                    else
                    {
                        uint* address = (uint*)ptr;
                        address += 6;
                        return new IntPtr(address);
                    }
                }

            }
        }

        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(MethodBase method)
        {
            RuntimeMethodHandle handle;

            if (Environment.Version.Major == 4)
            {
                MethodInfo getMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                handle = (RuntimeMethodHandle)getMethodDescriptorInfo.Invoke(method, null);
            }
            else
            {
                FieldInfo fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
                handle = ((RuntimeMethodHandle)fieldInfo.GetValue(method));
            }

            return handle;
        }

        private static IntPtr GetMethodAddress20SP2(MethodBase method)
        {
            unsafe
            {
                return new IntPtr(((int*)method.MethodHandle.Value.ToPointer() + 2));
            }
        }
        private static bool MethodSignaturesEqual(MethodBase x, MethodBase y)
        {
            if (x.CallingConvention != y.CallingConvention)
            {
                return false;
            }
            Type returnX = GetMethodReturnType(x), returnY = GetMethodReturnType(y);
            if (returnX != returnY)
            {
                return false;
            }
            ParameterInfo[] xParams = x.GetParameters(), yParams = y.GetParameters();
            if (xParams.Length != yParams.Length)
            {
                return false;
            }
            /*for (int i = 0; i < xParams.Length; i++)
            {
                if (xParams[i].ParameterType != yParams[i].ParameterType)
                {
                    return false;
                }
            }*/
            return true;
        }
        private static Type GetMethodReturnType(MethodBase method)
        {
            MethodInfo methodInfo = method as MethodInfo;
            if (methodInfo == null)
            {
                // Constructor info.
                throw new ArgumentException("Unsupported MethodBase : " + method.GetType().Name, "method");
            }
            return methodInfo.ReturnType;
        }
        private static bool IsNet20Sp2OrGreater()
        {
            if (Environment.Version.Major == 4)
            {
                return true;
            }

            return Environment.Version.Major == Net20SP2.Major &&
                Environment.Version.MinorRevision >= Net20SP2.MinorRevision;
        }
    }
}
