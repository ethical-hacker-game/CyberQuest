{
  "command": "RCPT",
  "output": "usage: RCPT TO: <address>\n\nDeclares a recipient for the message in progress.",
  "errorSub": "503 5.5.1 Error: need MAIL command",
  "subcommands": [
    {
      "subcommand": "TO: svc_bridgeworks@hallowayprecision.com",
      "output": "250 2.1.5 Ok"
    },
    {
      "subcommand": "TO: bw-alerts@hallowayprecision.com",
      "output": "250 2.1.5 Ok"
    },
    {
      "subcommand": "TO: notauser@hallowayprecision.com",
      "output": "550 5.1.1 <notauser@hallowayprecision.com>: Recipient address rejected: User unknown in local recipient table"
    }
  ]
}
