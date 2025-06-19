using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elnajjar.CQRS.Contracts
{
	public interface INotification;
	public interface INotificationHandler<in TNotification>
		where TNotification : INotification
	{
		Task Handle(TNotification notification, CancellationToken cancellationToken);
	}
}
