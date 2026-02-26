using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebASMTimeTabler.Core;

public class ScheduleGenerator
{
    private readonly List<List<Course>> _groups;
    private readonly List<IRealtimeFilter> _filters;

    public ScheduleGenerator(List<List<Course>> groups, List<IRealtimeFilter> filters)
    {
        _groups = groups;
        _filters = filters ?? new List<IRealtimeFilter>();
    }

    public async IAsyncEnumerable<List<Course>> GenerateAsync(int maxSchedules = int.MaxValue)
    {
        int count = 0;
        var currentSchedule = new List<Course>();
        var occupied = new HashSet<(DayOfWeek day, int hour)>();

        // 재귀 로직을 내부 로컬 함수로 정의
        await foreach (var result in Backtrack(0, currentSchedule, occupied))
        {
            yield return result;
            count++;
            if (count >= maxSchedules) break;
        }
    }

    private async IAsyncEnumerable<List<Course>> Backtrack(
        int groupIndex, 
        List<Course> currentSchedule, 
        HashSet<(DayOfWeek day, int hour)> occupied)
    {
        // 모든 그룹에서 과목을 하나씩 다 뽑았을 때 (성공)
        if (groupIndex == _groups.Count)
        {
            yield return new List<Course>(currentSchedule);
            yield break;
        }

        foreach (var course in _groups[groupIndex])
        {
            // 1. 실시간 필터 검사
            bool isValid = true;
            foreach (var filter in _filters)
            {
                if (!filter.Apply(course, occupied))
                {
                    isValid = false;
                    break;
                }
            }

            if (isValid)
            {
                // 2. 상태 추가 (Do)
                currentSchedule.Add(course);
                var addedSlots = new List<(DayOfWeek, int)>();
                foreach (var t in course.Times)
                {
                    for (int h = t.start; h <= t.end; h++)
                    {
                        if (occupied.Add((t.day, h))) 
                            addedSlots.Add((t.day, h));
                    }
                }

                // UI 스레드 점유 방지
                await Task.Yield();

                // 3. 다음 그룹으로 이동 (Recurse)
                await foreach (var subSchedule in Backtrack(groupIndex + 1, currentSchedule, occupied))
                {
                    yield return subSchedule;
                }

                // 4. 상태 복구 (Undo) - 백트래킹의 핵심
                currentSchedule.RemoveAt(currentSchedule.Count - 1);
                foreach (var slot in addedSlots)
                {
                    occupied.Remove(slot);
                }
            }
        }
    }
}

public class ScheduleService
{
    private readonly int _maxPages;
    private readonly List<IRealtimeFilter> _realtimeFilters;
    private readonly List<IFinalFilter> _finalFilters;

    public ScheduleService(int maxPages = 50, List<IRealtimeFilter>? realtimeFilters = null, List<IFinalFilter>? finalFilters = null)
    {
        _maxPages = maxPages; // 최대 반환 개수
        _realtimeFilters = realtimeFilters ?? new List<IRealtimeFilter>() { new TimeConflictFilter()};
        _finalFilters = finalFilters ?? new List<IFinalFilter>();
    }
    public async IAsyncEnumerable<List<Course>> GenerateSchedulesAsync(List<List<Course>> groupedCourses)
    {
        if (groupedCourses == null || groupedCourses.Count == 0)
            yield break;
        //var allCourses = selectedCourses;//reader.LoadSelectCourses(selectedCourses);
        /* 디리디리디디딕디리버기기깅깅
        foreach (var course in allCourses)
        {
            Console.WriteLine($"과목명: {course.Name}, 교수명: {course.Professor}");
            foreach (var time in course.Times)
            {
                Console.WriteLine(time);
            }
        }
        */
        /*
        var groupedCourses = selectedCourses.GroupBy(c => c.Name)
                      .Select(g => g.ToList())
                      .ToList();
        */
        /*
        var realtimeFilters = new List<IRealtimeFilter>
        {
            new TimeConflictFilter(),
            // new LunchBreakFilter((DayOfWeek.수, 12, 13)), 등등 추가 가능
        };
        var finalFilters = new List<IFinalFilter>
        {
            new IdleTimeFilter(2), // 최대 2시간 유휴 시간 허용
            // 필요시 추가 가능
        };
        */

        var generator = new ScheduleGenerator(groupedCourses, _realtimeFilters);

        // yield 기반 조합 생성, 최종 필터 적용 및 생성되는대로 반환
        await foreach (var schedule in generator.GenerateAsync(_maxPages))
        {
            if (_finalFilters.Any(f => !f.Apply(schedule)))
                continue;

            yield return schedule;
        };
    }
}

