using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace Types;

public record PhoneNumber
{
    public string StringNumber { get; }

    public PhoneNumber(string number)
    {
        if (!IsValid(number))
            throw new ArgumentException("Неверный формат номера телефона");

        StringNumber = Normalize(number);
    }

    private bool IsValid(string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return false;

        var regex = new Regex(@"^(\+7|8)[0-9]{10}$");
        return regex.IsMatch(Normalize(number));
    }

    private string Normalize(string number)
    {
        string normalized = number.StartsWith("+") ? "+" : "";

        normalized += new string(number.Where(c => char.IsDigit(c)).ToArray());
        if (normalized.Length > 0 && normalized[0] == '8')
            normalized = "+7" + (normalized.Length > 1 ? normalized[1..] : "");

        return normalized;
    }
    public override string ToString() { return StringNumber; }
}

public enum TimeOption
{
    Preffered,
    Fixed,
    NoMatter
}