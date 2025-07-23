using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dasof.Shared
{
#pragma warning disable S125 // Sections of code should not be commented out
    //Usage:
    //  var accessor = new PropertyAccessor<string>(() => myClient.WorkPhone);
    //  accessor.Set("12345");
    //VIR: https://stackoverflow.com/questions/1402803/passing-properties-by-reference-in-c-sharp ; https://stackoverflow.com/questions/11178864/pass-property-itself-to-function-as-parameter-in-c-sharp
#pragma warning restore S125 // Sections of code should not be commented out

    public class PropertyAccessor<T> // Base class with one type parameter
    {
        protected const string C_MethodName_SetValue = "SetValue";

        private Action<T> _Setter;
        private Func<T> _Getter;
        private MemberExpression _MemberExpression;

        //*
        public Func<T> PropertyAccessorFunc { get; protected set; }

        public PropertyAccessor(Func<T> func)
        {
            //Create Expression from Func: Func<int> func = () => 1; Expression<Func<int>> expr = Expression.Lambda<Func<int>>(Expression.Call(func.Method)); VIR: https://stackoverflow.com/questions/9377635/create-expression-from-func

            //Static method requires null instance, non-static method requires non-null instance
            //
            //The exception occurs because propertyAccessor.Method is an instance method (because it comes from a lambda that closes over an object)
            //and you must provide the correct "instance" expression to Expression.Call.
            //You cannot simply call Expression.Call(propertyAccessor.Method) without specifying the object(instance).

            PropertyAccessorFunc = func;
            
            // 1. Get the target (closure object) and the method
            object target = func.Target; // Dasof.Common.Klienti.PO_RO.AddressEditor
            var method = func.Method;

            // 2. Build an Expression representing the instance (or null for static methods)
            Expression instance = target != null ? Expression.Constant(target) : null;

            // 3. Build the MethodCallExpression
            MethodCallExpression call = Expression.Call(instance, method);

            // 4. Wrap in a lambda
            Expression<Func<T>> expr = Expression.Lambda<Func<T>>(call);

            ProcessExpressionFuncT(expr);
        }

        //*/

        public PropertyAccessor(Expression<Func<T>> expr)
        {
            ProcessExpressionFuncT(expr);
        }

        private void ProcessExpressionFuncT(Expression<Func<T>> expr)
        {
            //npr.: expr.DebugView = .Lambda #Lambda1<System.Func`1[System.String]>() { (.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Drzava }

            MemberExpression member = expr?.Body as MemberExpression; //npr.: DebugView = (.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Drzava

            UnaryExpression unary = null;
            if (member == null)
                unary = expr?.Body as UnaryExpression;
            _MemberExpression = member ?? (unary != null ? unary.Operand as MemberExpression : null);

            if (_MemberExpression != null) // -> is property
            {
                Expression instanceExpression = _MemberExpression.Expression; //npr.: DebugView = .Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient
                ParameterExpression parameter = Expression.Parameter(typeof(T));

                PropertyName = _MemberExpression?.Member?.Name; //npr.: "Drzava"

                if (_MemberExpression.Member is PropertyInfo propertyInfo)
                {
                    _Setter = Expression.Lambda<Action<T>>(Expression.Call(instanceExpression, propertyInfo.GetSetMethod(), parameter), parameter).Compile();
                    _Getter = Expression.Lambda<Func<T>>(Expression.Call(instanceExpression, propertyInfo.GetGetMethod())).Compile();
                }
                else if (_MemberExpression.Member is FieldInfo fieldInfo)
                {
                    _Setter = Expression.Lambda<Action<T>>(Expression.Assign(_MemberExpression, parameter), parameter).Compile();
                    _Getter = Expression.Lambda<Func<T>>(Expression.Field(instanceExpression, fieldInfo)).Compile();
                }
            }
            else // (_MemberExpression == null)
            {
                var method = expr?.Body as MethodCallExpression;
                if (method != null) //npr.: .Call ((.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Dodatno).GetValue("HIST")
                {
                    // Compile the original lambda for the getter
                    _Getter = expr.Compile();

                    #region SetValue method

                    // Try to deduce the corresponding SetValue method
                    Expression objExpr = method.Object; //npr.: (.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Dodatno
                    LambdaExpression methodObjLambda = Expression.Lambda(objExpr); //npr.: .Lambda #Lambda1<System.Func`1[Dasof.BusinesLogic.DodatniPodatki]>() { (.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Dodatno }
                    object methodObj = methodObjLambda.Compile().DynamicInvoke(); //npr.: Count = 163	object {Dasof.BusinesLogic.DodatniPodatki}

                    MethodInfo getMethod = method.Method; //npr.: GetValue
                    Type declaringType = getMethod.DeclaringType; //npr.: DodatniPodatki

                    // Try to find SetValue<T>(string, T)
                    MethodInfo setValueMethod = declaringType
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public) // | BindingFlags.NonPublic
                        .FirstOrDefault(m =>
                            m.Name == C_MethodName_SetValue && m.IsGenericMethodDefinition &&
                            m.GetParameters().Length == 2 &&
                            m.GetParameters()[0].ParameterType == typeof(string) &&
                            m.GetGenericArguments().Length == 1);

                    if (setValueMethod != null) //npr.: FullName = Dasof.BusinesLogic.DodatniPodatki.SetValue[T](System.String, T)
                    {
                        MethodInfo genericSetValue = setValueMethod.MakeGenericMethod(typeof(T)); //npr.: Name = "SetValue"
                        object[] setArgs = new object[2];

                        // First argument is the string property name argument from the original GetValue call
                        //npr.: method = .Call ((.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Dodatno).GetValue("HIST")
                        Expression propNameExpr = method.Arguments[0]; //npr.: "HIST"
                        object propName = Expression.Lambda(propNameExpr).Compile().DynamicInvoke(); //npr.: "HIST" ali "HIT2"

                        _Setter = value =>
                        {
                            setArgs[0] = propName;
                            setArgs[1] = value;
                            genericSetValue.Invoke(methodObj, setArgs);
                        };

                        // store property name
                        PropertyName = propName as string;
                        MethodName = method.Method.Name; // "GetValue"
                    }
                    else
                    {
                        // No setter found
                        _Setter = _ => throw new InvalidOperationException("PropertyAccessor: Setter for method call not found.");
                        PropertyName = method.Method.Name;
                    }

                    #endregion
                }
            }
        }

        public string PropertyName { get; protected set; } //npr.: _MemberExpression?.Member?.Name -> "Drzava"

        public string MethodName { get; protected set; }

        public void Set(T value) => _Setter(value);

        public T Get() => _Getter();
    }

    /// <summary>
    /// PropertyAccessor for accessing named properties via DodatniPodatki.
    /// </summary>
    /// <typeparam name="T">Type of the property value.</typeparam>
    /// <typeparam name="TOwner">Type of the DodatniPodatki instance.</typeparam>
    public class PropertyAccessor<T, TOwner> : PropertyAccessor<T> // Derived class with a second type parameter
    {
        public PropertyAccessor(Expression<Func<T>> expr) : base(expr)
        {
        }

        /// <summary>
        /// Constructs accessor with property name, using expression trees for compile-time safety.
        /// </summary>
        public PropertyAccessor(string propertyName) : base(expr: null)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            PropertyName = propertyName;

            var paramOwner = Expression.Parameter(typeof(TOwner), "owner");
            var paramName = Expression.Constant(propertyName, typeof(string));

            //You get "Ambiguous match found" because typeof(TOwner).GetMethod("GetValue") finds multiple overloads or generic definitions matching "GetValue".
            //To fix this, you must specify the method’s parameter types so that the correct generic method is selected.

            #region Getter

            //// Use reflection to call GetValue<T>(string name)
            //Func<object> dodatniPodatkiGetter = null; object _dodatniPodatki = dodatniPodatkiGetter()
            //var method = _dodatniPodatki.GetType().GetMethod("GetValue")?.MakeGenericMethod(typeof(T))
            //return (T)method.Invoke(_dodatniPodatki, new object[] { propertyName })

            // Find GetValue<T>(string name)
            MethodInfo getValueMethod = typeof(TOwner).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name == "GetValue" && m.IsGenericMethodDefinition)
                .Single(m =>
                {
                    var ps = m.GetParameters();
                    return ps.Length == 1 && ps[0].ParameterType == typeof(string);
                });

            // Getter: (owner) => owner.GetValue<T>(propertyName)
            MethodCallExpression callGetValue = Expression.Call(instance: paramOwner, method: getValueMethod.MakeGenericMethod(typeof(T)), arguments: paramName);
            Getter = Expression.Lambda<Func<TOwner, T>>(callGetValue, paramOwner).Compile();

            #endregion

            #region Setter

            //// Use reflection
            //var setMethod = _dodatniPodatki.GetType().GetMethod("SetValue")
            //_Setter = (value) => { var genericSetMethod = setMethod.MakeGenericMethod(typeof(T)); genericSetMethod.Invoke(_dodatniPodatki, new object[] { _propertyName, value }); }

            // Find SetValue<T>(string name, T value)
            var setValueMethod = typeof(TOwner).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name == C_MethodName_SetValue && m.IsGenericMethodDefinition)
            .Single(m =>
            {
                var ps = m.GetParameters();
                return ps.Length == 2
                    && ps[0].ParameterType == typeof(string)
                    && ps[1].ParameterType.IsGenericParameter;
            });

            // Setter: (owner, value) => owner.SetValue<T>(propertyName, value)
            ParameterExpression paramValue = Expression.Parameter(typeof(T), "value");
            MethodCallExpression callSetValue = Expression.Call(instance: paramOwner, method: setValueMethod.MakeGenericMethod(typeof(T)), arg0: paramName, arg1: paramValue);
            Setter = Expression.Lambda<Action<TOwner, T>>(callSetValue, paramOwner, paramValue).Compile();

            #endregion
        }

        /// <summary>
        /// Strongly-typed getter delegate.
        /// </summary>
        public Func<TOwner, T> Getter { get; }

        /// <summary>
        /// Strongly-typed setter delegate.
        /// </summary>
        public Action<TOwner, T> Setter { get; }

        /// <summary>
        /// Gets the property value from the owner.
        /// </summary>
        public T Get(TOwner owner) => Getter(owner);

        /// <summary>
        /// Sets the property value on the owner.
        /// </summary>
        public void Set(TOwner owner, T value) => Setter(owner, value);
    }
}
