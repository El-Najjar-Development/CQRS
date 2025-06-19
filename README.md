# Elnajjar.CQRS

A lightweight, high-performance CQRS implementation with decorator-based pipeline behaviors.

## Features

- Command and Query separation pattern
- Support for commands with and without return values
- Support for queries with return values
- Support for notification pattern
- Decorator-based pipeline behaviors
- High-performance handler invocation using compiled expressions
- Easy integration with dependency injection
- Simplified registration via extension methods

## Installation

```shell
dotnet add package Elnajjar.CQRS
```

## Usage

### Define Commands and Queries

```csharp
// Command without response
public class CreateUserCommand : ICommand
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Command with response
public class RegisterUserCommand : ICommand<int>
{
    public string Username { get; set; }
    public string Password { get; set; }
}

// Query with response
public class GetUserByIdQuery : IQuery<UserDto>
{
    public int UserId { get; set; }
}

// Notification
public class UserCreatedNotification : INotification
{
    public int UserId { get; set; }
    public string Username { get; set; }
}
```

### Implement Handlers

```csharp
// Command handler (no response)
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IUserRepository _repository;
    
    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }
    
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User { Name = command.Name, Email = command.Email };
        await _repository.AddAsync(user, cancellationToken);
    }
}

// Command handler (with response)
public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, int>
{
    private readonly IUserRepository _repository;
    
    public RegisterUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<int> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User { Username = command.Username };
        await _repository.RegisterAsync(user, command.Password, cancellationToken);
        return user.Id;
    }
}

// Query handler
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _repository;
    
    public GetUserByIdQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<UserDto> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(query.UserId, cancellationToken);
        return new UserDto { Id = user.Id, Name = user.Name, Email = user.Email };
    }
}

// Notification handler
public class UserCreatedEmailNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;
    
    public UserCreatedEmailNotificationHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(notification.UserId, notification.Username, cancellationToken);
    }
}
```

### Creating Decorators

This package comes with a built-in logging decorator that you can use out of the box:

```csharp
// Built-in logging decorator
public class LoggingCQRSDecorator<TRequest, TResponse> : ICQRSDecorators<TRequest, TResponse>
{
    private readonly ILogger<LoggingCQRSDecorator<TRequest, TResponse>> _logger;

    public LoggingCQRSDecorator(ILogger<LoggingCQRSDecorator<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("{@Request}", request);
        _logger.LogInformation("Starting request");

        var response = await next();
        _logger.LogInformation("Finished request");

        return response;
    }
}
```

You can easily add this built-in decorator using the extension method:

```csharp
services.AddLoggingCQRSDecorator();
```

#### Creating Custom Decorators

You can create your own custom decorators by implementing the `ICQRSDecorators<TRequest, TResponse>` interface:

```csharp
// Example validation decorator
public class ValidationDecorator<TRequest, TResponse> : ICQRSDecorators<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationDecorator(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();
            
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        if (failures.Any())
            throw new ValidationException(failures);
        
        return await next();
    }
}
```

And register it using the `AddPipelineDecorator` extension method:

```csharp
services.AddPipelineDecorator<ValidationDecorator<,>>();
```

### Register Services

You can easily register your CQRS services in your application startup using the provided extension methods:

```csharp
// In Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register CQRS with all handlers from the specified assemblies
    services.AddCQRS(typeof(Program).Assembly);
    
    // Add pipeline decorators
    services.AddLoggingCQRSDecorator();
    
    // Add a custom decorator
    services.AddPipelineDecorator<ValidationDecorator<,>>();
    
    // Other services...
}
```

The `AddCQRS` extension method automatically registers:
- The `ICQRS` implementation
- All command handlers
- All query handlers
- All notification handlers

For more granular control, you can use the individual registration methods:

```csharp
// Register components individually
services.AddScoped<ICQRS, CQRS>();
services.AddCommandHandlers(typeof(Program).Assembly);
services.AddQueryHandlers(typeof(Program).Assembly);
services.AddNotificationHandlers(typeof(Program).Assembly);
```

### Use CQRS in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ICQRS _cqrs;
    
    public UsersController(ICQRS cqrs)
    {
        _cqrs = cqrs;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
    {
        await _cqrs.Send(command);
        return Ok();
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var userId = await _cqrs.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = userId }, null);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = new GetUserByIdQuery { UserId = id };
        var result = await _cqrs.Send(query);
        return Ok(result);
    }
    
    [HttpPost("notify")]
    public async Task<IActionResult> NotifyUserCreated(int userId, string username)
    {
        var notification = new UserCreatedNotification { UserId = userId, Username = username };
        await _cqrs.Publish(notification);
        return Ok();
    }
}
```

## License

This project is licensed under the MIT License.
