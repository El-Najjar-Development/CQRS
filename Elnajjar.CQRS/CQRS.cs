using Elnajjar.CQRS.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Elnajjar.CQRS
{
	public class CQRS(IServiceProvider serviceProvider) : ICQRS
	{
		IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

		public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
		{
			return SendRequest<ICommand<TResponse>, TResponse>(command, cancellationToken);
		}

		public Task Send(ICommand command, CancellationToken cancellationToken = default)
		{
			return SendRequest<ICommand, object>(command, cancellationToken);
		}

		public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
		{
			return SendRequest<IQuery<TResponse>, TResponse>(query, cancellationToken);
		}

		public Task Publish(INotification notification, CancellationToken cancellationToken = default)
		{
			var notificationType = notification.GetType();
			var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

			var handlers = _serviceProvider.GetServices(handlerType);
			var tasks = handlers
				.Select(handler =>
				{
					var method = handlerType.GetMethod("Handle");
					return (Task)method!.Invoke(handler, [ notification, cancellationToken ])!;
				});

			return Task.WhenAll(tasks);
		}

		private Task<TResponse> SendRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
		{
			var requestType = request!.GetType();

			Type handlerInterfaceType;
			if (request is ICommand<TResponse>)
				handlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(requestType, typeof(TResponse));
			else if (request is ICommand)
				handlerInterfaceType = typeof(ICommandHandler<>).MakeGenericType(requestType);
			else if (request is IQuery<TResponse>)
				handlerInterfaceType = typeof(IQueryHandler<,>).MakeGenericType(requestType, typeof(TResponse));
			else
				throw new InvalidOperationException($"Request of type {requestType.Name} is not supported.");

			var decorators = _serviceProvider
				.GetServices<ICQRSDecorators<TRequest, TResponse>>()
				.ToArray();

			var handler = _serviceProvider.GetService(handlerInterfaceType);
			if (handler == null)
				throw new InvalidOperationException($"No handler registered for {requestType.Name}");

			RequestHandlerDelegate<TResponse> handlerDelegate = () =>
			{
				var method = handlerInterfaceType.GetMethod("Handle");
				return (Task<TResponse>)method!.Invoke(handler, [ request, cancellationToken ])!;
			};

			var pipeline = decorators.Reverse()
				.Aggregate(handlerDelegate,
					(next, decorator) => () => decorator.Handle(request, next, cancellationToken));

			return pipeline();
		}
	}
}
