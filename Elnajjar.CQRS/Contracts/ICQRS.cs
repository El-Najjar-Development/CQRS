namespace Elnajjar.CQRS.Contracts
{
	public interface ICQRS
	{
		Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
		Task Send(ICommand command, CancellationToken cancellationToken = default);
		Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
		Task Publish(INotification notification, CancellationToken cancellationToken = default);
	}
}
