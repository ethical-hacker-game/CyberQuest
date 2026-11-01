{
  "command": "nessus",
  "output": "Nessus vulnerability scanner\nWeb console: https://192.168.20.10:8834/\nOpening console in browser...",
  "errorSub": "Unknown Nessus scan. Open the web console and pick a scan from the list.",
  "action": "openBrowser",
  "actionInfo": "https://192.168.20.10:8834/",
  "subcommands": [
    {
      "subcommand": "verify portal",
      "output": "Nessus report retrieval\nConnecting to assessment console at 192.168.20.10:8834...\nSelecting saved scan: Northbridge Portal - External Verification\nLoading recorded findings, read-only HTTP validation evidence, and attached internal comparison data...\nReport ready. Opening the browser view.\nNo new scan or state-changing request has been started.",
      "action": "openBrowser",
      "actionInfo": "https://192.168.20.10:8834/scans/portal-verification.html"
    },
    {
      "subcommand": "compare internal",
      "output": "Nessus comparison retrieval\nConnecting to assessment console at 192.168.20.10:8834...\nSelecting saved scan: Northbridge - Internal Comparison\nLoading external-versus-internal reachability, affected hosts, and finding details...\nComparison ready. Opening the browser view.\nNo new scan has been started.",
      "action": "openBrowser",
      "actionInfo": "https://192.168.20.10:8834/scans/internal-comparison.html"
    }
  ]
}
