namespace Elnajjar.CQRS.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class SkipCQRSDecoratorsAttribute : Attribute
	{
		public Type DecoratorType { get; }

		public SkipCQRSDecoratorsAttribute(Type decoratorType)
		{
			DecoratorType = decoratorType;
		}
	}
}
