namespace GestaoPedidos.Application.Validators;

public static class CpfCnpjValidator
{
    public static bool Validar(string documento)
    {
        var d = new string(documento.Where(char.IsDigit).ToArray());
        return d.Length == 11 ? ValidarCpf(d) : d.Length == 14 && ValidarCnpj(d);
    }

    private static bool ValidarCpf(string cpf)
    {
        if (cpf.Distinct().Count() == 1) return false;

        int[] mult1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] mult2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

        int d1 = Digito(cpf, mult1);
        int d2 = Digito(cpf[..9] + d1, mult2);

        return cpf[9] - '0' == d1 && cpf[10] - '0' == d2;
    }

    private static bool ValidarCnpj(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1) return false;

        int[] mult1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] mult2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        int d1 = Digito(cnpj, mult1);
        int d2 = Digito(cnpj[..12] + d1, mult2);

        return cnpj[12] - '0' == d1 && cnpj[13] - '0' == d2;
    }

    private static int Digito(string s, int[] multiplicadores)
    {
        var soma = s.Take(multiplicadores.Length).Select((c, i) => (c - '0') * multiplicadores[i]).Sum();
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
