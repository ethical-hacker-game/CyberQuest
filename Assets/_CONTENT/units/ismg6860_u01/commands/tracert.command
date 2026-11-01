{
  "command": "tracert",
  "isTool": false,
  "output": "usage: tracert <hostname>",
  "errorSub": "No hostname provided. Please specify a domain.",
  "subcommands": [
    {
      "subcommand": "198.51.100.7",
      "output": "Tracing route to 198.51.100.7 over a maximum of 30 hops\r\n\r\n  1     1 ms     1 ms     1 ms  192.168.0.1\r\n  2    10 ms    11 ms    10 ms  203.0.113.1\r\n  3    35 ms    32 ms    30 ms  198.51.100.1\r\n  4    50 ms    48 ms    50 ms  192.0.2.1\r\n  5   512 ms   530 ms   508 ms  198.51.100.5\r\n  6    60 ms    58 ms    59 ms  198.51.100.7\r\n\r\nTrace complete."
    },
    {
      "subcommand": "www.ctf.shadowtrace.net",
      "output": "Tracing route to www.ctf.shadowtrace.net [198.51.100.42] over a maximum of 30 hops:\r\n  1     <1 ms    <1 ms    <1 ms  10.0.0.1\r\n  2      2 ms     2 ms     2 ms  172.16.0.1\r\n  3      8 ms    8 ms    8 ms  edge-gw.internal.lan [203.0.113.2]\r\n  4     12 ms   11 ms   12 ms  core1.provider.net [198.51.100.11]\r\n  5     15 ms   15 ms   14 ms  transit-a.provider.net [198.51.100.21]\r\n  6     23 ms   22 ms   23 ms  rtr-asm-1.globaltransit.net [203.0.113.5]\r\n  7     28 ms   28 ms   29 ms  agg-02.globaltransit.net [198.51.100.17]\r\n  8     35 ms   34 ms   35 ms  peering-1.globaltransit.net [198.51.100.29]\r\n  9     40 ms   41 ms   40 ms  dist-rtr.globaltransit.net [198.51.100.33]\r\n  10    46 ms   47 ms   46 ms  app-edge.globaltransit.net [198.51.100.36]\r\n  11    51 ms   51 ms   50 ms  hosting-rtr.globaltransit.net [198.51.100.40]\r\n  12    58 ms   57 ms   58 ms  www.ctf.shadowtrace.net [198.51.100.42]\r\n\r\nTracing complete."
    }
  ]
}