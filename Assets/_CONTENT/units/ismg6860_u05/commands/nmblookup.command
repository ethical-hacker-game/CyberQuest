{
  "command": "nmblookup",
  "output": "Usage: nmblookup [-S] [-B broadcast address] name\n\nThe Samba analog of nbtstat. Use -S to request a node status alongside the name lookup.",
  "errorSub": "name_query failed to find name. Check the target and try 'nmblookup' for usage.",
  "subcommands": [
    {
      "subcommand": "-S FORGE-HIST01",
      "output": "querying FORGE-HIST01 on 10.30.14.255\n10.30.14.52 FORGE-HIST01<00>\nLooking up status of 10.30.14.52\n\n    FORGE-HIST01    <00> -         H <ACTIVE>\n    FORGE-HIST01    <20> -         H <ACTIVE>\n    HALLOWAY        <00> - <GROUP> H <ACTIVE>\n\n    MAC Address = 00-1B-21-6C-4E-A2"
    }
  ]
}
