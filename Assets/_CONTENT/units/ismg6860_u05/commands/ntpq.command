{
  "command": "ntpq",
  "output": "ntpq 4.2.8p12\nUsage: ntpq [-inp] [-c command] [host]\n\nTry: ntpq peers",
  "errorSub": "ntpq: read: Connection refused",
  "subcommands": [
    {
      "subcommand": "peers",
      "output": "ntpq -p 10.30.14.52\n\n     remote           refid      st t when poll reach   delay   offset  jitter\n==============================================================================\n*time.bridgewor .GPS.            2 u  412 1024  377    41.882 +184.204   3.117\n\nntpq -p 10.30.14.10\n\n     remote           refid      st t when poll reach   delay   offset  jitter\n==============================================================================\n*hal-dc01.hallo .LOCL.           1 u   64 1024  377     0.412   +0.008   0.044"
    }
  ]
}
