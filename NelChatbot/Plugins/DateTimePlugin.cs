using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace NelChatbot.Plugins;
public sealed class DateTimePlugin
{
    [KernelFunction("today")]
    [Description("Gets today's date.")]
    public string Today() => DateTime.Now.ToString("dddd, d MMMM yyyy");

    [KernelFunction("days_between")]
    [Description("Calculates the number of days between two dates (format yyyy-MM-dd).")]
    public int DaysBetween(
        [Description("Start date, yyyy-MM-dd")] DateTime start,
        [Description("End date, yyyy-MM-dd")] DateTime end) => (int)(end.Date - start.Date).TotalDays;
}
