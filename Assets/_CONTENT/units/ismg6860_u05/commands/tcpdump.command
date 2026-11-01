{
  "command": "tcpdump",
  "output": "tcpdump version 4.9.3\nUsage: tcpdump [ -i interface ] [ expression ]\n\nTry: tcpdump snmp",
  "errorSub": "tcpdump: syntax error in filter expression.",
  "subcommands": [
    {
      "subcommand": "snmp",
      "output": "tcpdump: listening on eth0, link-type EN10MB (Ethernet), capture size 262144 bytes\n\n14:22:07.418233 IP 10.30.14.71.51204 > 10.30.14.1.snmp:  GetNextRequest(28)  .1.3.6.1.2.1.1.1\n        0x0020:  3029 0201 0104 0a62 7264 676b 732d 726f   0).....brdgwks-ro\n        community=brdgwks-ro  version=2c\n\n14:22:07.419887 IP 10.30.14.1.snmp > 10.30.14.71.51204:  GetResponse(72)  .1.3.6.1.2.1.1.1.0=\"Halloway plant core stack, IOS 15.2(7)E4\"\n        community=brdgwks-ro  version=2c\n\n2 packets captured"
    }
  ]
}
