using FluentValidation;
using HtmlElementProcessor.Models;

namespace HtmlElementProcessor.Validators;

public sealed class ProcessElementsRequestValidator: AbstractValidator<ProcessElementsRequest>
{
	public ProcessElementsRequestValidator()
	{
		RuleFor(request => request.Selector).NotEmpty()
			.WithErrorCode(ErrorCodes.ValidationError)
			.WithMessage("selector is required and must not be whitespace.")
			.OverridePropertyName("selector");

		RuleFor(request => request.Attribute).NotEmpty()
			.WithErrorCode(ErrorCodes.ValidationError)
			.WithMessage("attribute is required and must not be whitespace.")
			.OverridePropertyName("attribute");

		RuleFor(request => request.UrlB64).NotEmpty()
			.WithErrorCode(ErrorCodes.ValidationError)
			.WithMessage("url_b64 is required and must not be whitespace.")
			.OverridePropertyName("url_b64");

		RuleFor(request => request.EncryptedTextBytesB64).NotEmpty()
			.WithErrorCode(ErrorCodes.ValidationError)
			.WithMessage("encrypted_text_bytes_b64 is required and must not be whitespace.")
			.OverridePropertyName("encrypted_text_bytes_b64");

		RuleFor(request => request.KeyBytesB64).NotEmpty()
			.WithErrorCode(ErrorCodes.ValidationError)
			.WithMessage("key_bytes_b64 is required and must not be whitespace.")
			.OverridePropertyName("key_bytes_b64");

		RuleFor(request => request.PageB64).NotEmpty()
			.WithErrorCode(ErrorCodes.ValidationError)
			.WithMessage("page_b64 is required and must not be whitespace.")
			.OverridePropertyName("page_b64");
	}
}
