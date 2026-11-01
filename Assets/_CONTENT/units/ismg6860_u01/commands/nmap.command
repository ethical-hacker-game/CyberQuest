{
  "command": "nmap",
  "output": "add ip address",
  "errorSub": "ip is unreachable",
  "subcommands": [
    {
      "subcommand": "-sS 198.51.100.10",
      "output": "Starting Nmap 7.80 ( https://nmap.org ) at 2024-09-29 14:30 UTC\r\nNmap scan report for 198.51.100.10\r\nHost is up (0.0031s latency).\r\nNot shown: 996 closed ports\r\nPORT    STATE SERVICE\r\n21/tcp  open  ftp\r\n22/tcp  open  ssh\r\n80/tcp  open  http\r\n443/tcp open  https"
    },
    {
      "subcommand": "10.7.3.12",
      "output": "Starting Nmap 7.80 ( https://nmap.org ) at 2024-09-29 14:30 UTC\nNmap scan report for 10.7.3.12\nHost is up (0.0031s latency).\nNot shown: 996 closed ports\nPORT    STATE SERVICE\n8084/tcp  open  http-alt"
    }
  ]
}