﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Logging;

namespace ServiceStack
{
    public static class ExpressionUtils
    {
        private static ILog Log = LogManager.GetLogger(typeof(ExpressionUtils));

        public static PropertyInfo ToPropertyInfo(this Expression fieldExpr)
        {
            return ToPropertyInfo(fieldExpr as LambdaExpression)
                ?? ToPropertyInfo(fieldExpr as MemberExpression);
        }

        public static PropertyInfo ToPropertyInfo(LambdaExpression lambda)
        {
            if (lambda == null)
                return null;

            return lambda.Body.NodeType == ExpressionType.MemberAccess 
                ? ToPropertyInfo(lambda.Body as MemberExpression) 
                : null;
        }

        public static PropertyInfo ToPropertyInfo(MemberExpression m)
        {
            if (m == null)
                return null;

            var pi = m.Member as PropertyInfo;
            return pi;
        }

        public static string GetMemberName<T>(Expression<Func<T, object>> fieldExpr)
        {
            var m = GetMemberExpression(fieldExpr);
            if (m != null)
                return m.Member.Name;

            throw new NotSupportedException("Expected Property Expression");
        }

        public static MemberExpression GetMemberExpression<T>(Expression<Func<T, object>> expr)
        {
            var member = expr.Body as MemberExpression;
            var unary = expr.Body as UnaryExpression;
            return member ?? (unary != null ? unary.Operand as MemberExpression : null);
        }

        public static Dictionary<string, object> AssignedValues<T>(this Expression<Func<T>> expr)
        {
            if (expr == null)
                return null;

            var initExpr = expr.Body as MemberInitExpression;
            if (initExpr == null)
                return null;

            var to = new Dictionary<string, object>();
            foreach (MemberBinding binding in initExpr.Bindings)
            {
                to[binding.Member.Name] = binding.GetValue();
            }
            return to;
        }

        public static object GetValue(this MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    var assign = (MemberAssignment)binding;
                    var constant = assign.Expression as ConstantExpression;
                    if (constant != null)
                        return constant.Value;

                    try
                    {
                        return CachedExpressionCompiler.Evaluate(assign.Expression);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error compiling expression in MemberBinding.GetValue()", ex);

                        //Fallback to compile and execute
                        var member = Expression.Convert(assign.Expression, typeof(object));
                        var lambda = Expression.Lambda<Func<object>>(member);
                        var getter = lambda.Compile();
                        return getter();
                    }
            }
            return null;
        }
    }
}