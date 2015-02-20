using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Smith
{
    public interface ISmith<T> where T : class, new()
    {
        void SetContext(Hashtable context);

        T Clone(T original);
    }

    public abstract class SmithBase<T> : ISmith<T> where T : class, new()
    {
        private Hashtable _context;

        public void SetContext(Hashtable context)
        {
            _context = context;
        }

        protected SmithBase()
        {
            _context = new Hashtable();
        }

        protected virtual TProp CloneProp<TProp>(TProp original) where TProp : class, new()
        {
            if (_context.Contains(original))
            {
                return (TProp)_context[original];
            }

            return SmithGeneric.GetSmith<TProp>(_context).Clone(original);
        }

        protected virtual void CloneList<TItem>(IList<TItem> original, IList<TItem> clone) where TItem : class, new()
        {
            foreach (var item in original)
            {
                if (item == null)
                {
                    continue;
                }
                clone.Add(SmithGeneric.GetSmith<TItem>(_context).Clone(item));
            }
        }

        protected virtual void CloneArray<TItem>(IList<TItem> original, IList<TItem> clone) where TItem : class, new()
        {
            for (var i = 0; i < original.Count; i++)
            {
                if (original[i] == null)
                {
                    continue;
                }
                clone[i] = SmithGeneric.GetSmith<TItem>(_context).Clone(original[i]);
            }
        }

        protected virtual void CloneValueTypeList(IList original, IList clone)
        {
            foreach (var item in original)
            {
                clone.Add(item);
            }
        }

        protected virtual void CloneValueTypeArray(IList original, IList clone)
        {
            for (var i = 0; i < original.Count; i++)
            {
                clone[i] = original[i];
            }
        }

        protected virtual void CloneDictionary(IDictionary original, IDictionary clone)
        {
            foreach (var key in original.Keys)
            {
                var value = original[key];

                clone.Add(key, value);
            }
        }

        public virtual T Clone(T original)
        {
            if (original == null)
            {
                return null;
            }

            var clone = new T();
            _context.Add(original, clone);

            DeepClone(original, clone);

            return clone;
        }

        protected abstract void DeepClone(T original, T clone);
    }

    public static class SmithGeneric
    {
        private static readonly Hashtable Cache = new Hashtable();
        private static readonly ModuleBuilder ModuleBuilder;

        static SmithGeneric()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("Smith.Smiths"),
                AssemblyBuilderAccess.Run);

            var assemblyName = assemblyBuilder.GetName().Name;

            ModuleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName);
        }

        public static T Clone<T>(T t) where T : class, new()
        {
            return GetSmith<T>().Clone(t);
        }

        internal static ISmith<T> GetSmith<T>(Hashtable context = null) where T : class, new()
        {
            var smith = (ISmith<T>)GetSmith(typeof(T));

            if (context != null)
            {
                smith.SetContext(context);
            }

            return smith;
        }

        private static object GetSmith(Type type)
        {
            if (Cache.ContainsKey(type))
            {
                return Cache[type];
            }

            var mapper = CreateSmith(type);
            Cache.Add(type, mapper);
            return mapper;
        }

        private static object CreateSmith(Type type)
        {
            var typeName = type.Name + "Smith";

            var baseType = typeof(SmithBase<>).MakeGenericType(type);

            var typeBuilder = ModuleBuilder.DefineType(
                "Smith.Smiths." + typeName,
                TypeAttributes.Public | TypeAttributes.Class,
                baseType);

            var baseCtor = baseType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.HasThis,
                Type.EmptyTypes);

            // :base()
            var ctorIL = ctorBuilder.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, baseCtor);
            ctorIL.Emit(OpCodes.Ret);

            var deepCloneMethod = baseType.GetMethod("DeepClone", BindingFlags.Instance | BindingFlags.NonPublic);
            var clonePropMethod = baseType.GetMethod("CloneProp", BindingFlags.Instance | BindingFlags.NonPublic);
            var cloneListMethod = baseType.GetMethod("CloneList", BindingFlags.Instance | BindingFlags.NonPublic);
            var cloneArrayMethod = baseType.GetMethod("CloneArray", BindingFlags.Instance | BindingFlags.NonPublic);
            var cloneValueTypeListMethod = baseType.GetMethod("CloneValueTypeList", BindingFlags.Instance | BindingFlags.NonPublic);
            var cloneValueTypeArrayMethod = baseType.GetMethod("CloneValueTypeArray", BindingFlags.Instance | BindingFlags.NonPublic);

            var arrayLengthGetter = typeof(Array).GetProperty("Length").GetGetMethod();

            // protected override void DeepClone(T original, T clone)
            var il = typeBuilder
                .DefineMethod(deepCloneMethod.Name,
                    MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    deepCloneMethod.ReturnType,
                    GetParameterTypes(deepCloneMethod))
                .GetILGenerator();

            foreach (var prop in type.GetProperties())
            {
                var getter = prop.GetGetMethod();
                var setter = prop.GetSetMethod();

                if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                {
                    // clone.Prop = original.Prop;
                    il.Emit(OpCodes.Ldarg_2); // Push: clone
                    il.Emit(OpCodes.Ldarg_1); // Push: original
                    il.Emit(OpCodes.Callvirt, getter); // Push: original.Prop, Pop: original
                    il.Emit(OpCodes.Callvirt, setter); // Pop: clone, original.Prop

                    continue;
                }

                var ifnull = il.DefineLabel();

                // PropType tmp;
                var tmp = il.DeclareLocal(prop.PropertyType);

                // tmp = original.Prop;
                il.Emit(OpCodes.Ldarg_1); // Push: original
                il.Emit(OpCodes.Callvirt, getter); // Push: original.Prop, Pop: original
                il.Emit(OpCodes.Stloc, tmp); // Pop: original.Prop

                // if (tmp == null) goto: ifnull
                il.Emit(OpCodes.Ldloc, tmp);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Beq, ifnull);

                if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                {
                    if (prop.PropertyType.IsArray)
                    {
                        var elementType = prop.PropertyType.GetElementType();

                        // var len = tmp.Length
                        var len = il.DeclareLocal(typeof(int));
                        il.Emit(OpCodes.Ldloc, tmp); // Push: tmp
                        il.Emit(OpCodes.Call, arrayLengthGetter); // Push: tmp.Length, Pop: tmp
                        il.Emit(OpCodes.Stloc, len); // Pop: tmp.Length

                        // var cloneArr = new TItem[len];
                        var cloneArr = il.DeclareLocal(elementType);
                        il.Emit(OpCodes.Ldloc, len);
                        il.Emit(OpCodes.Newarr, elementType);
                        il.Emit(OpCodes.Stloc, cloneArr);

                        // CloneArray<TItem>(tmp, cloneArr);
                        il.Emit(OpCodes.Ldarg_0); // Push: this
                        il.Emit(OpCodes.Ldloc, tmp); // Push: tmp
                        il.Emit(OpCodes.Ldloc, cloneArr); // Push: cloneArr

                        if (elementType.IsValueType || elementType == typeof(string))
                        {
                            il.Emit(OpCodes.Call, cloneValueTypeArrayMethod); // Push: base.CloneValueTypeArray(tmp, cloneArr)
                        }
                        else
                        {
                            il.Emit(OpCodes.Call, cloneArrayMethod.MakeGenericMethod(elementType)); // Push: base.CloneArray<TItem>(tmp, cloneArr)
                        }

                        // clone.Prop = cloneArr;
                        il.Emit(OpCodes.Ldarg_2); // Push: clone
                        il.Emit(OpCodes.Ldloc, cloneArr); // Push: cloneArr
                        il.Emit(OpCodes.Callvirt, setter); // Pop: clone, cloneArr
                    }
                    else // List<>
                    {
                        var elementType = prop.PropertyType.GetGenericArguments()[0];

                        // var cloneList = new List<TItem>();
                        var cloneList = il.DeclareLocal(prop.PropertyType);
                        il.Emit(OpCodes.Newobj, prop.PropertyType.GetConstructor(Type.EmptyTypes));
                        il.Emit(OpCodes.Stloc, cloneList);

                        // CloneList<TItem>(tmp, cloneList);
                        il.Emit(OpCodes.Ldarg_0); // Push: this
                        il.Emit(OpCodes.Ldloc, tmp); // Push: tmp
                        il.Emit(OpCodes.Ldloc, cloneList); // Push: cloneList

                        if (elementType.IsValueType || elementType == typeof(string))
                        {
                            il.Emit(OpCodes.Call, cloneValueTypeListMethod); // Push: base.CloneValueTypeList(tmp, cloneList)
                        }
                        else
                        {
                            il.Emit(OpCodes.Call, cloneListMethod.MakeGenericMethod(elementType)); // Push: base.CloneList<TItem>(tmp, cloneList)
                        }

                        // clone.Prop = cloneList;
                        il.Emit(OpCodes.Ldarg_2); // Push: clone
                        il.Emit(OpCodes.Ldloc, cloneList); // Push: cloneList
                        il.Emit(OpCodes.Callvirt, setter); // Pop: clone, cloneList
                    }
                }
                else if (typeof(IDictionary).IsAssignableFrom(prop.PropertyType))
                {

                }
                else
                {
                    // clone.Prop = base.CloneProp<PropType>(tmp);
                    il.Emit(OpCodes.Ldarg_2); // Push: clone
                    il.Emit(OpCodes.Ldarg_0); // Push: this
                    il.Emit(OpCodes.Ldloc, tmp); // Push: tmp
                    il.Emit(OpCodes.Call, clonePropMethod.MakeGenericMethod(prop.PropertyType)); // Push: base.CloneProp<PropType>(tmp), Pop: original
                    il.Emit(OpCodes.Callvirt, setter); // Pop: clone, original.Prop
                }

                // ifnull:
                il.MarkLabel(ifnull);
            }

            // return;
            il.Emit(OpCodes.Ret);

            var smithType = typeBuilder.CreateType();
            return Activator.CreateInstance(smithType);
        }

        private static Type[] GetParameterTypes(this MethodInfo methodInfo)
        {
            return methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
        }
    }
}
