namespace HtmlElementProcessor.Models;

public sealed class ProcessElementsRequest
{
	public string? Selector { get; init; }
	public string? Attribute { get; init; }
	public string? UrlB64 { get; init; }
	public string? EncryptedTextBytesB64 { get; init; }
	public string? KeyBytesB64 { get; init; }
	public string? PageB64 { get; init; }
}
