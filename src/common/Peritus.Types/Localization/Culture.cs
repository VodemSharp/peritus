namespace Peritus.Types.Localization;

public readonly record struct Culture(CultureCode Code, CultureName Name)
{
    public static readonly Culture English = new(new CultureCode("en"), new CultureName("English"));
    public static readonly Culture Ukrainian = new(new CultureCode("uk"), new CultureName("Ukrainian"));

    public static readonly Culture Default = English;
    public static readonly Culture[] Supported = [English, Ukrainian];
}
