// // File: Realm.cs
// namespace JS;
//
// /*
//  * Copyright (c) 2021-2022, Linus Groh <linusg@serenityos.org>
//  * Copyright (c) 2022, Andreas Kling <andreas@ladybird.org>
//  *
//  * SPDX-License-Identifier: BSD-2-Clause
//  */
//
// using System;
// using System.Collections.Generic;
//
// public sealed class Realm : Cell
// {
//     // GC_CELL(Realm, Cell);
//     // GC_DECLARE_ALLOCATOR(Realm);
//
//     public abstract class HostDefined
//     {
//         // C++: virtual ~HostDefined() = default;
//         // C#: Use destructor if needed, but not required for stubs.
//
//         // C++: virtual void visit_edges(Cell::Visitor&) { }
//         public virtual void VisitEdges(Cell.Visitor visitor) { }
//
//         // C++: template<typename T> bool fast_is() const = delete;
//         // C#: Not applicable, so not implemented.
//
//         // C++: virtual bool is_principal_host_defined() const { return false; }
//         public virtual bool IsPrincipalHostDefined() => false;
//
//         // C++: virtual bool is_synthetic_host_defined() const { return false; }
//         public virtual bool IsSyntheticHostDefined() => false;
//     }
//
//     // C++: template<typename T, typename... Args>
//     //       GC::Ref<T> create(Args&&... args)
//     public GC.Ref<T> Create<T>(params object[] args) where T : class
//     {
//         // C++: auto object = heap().allocate<T>(forward<Args>(args)...);
//         var obj = Heap().Allocate<T>(args);
//         ((Cell)obj).Initialize(this);
//         return obj;
//     }
//
//     // C++: static ThrowCompletionOr<NonnullOwnPtr<ExecutionContext>> initialize_host_defined_realm(...)
//     public static Result<ExecutionContext> InitializeHostDefinedRealm(
//         VM vm,
//         Func<Realm, Object> createGlobalObject,
//         Func<Realm, Object> createGlobalThisValue)
//     {
//         // GC::DeferGC defer_gc(vm.heap());
//         using (new GC.DeferGC(vm.Heap()))
//         {
//             // 1. Let realm be a new Realm Record
//             var realm = vm.Heap().Allocate<Realm>();
//
//             // 2. Perform CreateIntrinsics(realm).
//             Intrinsics.Create(realm);
//
//             // FIXME: 3. Set realm.[[AgentSignifier]] to AgentSignifier().
//
//             // NOTE: Done on step 1.
//             // 4. Set realm.[[GlobalObject]] to undefined.
//             // 5. Set realm.[[GlobalEnv]] to undefined.
//
//             // FIXME: 6. Set realm.[[TemplateMap]] to a new empty List.
//
//             // 7. Let newContext be a new execution context.
//             var newContext = ExecutionContext.Create(0, 0);
//
//             // 8. Set the Function of newContext to null.
//             newContext.Function = null;
//
//             // 9. Set the Realm of newContext to realm.
//             newContext.Realm = realm;
//
//             // 10. Set the ScriptOrModule of newContext to null.
//             newContext.ScriptOrModule = null;
//
//             // 11. Push newContext onto the execution context stack; newContext is now the running execution context.
//             vm.PushExecutionContext(newContext);
//
//             // 12. If the host requires use of an exotic object to serve as realm's global object, then
//             Object global = null;
//             if (createGlobalObject != null)
//             {
//                 // a. Let global be such an object created in a host-defined manner.
//                 global = createGlobalObject(realm);
//             }
//             // 13. Else,
//             else
//             {
//                 // a. Let global be OrdinaryObjectCreate(realm.[[Intrinsics]].[[%Object.prototype%]]).
//                 // NOTE: We allocate a proper GlobalObject directly as this plain object is
//                 //       turned into one via SetDefaultGlobalBindings in the spec.
//                 global = vm.Heap().Allocate<GlobalObject>(realm);
//             }
//
//             // 14. If the host requires that the this binding in realm's global scope return an object other than the global object, then
//             Object thisValue = null;
//             if (createGlobalThisValue != null)
//             {
//                 // a. Let thisValue be such an object created in a host-defined manner.
//                 thisValue = createGlobalThisValue(realm);
//             }
//             // 15. Else,
//             else
//             {
//                 // a. Let thisValue be global.
//                 thisValue = global;
//             }
//
//             // 16. Set realm.[[GlobalObject]] to global.
//             realm._globalObject = global;
//
//             // 17. Set realm.[[GlobalEnv]] to NewGlobalEnvironment(global, thisValue).
//             realm._globalEnvironment = vm.Heap().Allocate<GlobalEnvironment>(global, thisValue);
//
//             // 18. Perform ? SetDefaultGlobalBindings(realm).
//             SetDefaultGlobalBindings(realm);
//
//             // 19. Create any host-defined global object properties on global.
//             global.Initialize(realm);
//
//             // 20. Return unused.
//             return Result<ExecutionContext>.Success(newContext);
//         }
//     }
//
//     // C++: void visit_edges(Visitor& visitor) override;
//     public override void VisitEdges(Cell.Visitor visitor)
//     {
//         base.VisitEdges(visitor);
//         visitor.Visit(_intrinsics);
//         visitor.Visit(_globalObject);
//         visitor.Visit(_globalEnvironment);
//         if (_hostDefined != null)
//             _hostDefined.VisitEdges(visitor);
//     }
//
//     // C++: [[nodiscard]] Object& global_object() const { return *m_global_object; }
//     public Object GlobalObject => _globalObject;
//
//     // C++: void set_global_object(GC::Ref<Object> global) { m_global_object = global; }
//     public void SetGlobalObject(GC.Ref<Object> global) => _globalObject = global;
//
//     // C++: [[nodiscard]] GlobalEnvironment& global_environment() const { return *m_global_environment; }
//     public GlobalEnvironment GlobalEnvironment => _globalEnvironment;
//
//     // C++: void set_global_environment(GC::Ref<GlobalEnvironment> environment) { m_global_environment = environment; }
//     public void SetGlobalEnvironment(GC.Ref<GlobalEnvironment> environment) => _globalEnvironment = environment;
//
//     // C++: [[nodiscard]] Intrinsics const& intrinsics() const { return *m_intrinsics; }
//     public Intrinsics Intrinsics => _intrinsics;
//
//     // C++: [[nodiscard]] Intrinsics& intrinsics() { return *m_intrinsics; }
//     public Intrinsics IntrinsicsMutable => _intrinsics;
//
//     // C++: void set_intrinsics(Badge<Intrinsics>, Intrinsics& intrinsics)
//     public void SetIntrinsics(AK.Badge<Intrinsics> badge, Intrinsics intrinsics)
//     {
//         if (_intrinsics != null)
//             throw new InvalidOperationException("Intrinsics already set");
//         _intrinsics = intrinsics;
//     }
//
//     // C++: HostDefined* host_defined() { return m_host_defined; }
//     public HostDefined HostDefinedObj => _hostDefined;
//
//     // C++: HostDefined const* host_defined() const { return m_host_defined; }
//     public HostDefined HostDefinedObjConst => _hostDefined;
//
//     // C++: void set_host_defined(OwnPtr<HostDefined> host_defined) { m_host_defined = move(host_defined); }
//     public void SetHostDefined(HostDefined hostDefined) => _hostDefined = hostDefined;
//
//     // C++: void define_builtin(Bytecode::Builtin builtin, GC::Ref<NativeFunction> value)
//     public void DefineBuiltin(Bytecode.Builtin builtin, GC.Ref<NativeFunction> value)
//     {
//         _builtins[(int)builtin] = value;
//     }
//
//     // C++: GC::Ref<NativeFunction> get_builtin_value(Bytecode::Builtin builtin)
//     public GC.Ref<NativeFunction> GetBuiltinValue(Bytecode.Builtin builtin)
//     {
//         return _builtins[(int)builtin];
//     }
//
//     // C++: private: Realm() = default;
//     public Realm() { }
//
//     // C++: private fields
//     private Intrinsics _intrinsics;                // [[Intrinsics]]
//     private Object _globalObject;                  // [[GlobalObject]]
//     private GlobalEnvironment _globalEnvironment;  // [[GlobalEnv]]
//     private HostDefined _hostDefined;              // [[HostDefined]]
//     private GC.Ref<NativeFunction>[] _builtins = new GC.Ref<NativeFunction>[Bytecode.ToUnderlying(Bytecode.Builtin.__Count)];
//
//     // C++: set_default_global_bindings(*realm);
//     private static void SetDefaultGlobalBindings(Realm realm)
//     {
//         // Stub for set_default_global_bindings
//         // This should be replaced with the actual implementation.
//     }
// }