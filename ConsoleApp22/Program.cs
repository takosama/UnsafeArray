using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;

namespace ConsoleApp22
{
    unsafe class Program
    {
        unsafe static void Main(string[] args)
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<Bentimark>();


            UnsafeArray<int> arr = new UnsafeArray<int>(100);

            arr[0] = 0;
            arr[100] = 100;

            int* p = arr.Pointer;
            *(p + 10) = 10;


            //解放時は下のうちどちらかで開放できます
            //つまりarrがなくてもポインタさえあれば解放できる
            //    arr.Free();
            // PointerManager.Free(p);


            //メイン関数にこれを置いておけば未開放分をすべて解放されます
            //今回の場合はarrが解放される
            PointerManager.FreeAll();
        }
    }


    public class Bentimark
    {
        int size = 1000000;
        [BenchmarkDotNet.Attributes.Benchmark]
        public void BentimarkNomal()
        {
            int[] arr = new int[size];
            for (int i = 0; i < size; i += 10)
            {
                arr[i] = i;
                arr[i + 1] = i;
                arr[i + 2] = i;
                arr[i + 3] = i;
                arr[i + 4] = i;
                arr[i + 5] = i;
                arr[i + 6] = i;
                arr[i + 7] = i;
                arr[i + 8] = i;
                arr[i + 9] = i;
            }
        }

        [BenchmarkDotNet.Attributes.Benchmark]
        public void BentimarkMyArray()
        {
            UnsafeArray<int> arr = new UnsafeArray<int>(size);
            for (int i = 0; i < size; i += 10)
            {
                arr[i] = i;
                arr[i + 1] = i;
                arr[i + 2] = i;
                arr[i + 3] = i;
                arr[i + 4] = i;
                arr[i + 5] = i;
                arr[i + 6] = i;
                arr[i + 7] = i;
                arr[i + 8] = i;
                arr[i + 9] = i;

            }
            arr.Free();
        }


    }

    unsafe class UnsafeArray<T> where T : unmanaged
    {
        public T* Pointer { get; }
        int size = 0;
        public UnsafeArray(int size)
        {
            var ptr = PointerManager.Alloc<T>(size);
            this.Pointer = ptr;
            this.size = size;
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
        static List<(IntPtr ptr, int size)> ps = new List<(IntPtr, int)>();

        static public T* Alloc<T>(int num) where T : unmanaged
        {
            int size = num * sizeof(T);
            var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            ps.Add((ptr, size));
            return (T*)ptr.ToPointer();
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
            if (ps.Remove(ps.Find(x => x.ptr == intPtr)))
                System.Runtime.InteropServices.Marshal.FreeHGlobal(intPtr);
            else
                throw new Exception("0x" + string.Format("{0:x}", intPtr.ToInt64()).PadLeft(16, '0') + " is not memory manage target in this library");
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