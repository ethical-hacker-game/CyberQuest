{
  "command": "gatesentry",
  "isTool": true,
  "output": "GateSentry NGFW management console (FW-2871)\nWeb console: https://10.42.18.1:8443/\nOpening console in browser...",
  "errorSub": "Unknown GateSentry command. Open the web console and review the IDS/IPS alerts, or try: alerts, sensors.",
  "action": "openBrowser",
  "actionInfo": "https://10.42.18.1:8443/",
  "subcommands": [
    {
      "subcommand": "alerts",
      "output": "Opening IDS / IPS alerts for FW-2871 in the GateSentry console...",
      "action": "openBrowser",
      "actionInfo": "https://10.42.18.1:8443/"
    },
    {
      "subcommand": "sensors",
      "output": "Opening sensor coverage view for FW-2871 in the GateSentry console...",
      "action": "openBrowser",
      "actionInfo": "https://10.42.18.1:8443/"
    }
  ]
}
