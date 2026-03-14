namespace Macrosharp.UserInterfaces.Reminders;

public static class ReminderRecurrenceCalculator
{
    public static DateTime? GetNextOccurrenceLocal(ReminderDefinition reminder, DateTime nowLocal, DateTime programStartLocal)
    {
        var recurrence = reminder.Recurrence;
        return recurrence.Kind switch
        {
            ReminderRecurrenceKind.Once => NextOnce(recurrence, nowLocal),
            ReminderRecurrenceKind.EveryInterval => NextInterval(recurrence, nowLocal, programStartLocal),
            ReminderRecurrenceKind.Daily => NextDaily(recurrence, nowLocal),
            ReminderRecurrenceKind.Weekly => NextWeekly(recurrence, nowLocal),
            ReminderRecurrenceKind.MonthlyDayOfMonth => NextMonthlyDay(reminder, recurrence, nowLocal),
            ReminderRecurrenceKind.MonthlyNthWeekday => NextMonthlyNthWeekday(recurrence, nowLocal),
            _ => null,
        };
    }

    private static DateTime? NextOnce(ReminderRecurrence recurrence, DateTime nowLocal)
    {
        if (!TryParseDateTime(recurrence.StartDate, recurrence.Time, out var scheduled))
        {
            return null;
        }

        return scheduled > nowLocal ? scheduled : null;
    }

    private static DateTime? NextInterval(ReminderRecurrence recurrence, DateTime nowLocal, DateTime programStartLocal)
    {
        if (!TryParseInterval(recurrence.Interval, out var interval) || interval <= TimeSpan.Zero)
        {
            return null;
        }

        var anchor = programStartLocal;
        if (recurrence.Anchor == ReminderIntervalAnchor.ExplicitStart)
        {
            if (!TryParseDateTime(recurrence.StartDate, recurrence.Time, out var explicitStart))
            {
                return null;
            }

            anchor = explicitStart;
        }

        if (nowLocal < anchor)
        {
            return anchor;
        }

        var elapsed = nowLocal - anchor;
        var ticksSince = elapsed.Ticks;
        var ticksStep = interval.Ticks;
        var multiplier = ticksSince / ticksStep + 1;
        return anchor + TimeSpan.FromTicks(multiplier * ticksStep);
    }

    private static DateTime? NextDaily(ReminderRecurrence recurrence, DateTime nowLocal)
    {
        if (!TryParseTime(recurrence.Time, out var time))
        {
            return null;
        }

        var candidate = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, time.Hours, time.Minutes, 0, DateTimeKind.Local);
        if (candidate <= nowLocal)
        {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }

    private static DateTime? NextWeekly(ReminderRecurrence recurrence, DateTime nowLocal)
    {
        if (!TryParseTime(recurrence.Time, out var time))
        {
            return null;
        }

        var days = recurrence.DaysOfWeek.Count == 0 ? new List<DayOfWeek> { nowLocal.DayOfWeek } : recurrence.DaysOfWeek;
        var today = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, time.Hours, time.Minutes, 0, DateTimeKind.Local);

        for (var i = 0; i < 14; i++)
        {
            var candidate = today.AddDays(i);
            if (!days.Contains(candidate.DayOfWeek))
            {
                continue;
            }

            if (candidate > nowLocal)
            {
                return candidate;
            }
        }

        return null;
    }

    private static DateTime? NextMonthlyDay(ReminderDefinition reminder, ReminderRecurrence recurrence, DateTime nowLocal)
    {
        if (!TryParseTime(recurrence.Time, out var time))
        {
            return null;
        }

        var targetDay = recurrence.DayOfMonth.GetValueOrDefault(nowLocal.Day);
        targetDay = Math.Clamp(targetDay, 1, 31);

        for (var monthOffset = 0; monthOffset < 24; monthOffset++)
        {
            var probe = nowLocal.AddMonths(monthOffset);
            var maxDay = DateTime.DaysInMonth(probe.Year, probe.Month);
            var day = Math.Min(targetDay, maxDay);
            var candidate = new DateTime(probe.Year, probe.Month, day, time.Hours, time.Minutes, 0, DateTimeKind.Local);

            reminder.LastTriggerWasMonthEndClamp = targetDay > maxDay;
            if (candidate > nowLocal)
            {
                return candidate;
            }
        }

        return null;
    }

    private static DateTime? NextMonthlyNthWeekday(ReminderRecurrence recurrence, DateTime nowLocal)
    {
        if (!TryParseTime(recurrence.Time, out var time))
        {
            return null;
        }

        var nth = Math.Clamp(recurrence.NthWeek.GetValueOrDefault(1), 1, 5);
        var weekday = recurrence.NthWeekday ?? DayOfWeek.Monday;

        for (var monthOffset = 0; monthOffset < 24; monthOffset++)
        {
            var probe = nowLocal.AddMonths(monthOffset);
            var candidateDay = NthWeekdayOfMonth(probe.Year, probe.Month, weekday, nth);
            if (!candidateDay.HasValue)
            {
                continue;
            }

            var candidate = new DateTime(probe.Year, probe.Month, candidateDay.Value, time.Hours, time.Minutes, 0, DateTimeKind.Local);
            if (candidate > nowLocal)
            {
                return candidate;
            }
        }

        return null;
    }

    private static int? NthWeekdayOfMonth(int year, int month, DayOfWeek weekday, int nth)
    {
        var first = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Local);
        var offset = ((int)weekday - (int)first.DayOfWeek + 7) % 7;
        var day = 1 + offset + (nth - 1) * 7;
        var maxDay = DateTime.DaysInMonth(year, month);

        return day <= maxDay ? day : null;
    }

    private static bool TryParseDateTime(string? date, string? time, out DateTime value)
    {
        value = default;
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return false;
        }

        if (!TryParseTime(time, out var parsedTime))
        {
            parsedTime = TimeSpan.Zero;
        }

        value = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, parsedTime.Hours, parsedTime.Minutes, 0, DateTimeKind.Local);
        return true;
    }

    private static bool TryParseTime(string? input, out TimeSpan value)
    {
        value = default;
        return TimeSpan.TryParseExact(input, "hh\\:mm", null, out value);
    }

    private static bool TryParseInterval(string? input, out TimeSpan value)
    {
        value = default;
        return TimeSpan.TryParseExact(input, "hh\\:mm\\:ss", null, out value);
    }
}
