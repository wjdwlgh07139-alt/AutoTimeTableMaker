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
    public HashSet<Course> SelectedCourses { get; } = new HashSet<Course>(new CourseComparer());
    public bool ShowSelectedCourses { get; set; } = false;

    // --- Filter List ---
    public List<IRealtimeFilter> RealtimeFilters { get; } = new List<IRealtimeFilter> { new TimeConflictFilter() }; // 시간 충돌은 항상 적용
    public List<IFinalFilter> FinalFilters { get; } = new List<IFinalFilter>();

    // 필터 바인딩 변수
    // Realtime Filters
    public bool useLunchBreak {get; set;}
    public bool useMorningFilter {get; set;}
    public bool useNoFriday{get; set;}    
    public bool useMaxConsecutive{get; set;}
    public int maxConsecutiveHours {get; set;} = 5; // MaxConsecutiveFilter 기본값

    public bool useBreakTimeFilter {get; set;}
    public CoreDay breakTimeDay {get; set;} = CoreDay.월; // BreakTimeFilter 기본 요일 (월)
    public int breakTimeStartHour {get; set;} = 7; // BreakTimeFilter 기본 시작 시간 (점심시간 12:00에 해당하는 시간 인덱스)
    public int breakTimeEndHour {get; set;} = 9;   // BreakTimeFilter 기본 종료 시간 (점심시간 13:00에 해당하는 시간 인덱스)

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
}
