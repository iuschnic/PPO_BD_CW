using System.Text.RegularExpressions;

namespace Types;

public record PhoneNumber
{
    public string StringNumber { get; }

    public PhoneNumber(string number)
    {
        if (!IsValid(number))
            throw new ArgumentException("Incorrect number format");

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
        if (string.IsNullOrWhiteSpace(number))
            return string.Empty;

        string normalized = number.StartsWith("+") ? "+" : "";

        normalized += new string(number.Where(c => char.IsDigit(c)).ToArray());
        if (normalized.Length > 0 && normalized[0] == '8')
            normalized = "+7" + (normalized.Length > 1 ? normalized[1..] : "");

        return normalized;
    }
    public void Print()
    {
        Console.WriteLine(StringNumber);
    }
}

public record WeekDay
{
    public string StringDay { get; }
    public WeekDay(string day)
    {
        if (!IsValid(day))
            throw new ArgumentException("Incorrect day format");
        StringDay = day;
    }
    private bool IsValid(string day)
    {
        if (day == "Monday" ||  day == "Tuesday" || day == "Wednesday" || day == "Thursday" 
            || day == "Friday" || day == "Saturday" || day == "Sunday")
            return true;
        return false;
    }
    public void Print()
    {
        Console.WriteLine(StringDay);
    }
}


public record TimeOption
{
    public string StringTimeOption { get; }
    public TimeOption(string opt)
    {
        if (!IsValid(opt))
            throw new ArgumentException("Incorrect time option format");
        StringTimeOption = opt;
    }
    private bool IsValid(string opt)
    {
        if (opt == "Preffered" || opt == "Fixed" || opt == "NoMatter")
            return true;
        return false;
    }
    public void Print()
    {
        System.Console.WriteLine(StringTimeOption);
    }
}