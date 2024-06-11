using System.Text;

public class RequestParser
{
  public string ReqString { get; set; }
  private readonly string[]? _reqStringChunks;
  public string Route { get; set; }
  public string Method { get; set; }
  public string Body { get; set; }
  public string? UserAgent { get; set; }
  public RequestParser(byte[] bytes)
  {
    ReqString = GetDataAsString(bytes) ?? "";
    _reqStringChunks = ReqString.Split("\r\n") ?? [];

    // Status line like GET /echo/abc HTTP/1.1
    var statusLine = _reqStringChunks[0];

    // Route from status line like /echo/abc
    Route = statusLine?.Split(" ")?[1].Trim()!;
    Method = statusLine?.Split(" ")?[0].Trim()!;
    Body = _reqStringChunks?.Last().Trim()!;

    // Get the user agent
    UserAgent = GetHeader("User-Agent");

    Console.WriteLine("Route: {0}", Route);
    Console.WriteLine("Method: {0}", Method);
    Console.WriteLine("Body: {0}", Body);
    Console.WriteLine("UserAgent: {0}", UserAgent);
  }

  private static string GetDataAsString(byte[] bytes)
  {
    return Encoding.UTF8.GetString(bytes).Trim('\0') ?? "";
  }

  public bool HasHeader(string headerKey)
  {
    return _reqStringChunks?.Where(chunk => chunk
      .StartsWith($"{headerKey}:", StringComparison.CurrentCultureIgnoreCase))
      .FirstOrDefault("")
      .Trim() != "";
  }

  public string? GetHeader(string headerKey)
  {
    if (!HasHeader(headerKey)) return "";

    string header = _reqStringChunks?.Where(chunk => chunk
      .StartsWith($"{headerKey}:", StringComparison.CurrentCultureIgnoreCase))
      .First()!;

    return header.Replace($"{headerKey}:", "").Trim();
  }
}