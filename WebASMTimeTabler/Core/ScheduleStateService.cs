using WebASMTimeTabler.Core;
using CoreDay = WebASMTimeTabler.Core.DayOfWeek;

public class ScheduleStateService
{
    // --- 데이터 원본 ---
    public Course[]? AllCourses { get; set; }
    public bool IsLoaded => AllCourses != null;

    // TimeTable 페이지 상태
    public List<List<Course>> generatedSchedules = new();

    // --- Courses 페이지 상태 ---
    public string SearchTerm { get; set; } = string.Empty;
    public bool autoGenerateOnLoad { get; set; } = false;
    public HashSet<Course> SelectedCourses { get; } = new HashSet<Course>(new CourseComparer());
    public bool ShowSelectedCourses { get; set; } = false;

    // --- Filter List ---
    public List<IRealtimeFilter> RealtimeFilters { get; } = new List<IRealtimeFilter> { new TimeConflictFilter() }; // 시간 충돌은 항상 적용
    public List<IFinalFilter> FinalFilters { get; } = new List<IFinalFilter>();

    // 필터 바인딩 변수
    // Realtime Filters
    public bool useLunchBreak {get; set;}
    public bool useMorningFilter {get; set;}
    public bool useNoDayFilter {get; set;}
    public HashSet<CoreDay> NoDays { get; } = new HashSet<CoreDay>();

    public void ToggleNoDay(CoreDay day)
    {
        if (NoDays.Contains(day))
            NoDays.Remove(day);
        else
            NoDays.Add(day);
    }
    public bool useMaxConsecutive{get; set;}
    public int maxConsecutiveHours {get; set;} = 5; // MaxConsecutiveFilter 기본값

    public bool useBreakTimeFilter {get; set;}
    public CoreDay tempBreakTimeDay {get; set;} = CoreDay.월;
    public int tempBreakTimeStartHour {get; set;} = 12;
    public int tempBreakTimeEndHour {get; set;} = 13;
    public List<BreakTimeRule> BreakTimeRules { get; } = new List<BreakTimeRule>();

    public void AddBreakTimeRule(CoreDay day, int start, int end)
    {
        if (start >= end) return;
        if (!BreakTimeRules.Any(r => r.Day == day && r.StartHour == start && r.EndHour == end))
        {
            BreakTimeRules.Add(new BreakTimeRule { Day = day, StartHour = start, EndHour = end });
        }
    }

    public void RemoveBreakTimeRule(Guid id)
    {
        BreakTimeRules.RemoveAll(r => r.Id == id);
    }

    // Final Filters
    public bool useIdleTimeFilter {get; set;} = false;
    public int maxIdleHours {get; set;} = 2; // IdleTimeFilter 기본값
    
    public bool useMaxDailyCourses {get; set;} = false;
    public int maxCoursesPerDay {get; set;} = 3; // MaxDailyCoursesFilter 기본값

    public bool useTotalCreditFilter {get; set;} = false;
    public int minCredit {get; set;} = 0;
    public int maxCredit {get; set;} = 18; // TotalCreditFilter 기본값

    // 캐싱된 그룹 데이터 (시간표 생성기에서 즉시 사용)
    // List<List<Course>> 형태: [ [수학분반1, 수학분반2], [영어분반1], ... ]
    public List<List<Course>> GroupedCourses { get; private set; } = new();
    public void ToggleCourse(Course course)
    {
        if (SelectedCourses.Contains(course))
            SelectedCourses.Remove(course);
        else
            SelectedCourses.Add(course);
        // 선택이 바뀔 때마다 딱 한 번만 미리 계산
        UpdateGroups();
    }
    private void UpdateGroups()
    {
        // 1. 이름(Name)으로 그룹화하여 분반끼리 묶음
        // 2. 시간표 생성 로직이 바로 먹을 수 있게 List<List<Course>>로 변환
        GroupedCourses = SelectedCourses
            .GroupBy(c => c.Name)
            .Select(g => g.ToList())
            .ToList();
            
        OnStateChanged?.Invoke();
    }
    public event Action? OnStateChanged;
    public bool IsSelected(Course course) => SelectedCourses.Contains(course);

    public void Clear() => SelectedCourses.Clear();

    // --- 시간표 선택 보관 상태 ---
    public List<List<Course>> SelectedSchedules { get; } = new();

    public void ToggleSelectSchedule(List<Course> schedule)
    {
        var match = SelectedSchedules.FirstOrDefault(s => IsSameSchedule(s, schedule));
        if (match != null)
        {
            SelectedSchedules.Remove(match);
        }
        else
        {
            SelectedSchedules.Add(schedule);
        }
        OnStateChanged?.Invoke();
    }

    public bool IsScheduleSelected(List<Course> schedule)
    {
        return SelectedSchedules.Any(s => IsSameSchedule(s, schedule));
    }

    private bool IsSameSchedule(List<Course> s1, List<Course> s2)
    {
        if (s1.Count != s2.Count) return false;
        var set1 = s1.Select(c => c.ClassNumber).ToHashSet();
        return s2.All(c => set1.Contains(c.ClassNumber));
    }
}

public class BreakTimeRule
{
    public Guid Id { get; } = Guid.NewGuid();
    public CoreDay Day { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public string DisplayText => $"{Day}요일 {StartHour}시 ~ {EndHour}시";
}
