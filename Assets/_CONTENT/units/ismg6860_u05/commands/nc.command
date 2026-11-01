{
  "command": "nc",
  "output": "usage: nc [-46lnruvz] [hostname] [port]\n\nExample: nc mail.hallowayprecision.com 25",
  "errorSub": "nc: connect failed: Connection refused",
  "subcommands": [
    {
      "subcommand": "mail.hallowayprecision.com 25",
      "output": "Connection to mail.hallowayprecision.com 25 port [tcp/smtp] succeeded!\n220 mail.hallowayprecision.com ESMTP Postfix (Ubuntu)\n\nSession open. Greet the server with EHLO to see what it supports."
    }
  ]
}
