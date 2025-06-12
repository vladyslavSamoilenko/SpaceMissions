namespace SpaceMissions.WebAP.DTOs;

public class Resourсe<T>
{
    public T Data { get; set; } = default!;
    public List<LinkInfo> Links { get; set; } = new(); 
}