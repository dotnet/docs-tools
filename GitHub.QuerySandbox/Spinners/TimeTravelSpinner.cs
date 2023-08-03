namespace GitHub.QuerySandbox.Spinners;

internal sealed class TimeTravelSpinner : Spinner
{
    public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);
    public override bool IsUnicode => true;
    public override IReadOnlyList<string> Frames => new List<string>
    {
        "🕚 ",
        "🕙 ",
        "🕘 ",
        "🕗 ",
        "🕖 ",
        "🕕 ",
        "🕔 ",
        "🕓 ",
        "🕒 ",
        "🕑 ",
        "🕐 ",
        "🕛 ",
    };
}
