{
  "command": "netstat",
  "isTool": true,
  "output": "Usage: netstat -rn | netstat -ano",
  "errorSub": "Unknown netstat option in this lab. Try netstat -rn or netstat -ano.",
  "subcommands": [
    {
      "subcommand": "-rn",
      "output": "Kernel IP routing table\nDestination        Gateway        Genmask          Iface\n0.0.0.0            10.42.18.1     0.0.0.0          jump0\n10.42.18.0         0.0.0.0        255.255.255.0    jump0\n10.42.22.0         10.42.18.1     255.255.255.0    jump0\n10.42.30.0         -              255.255.255.0    no approved route\n\nJump host address: 10.42.18.10"
    },
    {
      "subcommand": "-ano",
      "output": "Proto  Local Address        Foreign Address      State\nTCP    10.42.18.10:49820   10.42.18.73:443     ESTABLISHED\nTCP    10.42.18.10:49822   10.42.18.20:443     TIME_WAIT\nUDP    10.42.18.10:5353    *:*                 LISTENING\n\nNo local listener is bound to the vendor maintenance range."
    }
  ]
}
