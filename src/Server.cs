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

    var data = Encoding.UTF8.GetString(bytes) ?? "";

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
      var responseString = route.Replace("/echo/", "");
      socket.Send(Encoding.UTF8.GetBytes(
        "HTTP/1.1 200 OK\r\n" +
        $"Content-Type: text/plain\r\nContent-Length: {responseString.Length}\r\n\r\n" +
        responseString
      ));
    }
    else if (route.StartsWith("/files/"))
    {
      // Get file directory
      var fileDir = GetFileDirFromArgs();
      try
      {
        var fileText = File.ReadAllText(fileDir + route.Replace("/files/", ""), Encoding.UTF8);
        socket.Send(Encoding.UTF8.GetBytes(
          "HTTP/1.1 200 OK\r\n" +
          $"Content-Type: application/octet-stream\r\nContent-Length: {fileText?.Length}\r\n\r\n" +
          fileText
        ));
      }
      catch (Exception)
      {
        socket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
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

string GetFileDirFromArgs()
{
  for (int i = 0; i < args.Length; i++)
  {
    if (args[i] == "--directory")
    {
      return args[i + 1];
    }
  }
  return "";
}