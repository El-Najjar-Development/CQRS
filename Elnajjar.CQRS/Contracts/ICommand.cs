namespace Elnajjar.CQRS.Contracts
{
	public interface ICommand;
	public interface ICommand<TResponse>;

	public interface ICommandHandler<in TCommand>
		where TCommand : ICommand
	{
		Task Handle(TCommand command, CancellationToken cancellationToken);
	}

	public interface ICommandHandler<in TCommand, TResponse>
		where TCommand : ICommand<TResponse>
	{
		Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
	}
}
