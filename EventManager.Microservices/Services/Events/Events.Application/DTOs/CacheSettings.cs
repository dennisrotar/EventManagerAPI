namespace Events.Application.DTOs;

public class CacheSettings
{
	public TimeSpan EventByIdTtl { get; set; } = TimeSpan.FromMinutes(10);
	public TimeSpan TopEventsTtl { get; set; } = TimeSpan.FromMinutes(5);
}