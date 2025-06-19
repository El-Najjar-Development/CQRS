using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Elnajjar.CQRS
{
	public static class CQRSHandlerCompiler
	{
		private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>> HandlersCache =
			new ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>>();

		public static Func<object, object, CancellationToken, Task<object>> CompileHandler(Type handlerType, Type requestType, Type responseType)
		{
			return HandlersCache.GetOrAdd(handlerType, type =>
			{
				// handler parameter
				var handler = Expression.Parameter(typeof(object), "handler");
				// request parameter
				var request = Expression.Parameter(typeof(object), "request");
				// cancellation token parameter
				var cancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

				// Cast handler to concrete type
				var castHandler = Expression.Convert(handler, handlerType);
				// Cast request to concrete type
				var castRequest = Expression.Convert(request, requestType);

				// Get the Handle method
				var method = handlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) });

				// Create method call expression
				var call = Expression.Call(castHandler, method, castRequest, cancellationToken);

				// Handle Task vs Task<T>
				Expression convertResult;
				//if (responseType == typeof(Unit))
				//{
				//	// Task without result
				//	var taskResult = Expression.Property(call, "Result");
				//	// Convert to object
				//	convertResult = Expression.Convert(Expression.Constant(Unit.Value), typeof(object));
				//}
				//else
				//{
				//	// For Task<T>, access Result property of Task<T> and convert to object
				//	var resultProperty = call.Type.GetProperty("Result");
				//	var taskResult = Expression.Property(call, resultProperty);
				//	convertResult = Expression.Convert(taskResult, typeof(object));
				//}

				// Create lambda
				var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task<object>>>(
					call, handler, request, cancellationToken);

				return lambda.Compile();
			});
		}
	}
}
