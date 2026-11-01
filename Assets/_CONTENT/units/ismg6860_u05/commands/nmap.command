{
  "command": "nmap",
  "output": "Nmap 7.94 ( https://nmap.org )\nUsage: nmap [Scan Type(s)] [Options] {target specification}\n\nTry:\n  nmap -sV 10.30.14.0/24\n  nmap -sT -sU -p 5060,5061 --script sip-methods,sip-enum-users 10.30.16.5",
  "errorSub": "Nmap: unrecognized target or option. Try 'nmap' on its own for usage.",
  "subcommands": [
    {
      "subcommand": "-sV 10.30.14.0/24",
      "output": "Starting Nmap 7.94 ( https://nmap.org ) at 2026-07-27 09:12 CDT\n\nNmap scan report for HAL-DC01 (10.30.14.10)\nHost is up (0.00042s latency).\nPORT    STATE SERVICE  VERSION\n53/tcp  open  domain   Microsoft DNS 10.0.17763\n389/tcp open  ldap     Microsoft Windows Active Directory LDAP\n\nNmap scan report for HAL-MAIL01 (10.30.14.25)\nHost is up (0.00051s latency).\nPORT    STATE SERVICE  VERSION\n25/tcp  open  smtp     Postfix smtpd\n\nNmap scan report for FORGE-HIST01 (10.30.14.52)\nHost is up (0.00038s latency).\nPORT     STATE SERVICE     VERSION\n22/tcp   open  ssh         OpenSSH 7.6p1 Ubuntu\n111/tcp  open  rpcbind     2-4 (RPC #100000)\n139/tcp  open  netbios-ssn Samba smbd 4.7.6\n445/tcp  open  netbios-ssn Samba smbd 4.7.6\n1099/tcp open  java-rmi    Java RMI Registry\n2049/tcp open  nfs         3-4 (RPC #100003)\nService Info: OS: Linux; CPE: cpe:/o:linux:linux_kernel\n\nNmap done: 254 IP addresses (6 hosts up) scanned in 18.44 seconds"
    },
    {
      "subcommand": "-sT -sU -p 5060,5061 --script sip-methods,sip-enum-users 10.30.16.5",
      "output": "Starting Nmap 7.94 ( https://nmap.org ) at 2026-07-27 14:03 CDT\n\nNmap scan report for HAL-VOICE01 (10.30.16.5)\nHost is up (0.00090s latency).\n\nPORT     STATE SERVICE VERSION\n5060/tcp open  sip     (SIP end point; Status: 200 OK)\n5060/udp open  sip     (SIP end point; Status: 200 OK)\n5061/tcp open  sip-tls (SIP over TLS)\n| sip-enum-users:\n|   Extensions discovered: 2001-2048\n|   User-Agent: HallowayVoice-GW/4.2\n|_  Realm: voice.hallowayprecision.com\n\nNmap done: 1 IP address (1 host up) scanned in 6.21 seconds"
    }
  ]
}
