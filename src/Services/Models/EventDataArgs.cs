namespace Turbo.Maui.Services.Models;

public class EventDataArgs<T>
{
    public T Data { get; private set; }
    public EventDataArgs(T data) { Data = data; }
}