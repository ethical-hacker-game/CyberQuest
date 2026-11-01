{
  "command": "ntpdc",
  "output": "ntpdc 4.2.8p12\nUsage: ntpdc [-ilnps] [-c command] [host]\n\nTry: ntpdc sysinfo",
  "errorSub": "ntpdc: read: Connection refused",
  "subcommands": [
    {
      "subcommand": "sysinfo",
      "output": "ntpdc -c sysinfo 10.30.14.52\n\nsystem peer:          time.bridgeworksautomation.com\nsystem peer mode:     client\nleap indicator:       00\nstratum:              3\nroot distance:        0.04188 s\nreference time:       ec4a1f22.6d3c0000  Fri, Jul 24 2026  2:38:11.426"
    }
  ]
}
