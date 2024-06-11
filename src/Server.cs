using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Byte[] bytes = new Byte[256];

try
{
  while (true)
  {
    var socket = server.AcceptSocket();
    socket.Receive(bytes);

    var requestParser = new RequestParser(bytes);

    byte[] response = requestParser.Route switch
    {
      "/user-agent" => HandleUserAgentRequest(requestParser),
      _ when requestParser.Route?.StartsWith("/echo/") == true => HandleEchoRequest(requestParser),
      _ when requestParser.Route?.StartsWith("/files/") == true => HandleFileRequest(requestParser),
      "/" => Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n"),
      _ => Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n")
    };

    socket.Send(response);
    Array.Clear(bytes, 0, bytes.Length);
  }
}
catch (SocketException e)
{
  Console.WriteLine("Error connecting: {0}", e.Message);
  throw;
}
finally
{
  server.Stop();
}

string GetValueFromArgs(string argName)
{
  for (int i = 0; i < args.Length; i++)
  {
    if (args[i] == argName)
    {
      return args[i + 1];
    }
  }
  return "";
}

byte[] HandleUserAgentRequest(RequestParser requestParser)
{
  var userAgent = requestParser.GetHeader("User-Agent");
  return Encoding.UTF8.GetBytes(
    "HTTP/1.1 200 OK\r\n" +
    $"Content-Type: text/plain\r\nContent-Length: {userAgent?.Length}\r\n\r\n" +
    userAgent
  );
}

byte[] HandleEchoRequest(RequestParser requestParser)
{

  var responseString = requestParser.Route?.Replace("/echo/", "").Trim()!;
  var encodingHeader = requestParser.GetHeader("Accept-Encoding");
  var contentEncoding = encodingHeader != null && encodingHeader.Contains("gzip");

  Console.WriteLine($"Encoding Header: {encodingHeader}, contentEncoding: {contentEncoding}");

  if (contentEncoding)
  {
    byte[] responseBytes = Encoding.UTF8.GetBytes(responseString);

    using var outputStream = new MemoryStream();
    using var gZipStream = new GZipStream(outputStream, CompressionMode.Compress, true);
    gZipStream.Write(responseBytes, 0, responseBytes.Length);
    gZipStream.Flush();
    gZipStream.Close();

    byte[] compressed = outputStream.ToArray();

    var compressedResponse = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Encoding: gzip\r\nContent-Length: {compressed.Length}\r\n\r\n";
    byte[] response = [.. Encoding.UTF8.GetBytes(compressedResponse), .. compressed];

    return response;
  }

  return Encoding.UTF8.GetBytes(
      "HTTP/1.1 200 OK\r\n" +
      $"Content-Type: text/plain\r\nContent-Length: {responseString.Length}\r\n\r\n" +
      responseString
    );
}

byte[] HandleFileRequest(RequestParser requestParser)
{
  var fileDir = GetValueFromArgs("--directory");
  var filePath = fileDir + requestParser.Route.Replace("/files/", "");

  if (requestParser.Method == "GET")
  {
    if (!File.Exists(filePath)) return Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n");

    var fileText = File.ReadAllText(filePath, Encoding.UTF8);
    return Encoding.UTF8.GetBytes(
      $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileText?.Length}\r\n\r\n{fileText?.ToString()}"
    );
  }

  if (requestParser.Method == "POST")
  {
    File.WriteAllText(filePath, requestParser.Body);
    return Encoding.UTF8.GetBytes("HTTP/1.1 201 Created\r\n\r\n");
  }

  return Encoding.UTF8.GetBytes("HTTP/1.1 405 Method Not Allowed\r\n\r\n");
}