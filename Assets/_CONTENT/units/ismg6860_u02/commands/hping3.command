{
  "command": "hping3",
  "isTool": true,
  "output": "hping3 lab wrapper for controlled packet-response examples.",
  "errorSub": "Unknown hping3 lab command. Use the provided SYN, ACK, FIN, ICMP, or fragmentation examples.",
  "subcommands": [
    {
      "subcommand": "-S -p 8080 10.42.18.73",
      "output": "HPING 10.42.18.73 (jump0 10.42.18.73): S set, 40 headers + 0 data bytes\nlen=46 ip=10.42.18.73 ttl=64 DF id=18073 sport=8080 flags=SA seq=0 win=64240 rtt=6.1 ms\nInterpretation: SYN/ACK from 8080/tcp, service is open."
    },
    {
      "subcommand": "-A -p 8080 10.42.18.73",
      "output": "HPING 10.42.18.73: A set\nno response after 3 probes\nInterpretation: filtered by a stateful firewall or policy device."
    },
    {
      "subcommand": "-F -p 8080 10.42.18.73",
      "output": "HPING 10.42.18.73: F set\nno response after 3 probes\nInterpretation: ambiguous open|filtered behavior; corroborate with SYN and service scan."
    },
    {
      "subcommand": "--icmp 10.42.18.73",
      "output": "ICMP Echo Reply from 10.42.18.73: icmp_seq=0 ttl=64 time=5.8 ms\nICMP Echo Reply from 10.42.18.73: icmp_seq=1 ttl=64 time=6.0 ms"
    },
    {
      "subcommand": "-S -f -p 443 10.42.18.73",
      "output": "HPING 10.42.18.73: S set, fragmented\nno response after 3 probes\nFirewall note: fragmented probes are dropped and logged under FW-2871. This lab uses the example only to understand defensive behavior."
    }
  ]
}
