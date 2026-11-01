{
  "command": "traceroute",
  "isTool": false,
  "output": "usage: traceroute <hostname>",
  "errorSub": "No hostname provided. Please specify a domain.",
  "subcommands": [
    {
      "subcommand": "www.ctf.shadowtrace.net",
      "output": "traceroute to www.ctf.shadowtrace.net (198.51.100.42), 30 hops max, 60 byte packets\r\n 1  10.0.0.1 (10.0.0.1)  0.234 ms  0.182 ms  0.167 ms\r\n 2  172.16.0.1 (172.16.0.1)  1.982 ms  1.771 ms  1.654 ms\r\n 3  edge-gw.internal.lan (203.0.113.2)  7.765 ms  7.913 ms  7.804 ms\r\n 4  core1.provider.net (198.51.100.11)  10.362 ms 10.489 ms 10.411 ms\r\n 5  transit-a.provider.net (198.51.100.21)  13.208 ms 13.315 ms 13.149 ms\r\n 6  rtr-asm-1.globaltransit.net (203.0.113.5)  20.112 ms 20.005 ms 19.934 ms\r\n 7  agg-02.globaltransit.net (198.51.100.17)  25.404 ms 25.341 ms 25.289 ms\r\n 8  peering-1.globaltransit.net (198.51.100.29)  31.721 ms 31.652 ms 31.580 ms\r\n 9  dist-rtr.globaltransit.net (198.51.100.33)  36.982 ms 37.063 ms 36.950 ms\r\n 10  app-edge.globaltransit.net (198.51.100.36)  42.117 ms 42.204 ms 42.061 ms\r\n 11  hosting-rtr.globaltransit.net (198.51.100.40)  47.550 ms 47.462 ms 47.393 ms\r\n 12  www.ctf.shadowtrace.net (198.51.100.42)  54.913 ms 54.821 ms 54.765 ms\r\n\r\ntraceroute completed."
    }
  ]
}