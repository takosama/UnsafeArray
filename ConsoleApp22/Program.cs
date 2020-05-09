using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using UnsafeArray;

namespace ConsoleApp22
{
    unsafe class Program
    {
        unsafe static void Main(string[] args)
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<Bentimark>();


            UnsafeArray<int> p0 = new UnsafeArray<int>(100);
            int[] arr = new int[100];
            UnsafeArray<int> p1 = new UnsafeArray<int>(arr);
            int* p = p1.Pointer;

            p0.Free();
           
            //解放時は下のうちどちらかで開放できます
            //つまりarrがなくてもポインタさえあれば解放できる
            //    p1.Free();
            // PointerManager.Free(p);


            //メイン関数にこれを置いておけば未開放分をすべて解放されます
          
            PointerManager.FreeAll();
        }
    }


    public class Bentimark
    {
        int size = 200_000_000;
        [BenchmarkDotNet.Attributes.Benchmark]
        public void BentimarkNomal()
        {
            int[] arr = new int[size];
            for (int i = 0; i < size; i++)
                arr[i] = i;
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void BentimarkNomalToUnsafe()
        {
            int[] arr = new int[size];
            var p = new UnsafeArray<int>(arr);

            for (int i = 0; i < size; i++)
                p[i] = i;
            p.Free();
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void BentimarkMyArray()
        {
            UnsafeArray<int> arr = new UnsafeArray<int>(size);

            for (int i = 0; i < size; i++)
                arr[i] = i;
            arr.Free();
        }


    }
}

namespace UnsafeArray
{
    public unsafe class UnsafeArray<T> where T : unmanaged
    {
        public T* Pointer { get; private set; }
        public T[] Array { get; private set; }
        int size = 0;
        public UnsafeArray(int size)
        {
            this.Array = new T[size];
            this.Pointer = PointerManager.AddArray(this.Array);
            this.size = this.Array.Length;
        }

        public UnsafeArray(T[] array)
        {
            this.Array = array;
            this.Pointer = PointerManager.AddArray(array);
            this.size = array.Length;
        }
    
        public T this[int index]
        {
            get
            {
                return *(Pointer + index);
            }
            set
            {
                *(Pointer + index) = value;
            }
        }

        public void Free()
        {
            PointerManager.Free(this.Pointer);
        }


    }

    unsafe static class PointerManager
    {
        static List<(IntPtr ptr, int size, GCHandle? hdl)> ps = new List<(IntPtr, int, GCHandle?)>();

        static public T* Alloc<T>(int num) where T : unmanaged
        {
            int size = num * sizeof(T);
            var ptr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(size);
            ps.Add((ptr, size, null));
            return (T*)ptr.ToPointer();
        }

        static public T* AddArray<T>(T[] array) where T : unmanaged
        {
            var hdl = System.Runtime.InteropServices.GCHandle.Alloc(array, GCHandleType.Pinned);
            int size = array.Length * sizeof(T);
            var ptr = hdl.AddrOfPinnedObject();
            ps.Add((ptr, size, hdl));
            return (T*)ptr;
        }

        static public int GetSize<T>(T* ptr) where T : unmanaged
        {
            IntPtr intPtr = (IntPtr)ptr;
            var obj = ps.Find(x => x.ptr == intPtr);
            if (obj.ptr.ToPointer() != null)
                return obj.size / sizeof(T);
            else
                throw new Exception("0x" + string.Format("{0:x}", intPtr.ToInt64()).PadLeft(16, '0') + " is not memory manage target in this library");
        }

        static public void Free<T>(T* ptr) where T : unmanaged
        {
            IntPtr intPtr = (IntPtr)ptr;

            var item = ps.Find(x => x.ptr == intPtr);
            if (!item.hdl.HasValue)
                if (ps.Remove(item))
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(intPtr);
                else
                    throw new Exception("0x" + string.Format("{0:x}", intPtr.ToInt64()).PadLeft(16, '0') + " is not memory manage target in this library");
            else
            {
                if (ps.Remove(item))
                    item.hdl.Value.Free();
                else
                    throw new Exception("0x" + string.Format("{0:x}", intPtr.ToInt64()).PadLeft(16, '0') + " is not memory manage target in this library");
            }
        }

        static public void FreeAll()
        {
            foreach (var item in ps)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(item.ptr);
            }
        }
    }
}
