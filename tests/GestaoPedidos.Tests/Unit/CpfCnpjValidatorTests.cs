using FluentAssertions;
using GestaoPedidos.Application.Validators;

namespace GestaoPedidos.Tests.Unit;

public class CpfCnpjValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void CPF_valido_deve_retornar_true(string cpf)
        => CpfCnpjValidator.Validar(cpf).Should().BeTrue();

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("000.000.000-00")]
    [InlineData("123.456.789-00")]
    [InlineData("529.982.247-26")]
    [InlineData("abc")]
    [InlineData("")]
    public void CPF_invalido_deve_retornar_false(string cpf)
        => CpfCnpjValidator.Validar(cpf).Should().BeFalse();

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void CNPJ_valido_deve_retornar_true(string cnpj)
        => CpfCnpjValidator.Validar(cnpj).Should().BeTrue();

    [Theory]
    [InlineData("11.111.111/1111-11")]
    [InlineData("00.000.000/0000-00")]
    [InlineData("11.222.333/0001-82")]
    public void CNPJ_invalido_deve_retornar_false(string cnpj)
        => CpfCnpjValidator.Validar(cnpj).Should().BeFalse();
}
