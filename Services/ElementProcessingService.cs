using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using FluentValidation;
using HtmlElementProcessor.Models;

namespace HtmlElementProcessor.Services;

public sealed class ElementProcessingService(
	IValidator<ProcessElementsRequest> validator,
	IElementStore elementStore)
{
	private static readonly Regex EmailRegex = new(
		@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
		TimeSpan.FromSeconds(1));

	public async Task<ProcessElementsResponse> ProcessAsync(
		ProcessElementsRequest? request, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (request is null) return Error(ErrorCodes.ValidationError, "Request is required.");

			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (!validation.IsValid)
				return Error(ErrorCodes.ValidationError, string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));

			// Base64, Regex and AES operate in memory: Task.Run would only add scheduling overhead.
			string url;
			try
			{
				url = Encoding.UTF8.GetString(Convert.FromBase64String(request.UrlB64!));
			}
			catch (FormatException exception)
			{
				return Error(ErrorCodes.UrlBase64DecodeError, exception.Message);
			}

			string page;
			try
			{
				page = Encoding.UTF8.GetString(Convert.FromBase64String(request.PageB64!));
			}
			catch (FormatException exception)
			{
				return Error(ErrorCodes.PageBase64DecodeError, exception.Message);
			}

			string plainText;
			try
			{
				plainText = Decrypt(request.EncryptedTextBytesB64!, request.KeyBytesB64!);
			}
			catch (Exception exception) when (exception is FormatException or CryptographicException or ArgumentException)
			{
				return Error(ErrorCodes.DecryptionError, exception.Message);
			}

			using var document = await new HtmlParser().ParseDocumentAsync(page, cancellationToken);
			var selectedElements = document.QuerySelectorAll(request.Selector!);
			var elements = new List<(string AttributeValue, string Html)>(selectedElements.Length);

			foreach (var element in selectedElements)
			{
				cancellationToken.ThrowIfCancellationRequested();
				elements.Add((element.GetAttribute(request.Attribute!) ?? string.Empty, element.OuterHtml));
			}

			var emails = new List<string>();
			foreach (Match match in EmailRegex.Matches(page))
			{
				cancellationToken.ThrowIfCancellationRequested();
				emails.Add(match.Value);
			}

			cancellationToken.ThrowIfCancellationRequested();
			await elementStore.SaveAsync(elements, cancellationToken);

			return new ProcessElementsResponse
			{
				ElementsCount = elements.Count,
				EmailsCount = emails.Count,
				Url = url,
				DecryptedPlainText = plainText,
				ElementsAttrList = elements.Select(element => element.AttributeValue).ToList(),
				EmailsList = emails
			};
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception exception)
		{
			return Error(ErrorCodes.ProcessingError, exception.Message);
		}
	}

	private static string Decrypt(string encryptedTextBase64, string keyBase64)
	{
		var ciphertext = Convert.FromBase64String(encryptedTextBase64);
		var key = Convert.FromBase64String(keyBase64);

		if (key.Length != 32)
			throw new CryptographicException("AES-256 requires a 32-byte key.");
		if (ciphertext.Length == 0 || ciphertext.Length % 16 != 0)
			throw new CryptographicException("Ciphertext must contain complete 16-byte AES blocks.");

		using var aes = Aes.Create();
		aes.KeySize = 256;
		aes.Mode = CipherMode.ECB;
		aes.Padding = PaddingMode.None;
		aes.Key = key;
		using var decryptor = aes.CreateDecryptor();

		return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length));
	}

	private static ProcessElementsResponse Error(string code, string message) => new()
	{
		IsError = 1,
		ErrorCode = code,
		ErrorMessage = message
	};
}
