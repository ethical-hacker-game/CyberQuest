{
  "command": "VRFY",
  "output": "usage: VRFY <user>\n\nAsks an SMTP server to verify a local user or address.",
  "errorSub": "501 Syntax: VRFY address",
  "subcommands": [
    {
      "subcommand": "svc_bridgeworks",
      "output": "252 2.0.0 Cannot VRFY user, but will accept message and attempt delivery"
    },
    {
      "subcommand": "notauser",
      "output": "550 5.1.1 <notauser>: Recipient address rejected: User unknown in local recipient table"
    }
  ]
}
