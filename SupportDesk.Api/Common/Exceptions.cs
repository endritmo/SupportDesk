namespace SupportDesk.Api.Common;

// Thrown when a ticket/agent id doesn't exist — Chapter 4's middleware will map this to 404
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

// Thrown for any of the 7 business rule violations from the PDF.
// Code is the machine-readable part the PDF asks for ("tell the caller why").
public class BusinessRuleException : Exception
{
    public string Code { get; }
    public BusinessRuleException(string code, string message) : base(message)
    {
        Code = code;
    }
}