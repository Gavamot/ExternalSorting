namespace Domain;

public class AppException : Exception
{
    public AppException(string message) : base(message) { }
    public AppException(string message, Exception innerException) : base(message, innerException) { }
    private string InterExceptionStr => string.IsNullOrWhiteSpace(InnerException?.Message) ? "" : $"\n\r{InnerException.Message}";
    public void Print() => Console.Error.WriteLine($"{Message} {InterExceptionStr}");

}