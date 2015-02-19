using System;
using System.Collections;
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
            var smithCloneMethod = typeof(Smith).GetMethod("Clone");

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

            // public override? ReturnType Method(arguments...)
            var il = typeBuilder.DefineMethod(deepCloneMethod.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
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
                }
                else if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                {

                }
                else if (typeof(IDictionary).IsAssignableFrom(prop.PropertyType))
                {

                }
                else
                {
                    var ifnull = il.DefineLabel();
                    var endif = il.DefineLabel();

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

                    // clone.Prop = base.CloneProp<PropType>(tmp);
                    il.Emit(OpCodes.Ldarg_2); // Push: clone
                    il.Emit(OpCodes.Ldarg_0); // Push: this
                    il.Emit(OpCodes.Ldloc, tmp); // Push: tmp
                    il.Emit(OpCodes.Call, clonePropMethod.MakeGenericMethod(prop.PropertyType)); // Push: base.CloneProp<PropType>(tmp), Pop: original
                    il.Emit(OpCodes.Callvirt, setter); // Pop: clone, original.Prop

                    // goto endif;
                    il.Emit(OpCodes.Br, endif);

                    // ifnull:
                    il.MarkLabel(ifnull);
                    // endIf:
                    il.MarkLabel(endif);
                }
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
