﻿using Elnajjar.CQRS.Contracts;
using Microsoft.Extensions.Logging;

namespace Elnajjar.CQRS.Decorators
{
	public class LoggingCQRSDecorator<TRequest, TResponse>(
		ILogger<LoggingCQRSDecorator<TRequest, TResponse>> logger
		) : ICQRSDecorators<TRequest, TResponse>
	{
		ILogger<LoggingCQRSDecorator<TRequest, TResponse>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

		public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
		{
			using var scope = _logger.BeginScope("{@Request}", request);
			_logger.LogInformation("Starting request");

			var response = await next();
			_logger.LogInformation("Finished request");

			return response;
		}
	}

	public class LoggingCQRSDecorator<TRequest>(
		ILogger<LoggingCQRSDecorator<TRequest>> logger
		) : ICQRSDecorators<TRequest>
	{
		ILogger<LoggingCQRSDecorator<TRequest>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

		public async Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken)
		{
			using var scope = _logger.BeginScope("{@Request}", request);
			_logger.LogInformation("Starting request");

			await next();
			_logger.LogInformation("Finished request");
		}
	}
}
