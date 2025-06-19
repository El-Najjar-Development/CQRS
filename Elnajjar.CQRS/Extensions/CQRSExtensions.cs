using Elnajjar.CQRS.Contracts;
using Elnajjar.CQRS.Decorators;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Elnajjar.CQRS.Extensions
{
	public static class CQRSExtensions
	{
		public static IServiceCollection AddCQRS(this IServiceCollection services, params Assembly[] assemblies)
		{
			services.AddScoped<ICQRS, CQRS>();
			services.AddCommandHandlers(assemblies);
			services.AddQueryHandlers(assemblies);
			services.AddNotificationHandlers(assemblies);

			return services;
		}

		public static IServiceCollection AddCommandHandlers(this IServiceCollection services, params Assembly[] assemblies)
		{
			var commandHandlerTypes = assemblies
				.SelectMany(a => a.GetExportedTypes())
				.Where(t => !t.IsAbstract && !t.IsInterface)
				.SelectMany(t => t.GetInterfaces(), (handlerType, interfaceType) =>
					new { HandlerType = handlerType, InterfaceType = interfaceType })
				.Where(types =>
					types.InterfaceType.IsGenericType &&
					(types.InterfaceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
					 types.InterfaceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
				.ToList();

			foreach (var handler in commandHandlerTypes)
			{
				services.AddTransient(handler.InterfaceType, handler.HandlerType);
			}

			return services;
		}

		public static IServiceCollection AddQueryHandlers(this IServiceCollection services, params Assembly[] assemblies)
		{
			var queryHandlerTypes = assemblies
				.SelectMany(a => a.GetExportedTypes())
				.Where(t => !t.IsAbstract && !t.IsInterface)
				.SelectMany(t => t.GetInterfaces(), (handlerType, interfaceType) =>
					new { HandlerType = handlerType, InterfaceType = interfaceType })
				.Where(types =>
					types.InterfaceType.IsGenericType &&
					types.InterfaceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
				.ToList();

			foreach (var handler in queryHandlerTypes)
			{
				services.AddTransient(handler.InterfaceType, handler.HandlerType);
			}

			return services;
		}

		public static IServiceCollection AddNotificationHandlers(this IServiceCollection services, params Assembly[] assemblies)
		{
			var notificationHandlerTypes = assemblies
				.SelectMany(a => a.GetExportedTypes())
				.Where(t => !t.IsAbstract && !t.IsInterface)
				.SelectMany(t => t.GetInterfaces(), (handlerType, interfaceType) =>
					new { HandlerType = handlerType, InterfaceType = interfaceType })
				.Where(types =>
					types.InterfaceType.IsGenericType &&
					types.InterfaceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
				.ToList();

			foreach (var handler in notificationHandlerTypes)
			{
				services.AddTransient(handler.InterfaceType, handler.HandlerType);
			}

			return services;
		}

		public static IServiceCollection AddPipelineDecorator<TDecorator>(this IServiceCollection services)
			where TDecorator : class
		{
			var decoratorType = typeof(TDecorator);
			var interfaces = decoratorType.GetInterfaces();

			var decoratorInterface = interfaces.FirstOrDefault(i =>
				i.IsGenericType &&
				(i.GetGenericTypeDefinition() == typeof(ICQRSDecorators<,>) ||
				i.GetGenericTypeDefinition() == typeof(ICQRSDecorators<>)));

			if (decoratorInterface == null)
				throw new ArgumentException($"Type {decoratorType.Name} does not implement ICQRSDecorators<,> or ICQRSDecorators<>");

			var typeArgs = decoratorInterface.GetGenericArguments();

			services.AddTransient(typeof(ICQRSDecorators<,>).MakeGenericType(typeArgs), decoratorType);

			return services;
		}

		public static IServiceCollection AddLoggingCQRSDecorator(this IServiceCollection services)
		{
			services.AddTransient(typeof(ICQRSDecorators<,>), typeof(LoggingCQRSDecorator<,>));
			services.AddTransient(typeof(ICQRSDecorators<>), typeof(LoggingCQRSDecorator<>));
			return services;
		}
	}
}
