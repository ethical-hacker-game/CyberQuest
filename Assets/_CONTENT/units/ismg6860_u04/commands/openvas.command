{
  "command": "openvas",
  "output": "Greenbone Security Assistant (OpenVAS)\nWeb console: https://192.168.20.10:9392/\nOpening console in browser...",
  "errorSub": "Unknown OpenVAS report. Open the web console and pick a report from the list.",
  "action": "openBrowser",
  "actionInfo": "https://192.168.20.10:9392/",
  "subcommands": [
    {
      "subcommand": "report northbridge-external",
      "output": "Greenbone report retrieval\nConnecting to 192.168.20.10:9392...\nSelecting saved report: Northbridge External Scan\nLoading 4 recorded results, evidence confidence, locations, and tracking IDs...\nReport ready. Opening the browser view.\nNo new scan has been started.",
      "action": "openBrowser",
      "actionInfo": "https://192.168.20.10:9392/reports/external.html"
    },
    {
      "subcommand": "report northbridge-internal",
      "output": "Greenbone report retrieval\nConnecting to 192.168.20.10:9392...\nSelecting saved report: Northbridge Internal Scan\nLoading 4 internal-only results for portal-app-01 and file-share-02...\nReport ready. Opening the browser view.\nNo new scan has been started.",
      "action": "openBrowser",
      "actionInfo": "https://192.168.20.10:9392/reports/internal.html"
    }
  ]
}
