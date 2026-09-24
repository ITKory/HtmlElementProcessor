using Dapper;
using Npgsql;

namespace HtmlElementProcessor.Services;

public sealed class PostgresElementStore(NpgsqlDataSource dataSource): IElementStore
{
	public async Task InitializeAsync(CancellationToken cancellationToken)
	{
		const string sql = """
			CREATE TABLE IF NOT EXISTS elements (
				id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
				attribute_value text,
				html_code text
			);
			""";

		await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
		await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
	}

	public async Task SaveAsync(IReadOnlyList<(string AttributeValue, string Html)> elements, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (elements.Count == 0) return;

		const string sql = """
			INSERT INTO elements (attribute_value, html_code)
			VALUES (@AttributeValue, @Html);
			""";

		await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
		await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

		var parameters = elements.Select(element => new { element.AttributeValue, element.Html });

		await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken));
		await transaction.CommitAsync(cancellationToken);
	}
}
