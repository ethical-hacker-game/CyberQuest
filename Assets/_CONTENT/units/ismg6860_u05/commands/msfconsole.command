{
  "command": "msfconsole",
  "output": "       =[ metasploit v6.3.55-dev                          ]\n+ -- --=[ 2397 exploits - 1235 auxiliary - 422 post       ]\n\nNon-interactive module runs available in this engagement:\n  msfconsole smb_enumshares     SMB share enumeration\n  msfconsole smtp_enum          SMTP user enumeration\n  msfconsole sunrpc             SunRPC portmapper scan\n  msfconsole rmi                Java RMI registry gather\n  msfconsole wordpress          WordPress user enumeration",
  "errorSub": "[-] Unknown module alias. Run 'msfconsole' to list the available module runs.",
  "subcommands": [
    {
      "subcommand": "smb_enumshares",
      "output": "msfconsole -q -x \"use auxiliary/scanner/smb/smb_enumshares; set RHOSTS 10.30.14.52; run; exit\"\n\n[*] 10.30.14.52:139 - historian - (DS) Process historian archive\n[*] 10.30.14.52:139 - print$ - (DS) Printer Drivers\n[*] 10.30.14.52:139 - IPC$ - (I) IPC Service\n[+] 10.30.14.52:139 - BRIDGEWORKS$ - (DS) BridgeWorks Automation - integration share\n[*] Scanned 1 of 1 hosts (100% complete)\n[*] Auxiliary module execution completed"
    },
    {
      "subcommand": "smtp_enum",
      "output": "msfconsole -q -x \"use auxiliary/scanner/smtp/smtp_enum; set RHOSTS 10.30.14.25; run; exit\"\n\n[*] 10.30.14.25:25 - Banner: 220 mail.hallowayprecision.com ESMTP Postfix (Ubuntu)\n[+] 10.30.14.25:25 - Users found: bw-alerts, d.osei, hist_reader, postmaster, root, svc_bridgeworks\n[*] 10.30.14.25:25 - bw-alerts@hallowayprecision.com forwards to noc@bridgeworksautomation.com\n[*] Scanned 1 of 1 hosts (100% complete)\n[*] Auxiliary module execution completed"
    },
    {
      "subcommand": "sunrpc",
      "output": "msfconsole -q -x \"use auxiliary/scanner/misc/sunrpc_portmapper; set RHOSTS 10.30.14.52; run; exit\"\n\n[+] 10.30.14.52:111 - SunRPC Programs for 10.30.14.52\n=================================\n\nName      Number  Version  Port   Protocol\n----      ------  -------  ----   --------\nmountd    100005  1        48122  udp\nmountd    100005  3        56031  tcp\nnfs       100003  3        2049   tcp\nnfs       100003  4        2049   tcp\nnlockmgr  100021  4        39114  tcp\nrpcbind   100000  4        111    tcp\n\n[*] Auxiliary module execution completed"
    },
    {
      "subcommand": "rmi",
      "output": "msfconsole -q -x \"use auxiliary/gather/java_rmi_registry; set RHOST 10.30.14.52; run; exit\"\n\n[*] 10.30.14.52:1099 - Sending RMI Header...\n[*] 10.30.14.52:1099 - Listing names in the Registry...\n[+] 10.30.14.52:1099 - 1 names found in the Registry\n[+] 10.30.14.52:1099 - Name HistorianFeed (HistorianFeed_Stub) found on 127.0.1.1:41855\n[*] Auxiliary module execution completed"
    },
    {
      "subcommand": "rmi 1099",
      "output": "msfconsole -q -x \"use auxiliary/gather/java_rmi_registry; set RHOST 10.30.14.52; run; exit\"\n\n[*] 10.30.14.52:1099 - Sending RMI Header...\n[*] 10.30.14.52:1099 - Listing names in the Registry...\n[+] 10.30.14.52:1099 - 1 names found in the Registry\n[+] 10.30.14.52:1099 - Name HistorianFeed (HistorianFeed_Stub) found on 127.0.1.1:41855\n[*] Auxiliary module execution completed"
    },
    {
      "subcommand": "wordpress",
      "output": "msfconsole -q -x \"use auxiliary/scanner/http/wordpress_login_enum; set RHOSTS hallowayprecision.com; run; exit\"\n\n[*] / - WordPress Version 6.4.2 detected\n[*] /support-legacy/ - WordPress User-Enumeration - Running User Enumeration\n[+] /support-legacy/ - Found user 'bw_tech1' with id 4\n[+] /support-legacy/ - Found user 'bw_tech2' with id 5\n[*] Auxiliary module execution completed"
    }
  ]
}
