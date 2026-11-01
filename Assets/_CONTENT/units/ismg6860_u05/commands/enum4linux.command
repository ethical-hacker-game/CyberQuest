{
  "command": "enum4linux",
  "output": "enum4linux v0.9.1\nUsage: enum4linux [options] ip\n\n  -S   Get share list via RPC\n  -U   Get userlist\n  -a   Do all simple enumeration",
  "errorSub": "Target unreachable or option not recognised. Try 'enum4linux' for usage.",
  "subcommands": [
    {
      "subcommand": "-S 10.30.14.52",
      "output": "Starting enum4linux v0.9.1 on Mon Jul 27 09:41:02 2026\n\n==========================\n|    Target Information    |\n==========================\nTarget ........... 10.30.14.52\nUsername ......... ''\nPassword ......... ''\n\n=====================================================\n|    Enumerating Workgroup/Domain on 10.30.14.52    |\n=====================================================\n[+] Got domain/workgroup name: HALLOWAY\n\n======================================\n|    Session Check on 10.30.14.52    |\n======================================\n[+] Server 10.30.14.52 allows sessions using username '', password ''\n\n==========================================\n|    Share Enumeration on 10.30.14.52    |\n==========================================\n\n    Sharename       Type      Comment\n    ---------       ----      -------\n    historian       Disk      Process historian archive\n    print$          Disk      Printer Drivers\n    IPC$            IPC       IPC Service (forge-hist01 server (Samba 4.7.6))\n    BRIDGEWORKS$    Disk      BridgeWorks Automation - integration share\n\n[+] Attempting to map shares on 10.30.14.52\n//10.30.14.52/BRIDGEWORKS$    Mapping: OK, Listing: OK"
    },
    {
      "subcommand": "-U 10.30.14.52",
      "output": "Starting enum4linux v0.9.1 on Mon Jul 27 09:44:18 2026\n\n================================================\n|    Users on 10.30.14.52 via RID cycling      |\n================================================\nS-1-5-21-1844237615-3092331701-2044718958-1001 HALLOWAY\\d.osei (Local User)\nS-1-5-21-1844237615-3092331701-2044718958-1104 HALLOWAY\\hist_reader (Local User)\nS-1-5-21-1844237615-3092331701-2044718958-1219 HALLOWAY\\svc_bridgeworks (Local User)\n\n[+] Account svc_bridgeworks: enabled, password never expires\n[+] Account svc_bridgeworks: last logon 2026-07-24 02:41:17"
    }
  ]
}
