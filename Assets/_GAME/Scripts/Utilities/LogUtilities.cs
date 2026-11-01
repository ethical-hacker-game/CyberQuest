using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using Object = System.Object;

public class LogUtilities : MonoBehaviour
{
    /// <summary>
    /// Usage example: LogNull ( ( )  => variable );
    /// </summary>
    /// <param name="obj">() => variable</param>
    public static void LogNull(Expression<Func<Object>> expression)
    {
        MemberExpression memberExpression = (MemberExpression)expression.Body;
        string variableName = memberExpression.Member.Name;
        
        Object obj = expression.Compile().Invoke();

        Debug.Log($"{variableName}==null {obj == null}");
    }

    public static void LogNull(Object obj, string variableName)
    {
        Debug.Log($"{variableName}==null {obj == null}");
    }
}
