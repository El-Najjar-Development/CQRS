namespace Elnajjar.CQRS.Contracts
{
	public interface IQuery<TResponse>;

	public interface IQueryHandler<in TQuery, TResponse>
		where TQuery : IQuery<TResponse>
	{
		Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken);
	}
}
