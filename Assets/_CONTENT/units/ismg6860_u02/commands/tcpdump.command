{
  "command": "tcpdump",
  "isTool": true,
  "output": "tcpdump lab capture. Use the provided filters.",
  "errorSub": "Unknown capture filter in this lab.",
  "subcommands": [
    {
      "subcommand": "icmp and net 10.42.18.0/24",
      "output": "22:08:11.102 IP 10.42.18.10 > 10.42.18.73: ICMP echo request\n22:08:11.108 IP 10.42.18.73 > 10.42.18.10: ICMP echo reply\n22:08:11.120 IP 10.42.18.10 > 10.42.18.42: ICMP echo request\n22:08:11.126 IP 10.42.18.42 > 10.42.18.10: ICMP echo reply"
    },
    {
      "subcommand": "host 10.42.18.73",
      "output": "22:21:04.012 IP 10.42.18.73.51644 > 10.42.22.41.443: Flags [S]\n22:21:04.018 IP 10.42.22.41.443 > 10.42.18.73.51644: Flags [S.]\n22:21:04.019 IP 10.42.18.73.51644 > 10.42.22.41.443: Flags [.]\nFlow note: ECHO-73 initiated TLS session to ticketing host 10.42.22.41."
    },
    {
      "subcommand": "udp or icmp",
      "output": "22:32:18.201 IP 10.42.18.10.42122 > 10.42.18.73.123: UDP\n22:32:18.207 IP 10.42.18.73 > 10.42.18.10: ICMP 10.42.18.73 udp port 123 unreachable (type 3 code 3)\n22:32:19.410 IP 10.42.18.10.42125 > 10.42.18.73.161: UDP\n22:32:21.410 no response observed for 161/udp within retry window"
    },
    {
      "subcommand": "ip6",
      "output": "22:39:58.774 IP6 fe80::1 > ff02::1: ICMP6 router advertisement, prefix fd42:18::/64\n22:40:00.318 IP6 fd42:18::20.51120 > fd42:18::1.53: AAAA? echo-73-v6\n22:40:00.322 IP6 fd42:18::1.53 > fd42:18::20.51120: AAAA echo-73-v6 fd42:18::73\n22:40:04.142 IP6 fd42:18::10 > fd42:18::73: ICMP6 echo request\n22:40:04.147 IP6 fd42:18::73 > fd42:18::10: ICMP6 echo reply\nFlow note: the v6 prefix carries the same appliance the v4 sweep logged as ECHO-73."
    },
    {
      "subcommand": "ip6 and host fd42:18::73",
      "output": "22:40:02.001 IP6 fe80::1 > ff02::1: ICMP6 router advertisement, prefix fd42:18::/64\n22:40:04.142 IP6 fd42:18::10 > fd42:18::73: ICMP6 echo request\n22:40:04.147 IP6 fd42:18::73 > fd42:18::10: ICMP6 echo reply"
    }
  ]
}
