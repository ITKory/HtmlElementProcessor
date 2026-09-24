namespace HtmlElementProcessor.Services;

public interface IElementStore
{
	Task SaveAsync(IReadOnlyList<(string AttributeValue, string Html)> elements, CancellationToken cancellationToken);
}
