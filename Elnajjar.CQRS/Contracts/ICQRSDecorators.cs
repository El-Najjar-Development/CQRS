namespace Elnajjar.CQRS.Contracts
{
	public interface ICQRSDecorators<TRequest, TResponse>
	{
		Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
	}

	public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

	public interface ICQRSDecorators<TRequest>
	{
		Task Handle(TRequest request, RequestHandlerDelegate next, CancellationToken cancellationToken);
	}

	public delegate Task RequestHandlerDelegate();
}
