namespace BuildingBlocks.Exceptions;

/// <summary>Thrown when a request conflicts with existing state (e.g. duplicate email).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
