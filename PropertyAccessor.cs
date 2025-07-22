using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;

namespace Dasof.Shared
{
    //Usage:
    //  var accessor = new PropertyAccessor<string>(() => myClient.WorkPhone);
    //  accessor.Set("12345");
    //VIR: https://stackoverflow.com/questions/1402803/passing-properties-by-reference-in-c-sharp ; https://stackoverflow.com/questions/11178864/pass-property-itself-to-function-as-parameter-in-c-sharp

    public class PropertyAccessor<T> // Base class with one type parameter
{
    private readonly Action<T> _Setter;
    private readonly Func<T> _Getter;
    private readonly MemberExpression _MemberExpression;

    public readonly Func<T, string> _GetterM;

    public PropertyAccessor(Expression<Func<T>> expr)
    {
        //npr.: expr.DebugView = .Lambda #Lambda1<System.Func`1[System.String]>() { (.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Drzava }

        MemberExpression member = expr?.Body as MemberExpression; //npr.: DebugView = (.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Drzava

        UnaryExpression unary = null;
        if (member == null)
            unary = expr?.Body as UnaryExpression;
        _MemberExpression = member ?? (unary != null ? unary.Operand as MemberExpression : null);

        if (_MemberExpression != null)
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
            if (method != null) // .Call ((.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Dodatno).GetValue("HIST")
            {
                MethodInfo methodInfo = method.Method; // GetValue - > public T DodatniPodatki.GetValue<T>(string name)
                Expression obj = method.Object; // (.Constant<Dasof.Common.Klienti.PO_RO.AddressEditor>(Dasof.Common.Klienti.PO_RO.AddressEditor).Klient).Dodatno
                ParameterInfo pr = methodInfo.ReturnParameter;
                ////var x = methodInfo.Invoke(obj, new[] { "HIST" });
                //object result = Expression.Lambda(method).Compile().DynamicInvoke(obj); // Parameter count mismatch. VIR: https://stackoverflow.com/questions/776442/how-to-call-the-method-from-a-methodcallexpression-in-c-sharp

                ////var methodInfo = typeof(MyType).GetMethod(nameof(MyType.MyMethod), BindingFlags.Public | BindingFlags.Static);
                //ParameterExpression parameter1 = Expression.Parameter(typeof(string), "name");
                //MethodCallExpression call = Expression.Call(obj, methodInfo, parameter1); // Static method requires null instance, non-static method requires non-null instance.
                //Expression<Func<T, string>> lambda = Expression.Lambda<Func<T, string>>(call, call.Arguments.OfType<ParameterExpression>());
                //Func<T, string> func = lambda.Compile();
                //_GetterM = lambda.Compile();
                ////var result1 = func("HIST"); // Object reference not set to an instance of an object.
            }
        }
    }

    public string PropertyName { get; protected set; } // => _MemberExpression?.Member?.Name; //npr.: "Drzava"

    public void Set(T value) => _Setter(value);

    public T Get() => _Getter();
}
}
