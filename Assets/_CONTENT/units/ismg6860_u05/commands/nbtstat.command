{
  "command": "nbtstat",
  "output": "Displays protocol statistics and current TCP/IP connections using NBT\n(NetBIOS over TCP/IP).\n\nNBTSTAT [-a RemoteName] [-A IP address] [-c] [-n] [-r] [-s] [-S]\n\n  -a  (adapter status) Lists the remote machine's name table given its name\n  -A  (Adapter status) Lists the remote machine's name table given its IP address\n  -c  (cache)          Lists NBT's cache of remote machine names and IP addresses\n  -n  (names)          Lists local NetBIOS names\n  -r  (resolved)       Lists names resolved by broadcast and via WINS",
  "errorSub": "Host not found. Check the address and try 'nbtstat' for usage.",
  "subcommands": [
    {
      "subcommand": "-A 10.30.14.52",
      "output": "Local Area Connection:\nNode IpAddress: [10.30.14.71] Scope Id: []\n\n           NetBIOS Remote Machine Name Table\n\n       Name               Type         Status\n    ---------------------------------------------\n    FORGE-HIST01   <00>  UNIQUE      Registered\n    FORGE-HIST01   <20>  UNIQUE      Registered\n    HALLOWAY       <00>  GROUP       Registered\n\n    MAC Address = 00-1B-21-6C-4E-A2"
    },
    {
      "subcommand": "-c",
      "output": "Local Area Connection:\nNode IpAddress: [10.30.14.71] Scope Id: []\n\n               NetBIOS Remote Cache Name Table\n\n    Name              Type       Host Address    Life [sec]\n    ------------------------------------------------------------\n    FORGE-HIST01 <20> UNIQUE     10.30.14.52     540\n    HAL-DC01     <20> UNIQUE     10.30.14.10     600"
    }
  ]
}
