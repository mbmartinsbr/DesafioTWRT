using FluentValidation;
using GestaoPedidos.Application.DTOs.Clientes;

namespace GestaoPedidos.Application.Validators;

public class CriarClienteValidator : AbstractValidator<CriarClienteRequest>
{
    public CriarClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Documento)
            .NotEmpty()
            .Must(CpfCnpjValidator.Validar)
            .WithMessage("Documento inválido. Informe um CPF ou CNPJ válido.");
    }
}
