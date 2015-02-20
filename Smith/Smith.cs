using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Smith
{
    public static class Smith
    {
        private static readonly Hashtable Cache = new Hashtable();
        private static readonly ModuleBuilder ModuleBuilder;

        static Smith()
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("Smith.Smiths"),
                AssemblyBuilderAccess.Run);

            var assemblyName = assemblyBuilder.GetName().Name;

            ModuleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName);
        }
        
        public static object Clone(object original, Hashtable context)
        {
            if (original == null)
            {
                return null;
            }

            if (context != null && context.ContainsKey(original))
            {
                return context[original];
            }

            return GetSmith(original.GetType(), context).Clone(original);
        }

        private static ISmith GetSmith(Type type, Hashtable context)
        {
            ISmith smith;

            if (Cache.ContainsKey(type))
            {
                smith = (ISmith)Cache[type];
            }
            else
            {
                smith = CreateSmith(type);
                Cache.Add(type, smith);
            }

            if (context != null)
            {
                smith.SetContext(context);
            }

            return smith;
        }

        private static ISmith CreateSmith(Type type)
        {
            if (type.IsValueType || type == typeof(string))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    
                }
                return DefaultSmith.Instance;
            }

            var typeName = type.Name + "Smith2";

            var baseType = typeof(SmithBase);

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

            var cloneMethod = baseType.GetMethod("Clone", BindingFlags.Instance | BindingFlags.Public);
            var clonePropMethod = baseType.GetMethod("CloneProp", BindingFlags.Instance | BindingFlags.NonPublic);
            var cloneListMethod = baseType.GetMethod("CloneList", BindingFlags.Instance | BindingFlags.NonPublic);
            var cloneArrayMethod = baseType.GetMethod("CloneArray", BindingFlags.Instance | BindingFlags.NonPublic);
            var cloneDictionaryMethod = baseType.GetMethod("CloneDictionary", BindingFlags.Instance | BindingFlags.NonPublic);
            var addToContextMethod = baseType.GetMethod("AddToContext", BindingFlags.Instance | BindingFlags.NonPublic);

            var arrayLengthGetter = typeof(Array).GetProperty("Length").GetGetMethod();

            // protected override void DeepClone(T original, T clone)
            var il = typeBuilder
                .DefineMethod(cloneMethod.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    cloneMethod.ReturnType,
                    cloneMethod.GetParameterTypes())
                .GetILGenerator();

            var clone = il.DeclareLocal(type);
            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc, clone);

            // AddToContext(original, clone);
            il.Emit(OpCodes.Ldarg_0); // Push: this
            il.Emit(OpCodes.Ldarg_1); // Push: original
            il.Emit(OpCodes.Ldloc, clone); // Push: clone
            il.Emit(OpCodes.Call, addToContextMethod); // Push: base.AddToContext(tmp, cloneArr)

            foreach (var prop in type.GetProperties())
            {
                var getter = prop.GetGetMethod();
                var setter = prop.GetSetMethod();

                if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                {
                    // clone.Prop = original.Prop;
                    il.Emit(OpCodes.Ldloc, clone); // Push: clone
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

                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
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

                        // CloneArray(tmp, cloneArr);
                        il.Emit(OpCodes.Ldarg_0); // Push: this
                        il.Emit(OpCodes.Ldloc, tmp); // Push: tmp
                        il.Emit(OpCodes.Ldloc, cloneArr); // Push: cloneArr
                        il.Emit(OpCodes.Call, cloneArrayMethod); // Push: base.CloneArray(tmp, cloneArr)

                        // clone.Prop = cloneArr;
                        il.Emit(OpCodes.Ldloc, clone); // Push: clone
                        il.Emit(OpCodes.Ldloc, cloneArr); // Push: cloneArr
                        il.Emit(OpCodes.Callvirt, setter); // Pop: clone, cloneArr
                    }
                    else
                    {
                        // var cloneList = new ListImpl();
                        var cloneList = il.DeclareLocal(prop.PropertyType);
                        il.Emit(OpCodes.Newobj, prop.PropertyType.GetConstructor(Type.EmptyTypes));
                        il.Emit(OpCodes.Stloc, cloneList);

                        // CloneList(tmp, cloneList);
                        il.Emit(OpCodes.Ldarg_0); // Push: this
                        il.Emit(OpCodes.Ldloc, tmp); // Push: tmp
                        il.Emit(OpCodes.Ldloc, cloneList); // Push: cloneList

                        if (typeof(IDictionary).IsAssignableFrom(prop.PropertyType))
                        {
                            il.Emit(OpCodes.Call, cloneDictionaryMethod); // Push: base.CloneDictionary(tmp, cloneDic)
                        }
                        else if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                        {
                            il.Emit(OpCodes.Call, cloneListMethod); // Push: base.CloneList(tmp, cloneList)
                        }
                        else
                        {
                            throw new NotSupportedException("Unsupported IEnumerable Implementation: " + prop.PropertyType);
                        }
                        
                        // clone.Prop = cloneList;
                        il.Emit(OpCodes.Ldloc, clone); // Push: clone
                        il.Emit(OpCodes.Ldloc, cloneList); // Push: cloneList
                        il.Emit(OpCodes.Callvirt, setter); // Pop: clone, cloneList
                    }
                }
                else
                {
                    // clone.Prop = base.CloneProp(tmp);
                    il.Emit(OpCodes.Ldloc, clone); // Push: clone
                    il.Emit(OpCodes.Ldarg_0); // Push: this
                    il.Emit(OpCodes.Ldloc, tmp); // Push: tmp
                    il.Emit(OpCodes.Call, clonePropMethod); // Push: base.CloneProp(tmp), Pop: original
                    il.Emit(OpCodes.Callvirt, setter); // Pop: clone, original.Prop
                }

                // ifnull:
                il.MarkLabel(ifnull);
            }

            // return clone;
            il.Emit(OpCodes.Ldloc, clone); // Push: clone
            il.Emit(OpCodes.Ret);

            var smithType = typeBuilder.CreateType();
            return (ISmith)Activator.CreateInstance(smithType);
        }

        private static Type[] GetParameterTypes(this MethodInfo methodInfo)
        {
            return methodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();
        }
    }
}
