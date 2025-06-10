// /*
//  * Copyright (c) 2021, Ali Mohammad Pur <mpfard@serenityos.org>
//  *
//  * SPDX-License-Identifier: BSD-2-Clause
//  */
//
// namespace AK;
//
// using System;
//
// // NOTE: C# has built-in tuple support with System.ValueTuple
// // This implementation is for compatibility with the C++ AK::Tuple API
//
// namespace Detail
// {
//     // Base tuple implementation
//     public struct TupleBase<T>
//     {
//         private T _value;
//
//         public TupleBase(T value)
//         {
//             _value = value;
//         }
//
//         public TupleBase(in T value)
//         {
//             _value = value;
//         }
//
//         public ref T Get<U>() where U : T
//         {
//             // Static assert equivalent in C#
//             if (!typeof(U).IsAssignableFrom(typeof(T)))
//                 throw new InvalidOperationException("Invalid tuple access");
//             return ref _value;
//         }
//
//         public ref readonly T GetConst<U>() where U : T
//         {
//             return ref Get<U>();
//         }
//
//         public ref T GetWithIndex<U>(int index) where U : T
//         {
//             if (!typeof(U).IsAssignableFrom(typeof(T)) || index != 0)
//                 throw new InvalidOperationException("Invalid tuple access");
//             return ref _value;
//         }
//
//         public ref readonly T GetConstWithIndex<U>(int index) where U : T
//         {
//             return ref GetWithIndex<U>(index);
//         }
//     }
//
//     // Recursive tuple implementation for multiple elements
//     public struct TupleMultiple<T, TRest> where TRest : struct
//     {
//         private T _value;
//         private TRest _rest;
//
//         public TupleMultiple(T first, TRest rest)
//         {
//             _value = first;
//             _rest = rest;
//         }
//
//         public ref T Get<U>() where U : T
//         {
//             if (typeof(U) == typeof(T))
//                 return ref _value;
//
//             // Try to get from rest - this would need reflection in C#
//             throw new NotImplementedException("Getting non-first element requires reflection");
//         }
//
//         public ref readonly T GetConst<U>() where U : T
//         {
//             return ref Get<U>();
//         }
//     }
// }
//
// // Main Tuple class
// public struct Tuple<T1> : IEquatable<Tuple<T1>>
// {
//     private Detail.TupleBase<T1> _base;
//
//     public Tuple(T1 value)
//     {
//         _base = new Detail.TupleBase<T1>(value);
//     }
//
//     public Tuple(in T1 value)
//     {
//         _base = new Detail.TupleBase<T1>(value);
//     }
//
//     public ref T1 Get<T>() where T : T1
//     {
//         return ref _base.Get<T>();
//     }
//
//     public ref T1 Get(int index)
//     {
//         if (index != 0)
//             throw new ArgumentOutOfRangeException(nameof(index));
//         return ref _base.GetWithIndex<T1>(0);
//     }
//
//     public ref readonly T1 GetConst<T>() where T : T1
//     {
//         return ref _base.GetConst<T>();
//     }
//
//     public ref readonly T1 GetConst(int index)
//     {
//         if (index != 0)
//             throw new ArgumentOutOfRangeException(nameof(index));
//         return ref _base.GetConstWithIndex<T1>(0);
//     }
//
//     public int Size => 1;
//
//     public bool Equals(Tuple<T1> other)
//     {
//         return EqualityComparer<T1>.Default.Equals(Get<T1>(), other.Get<T1>());
//     }
//
//     public override bool Equals(object obj)
//     {
//         return obj is Tuple<T1> other && Equals(other);
//     }
//
//     public override int GetHashCode()
//     {
//         return Get<T1>()?.GetHashCode() ?? 0;
//     }
// }
//
// // Two-element tuple
// public struct Tuple<T1, T2> : IEquatable<Tuple<T1, T2>>
// {
//     private T1 _value1;
//     private T2 _value2;
//
//     public Tuple(T1 first, T2 second)
//     {
//         _value1 = first;
//         _value2 = second;
//     }
//
//     public ref T Get<T>()
//     {
//         if (typeof(T) == typeof(T1))
//             return ref Unsafe.As<T1, T>(ref _value1);
//         if (typeof(T) == typeof(T2))
//             return ref Unsafe.As<T2, T>(ref _value2);
//         throw new InvalidOperationException("Invalid tuple access");
//     }
//
//     public ref T GetAt<T>(int index)
//     {
//         return index switch
//         {
//             0 when typeof(T) == typeof(T1) => ref Unsafe.As<T1, T>(ref _value1),
//             1 when typeof(T) == typeof(T2) => ref Unsafe.As<T2, T>(ref _value2),
//             _ => throw new ArgumentOutOfRangeException(nameof(index))
//         };
//     }
//
//     public object Get(int index)
//     {
//         return index switch
//         {
//             0 => _value1,
//             1 => _value2,
//             _ => throw new ArgumentOutOfRangeException(nameof(index))
//         };
//     }
//
//     public T Apply<T>(Func<T1, T2, T> f)
//     {
//         return f(_value1, _value2);
//     }
//
//     public void Apply(Action<T1, T2> f)
//     {
//         f(_value1, _value2);
//     }
//
//     public int Size => 2;
//
//     public bool Equals(Tuple<T1, T2> other)
//     {
//         return EqualityComparer<T1>.Default.Equals(_value1, other._value1) &&
//                EqualityComparer<T2>.Default.Equals(_value2, other._value2);
//     }
//
//     public override bool Equals(object obj)
//     {
//         return obj is Tuple<T1, T2> other && Equals(other);
//     }
//
//     public override int GetHashCode()
//     {
//         return HashCode.Combine(_value1, _value2);
//     }
// }
//
// // NOTE: In real usage, C# developers would use System.ValueTuple instead:
// // (T1, T2) tuple = (value1, value2);
// // But this implementation maintains API compatibility with AK::Tuple