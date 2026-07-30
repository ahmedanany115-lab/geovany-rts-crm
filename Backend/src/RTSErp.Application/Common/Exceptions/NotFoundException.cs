namespace RTSErp.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }
}

public class UnauthorizedAccessAppException : Exception
{
    public UnauthorizedAccessAppException(string message) : base(message) { }
}
