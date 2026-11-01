{
  "command": "wpscan",
  "output": "_______________________________________________________________\n         __          _______   _____\n         \\ \\        / /  __ \\ / ____|\n          \\ \\  /\\  / /| |__) | (___   ___  __ _ _ __\n           \\ \\/  \\/ / |  ___/ \\___ \\ / __|/ _` | '_ \\\n            \\  /\\  /  | |     ____) | (__| (_| | | | |\n             \\/  \\/   |_|    |_____/ \\___|\\__,_|_| |_|\n\n         WordPress Security Scanner by the WPScan Team\n_______________________________________________________________\n\nUsage: wpscan --url <target>",
  "errorSub": "Scan Aborted: the remote website is up, but does not seem to be running WordPress.",
  "subcommands": [
    {
      "subcommand": "--url https://hallowayprecision.com/support-legacy/ --enumerate u",
      "output": "[+] URL: https://hallowayprecision.com/support-legacy/ [203.0.113.18]\n[+] Started: Mon Jul 27 15:11:04 2026\n\nInteresting Finding(s):\n\n[+] Headers\n | Interesting Entry: Server: Apache/2.4.52 (Ubuntu)\n | Found By: Headers (Passive Detection)\n\n[+] WordPress version 5.4.1 identified (outdated).\n[!] User enumeration is enabled via the REST API\n\n[i] User(s) Identified:\n\n[+] bw_tech1\n | Found By: Rest Api (Aggressive Detection)\n | Last post: 2022-05-17\n\n[+] bw_tech2\n | Found By: Rest Api (Aggressive Detection)\n | Last post: 2023-06-02\n\n[+] Finished: Mon Jul 27 15:11:19 2026"
    }
  ]
}
