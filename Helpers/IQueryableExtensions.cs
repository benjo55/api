// using Microsoft.EntityFrameworkCore;
// using System;
// using System.Linq;
// using System.Linq.Expressions;
// using System.Reflection;

// namespace api.Helpers
// {
//     public static class IQueryableExtensions
//     {
//         public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName, bool desc)
//         {
//             if (string.IsNullOrWhiteSpace(propertyName))
//                 return source;

//             var property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
//             if (property == null)
//                 return source.OrderBy(x => EF.Property<object>(x, "Id")); // fallback

//             var parameter = Expression.Parameter(typeof(T), "x");
//             var propertyAccess = Expression.Property(parameter, property);
//             var lambda = Expression.Lambda(propertyAccess, parameter);

//             string methodName = desc ? "OrderByDescending" : "OrderBy";

//             var result = Expression.Call(
//                 typeof(Queryable),
//                 methodName,
//                 new Type[] { typeof(T), property.PropertyType },
//                 source.Expression,
//                 Expression.Quote(lambda)
//             );

//             return source.Provider.CreateQuery<T>(result);
//         }
//     }
// }
