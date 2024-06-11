using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Byte[] bytes = new Byte[256];

try
{
  while (true)
  {
    var socket = server.AcceptSocket(); // wait for client
    socket.Receive(bytes);

    var data = Encoding.UTF8.GetString(bytes).Trim('\0') ?? "";

    var reqDataChunks = data?.Split("\r\n");

    // Status line like GET /echo/abc HTTP/1.1
    var statusLine = reqDataChunks?[0];

    // Route from status line like /echo/abc
    string route = statusLine?.Split(" ")?[1].Trim()!;

    Console.WriteLine("THIS IS MY data: {0}", data);

    if (route == "/user-agent")
    {
      var userAgent = reqDataChunks?[2].Replace("User-Agent: ", "");
      socket.Send(Encoding.UTF8.GetBytes(
        "HTTP/1.1 200 OK\r\n" +
        $"Content-Type: text/plain\r\nContent-Length: {userAgent?.Length}\r\n\r\n" +
        userAgent
      ));
    }
    else if (route.StartsWith("/echo/"))
    {
      var responseString = route.Replace("/echo/", "").Trim();
      var encodingHeader = reqDataChunks?.Where(x => x.StartsWith("Accept-Encoding:")).FirstOrDefault("");
      Console.WriteLine($"Encoding Header: {encodingHeader}");

      var contentEncoding = encodingHeader?.Contains("gzip") == true;

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
        byte[] response = [..Encoding.UTF8.GetBytes(compressedResponse), ..compressed];

        socket.Send(response);
      }
      else
      {
        socket.Send(Encoding.UTF8.GetBytes(
          "HTTP/1.1 200 OK\r\n" +
          $"Content-Type: text/plain\r\nContent-Length: {responseString.Length}\r\n\r\n" +
          responseString
        ));

      }

    }
    else if (route.StartsWith("/files/"))
    {
      // Get file directory
      var fileDir = GetValueFromArgs("--directory");
      string method = statusLine?.Split(" ")?[0].Trim()!;
      var filePath = fileDir + route.Replace("/files/", "");
      Console.WriteLine($"{method} {fileDir}");

      if (method == "GET")
      {
        if (File.Exists(filePath))
        {
          var fileText = File.ReadAllText(filePath, Encoding.UTF8);
          socket.Send(Encoding.UTF8.GetBytes(
            $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileText?.Length}\r\n\r\n{fileText?.ToString()}"
          ));
        }
        else
        {
          socket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
        }
      }
      else if (method == "POST")
      {
        string requestBody = reqDataChunks?.Last().Trim()!;
        File.WriteAllText(filePath, requestBody);
        socket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 201 Created\r\n\r\n"));
      }
    }
    else if (route == "/")
    {
      socket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));
    }
    else
    {
      socket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
    }

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