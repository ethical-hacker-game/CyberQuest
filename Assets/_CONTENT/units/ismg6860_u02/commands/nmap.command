{
  "command": "nmap",
  "isTool": true,
  "output": "Nmap lab wrapper. Use the provided subcommands for the Asterlink scan window.",
  "errorSub": "Unknown nmap lab command. Try -sn 10.42.18.0/24, -sS 10.42.18.73, -sV 10.42.18.73, -O 10.42.18.73, -sU 10.42.18.73, or -6 -sn fd42:18::/64.",
  "subcommands": [
    {
      "subcommand": "-sn 10.42.18.0/24",
      "output": "Starting Nmap 7.94 at 22:08\nNmap scan report for jump-01.asterlink.local (10.42.18.10)\nHost is up (0.0011s latency).\nNmap scan report for portal-dmz.asterlink.local (10.42.18.20)\nHost is up (0.0027s latency).\nNmap scan report for signage-api.asterlink.local (10.42.18.42)\nHost is up (0.0040s latency).\nNmap scan report for ECHO-73 (10.42.18.73)\nHost is up (0.0062s latency).\nNmap done: 256 IP addresses (4 hosts up) scanned in 13.42 seconds"
    },
    {
      "subcommand": "-sS 10.42.18.73",
      "output": "Starting Nmap 7.94 SYN scan against 10.42.18.73\nNmap scan report for ECHO-73 (10.42.18.73)\nHost is up (0.0062s latency).\nPORT     STATE    SERVICE\n22/tcp   closed   ssh\n443/tcp  open     https\n8080/tcp open     http-proxy\n9100/tcp filtered jetdirect\n\nOpen service needing review: 8080/tcp"
    },
    {
      "subcommand": "-sV 10.42.18.73",
      "output": "Starting Nmap 7.94 service scan against 10.42.18.73\nPORT     STATE    SERVICE       VERSION\n443/tcp  open     ssl/http      DeviceControl API 2.1\n8080/tcp open     http          RailGate Diagnostics 3.4.7\n9100/tcp filtered jetdirect\nService Info: Device: vendor diagnostics appliance; Hostname: ECHO-73"
    },
    {
      "subcommand": "-O 10.42.18.73",
      "output": "OS detection report for ECHO-73\nDevice type: specialized\nRunning: Linux 4.X\nOS CPE: cpe:/o:linux:linux_kernel:4\nOS details: Embedded Linux appliance, confidence 87%\nNetwork Distance: 2 hops\nObserved TTL: 64\nArchitecture hint: ARM appliance"
    },
    {
      "subcommand": "-sU 10.42.18.73",
      "output": "UDP scan report for ECHO-73\nPORT    STATE         SERVICE\n123/udp closed        ntp\n161/udp open|filtered snmp\n500/udp filtered      isakmp\n\nNote: open|filtered means the scanner cannot distinguish a quiet open service from a filtered response."
    },
    {
      "subcommand": "-6 -sn fd42:18::/64",
      "output": "Starting Nmap 7.94 IPv6 discovery\nNmap scan report for echo-73-v6 (fd42:18::73)\nHost is up (0.0058s latency).\nNmap done: 1 IP address (1 host up) scanned in 2.02 seconds"
    }
  ]
}
