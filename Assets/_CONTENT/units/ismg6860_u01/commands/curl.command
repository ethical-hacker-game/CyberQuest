{
  "command": "curl",
  "isTool": false,
  "output": "usage: curl [options...] <url>",
  "errorSub": "Invalid URL or network issue. Please check your command.",
  "subcommands": [
    {
      "subcommand": "-sI https://expose.freepress.org",
      "output": "HTTP/1.1 302 Found\nLocation: https://expose-login.freepress-check.com/login\nDate: 2025-07-14T10:12:37Z\nServer: mock-httpd/1.0"
    }
  ]
}