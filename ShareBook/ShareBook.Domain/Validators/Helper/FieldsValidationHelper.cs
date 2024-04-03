namespace ShareBook.Domain.Validators.Helper;

public static class FieldsValidationHelper
{
    public static bool OptionalFieldIsValid(string value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        return value.Length is > 0 and < 100;
    }

    public static bool OptionalFieldIsValid(string value, int minimum, int maximum)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        return value.Length > minimum && value.Length < maximum;
    }
}