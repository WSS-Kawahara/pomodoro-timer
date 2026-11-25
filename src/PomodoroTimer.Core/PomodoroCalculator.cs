namespace PomodoroTimer.Core;

/// <summary>時刻・設定・区間の開始時刻から状態を計算する。状態を持たず、副作用もない。</summary>
public static class PomodoroCalculator
{
    /// <summary>現在の状態を計算する。</summary>
    /// <param name="now">現在時刻。</param>
    /// <param name="settings">設定値。</param>
    /// <param name="baseTime">起動した時刻、または ON にした時刻。区間の始まりより遅ければ、こちらを区間の開始時刻にする。</param>
    /// <param name="holidays">祝日判定。</param>
    public static TimerStatus Calculate(DateTime now, TimerSettings settings, DateTime? baseTime, IHolidayCalendar holidays)
    {
        var today = DateOnly.FromDateTime(now);
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || holidays.IsHoliday(today))
        {
            return TimerStatus.Idle(IdleReason.Holiday);
        }

        // 区間は「開始を含み、終了を含まない」
        var time = TimeOnly.FromDateTime(now);
        TimeOnly segmentBegin;
        TimeOnly segmentEnd;
        if (settings.WorkStart <= time && time < settings.LunchStart)
        {
            (segmentBegin, segmentEnd) = (settings.WorkStart, settings.LunchStart);
        }
        else if (settings.LunchEnd <= time && time < settings.WorkEnd)
        {
            (segmentBegin, segmentEnd) = (settings.LunchEnd, settings.WorkEnd);
        }
        else if (settings.LunchStart <= time && time < settings.LunchEnd)
        {
            return TimerStatus.Idle(IdleReason.LunchBreak);
        }
        else
        {
            return TimerStatus.Idle(IdleReason.OutsideWorkingHours);
        }

        // 区間の開始時刻 ＝「区間の始まり」と「起動・ONの時刻」の遅いほう
        var segmentStart = today.ToDateTime(segmentBegin);
        if (baseTime is { } b && b > segmentStart)
        {
            segmentStart = b;
        }

        // 基準時刻が現在より後になることは通常ないが、念のため区間外として扱う
        if (now < segmentStart)
        {
            return TimerStatus.Idle(IdleReason.OutsideWorkingHours);
        }

        var segmentEndTime = today.ToDateTime(segmentEnd);
        return CalculateInSegment(now, segmentStart, segmentEndTime, settings);
    }

    /// <summary>区間の中で、セットの何番目のフェーズにいるかを求める。</summary>
    private static TimerStatus CalculateInSegment(DateTime now, DateTime segmentStart, DateTime segmentEnd, TimerSettings settings)
    {
        var setLength = TimerSettings.WorkDuration * TimerSettings.CyclesPerSet
            + TimerSettings.ShortBreakDuration * (TimerSettings.CyclesPerSet - 1)
            + settings.LongBreakDuration;

        var elapsed = now - segmentStart;
        var setCount = elapsed.Ticks / setLength.Ticks;
        var phaseStart = segmentStart + setLength * setCount;

        for (var cycle = 1; cycle <= TimerSettings.CyclesPerSet; cycle++)
        {
            var isLastCycle = cycle == TimerSettings.CyclesPerSet;
            var breakDuration = isLastCycle ? settings.LongBreakDuration : TimerSettings.ShortBreakDuration;
            var breakPhase = isLastCycle ? TimerPhase.LongBreak : TimerPhase.ShortBreak;

            if (now < phaseStart + TimerSettings.WorkDuration)
            {
                return Running(TimerPhase.Working, cycle, phaseStart, TimerSettings.WorkDuration, now, segmentEnd);
            }

            phaseStart += TimerSettings.WorkDuration;

            if (now < phaseStart + breakDuration)
            {
                return Running(breakPhase, cycle, phaseStart, breakDuration, now, segmentEnd);
            }

            phaseStart += breakDuration;
        }

        // setCount の計算により、ここには来ない
        throw new InvalidOperationException("セット内のフェーズを特定できませんでした。");
    }

    /// <summary>残り時間は「フェーズの終了」と「区間の終了」の早いほうまで。</summary>
    private static TimerStatus Running(TimerPhase phase, int cycle, DateTime phaseStart, TimeSpan duration, DateTime now, DateTime segmentEnd)
    {
        var phaseEnd = phaseStart + duration;
        var end = phaseEnd < segmentEnd ? phaseEnd : segmentEnd;
        return new TimerStatus(phase, end - now, cycle, IdleReason.None, phaseStart);
    }
}
